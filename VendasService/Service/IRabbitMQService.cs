using RabbitMQ.Client;
using Shared.Models;
namespace VendasService.Services
{
    public interface IRabbitMQService : IDisposable
    {
        Task PublicarVendaRealizada(VendaRealizadaMessage message);
    }
}