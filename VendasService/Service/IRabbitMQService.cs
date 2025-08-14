using RabbitMQ.Client;
using Shared.Models;
namespace VendasService.Services
{
    public interface IRabbitMQService : IDisposable
    {
        Task<VendaRespostaMessage> PublicarVendaRealizada(VendaRealizadaMessage message);

        IChannel GetChannelAsync();

    }
}