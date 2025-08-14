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

            try
            {
                var resposta = await _rabbitService.PublicarVendaRealizada(mensagem)
                    .WaitAsync(TimeSpan.FromSeconds(30));
                
                _logger.LogInformation("Resposta recebida: {@Resposta}", resposta);

                if (!resposta.EstoqueOK)
                {
                    throw new InvalidOperationException($"Estoque insuficiente: {resposta.Motivo}");
                }

                await _dbContext.SaveChangesAsync();
                return pedido;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao verificar estoque para PedidoId: {PedidoId}", pedido.Id);
                throw new InvalidOperationException("Tempo excedido ao verificar estoque", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar venda");
            throw;
        }
    }
}
