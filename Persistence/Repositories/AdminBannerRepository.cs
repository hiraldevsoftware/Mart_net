using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminBannerRepository : IAdminBannerRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public AdminBannerRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;
        public async Task<int> AddBannerAsync(dynamic banner)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
            INSERT INTO Banners (ImageUrl, LinkType, LinkId, IsActive)
            VALUES (@ImageUrl, @LinkType, @LinkId, 1);
            SELECT CAST(scope_identity() AS int);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ImageUrl", banner.ImageUrl);
            command.Parameters.AddWithValue("@LinkType", banner.LinkType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LinkId", banner.LinkId ?? (object)DBNull.Value);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<bool> DeleteBannerAsync(int id)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "DELETE FROM Banners WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IEnumerable<object>> GetAllBannersAsync()
        {
            var banners = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT Id, ImageUrl, LinkType, LinkId, IsActive FROM Banners ORDER BY Id DESC";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                banners.Add(new
                {
                    Id = reader["Id"],
                    ImageUrl = reader["ImageUrl"],
                    LinkType = reader["LinkType"],
                    LinkId = reader["LinkId"],
                    IsActive = reader["IsActive"]
                });
            }
            return banners;
        }

        public async Task<bool> ToggleBannerStatusAsync(int id, bool isActive)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Banners SET IsActive = @IsActive WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IsActive", isActive);
            command.Parameters.AddWithValue("@Id", id);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
