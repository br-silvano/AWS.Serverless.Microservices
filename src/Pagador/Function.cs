using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Compartilhado;
using Model;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Pagador;

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
            pedido.Pago = true;

            if (pedido.Pago is true)
            {
                pedido.Status = StatusPedido.Pago;

                await Util.EnviarParaFila(EnumFilasSQS.pago, pedido);

                context.Logger.LogInformation($"Pedido {pedido.Id} pago");

                pedido.DataAlteracao = DateTime.Now;

                await pedido.SalvarAsync();
            }

        }
    }
}