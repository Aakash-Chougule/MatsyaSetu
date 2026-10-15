
namespace MatsyaSetu.Application.Interfaces
{
    public interface IWhatsAppService
    {
        // Send the original interactive welcome menu
        Task SendWelcomeMenuAsync(
            string recipientWaId,
            CancellationToken cancellationToken = default);

        // Send a normal WhatsApp text response
        Task SendTextMessageAsync(
            string recipientWaId,
            string message,
            CancellationToken cancellationToken = default);
    }
}
