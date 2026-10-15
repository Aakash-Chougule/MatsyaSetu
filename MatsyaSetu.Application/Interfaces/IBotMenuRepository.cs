using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IBotMenuRepository
    {
        // Get all active menu items
        Task<List<BotMenuItem>> GetActiveMenuItemsAsync(
            CancellationToken cancellationToken = default);

        // Get one menu item using WhatsApp option ID
        Task<BotMenuItem?> GetByOptionKeyAsync(
            string optionKey,
            CancellationToken cancellationToken = default);
    }
}