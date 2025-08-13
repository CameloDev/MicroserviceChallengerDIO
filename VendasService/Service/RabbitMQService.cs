using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using VendasService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders.Testing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Models;
namespace VendasService.Services
{

    public class RabbitMQService : IRabbitMQService
    {
        private readonly Task<IConnection> _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;
        private const string ExchangeName = "venda_realizada";
        private const string QueueName = "atualizar_estoque";

        public RabbitMQService(Task<IConnection> connection, ILogger<RabbitMQService> logger, Task<IChannel> channel)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _channel = channel.GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: string.Empty);

            _logger.LogInformation("Canal RabbitMQ configurado com exchange '{ExchangeName}' e queue '{QueueName}'",
                ExchangeName, QueueName);
        }

        public async Task PublicarVendaRealizada(VendaRealizadaMessage message)
        {
            try
            {
                var connection = await _connection;
                using var channel = await connection.CreateChannelAsync();

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var props = new BasicProperties();
                props.ContentType = "text/plain";
                props.DeliveryMode = (DeliveryModes)2;

                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: string.Empty,
                    mandatory: false,
                    props,
                    body: body);

                _logger.LogInformation("Mensagem publicada no RabbitMQ - PedidoId: {PedidoId}", message.PedidoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem no RabbitMQ");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_channel != null)
                {
                    _channel.CloseAsync();
                    _channel.DisposeAsync();
                    _logger.LogInformation("Canal RabbitMQ fechado");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar o canal RabbitMQ");
            }

        }
    }
}