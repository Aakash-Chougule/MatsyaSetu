
namespace MatsyaSetu.Application.Models
{
    public class BotMessageTemplate
    {
        public int TemplateId { get; set; }

        // Unique identifier, e.g. CONSENT_REQUEST
        public string TemplateKey { get; set; } = string.Empty;

        // Message retrieved from SQL Server
        public string MessageMarathi { get; set; } = string.Empty;

        // Used for tracking which consent text
        // the customer agreed to
        public string TemplateVersion { get; set; } = "1.0";

        public bool IsActive { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
