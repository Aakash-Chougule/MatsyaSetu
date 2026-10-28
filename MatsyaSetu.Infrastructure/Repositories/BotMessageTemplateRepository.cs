
using System.Data;
using Microsoft.Data.SqlClient;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class BotMessageTemplateRepository
        : IBotMessageTemplateRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        // ============================================================
        // Constructor - Dependency Injection
        // ============================================================

        public BotMessageTemplateRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ============================================================
        // Get an active message template using its unique key
        // ============================================================

        public async Task<BotMessageTemplate?> GetActiveByKeyAsync(
            string templateKey,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                SELECT
                    TemplateId,
                    TemplateKey,
                    MessageMarathi,
                    TemplateVersion,
                    IsActive,
                    UpdatedAt
                FROM dbo.BotMessageTemplates
                WHERE TemplateKey = @TemplateKey
                  AND IsActive = 1;";

            // Open SQL Server connection using existing factory
            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken);

            // Create SQL command
            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

            // Parameterized query prevents SQL injection
            command.Parameters.Add(
                new SqlParameter(
                    "@TemplateKey",
                    SqlDbType.VarChar,
                    50)
                {
                    Value = templateKey
                });

            // Execute query asynchronously
            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            // Return null when the template is not found
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            // Map database row to our application model
            return new BotMessageTemplate
            {
                TemplateId =
                    reader.GetInt32(
                        reader.GetOrdinal("TemplateId")),

                TemplateKey =
                    reader.GetString(
                        reader.GetOrdinal("TemplateKey")),

                MessageMarathi =
                    reader.GetString(
                        reader.GetOrdinal("MessageMarathi")),

                TemplateVersion =
                    reader.GetString(
                        reader.GetOrdinal("TemplateVersion")),

                IsActive =
                    reader.GetBoolean(
                        reader.GetOrdinal("IsActive")),

                UpdatedAt =
                    reader.GetDateTime(
                        reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
