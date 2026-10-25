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

        public async Task<List<BotMenuItem>> GetActiveMenuItemsAsync(
            CancellationToken cancellationToken = default,
            int? parentMenuItemId = null)
        {
            const string query = @"
                SELECT
                    MenuItemId,
                    OptionKey,
                    MarathiTitle,
                    ActionCode,
                    DisplayOrder,
                    IsActive,
                    ParentMenuItemId,
                    ResponseType,
                    ResponseMessageMarathi,
                    DocumentUrl,
                    DocumentFileName
                FROM dbo.BotMenuItems
                WHERE IsActive = 1
                  AND (
                        (@ParentMenuItemId IS NULL
                         AND ParentMenuItemId IS NULL)
                        OR
                        ParentMenuItemId = @ParentMenuItemId
                      )
                ORDER BY DisplayOrder;";

            var menuItems = new List<BotMenuItem>();

            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken);

            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

            var parentParameter =
                new SqlParameter(
                    "@ParentMenuItemId",
                    SqlDbType.Int)
                {
                    Value = parentMenuItemId.HasValue
                        ? parentMenuItemId.Value
                        : DBNull.Value
                };

            command.Parameters.Add(parentParameter);

            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                menuItems.Add(MapBotMenuItem(reader));
            }

            return menuItems;
        }

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
                    IsActive,
                    ParentMenuItemId,
                    ResponseType,
                    ResponseMessageMarathi,
                    DocumentUrl,
                    DocumentFileName
                FROM dbo.BotMenuItems
                WHERE OptionKey = @OptionKey
                  AND IsActive = 1;";

            await using var connection =
                await _connectionFactory
                    .CreateOpenConnectionAsync(cancellationToken);

            await using var command =
                connection.CreateCommand();

            command.CommandText = query;

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

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapBotMenuItem(reader);
        }

        private static BotMenuItem MapBotMenuItem(
            IDataRecord reader)
        {
            int parentMenuItemIdOrdinal =
                reader.GetOrdinal("ParentMenuItemId");

            int responseMessageOrdinal =
                reader.GetOrdinal(
                    "ResponseMessageMarathi");

            int documentUrlOrdinal =
                reader.GetOrdinal("DocumentUrl");

            int documentFileNameOrdinal =
                reader.GetOrdinal(
                    "DocumentFileName");

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
                        reader.GetOrdinal("IsActive")),

                ParentMenuItemId =
                    reader.IsDBNull(parentMenuItemIdOrdinal)
                        ? null
                        : reader.GetInt32(
                            parentMenuItemIdOrdinal),

                ResponseType =
                    reader.GetString(
                        reader.GetOrdinal("ResponseType")),

                ResponseMessageMarathi =
                    reader.IsDBNull(responseMessageOrdinal)
                        ? null
                        : reader.GetString(
                            responseMessageOrdinal),

                DocumentUrl =
                    reader.IsDBNull(documentUrlOrdinal)
                        ? null
                        : reader.GetString(
                            documentUrlOrdinal),

                DocumentFileName =
                    reader.IsDBNull(documentFileNameOrdinal)
                        ? null
                        : reader.GetString(
                            documentFileNameOrdinal)
            };
        }
    }
}