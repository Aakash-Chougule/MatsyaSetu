namespace MatsyaSetu.Application.Interfaces
{
    public interface IWhatsAppService
    {
        Task SendTextMessageAsync(
            string recipientWaId,
            string message,
            CancellationToken cancellationToken = default);

        Task SendWelcomeMenuAsync(
            string recipientWaId,
            CancellationToken cancellationToken = default);

        Task SendMenuAsync(
            string recipientWaId,
            CancellationToken cancellationToken = default,
            int? parentMenuItemId = null);

        Task SendDocumentMessageAsync(
            string recipientWaId,
            string documentUrl,
            string? documentFileName = null,
            string? caption = null,
            CancellationToken cancellationToken = default);
    }
}