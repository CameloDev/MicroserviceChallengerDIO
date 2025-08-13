namespace Shared.Models;
using System.Text.Json.Serialization;
public class ItemVenda
{
    [JsonPropertyName("produtoId")]
    public int ProdutoId { get; set; }
    
    [JsonPropertyName("quantidade")]
    public int Quantidade { get; set; }
}