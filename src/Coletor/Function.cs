using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Compartilhado;
using Model;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Coletor;

public class Function
{
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {
        foreach (var record in dynamoEvent.Records)
        {
            if (record.EventName == "INSERT")
            {
                var pedido = record.Dynamodb.NewImage.ToObject<Pedido>();
                
                try
                {
                    await ProcessarValorPedido(pedido);
                    await Util.EnviarParaFila(EnumFilasSQS.pedido, pedido);
                    pedido.Status = StatusPedido.Coletado;
                    context.Logger.LogInformation($"Sucesso na coleta do pedido: '{pedido.Id}'");
                }
                catch (Exception ex)
                {
                    context.Logger.LogError($"Erro: '{ex.Message}'");
                    pedido.JustificativaCancelamento = ex.Message;
                    pedido.Cancelado = true;
                    await Util.EnviarParaFila(EnumFilasSNS.falha, pedido);
                }

                pedido.DataAlteracao = DateTime.Now;

                await pedido.SalvarAsync();
            }
        }
    }

    private async Task ProcessarValorPedido(Pedido pedido)
    {
        foreach (var produto in pedido.Produtos)
        {
            var produtoEstoque = await ObterProdutoDynamoDBAsync(produto.Id);
            if (produtoEstoque is null)
            {
                throw new InvalidOperationException($"Produto {produto.Id} não encontrado na tabela estoque.");
            }

            produto.Valor = produtoEstoque.Valor;
            produto.Nome = produtoEstoque.Nome;
        }

        var valorTotal = pedido.Produtos.Sum(x => x.Valor * x.Quantidade);
        if (pedido.ValorTotal != 0 && pedido.ValorTotal != valorTotal)
        {
            throw new InvalidOperationException(
                $"O valor do pedido é de R$ {pedido.ValorTotal} e o valor verdadeiro é R$ {valorTotal}");
        }

        pedido.ValorTotal = valorTotal;
    }

    private async Task<Produto?> ObterProdutoDynamoDBAsync(string id)
    {
        var client = new AmazonDynamoDBClient();
        var request = new QueryRequest
        {
            TableName = "shop-wit-estoque",
            KeyConditionExpression = "Id = :v_id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                {
                    ":v_id", new AttributeValue { S = id }
                }
            }
        };

        var response = await client.QueryAsync(request);
        var item = response.Items.FirstOrDefault();
        if (item is null)
        {
           return null;
        }
        return item.ToObject<Produto>();
    }
}