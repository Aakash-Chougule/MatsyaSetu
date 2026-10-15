using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Handlers
{
    public class StartApplicationHandler : IBotActionHandler
    {
        public string ActionCode => "START_APPLICATION";

        public Task<string> HandleAsync(
            BotActionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                "📝 नवीन अर्ज प्रक्रिया सुरू करण्यासाठी आपले स्वागत आहे.\n\n" +
                "अर्ज प्रक्रिया लवकरच उपलब्ध होईल.");
        }
    }
}