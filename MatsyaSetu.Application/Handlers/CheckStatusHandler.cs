using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Handlers
{
    public class CheckStatusHandler : IBotActionHandler
    {
        public string ActionCode => "CHECK_STATUS";

        public Task<string> HandleAsync(
            BotActionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                "🔎 अर्जाची स्थिती तपासण्यासाठी आपले स्वागत आहे.\n\n" +
                "अर्ज क्रमांकावर आधारित स्थिती तपासण्याची सुविधा " +
                "लवकरच उपलब्ध होईल.");
        }
    }
}