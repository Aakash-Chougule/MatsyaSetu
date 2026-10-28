
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IBotMessageTemplateRepository
    {
        // Get active Marathi message by template key
        Task<BotMessageTemplate?> GetActiveByKeyAsync(
            string templateKey,
            CancellationToken cancellationToken = default);
    }
}
