namespace MatsyaSetu.Application.Models
{
    public class BotMenuItem
    {
        public int MenuItemId { get; set; }

        public string OptionKey { get; set; } = string.Empty;

        public string MarathiTitle { get; set; } = string.Empty;

        public string ActionCode { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }

        // Parent menu support
        public int? ParentMenuItemId { get; set; }

        // TEXT / SUBMENU / DOCUMENT / ACTION
        public string ResponseType { get; set; } = "ACTION";

        // Message that can be changed directly from SQL
        public string? ResponseMessageMarathi { get; set; }

        // PDF/document URL
        public string? DocumentUrl { get; set; }

        // PDF/document filename
        public string? DocumentFileName { get; set; }
    }
}