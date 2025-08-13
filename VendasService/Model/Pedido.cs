namespace VendasService.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public string ClienteId { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public string Status { get; set; } = "Pendente";

        public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
    }
}
