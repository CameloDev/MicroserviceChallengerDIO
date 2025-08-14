using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using EstoqueService.Data;
using EstoqueService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Shared.Models;

namespace EstoqueService.Services
{
    public class AtualizarEstoqueConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AtualizarEstoqueConsumer> _logger;
        private const string QueueName = "atualizar_estoque";

        public AtualizarEstoqueConsumer(
            IConnection connection,
            IChannel channel,
            IServiceProvider serviceProvider,
            ILogger<AtualizarEstoqueConsumer> logger)
        {
            _connection = connection;
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("Consumer inicializado - Pronto para receber mensagens");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await SetupQueueAsync();

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    _logger.LogInformation("--- NOVA MENSAGEM RECEBIDA ---");
                    _logger.LogInformation("DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                    
                    try
                    {
                        // Processamento da mensagem
                        var body = ea.Body.ToArray();
                        _logger.LogDebug("Corpo da mensagem recebido: {length} bytes", body.Length);
                        
                        var message = JsonSerializer.Deserialize<VendaRealizadaMessage>(Encoding.UTF8.GetString(body))
                            ?? throw new InvalidOperationException("Mensagem inválida");

                        _logger.LogInformation("Mensagem deserializada - PedidoId: {PedidoId}, Itens: {QuantidadeItens}", 
                            message.PedidoId, message.Itens.Count);

                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<EstoqueContext>();

                        _logger.LogInformation("Verificando estoque para os itens...");
                        var (sucesso, motivo) = await ProcessarEstoque(context, message);

                        _logger.LogInformation("Preparando resposta RPC...");
                        await ResponderRPC(ea, message.PedidoId, sucesso, motivo);

                        if (sucesso)
                        {
                            _logger.LogInformation("Estoque atualizado com sucesso - Confirmando mensagem (ACK)");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        }
                        else
                        {
                            _logger.LogWarning("Problema no estoque - Rejeitando mensagem (NACK). Motivo: {Motivo}", motivo);
                            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERRO AO PROCESSAR MENSAGEM - Reenfileirando (NACK)");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                    }
                    finally
                    {
                        _logger.LogInformation("--- PROCESSAMENTO FINALIZADO ---\n");
                    }
                };

                _logger.LogInformation("Iniciando consumo da fila '{QueueName}'...", QueueName);
                await _channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Consumer pronto e aguardando mensagens...");

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "ERRO FATAL no consumer");
                throw;
            }
        }

        private async Task SetupQueueAsync()
        {
            _logger.LogInformation("Configurando fila '{QueueName}'...", QueueName);
            
            var queueResult = await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Fila configurada - {MessageCount} mensagens pendentes, {ConsumerCount} consumidores",
                queueResult.MessageCount, queueResult.ConsumerCount);

            await _channel.BasicQosAsync(0, 1, false);
            _logger.LogInformation("QoS configurado - PrefetchCount: 1");
        }

        private async Task<(bool sucesso, string? motivo)> ProcessarEstoque(
            EstoqueContext context, VendaRealizadaMessage message)
        {
            _logger.LogInformation("Processando {QuantidadeItens} itens do pedido", message.Itens.Count);
            
            foreach (var item in message.Itens)
            {
                _logger.LogDebug("Verificando item - ProdutoId: {ProdutoId}, Quantidade: {Quantidade}", 
                    item.ProdutoId, item.Quantidade);

                var produto = await context.Produtos.FindAsync(item.ProdutoId);
                if (produto == null)
                {
                    _logger.LogWarning("Produto não encontrado - ID: {ProdutoId}", item.ProdutoId);
                    return (false, $"Produto {item.ProdutoId} não encontrado");
                }
                
                _logger.LogDebug("Estoque atual - ProdutoId: {ProdutoId}, Quantidade: {Quantidade}", 
                    produto.Id, produto.Quantidade);

                if (produto.Quantidade < item.Quantidade)
                {
                    _logger.LogWarning("Estoque insuficiente - ProdutoId: {ProdutoId}, Disponível: {Disponivel}, Requerido: {Requerido}",
                        produto.Id, produto.Quantidade, item.Quantidade);
                    return (false, $"Estoque insuficiente para produto {item.ProdutoId}");
                }
                
                _logger.LogDebug("Atualizando estoque - Subtraindo {Quantidade} unidades", item.Quantidade);
                produto.Quantidade -= item.Quantidade;
            }

            _logger.LogInformation("Salvando alterações no banco de dados...");
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Estoque atualizado com sucesso para o PedidoId: {PedidoId}", message.PedidoId);
            return (true, null);
        }

        private async Task ResponderRPC(
            BasicDeliverEventArgs ea, 
            int pedidoId, 
            bool sucesso, 
            string? motivo)
        {
            if (string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
            {
                _logger.LogWarning("Nenhuma fila de resposta (ReplyTo) especificada - Não será enviada resposta");
                return;
            }

            _logger.LogInformation("Preparando resposta RPC para o PedidoId: {PedidoId}", pedidoId);
            
            var resposta = new VendaRespostaMessage
            {
                PedidoId = pedidoId,
                EstoqueOK = sucesso,
                Status = sucesso ? "OK" : "Error",
                Motivo = motivo
            };

            _logger.LogDebug("Resposta preparada: {@Resposta}", resposta);

            var props = new BasicProperties();
            props.CorrelationId = ea.BasicProperties.CorrelationId;
            _logger.LogInformation("Enviando resposta RPC - CorrelationId: {CorrelationId}", ea.BasicProperties.CorrelationId);

            _logger.LogInformation("Publicando resposta na fila '{ReplyQueue}'...", ea.BasicProperties.ReplyTo);
            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: ea.BasicProperties.ReplyTo,
                mandatory: false,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resposta)));

            _logger.LogInformation("Resposta enviada com sucesso");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Encerrando consumer...");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Consumer encerrado");
        }
    }
}