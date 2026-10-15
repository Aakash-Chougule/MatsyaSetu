using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Services
{
    public class BotActionDispatcher : IBotActionDispatcher
    {
        private readonly IReadOnlyDictionary<string, IBotActionHandler>
            _handlers;

        public BotActionDispatcher(
            IEnumerable<IBotActionHandler> handlers)
        {
            _handlers = handlers.ToDictionary(
                handler => handler.ActionCode,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<string?> DispatchAsync(
            string actionCode,
            BotActionContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(actionCode))
            {
                return null;
            }

            if (!_handlers.TryGetValue(
                    actionCode,
                    out IBotActionHandler? handler))
            {
                return null;
            }

            return await handler.HandleAsync(
                context,
                cancellationToken);
        }
    }
}