using Microsoft.AspNetCore.Mvc;
using EstoqueService.Data;
using EstoqueService.Models;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly EstoqueContext _context;

        public ProdutosController(EstoqueContext context)
        {
            _context = context;
        }


        [HttpGet]
        public ActionResult<List<Produto>> Get([FromQuery] int? quantidade, [FromQuery] int? id)
        {
            var query = _context.Produtos.AsQueryable();

            if (quantidade.HasValue)
                query = query.Where(p => p.Quantidade <= quantidade.Value);
            if (id.HasValue)
                query = query.Where(p => p.Id == id.Value);

            var produtos = query.ToList();
        
            return produtos;
        }
        [HttpPost]
        public IActionResult CriarProduto([FromBody] Produto produto)
        {
            _context.Produtos.Add(produto);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = produto.Id }, produto);
        }

        [HttpPut("{id}")]
        public IActionResult AtualizarProduto(int id, [FromBody] Produto produto)
        {
            var prod = _context.Produtos.Find(id);
            if (prod == null)
                return NotFound();

            prod.Nome = produto.Nome;
            prod.Descricao = produto.Descricao;
            prod.Preco = produto.Preco;
            prod.Quantidade = produto.Quantidade;
            _context.SaveChanges();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeletarProduto(int id)
        {
            var prod = _context.Produtos.Find(id);
            if (prod == null)
                return NotFound();

            _context.Produtos.Remove(prod);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
