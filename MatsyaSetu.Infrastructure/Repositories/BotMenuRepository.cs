using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class BotMenuRepository : IBotMenuRepository
    {
        private readonly string _connectionString;

        public BotMenuRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("MatsyaSetuDB")
                ?? throw new InvalidOperationException(
                    "Database connection string is missing.");
        }

        public async Task<BotMenuItem?> GetByOptionKeyAsync(
    string optionKey,
    CancellationToken cancellationToken = default)
        {
            string query = @"
        SELECT
            MenuItemId,
            OptionKey,
            MarathiTitle,
            ActionCode,
            DisplayOrder,
            IsActive
        FROM dbo.BotMenuItems
        WHERE OptionKey = @OptionKey
          AND IsActive = 1;";

            await using SqlConnection con =
                new SqlConnection(_connectionString);

            await using SqlCommand cmd =
                new SqlCommand(query, con);

            cmd.Parameters.AddWithValue(
                "@OptionKey",
                optionKey);

            await con.OpenAsync(cancellationToken);

            await using SqlDataReader reader =
                await cmd.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return new BotMenuItem
                {
                    MenuItemId =
                        reader.GetInt32(
                            reader.GetOrdinal("MenuItemId")),

                    OptionKey =
                        reader.GetString(
                            reader.GetOrdinal("OptionKey")),

                    MarathiTitle =
                        reader.GetString(
                            reader.GetOrdinal("MarathiTitle")),

                    ActionCode =
                        reader.GetString(
                            reader.GetOrdinal("ActionCode")),

                    DisplayOrder =
                        reader.GetInt32(
                            reader.GetOrdinal("DisplayOrder")),

                    IsActive =
                        reader.GetBoolean(
                            reader.GetOrdinal("IsActive"))
                };
            }

            return null;
        }
        public async Task<List<BotMenuItem>> GetActiveMenuItemsAsync(
            CancellationToken cancellationToken = default)
        {
            List<BotMenuItem> menuItems = new();

            string query = @"
                SELECT
                    MenuItemId,
                    OptionKey,
                    MarathiTitle,
                    ActionCode,
                    DisplayOrder,
                    IsActive
                FROM dbo.BotMenuItems
                WHERE IsActive = 1
                ORDER BY DisplayOrder;";

            // Create SQL connection
            await using SqlConnection con =
                new SqlConnection(_connectionString);

            // Create SQL command
            await using SqlCommand cmd =
                new SqlCommand(query, con);

            await con.OpenAsync(cancellationToken);

            // Execute SELECT query
            await using SqlDataReader reader =
                await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                BotMenuItem item = new BotMenuItem
                {
                    MenuItemId =
                        reader.GetInt32(
                            reader.GetOrdinal("MenuItemId")),

                    OptionKey =
                        reader.GetString(
                            reader.GetOrdinal("OptionKey")),

                    MarathiTitle =
                        reader.GetString(
                            reader.GetOrdinal("MarathiTitle")),

                    ActionCode =
                        reader.GetString(
                            reader.GetOrdinal("ActionCode")),

                    DisplayOrder =
                        reader.GetInt32(
                            reader.GetOrdinal("DisplayOrder")),

                    IsActive =
                        reader.GetBoolean(
                            reader.GetOrdinal("IsActive"))
                };

                menuItems.Add(item);
            }

            return menuItems;
        }
    }
}