namespace MatsyaSetu.Application.Models
{
    public class BotMenuItem
    {
        public int MenuItemId { get; set; }

        // WhatsApp returns this ID
        // Example: scheme_info
        public string OptionKey { get; set; } = string.Empty;

        // Marathi title shown on WhatsApp
        public string MarathiTitle { get; set; } = string.Empty;

        // Tells our application what action to perform
        // Example: SHOW_SCHEMES
        public string ActionCode { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }
}