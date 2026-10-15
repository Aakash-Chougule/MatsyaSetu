using System.Text;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Handlers
{
    public class ShowSchemesHandler : IBotActionHandler
    {
        private readonly ISchemeRepository _schemeRepository;

        public ShowSchemesHandler(
            ISchemeRepository schemeRepository)
        {
            _schemeRepository = schemeRepository;
        }

        public string ActionCode => "SHOW_SCHEMES";

        public async Task<string> HandleAsync(
            BotActionContext context,
            CancellationToken cancellationToken = default)
        {
            var schemes =
                await _schemeRepository.GetActiveSchemesAsync(
                    cancellationToken);

            if (schemes.Count == 0)
            {
                return "सध्या कोणत्याही शासकीय योजना उपलब्ध नाहीत.";
            }

            var message = new StringBuilder();

            message.AppendLine("🐟 *उपलब्ध मत्स्य योजना*");
            message.AppendLine();

            int number = 1;

            foreach (var scheme in schemes)
            {
                message.AppendLine(
                    $"{number}. *{scheme.SchemeNameMarathi}*");

                if (!string.IsNullOrWhiteSpace(
                    scheme.ShortDescriptionMarathi))
                {
                    message.AppendLine(
                        scheme.ShortDescriptionMarathi);
                }

                if (!string.IsNullOrWhiteSpace(
                    scheme.EligibilityMarathi))
                {
                    message.AppendLine(
                        $"पात्रता: {scheme.EligibilityMarathi}");
                }

                message.AppendLine();

                number++;
            }

            return message.ToString().Trim();
        }
    }
}