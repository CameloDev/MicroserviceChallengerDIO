namespace EstoqueService.Models;
    public class Produto
    {
        public int Id { get; set; } // PK
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Preco { get; set; }
        public int Quantidade { get; set; }
    }
