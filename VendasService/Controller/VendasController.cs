using Microsoft.AspNetCore.Mvc;
using VendasService.Data;
using VendasService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using VendasService.Services;
using Microsoft.AspNetCore.Authorization;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendasController : ControllerBase
    {
        private readonly VendasDbContext _context;
        private readonly VendaService _vendasService;

        public VendasController(VendasDbContext context, VendaService vendasService)
        {
            _context = context;
            _vendasService = vendasService;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetPedidos()
        {
            var pedidos = await _context.Pedidos
                                        .Include(p => p.Itens)
                                        .ToListAsync();
            return pedidos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Pedido>> GetPedido(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
                return NotFound();

            return pedido;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CriarVenda([FromBody] Pedido pedido)
        {
            try
            {
                var pedidoExistente = await _context.Pedidos.FindAsync(pedido.Id);
                if (pedidoExistente != null)
                {
                    return Conflict(new { message = $"Já existe um pedido com o ID {pedido.Id}" });
                }
                if (pedido.Id <= 0)
                {
                     return BadRequest(new { message = "O ID do pedido deve ser maior que 0." });
                }
                var resultado = await _vendasService.CriarVendaAsync(pedido);
                return Ok(resultado);
            }
            catch
            {
                return StatusCode(500, "Erro ao processar a venda");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePedido(int id, Pedido pedido)
        {
            if (id != pedido.Id)
                return BadRequest();

            _context.Entry(pedido).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePedido(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound();

            _context.Pedidos.Remove(pedido);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }
    }
}
