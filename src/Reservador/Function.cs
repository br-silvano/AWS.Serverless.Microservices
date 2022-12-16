using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Reservador;

public class Function
{
    private AmazonDynamoDBClient amazonDynamoDBClient { get; }

    public Function()
    {
        amazonDynamoDBClient = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        if (evnt.Records.Count > 1)
        {
            throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");
        }

        var message = evnt.Records.FirstOrDefault();
        if (message is null)
        {
            return;
        }

        await ProcessMessageAsync(message, context);
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        var pedido = JsonConvert.DeserializeObject<Pedido>(message.Body);

        if (pedido is not null)
        {
            foreach (var produto in pedido.Produtos)
            {
                try
                {
                    await BaixarEstoque(produto.Id, produto.Quantidade);
                    produto.Reservado = true;
                    context.Logger.LogInformation($"Produdo {produto.Id} baixado do estoque");
                }
                catch (ConditionalCheckFailedException)
                {
                    pedido.JustificativaCancelamento = $"Produto {produto.Id} indisponível no estoque";
                    pedido.Cancelado = true;
                    context.Logger.LogError($"Erro: Produto {produto.Id} indisponível no estoque");
                    break;
                }
            }

            if (pedido.Cancelado is true)
            {
                await Util.EnviarParaFila(EnumFilasSNS.falha, pedido);
            }
            else
            {
                pedido.Status = StatusPedido.Reservado;
                await Util.EnviarParaFila(EnumFilasSQS.reservado, pedido);
            }

            pedido.DataAlteracao = DateTime.Now;

            await pedido.SalvarAsync();
        }
    }

    private async Task BaixarEstoque(string id, int quantidade)
    {
        var request = new UpdateItemRequest
        {
            TableName = "shop-wit-estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                { "Id", new AttributeValue{ S = id } }
            },
            UpdateExpression = "SET Quantidade = (Quantidade - :quantidadePedida)",
            ConditionExpression = "Quantidade >= :quantidadePedida",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":quantidadePedida", new AttributeValue{ N = quantidade.ToString() } }
            }
        };

        await amazonDynamoDBClient.UpdateItemAsync(request);
    }
}