using MatsyaSetu.Domain.Entities;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IWhatsAppCustomerRepository
    {
        // Find an existing WhatsApp customer
        Task<WhatsAppCustomer?> GetByWhatsAppWaIdAsync(
            string whatsAppWaId,
            CancellationToken cancellationToken = default);

        // Register a customer after consent
        Task<int> CreateWithConsentAsync(
            string whatsAppWaId,
            DateTime consentAtUtc,
            CancellationToken cancellationToken = default);

        // Update customer information
        Task UpdateProfileAsync(
            int customerId,
            string fullName,
            string? contactMobile,
            CancellationToken cancellationToken = default);

        // Track last customer activity
        Task UpdateLastSeenAsync(
            int customerId,
            CancellationToken cancellationToken = default);
    }
}