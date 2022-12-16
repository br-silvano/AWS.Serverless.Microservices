using Amazon.DynamoDBv2.DataModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Model
{
    public enum StatusPedido
    {
        Criado,
        Coletado,
        Reservado,
        Pago,
        Faturado
    }

    [DynamoDBTable("shop-wit-pedido")]
    public class Pedido
    {
        public string? Id { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataAlteracao { get; set; }
        public List<Produto> Produtos { get; set; }
        public Cliente Cliente { get; set; }
        public Pagamento Pagamento { get; set; }
        public string? JustificativaCancelamento { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public StatusPedido Status { get; set; }
        public bool? Pago { get; set; }
        public bool? Faturado { get; set; }
        public bool? Cancelado { get; set; }
    }
}
