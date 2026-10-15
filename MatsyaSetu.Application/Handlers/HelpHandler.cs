using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Handlers
{
    public class HelpHandler : IBotActionHandler
    {
        public string ActionCode => "SHOW_HELP";

        public Task<string> HandleAsync(
            BotActionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                "ℹ️ मत्स्यसेतू मदत विभागात आपले स्वागत आहे.\n\n" +
                "आपल्याला आवश्यक असलेला पर्याय निवडा.");
        }
    }
}