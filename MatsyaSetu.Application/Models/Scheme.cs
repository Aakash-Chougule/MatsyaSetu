namespace MatsyaSetu.Application.Models
{
    public class Scheme
    {
        public int SchemeId { get; set; }

        public string SchemeCode { get; set; } = string.Empty;

        public string SchemeNameMarathi { get; set; } = string.Empty;

        public string? ShortDescriptionMarathi { get; set; }

        public string? EligibilityMarathi { get; set; }

        public string? SourceUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }
    }
}