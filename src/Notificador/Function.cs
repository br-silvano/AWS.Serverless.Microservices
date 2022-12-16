using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Notificador;

public class Function
{
    private AmazonSimpleEmailServiceV2Client amazonSimpleEmailServiceV2Client { get; }

    public Function()
    {
        amazonSimpleEmailServiceV2Client = new AmazonSimpleEmailServiceV2Client(RegionEndpoint.USEast1);
    }

    public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
    {
        foreach(var record in evnt.Records)
        {
            await ProcessRecordAsync(record, context);
        }
    }

    private async Task ProcessRecordAsync(SNSEvent.SNSRecord record, ILambdaContext context)
    {
        var pedido = JsonConvert.DeserializeObject<Pedido>(record.Sns.Message);

        if (pedido is not null)
        {
            if (pedido.Cancelado is true)
            {
                await NotificarPedidoCancelado(pedido.Id, pedido.JustificativaCancelamento);
                context.Logger.LogInformation($"Pedido {pedido.Id} Cancelado. Justificativa do cancelamento: {pedido.JustificativaCancelamento}");
            }
            else if (pedido.Status == StatusPedido.Faturado)
            {
                await NotificarPedidoFaturado(pedido.Id);
                context.Logger.LogInformation($"Pedido {pedido.Id} faturado");
            }
        }
    }

    private async Task NotificarPedidoCancelado(string? id, string? justificativa)
    {
        string htmlBody = @$"
            <html>
                <head>
                    <title>Pedido Cancelado</title>
                </head>
                <body>
                    <h1>Pedido {id} Cancelado</h1>
                    <p>Justificativa do cancelamento: {justificativa}</p>
                </body>
            </html>";

        var request = new SendEmailRequest
        {
            FromEmailAddress = "test@test.com",
            Destination = new Destination
            {
                ToAddresses = new List<string>
                {
                    "test@test.com"
                }
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Charset = "UTF-8",
                        Data = $"Pedido {id} Cancelado"
                    },
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    }
                }
            }
        };

        await amazonSimpleEmailServiceV2Client.SendEmailAsync(request);
    }

    private async Task NotificarPedidoFaturado(string? id)
    {
        string htmlBody = @$"
            <html>
                <head>
                    <title>Pedido Faturado</title>
                </head>
                <body>
                    <h1>Pedido {id} Faturado</h1>
                    <p>Obrigado, volte sempre!</p>
                </body>
            </html>";

        var request = new SendEmailRequest
        {
            FromEmailAddress = "test@test.com",
            Destination = new Destination
            {
                ToAddresses = new List<string>
                {
                    "test@test.com"
                }
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content
                    {
                        Charset = "UTF-8",
                        Data = $"Pedido {id} Faturado"
                    },
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    }
                }
            }
        };

        await amazonSimpleEmailServiceV2Client.SendEmailAsync(request);
    }
}