namespace Compartilhado
{
    public enum EnumFilasSQS
    {
        pedido = 1,
        reservado = 2,
        pago = 3
    }

    public enum EnumFilasSNS
    {
        falha = 1,
        faturado = 2
    }

    public static class Filas
    {
        public static Func<EnumFilasSQS, string> GetSQS = (EnumFilasSQS fila) =>
        {
            return $"shop-wit-{fila.ToString()}";
        };

        public static Func<EnumFilasSNS, string> GetSNS = (EnumFilasSNS fila) =>
        {
            return $"shop-wit-{fila.ToString()}";
        };
    }
}
