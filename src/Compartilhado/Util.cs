using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Model;
using Newtonsoft.Json;

namespace Compartilhado
{
    public static class Util
    {
        public static async Task SalvarAsync(this Pedido pedido)
        {
            var client = new AmazonDynamoDBClient();
            var context = new DynamoDBContext(client);
            await context.SaveAsync(pedido);
        }

        public static T ToObject<T>(this Dictionary<string, AttributeValue> dictionary)
        {
            var client = new AmazonDynamoDBClient();
            var context = new DynamoDBContext(client);

            var doc = Document.FromAttributeMap(dictionary);
            return context.FromDocument<T>(doc);
        }

        public static async Task EnviarParaFila(EnumFilasSQS fila, Pedido pedido)
        {
            var json = JsonConvert.SerializeObject(pedido);

            var client = new AmazonSQSClient();
            var request = new SendMessageRequest
            {
                QueueUrl = $"https://sqs.sa-east-1.amazonaws.com/account_id/{Filas.GetSQS(fila)}",
                MessageBody = json
            };

            await client.SendMessageAsync(request);
        }

        public static async Task EnviarParaFila(EnumFilasSNS fila, Pedido pedido)
        {
            var json = JsonConvert.SerializeObject(pedido);

            var client = new AmazonSimpleNotificationServiceClient();
            var publishRequest = new PublishRequest
            {
                TopicArn = $"arn:aws:sns:sa-east-1:account_id:{Filas.GetSNS(fila)}",
                Message = json,
            };

            await client.PublishAsync(publishRequest);
        }
    }
}
