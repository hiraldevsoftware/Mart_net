using Mart.Domain.Interface;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public WalletRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async  Task<decimal>GetWalletBalanceAsync(int userId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT WalletBalance FROM Users WHERE Id = @UserId";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            return (decimal)(await cmd.ExecuteScalarAsync() ?? 0.00m);

        }

        public async  Task UpdateWalletBalanceAsync(int userId, decimal amount)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Users SET WalletBalance = WalletBalance + @Amount WHERE Id = @UserId";
            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await connection.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
