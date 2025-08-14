namespace Shared.Models
{
    using System.Text.Json.Serialization;
    public class VendaRespostaMessage
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("motivo")]
        public string? Motivo { get; set; }

        [JsonPropertyName("pedidoid")]
        public int PedidoId { get; set; }

        
        [JsonPropertyName("estoqueok")]
        public bool EstoqueOK { get; set; } = true;
    }
}