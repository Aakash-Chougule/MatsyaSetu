using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class SchemeRepository : ISchemeRepository
    {
        private readonly string _connectionString;

        public SchemeRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("MatsyaSetuDB")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing.");
        }

        public async Task<List<Scheme>> GetActiveSchemesAsync(
            CancellationToken cancellationToken = default)
        {
            List<Scheme> schemes = new();

            string query = @"
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

            await using SqlConnection con =
                new SqlConnection(_connectionString);

            await using SqlCommand cmd =
                new SqlCommand(query, con);

            await con.OpenAsync(cancellationToken);

            await using SqlDataReader reader =
                await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                Scheme scheme = new Scheme
                {
                    SchemeId =
                        reader.GetInt32(
                            reader.GetOrdinal("SchemeId")),

                    SchemeCode =
                        reader.GetString(
                            reader.GetOrdinal("SchemeCode")),

                    SchemeNameMarathi =
                        reader.GetString(
                            reader.GetOrdinal("SchemeNameMarathi")),

                    ShortDescriptionMarathi =
                        reader.IsDBNull(
                            reader.GetOrdinal("ShortDescriptionMarathi"))
                            ? null
                            : reader.GetString(
                                reader.GetOrdinal("ShortDescriptionMarathi")),

                    EligibilityMarathi =
                        reader.IsDBNull(
                            reader.GetOrdinal("EligibilityMarathi"))
                            ? null
                            : reader.GetString(
                                reader.GetOrdinal("EligibilityMarathi")),

                    SourceUrl =
                        reader.IsDBNull(
                            reader.GetOrdinal("SourceUrl"))
                            ? null
                            : reader.GetString(
                                reader.GetOrdinal("SourceUrl")),

                    DisplayOrder =
                        reader.GetInt32(
                            reader.GetOrdinal("DisplayOrder")),

                    IsActive =
                        reader.GetBoolean(
                            reader.GetOrdinal("IsActive"))
                };

                schemes.Add(scheme);
            }

            return schemes;
        }
    }
}