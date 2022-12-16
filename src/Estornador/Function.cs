using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Model;
using Compartilhado;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Estornador;

public class Function
{
    public Function()
    {

    }

    public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
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

        await ProcessRecordAsync(message, context);
    }

    private async Task ProcessRecordAsync(SNSEvent.SNSRecord record, ILambdaContext context)
    {
        var pedido = JsonConvert.DeserializeObject<Pedido>(record.Sns.Message);
        if (pedido is not null)
        {
            if (pedido.Cancelado is true)
            {
                foreach (var produto in pedido.Produtos)
                {
                    if (produto.Reservado)
                    {
                        await DevolverAoEstoque(produto.Id, produto.Quantidade);
                        produto.Reservado = false;
                        context.Logger.LogInformation($"Produto {produto.Id} devolvido ao estoque");
                    }
                }

                pedido.DataAlteracao = DateTime.Now;

                await pedido.SalvarAsync();
            }

        }
    }

    private async Task DevolverAoEstoque(string id, int quantidade)
    {
        var client = new AmazonDynamoDBClient();

        var request = new UpdateItemRequest
        {
            TableName = "shop-wit-estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                { "Id", new AttributeValue { S = id } }
            },
            UpdateExpression = "SET Quantidade = (Quantidade + :quantidadePedida)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":quantidadePedida", new AttributeValue{ N = quantidade.ToString() } }
            }
        };

        await client.UpdateItemAsync(request);
    }
}