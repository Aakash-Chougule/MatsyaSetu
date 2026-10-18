using System.Data;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Application.Models;
using Microsoft.Data.SqlClient;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class BotMenuRepository : IBotMenuRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public BotMenuRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ============================================================
        // Get all active menu items
        // ============================================================
        public async Task<List<BotMenuItem>> GetActiveMenuItemsAsync(
            CancellationToken cancellationToken = default)
        {
            const string query = @"
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

            var menuItems = new List<BotMenuItem>();

            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(
                        cancellationToken);

            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            while (await reader.ReadAsync(
                cancellationToken))
            {
                menuItems.Add(
                    MapBotMenuItem(reader));
            }

            return menuItems;
        }

        // ============================================================
        // Get menu item by OptionKey
        // ============================================================
        public async Task<BotMenuItem?> GetByOptionKeyAsync(
            string optionKey,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
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

            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(
                        cancellationToken);

            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

            // Parameterized query
            var parameter =
                new SqlParameter(
                    "@OptionKey",
                    SqlDbType.VarChar,
                    50)
                {
                    Value = optionKey
                };

            command.Parameters.Add(parameter);

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            if (!await reader.ReadAsync(
                cancellationToken))
            {
                return null;
            }

            return MapBotMenuItem(reader);
        }

        // ============================================================
        // Map database row to BotMenuItem
        // ============================================================
        private static BotMenuItem MapBotMenuItem(
            IDataRecord reader)
        {
            return new BotMenuItem
            {
                MenuItemId =
                    reader.GetInt32(
                        reader.GetOrdinal(
                            "MenuItemId")),

                OptionKey =
                    reader.GetString(
                        reader.GetOrdinal(
                            "OptionKey")),

                MarathiTitle =
                    reader.GetString(
                        reader.GetOrdinal(
                            "MarathiTitle")),

                ActionCode =
                    reader.GetString(
                        reader.GetOrdinal(
                            "ActionCode")),

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