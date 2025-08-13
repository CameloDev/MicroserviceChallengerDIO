using System.Text.Json.Serialization;

namespace Shared.Models;
public class VendaRealizadaMessage
{
    [JsonPropertyName("pedidoId")]
    public int PedidoId { get; set; }

    [JsonPropertyName("itens")]
    public List<ItemVenda> Itens { get; set; } = new List<ItemVenda>();

    [JsonPropertyName("dataVenda")]
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
}
