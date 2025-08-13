using Microsoft.EntityFrameworkCore;
using VendasService.Data;
using VendasService.Models;
using Shared.Models;

namespace VendasService.Services;

public class VendaService
{
    private readonly VendasDbContext _dbContext;
    private readonly IRabbitMQService _rabbitService;
    private readonly ILogger<VendaService> _logger;

    public VendaService(VendasDbContext dbContext, IRabbitMQService rabbitService, ILogger<VendaService> logger)
    {
        _dbContext = dbContext;
        _rabbitService = rabbitService;
        _logger = logger;
    }

    public async Task<Pedido> CriarVendaAsync(Pedido pedido)
    {
        try
        {
            _dbContext.Pedidos.Add(pedido);
            await _dbContext.SaveChangesAsync();
            
            var mensagem = new VendaRealizadaMessage
            {
                PedidoId = pedido.Id,
                DataVenda = pedido.DataCriacao,
                Itens = pedido.Itens.Select(i => new ItemVenda
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade
                }).ToList()
            };

            await _rabbitService.PublicarVendaRealizada(mensagem);

            return pedido;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar venda");
            throw;
        }
    }
}
