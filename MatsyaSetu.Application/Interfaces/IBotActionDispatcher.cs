using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IBotActionDispatcher
    {
        Task<string?> DispatchAsync(
            string actionCode,
            BotActionContext context,
            CancellationToken cancellationToken = default);
    }
}