using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IBotActionHandler
    {
        string ActionCode { get; }

        Task<string> HandleAsync(
            BotActionContext context,
            CancellationToken cancellationToken = default);
    }
}