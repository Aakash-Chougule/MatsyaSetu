
using System.Data;
using Microsoft.Data.SqlClient;
using MatsyaSetu.Application.Interfaces;
using MatsyaSetu.Domain.Entities;

namespace MatsyaSetu.Infrastructure.Repositories
{
    public class ApplicantRepository : IApplicantRepository
    {
        // Database connection factory
        private readonly IDbConnectionFactory _factory;

        // Constructor Dependency Injection
        public ApplicantRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // Insert an applicant and return their generated ID
        public async Task<int> CreateAsync(
            Applicant applicant,
            CancellationToken cancellationToken = default)
        {
            // Create a new database connection
            await using var connection =
                _factory.CreateConnection();

            // Open the connection asynchronously
            await connection.OpenAsync(cancellationToken);

            // Create an SQL command
            await using var command =
                connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO Applicants
                    (FullName, MobileNumber)
                OUTPUT INSERTED.ApplicantId
                VALUES
                    (@FullName, @MobileNumber);";

            // Parameterized queries prevent SQL injection
            command.Parameters.Add(
                new SqlParameter("@FullName", SqlDbType.NVarChar, 150)
                {
                    Value = applicant.FullName
                });

            command.Parameters.Add(
                new SqlParameter("@MobileNumber", SqlDbType.VarChar, 10)
                {
                    Value = applicant.MobileNumber
                });

            // Execute query and get generated ApplicantId
            object? result =
                await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(result);
        }
    }
}
