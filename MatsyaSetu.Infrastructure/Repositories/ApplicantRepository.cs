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
        public ApplicantRepository(
            IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        // ============================================================
        // Create applicant
        // ============================================================
        public async Task<int> CreateAsync(
            Applicant applicant,
            CancellationToken cancellationToken = default)
        {
            // --------------------------------------------------------
            // Factory creates AND opens the connection
            // --------------------------------------------------------

            await using var connection =
                await _factory
                    .CreateOpenConnectionAsync(
                        cancellationToken);


            // --------------------------------------------------------
            // Create SQL command
            // --------------------------------------------------------

            await using var command =
                connection.CreateCommand();


            // --------------------------------------------------------
            // Parameterized INSERT query
            // --------------------------------------------------------

            command.CommandText = @"
                INSERT INTO Applicants
                    (FullName, MobileNumber)
                OUTPUT INSERTED.ApplicantId
                VALUES
                    (@FullName, @MobileNumber);";


            // --------------------------------------------------------
            // FullName parameter
            // --------------------------------------------------------

            command.Parameters.Add(
                new SqlParameter(
                    "@FullName",
                    SqlDbType.NVarChar,
                    150)
                {
                    Value = applicant.FullName
                });


            // --------------------------------------------------------
            // MobileNumber parameter
            // --------------------------------------------------------

            command.Parameters.Add(
                new SqlParameter(
                    "@MobileNumber",
                    SqlDbType.VarChar,
                    10)
                {
                    Value = applicant.MobileNumber
                });


            // --------------------------------------------------------
            // Execute INSERT and get generated ApplicantId
            // --------------------------------------------------------

            object? result =
                await command.ExecuteScalarAsync(
                    cancellationToken);


            return Convert.ToInt32(result);
        }
    }
}