
using System.Data.Common;
using Microsoft.Data.SqlClient;
using MatsyaSetu.Application.Interfaces;

namespace MatsyaSetu.Infrastructure.Data
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        // Stores our database connection string
        private readonly string _connectionString;

        // Constructor receives the connection string
        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Creates a new SQL connection
        public DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
