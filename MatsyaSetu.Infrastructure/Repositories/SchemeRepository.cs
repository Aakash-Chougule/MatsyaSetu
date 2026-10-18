using System.Data;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class SchemeRepository : ISchemeRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public SchemeRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ============================================================
        // Get all active schemes
        // ============================================================
        public async Task<List<Scheme>> GetActiveSchemesAsync(
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                SELECT
                    SchemeId,
                    SchemeCode,
                    SchemeNameMarathi,
                    ShortDescriptionMarathi,
                    EligibilityMarathi,
                    SourceUrl,
                    DisplayOrder,
                    IsActive
                FROM dbo.Schemes
                WHERE IsActive = 1
                ORDER BY DisplayOrder;";

            var schemes = new List<Scheme>();

            // Factory creates AND opens the connection
            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(
                        cancellationToken);

            // Create SQL command
            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

            // Execute query
            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            // Map database rows
            while (await reader.ReadAsync(
                cancellationToken))
            {
                schemes.Add(
                    MapScheme(reader));
            }

            return schemes;
        }

        // ============================================================
        // Map database row to Scheme model
        // ============================================================
        private static Scheme MapScheme(
            IDataRecord reader)
        {
            return new Scheme
            {
                SchemeId =
                    reader.GetInt32(
                        reader.GetOrdinal(
                            "SchemeId")),

                SchemeCode =
                    reader.GetString(
                        reader.GetOrdinal(
                            "SchemeCode")),

                SchemeNameMarathi =
                    reader.GetString(
                        reader.GetOrdinal(
                            "SchemeNameMarathi")),

                ShortDescriptionMarathi =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "ShortDescriptionMarathi"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal(
                            "ShortDescriptionMarathi")),

                EligibilityMarathi =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "EligibilityMarathi"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal(
                            "EligibilityMarathi")),

                SourceUrl =
                    reader.IsDBNull(
                        reader.GetOrdinal(
                            "SourceUrl"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal(
                            "SourceUrl")),

                DisplayOrder =
                    reader.GetInt32(
                        reader.GetOrdinal(
                            "DisplayOrder")),

                IsActive =
                    reader.GetBoolean(
                        reader.GetOrdinal(
                            "IsActive"))
            };
        }
    }
}