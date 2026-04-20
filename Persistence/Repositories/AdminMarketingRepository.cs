using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using Razorpay.Api;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminMarketingRepository : IAdminMarketingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public AdminMarketingRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
        public async Task<decimal> GetReferralBonusAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT SettingValue FROM AppSettings WHERE SettingKey = 'ReferralBonusAmount'";
            using var command = new SqlCommand(sql, connection);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return decimal.Parse(result?.ToString() ?? "0");
        }

        public async Task<IEnumerable<object>> GetTopReferrersAsync()
        {
            var list = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
            SELECT TOP 10 ReferrerUserId, COUNT(*) as TotalReferrals, SUM(BonusAmount) as TotalEarned
            FROM UserReferrals
            GROUP BY ReferrerUserId
            ORDER BY TotalReferrals DESC";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    UserId = reader["ReferrerUserId"],
                    Count = reader["TotalReferrals"],
                    Earnings = reader["TotalEarned"]
                });
            }
            return list;
        }

        public async Task<bool> UpdateReferralBonusAsync(decimal amount)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE AppSettings SET SettingValue = @Amount WHERE SettingKey = 'ReferralBonusAmount'";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Amount", amount.ToString());

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
