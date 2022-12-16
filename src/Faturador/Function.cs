using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Faturador;

public class Function
{
    public Function()
    {

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
            pedido.Faturado = true;

            if (pedido.Faturado is true)
            {
                pedido.Status = StatusPedido.Faturado;

                await Util.EnviarParaFila(EnumFilasSNS.faturado, pedido);

                context.Logger.LogInformation($"Pedido {pedido.Id} faturado");

                pedido.DataAlteracao = DateTime.Now;

                await pedido.SalvarAsync();
            }

        }
    }
}