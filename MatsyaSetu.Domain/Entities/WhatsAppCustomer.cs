namespace MatsyaSetu.Domain.Entities
{
    public class WhatsAppCustomer
    {
        public int CustomerId { get; set; }

        // WhatsApp sender ID received from Meta
        public string WhatsAppWaId { get; set; } = string.Empty;

        // Customer information
        public string? FullName { get; set; }
        public string? ContactMobile { get; set; }

        // Contact verification
        public DateTime? ContactVerifiedAt { get; set; }

        // Aadhaar verification status
        public string AadhaarVerificationStatus { get; set; }
            = "NOT_STARTED";

        public DateTime? AadhaarVerifiedAt { get; set; }

        // Customer consent
        public DateTime? ConsentAt { get; set; }

        // Tracking (UTC)
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}