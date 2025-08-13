
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using EstoqueService.Data;
using EstoqueService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace EstoqueService.Services
{
    public class AtualizarEstoqueConsumer : BackgroundService
    {
        private readonly IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtualizarEstoqueConsumer> _logger;
        private const string QueueName = "atualizar_estoque";

        public AtualizarEstoqueConsumer(
            IChannel channel,
            IServiceProvider serviceProvider,
            ILogger<AtualizarEstoqueConsumer> logger)
        {
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<VendaRealizadaMessage>(
                        Encoding.UTF8.GetString(body)) ?? throw new InvalidOperationException("Message Estoque Nulo");
                    
                    _logger.LogInformation("Processando mensagem - PedidoId: {PedidoId}", message?.PedidoId);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();
                        
                        foreach (var item in message!.Itens)
                        {
                            var produto = await context.Produtos.FindAsync(item.ProdutoId);
                            if (produto != null)
                            {
                                produto.Quantidade -= item.Quantidade;
                                await context.SaveChangesAsync();
                                _logger.LogInformation(
                                    "Estoque atualizado - ProdutoId: {ProdutoId}, Quantidade: {Quantidade}", 
                                    item.ProdutoId, item.Quantidade);
                            }
                        }
                    }

                    await _channel.BasicAckAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem");
                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false);
                }
            };

            _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _channel?.CloseAsync();
        }
    }
}