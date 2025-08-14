using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Shared.Models;

namespace VendasService.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;
        private const string ExchangeName = "venda_realizada";
        private const string QueueName = "atualizar_estoque";
        private bool _disposed;

        public RabbitMQService(Task<IConnection> connection, ILogger<RabbitMQService> logger, Task<IChannel> channel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            try
            {
                // Obtenha a conexão e o canal de forma síncrona (evitando deadlocks)
                _connection = connection.GetAwaiter().GetResult();
                _channel = channel.GetAwaiter().GetResult();

                // Configuração inicial do canal
                ConfigureChannel().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na inicialização do RabbitMQService");
                Dispose();
                throw;
            }
        }

        private async Task ConfigureChannel()
        {
            await _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: string.Empty);

            _logger.LogInformation("Canal RabbitMQ configurado com exchange '{ExchangeName}' e queue '{QueueName}'",
                ExchangeName, QueueName);
        }

        public async Task<VendaRespostaMessage> PublicarVendaRealizada(VendaRealizadaMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException("RabbitMQService foi descartado");

            // Cria um novo canal dedicado para esta operação RPC
            using var channel = await _connection.CreateChannelAsync();
            
            try
            {
                // Configura fila de resposta temporária
                var replyQueue = (await channel.QueueDeclareAsync(exclusive: true)).QueueName;
                var correlationId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<VendaRespostaMessage>();

                // Configura consumer para a resposta
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (model, ea) =>
                {
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        var response = JsonSerializer.Deserialize<VendaRespostaMessage>(
                            Encoding.UTF8.GetString(ea.Body.ToArray()));
                        tcs.SetResult(response!);
                    }
                    return Task.CompletedTask;
                };

                // Inicia o consumer antes de publicar a mensagem
                await channel.BasicConsumeAsync(
                    queue: replyQueue,
                    autoAck: true,
                    consumer: consumer);

                // Configura propriedades da mensagem
                var props = new BasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueue;
                props.DeliveryMode = (DeliveryModes)2; // Persistente

                _logger.LogInformation("Publicando mensagem para o PedidoId: {PedidoId}", message.PedidoId);
                
                // Publica a mensagem
                await channel.BasicPublishAsync(
                    exchange: ExchangeName,
                    routingKey: "",
                    mandatory: false,
                    basicProperties: props,
                    body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));

                // Configura timeout para evitar espera infinita
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                _logger.LogDebug("Aguardando resposta RPC...");
                var resposta = await tcs.Task.WaitAsync(cts.Token);
                _logger.LogInformation("Resposta RPC recebida para PedidoId: {PedidoId}", message.PedidoId);
                
                return resposta;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Timeout ao aguardar resposta RPC para PedidoId: {PedidoId}", message.PedidoId);
                throw new TimeoutException("Tempo excedido ao aguardar resposta do estoque");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no RPC para PedidoId: {PedidoId}", message.PedidoId);
                throw;
            }
            finally
            {
                await channel.CloseAsync();
            }
        }

        public IChannel GetChannelAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException("RabbitMQService foi descartado");
                
            return _channel;
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.CloseAsync().GetAwaiter().GetResult();
                _channel?.Dispose();
                _logger.LogInformation("Canal RabbitMQ fechado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar o canal RabbitMQ");
            }
            finally
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}