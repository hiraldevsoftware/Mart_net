using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence
{
    public class DbConnectionFactory: IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            
            if (configuration == null) throw new Exception("Configuration is null!");

          
            _connectionString = configuration["ConnectionStrings:DefaultConnection"];

         
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = "Server=94.249.151.102;Database=JPMart;User Id=P@sqlAp@x2025;Password=Par@1o!5S*.*_EsH9;TrustServerCertificate=True;";
            }
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
