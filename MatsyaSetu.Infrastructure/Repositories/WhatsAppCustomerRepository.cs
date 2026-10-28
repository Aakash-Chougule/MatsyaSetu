
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Domain.Entities;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class WhatsAppCustomerRepository
        : IWhatsAppCustomerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public WhatsAppCustomerRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // ============================================================
        // 1. Find customer by WhatsApp number
        // ============================================================

        public async Task<WhatsAppCustomer?> GetByWhatsAppWaIdAsync(
            string whatsAppWaId,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                SELECT
                    CustomerId,
                    WhatsAppWaId,
                    FullName,
                    ContactMobile,
                    ContactVerifiedAt,
                    AadhaarVerificationStatus,
                    AadhaarVerifiedAt,
                    ConsentAt,
                    CreatedAt,
                    LastSeenAt
                FROM dbo.WhatsAppCustomers
                WHERE WhatsAppWaId = @WhatsAppWaId;";

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = query;

            command.Parameters.Add(
                new SqlParameter("@WhatsAppWaId", SqlDbType.VarChar, 20)
                {
                    Value = whatsAppWaId
                });

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new WhatsAppCustomer
            {
                CustomerId = reader.GetInt32(0),
                WhatsAppWaId = reader.GetString(1),

                FullName = reader.IsDBNull(2)
                    ? null : reader.GetString(2),

                ContactMobile = reader.IsDBNull(3)
                    ? null : reader.GetString(3),

                ContactVerifiedAt = reader.IsDBNull(4)
                    ? null : reader.GetDateTime(4),

                AadhaarVerificationStatus = reader.GetString(5),

                AadhaarVerifiedAt = reader.IsDBNull(6)
                    ? null : reader.GetDateTime(6),

                ConsentAt = reader.IsDBNull(7)
                    ? null : reader.GetDateTime(7),

                CreatedAt = reader.GetDateTime(8),
                LastSeenAt = reader.GetDateTime(9)
            };
        }

        // ============================================================
        // 2. Create customer after explicit consent
        // ============================================================

        public async Task<int> CreateWithConsentAsync(
            string whatsAppWaId,
            DateTime consentAtUtc,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                INSERT INTO dbo.WhatsAppCustomers
                (
                    WhatsAppWaId,
                    ConsentAt
                )
                OUTPUT INSERTED.CustomerId
                VALUES
                (
                    @WhatsAppWaId,
                    @ConsentAt
                );";

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = query;

            command.Parameters.Add(
                new SqlParameter("@WhatsAppWaId", SqlDbType.VarChar, 20)
                {
                    Value = whatsAppWaId
                });

            command.Parameters.Add(
                new SqlParameter("@ConsentAt", SqlDbType.DateTime2)
                {
                    Value = consentAtUtc
                });

            try
            {
                object? result =
                    await command.ExecuteScalarAsync(cancellationToken);

                return Convert.ToInt32(result);
            }
            catch (SqlException ex)
                when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Prevent duplicate registrations when Meta
                // retries a webhook for the same customer.
                await using var lookup = connection.CreateCommand();

                lookup.CommandText = @"
                    SELECT CustomerId
                    FROM dbo.WhatsAppCustomers
                    WHERE WhatsAppWaId = @WhatsAppWaId;";

                lookup.Parameters.Add(
                    new SqlParameter(
                        "@WhatsAppWaId", SqlDbType.VarChar, 20)
                    {
                        Value = whatsAppWaId
                    });

                object? existingId =
                    await lookup.ExecuteScalarAsync(cancellationToken);

                if (existingId == null || existingId == DBNull.Value)
                {
                    throw;
                }

                return Convert.ToInt32(existingId);
            }
        }

        // ============================================================
        // 3. Update customer profile
        // ============================================================

        public async Task UpdateProfileAsync(
            int customerId,
            string fullName,
            string? contactMobile,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                UPDATE dbo.WhatsAppCustomers
                SET
                    FullName = @FullName,

                    -- Invalidate verification if mobile changes
                    ContactVerifiedAt =
                        CASE
                            WHEN ContactMobile = @ContactMobile
                                 OR (
                                     ContactMobile IS NULL
                                     AND @ContactMobile IS NULL
                                 )
                            THEN ContactVerifiedAt
                            ELSE NULL
                        END,

                    ContactMobile = @ContactMobile,
                    LastSeenAt = SYSUTCDATETIME()
                WHERE CustomerId = @CustomerId;";

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = query;

            command.Parameters.Add(
                new SqlParameter("@CustomerId", SqlDbType.Int)
                {
                    Value = customerId
                });

            command.Parameters.Add(
                new SqlParameter("@FullName", SqlDbType.NVarChar, 150)
                {
                    Value = fullName
                });

            command.Parameters.Add(
                new SqlParameter("@ContactMobile", SqlDbType.VarChar, 15)
                {
                    Value = (object?)contactMobile ?? DBNull.Value
                });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // ============================================================
        // 4. Update last active timestamp
        // ============================================================

        public async Task UpdateLastSeenAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            const string query = @"
                UPDATE dbo.WhatsAppCustomers
                SET LastSeenAt = SYSUTCDATETIME()
                WHERE CustomerId = @CustomerId;";

            await using var connection =
                await _connectionFactory.CreateOpenConnectionAsync(
                    cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = query;

            command.Parameters.Add(
                new SqlParameter("@CustomerId", SqlDbType.Int)
                {
                    Value = customerId
                });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
