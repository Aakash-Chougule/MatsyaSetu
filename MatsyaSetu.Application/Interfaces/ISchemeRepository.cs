using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Application.Interfaces
{
    public interface ISchemeRepository
    {
        Task<List<Scheme>> GetActiveSchemesAsync(
            CancellationToken cancellationToken = default);
    }
}