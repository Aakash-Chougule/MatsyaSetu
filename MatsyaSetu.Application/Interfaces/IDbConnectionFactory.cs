
using System.Data.Common;

namespace MatsyaSetu.Application.Interfaces
{
    public interface IDbConnectionFactory
    {
        // Defines a method for creating database connections
        DbConnection CreateConnection();
    }
}
