using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminSupportRepository : IAdminSupportRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public AdminSupportRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
        public async Task<bool>IsMaintenanceModeOnAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT SettingValue FROM AppSettings WHERE SettingKey = 'IsMaintenanceMode'";
            using var command = new SqlCommand(sql, connection);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() == "true";
        }

        public async Task<bool> SetMaintenanceModeAsync(bool isOn)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE AppSettings SET SettingValue = @Value, UpdatedAt = GETDATE() WHERE SettingKey = 'IsMaintenanceMode'";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Value", isOn.ToString().ToLower());

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateTaxRateAsync(int taxId, decimal newRate)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE TaxSettings SET Percentage = @Rate WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Rate", newRate);
            command.Parameters.AddWithValue("@Id", taxId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
