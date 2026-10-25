using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IBotMenuRepository
    {
        Task<List<BotMenuItem>> GetActiveMenuItemsAsync(
            CancellationToken cancellationToken = default,
            int? parentMenuItemId = null);

        Task<BotMenuItem?> GetByOptionKeyAsync(
            string optionKey,
            CancellationToken cancellationToken = default);
    }
}