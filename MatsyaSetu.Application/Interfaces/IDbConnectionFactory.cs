using System.Data.Common;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateOpenConnectionAsync(
            CancellationToken cancellationToken = default);
    }
}