
namespace MatsyaSetu.Application.Models
{
    // Information required to process a WhatsApp action
    public class BotActionContext
    {
        // WhatsApp customer number
        public string SenderWaId { get; set; } = string.Empty;

        // Selected menu option, e.g. scheme_info
        public string OptionKey { get; set; } = string.Empty;
    }
}
