using Compartilhado;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Gerenciador.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PedidoController : ControllerBase
    {
        private readonly ILogger<PedidoController> _logger;

        public PedidoController(ILogger<PedidoController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task PostAsync([FromBody] Pedido pedido)
        {
            pedido.Id = Guid.NewGuid().ToString();
            pedido.DataCriacao = DateTime.Now;
            pedido.Status = StatusPedido.Criado;

            await pedido.SalvarAsync();

            Console.WriteLine($"Pedido salvo com sucesso: id {pedido.Id}");
        }
    }
}
