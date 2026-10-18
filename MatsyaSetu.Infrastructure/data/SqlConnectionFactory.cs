using System.Data.Common;
using MatsyaSetu.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatsyaSetu.Infrastructure.Database
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public SqlConnectionFactory(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<DbConnection> CreateOpenConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            string connectionString =
                _configuration.GetConnectionString(
                    "MatsyaSetuDB")
                ?? throw new InvalidOperationException(
                    "MatsyaSetuDB connection string is missing.");

            var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync(
                cancellationToken);

            return connection;
        }
    }
}