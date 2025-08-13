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
        private readonly IConnection _connection;
        private IChannel _channel; 
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtualizarEstoqueConsumer> _logger;
        private const string QueueName = "atualizar_estoque";

        public AtualizarEstoqueConsumer(IConnection connection, IServiceProvider serviceProvider, ILogger<AtualizarEstoqueConsumer> logger, IChannel channel)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channel = channel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<VendaRealizadaMessage>(
                        Encoding.UTF8.GetString(body)) ?? throw new InvalidOperationException("Message Estoque Nulo");

                    _logger.LogInformation("Processando mensagem - PedidoId: {PedidoId}", message.PedidoId);

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

                    foreach (var item in message.Itens)
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
                        if (produto?.Quantidade < item.Quantidade)
                        {
                            _logger.LogWarning(
                                "Estoque insuficiente - ProdutoId: {ProdutoId}, Quantidade solicitada: {Quantidade}, Quantidade disponível: {Disponivel}",
                                item.ProdutoId, item.Quantidade, produto.Quantidade);
                                
                            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                            return;
                        }
                    }

                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem");
                   await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

           await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _channel?.CloseAsync();
                _channel?.Dispose();
                _logger.LogInformation("Canal RabbitMQ fechado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar o canal RabbitMQ");
            }

            return base.StopAsync(cancellationToken);
        }
    }
}
