
using Dapper;
using Mart.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Data;


namespace Mart.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UserRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Users (Name, PhoneNumber, Role, OtpCode, OtpExpiry, IsPhoneVerified, WalletBalance, CreatedAt) 
                VALUES (@Name, @PhoneNumber, @Role, @OtpCode, @OtpExpiry, @IsPhoneVerified, @WalletBalance, GETUTCDATE()); 
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Name", user.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@OtpCode", user.OtpCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OtpExpiry", user.OtpExpiry ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsPhoneVerified", user.IsPhoneVerified);
            command.Parameters.AddWithValue("@WalletBalance", user.WalletBalance);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }


        //public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
        //{
        //    using var connection = _connectionFactory.CreateConnection();
        //    const string sql = "SELECT * FROM Users WHERE PhoneNumber = @Phone AND IsDeleted = 0";

        //    using var command = new SqlCommand(sql, (SqlConnection)connection);
        //    command.Parameters.AddWithValue("@Phone", phoneNumber);

        //    if (connection.State != ConnectionState.Open)
        //        await ((SqlConnection)connection).OpenAsync();

        //    using var reader = await command.ExecuteReaderAsync();

        //    if (await reader.ReadAsync())
        //    {
        //        return new User
        //        {
        //            Id = (int)reader["Id"],
        //            Name = reader["Name"]?.ToString(),
        //            PhoneNumber = reader["PhoneNumber"].ToString()!,
        //            Role = reader["Role"].ToString()!,
        //            IsPhoneVerified = (bool)reader["IsPhoneVerified"],
        //            WalletBalance = (decimal)reader["WalletBalance"],
        //            OtpCode = reader["OtpCode"]?.ToString(),
        //            OtpExpiry = reader["OtpExpiry"] != DBNull.Value ? (DateTime)reader["OtpExpiry"] : null
        //        };
        //    }
        //    return null;
        //}



        public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Users WHERE PhoneNumber = @Phone AND IsDeleted = 0";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Phone", phoneNumber);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"]?.ToString(),
                    PhoneNumber = reader["PhoneNumber"].ToString()!,
                    Role = reader["Role"].ToString()!,
                    IsPhoneVerified = (bool)reader["IsPhoneVerified"],
                    WalletBalance = (decimal)reader["WalletBalance"],
                  
                    OtpCode = reader["OtpCode"] != DBNull.Value ? reader["OtpCode"].ToString() : null,
                    OtpExpiry = reader["OtpExpiry"] != DBNull.Value ? (DateTime)reader["OtpExpiry"] : null
                };
                return user;
            }
            return null;
        }

        public async Task UpdateUserAsync(User user)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string query = @"
                UPDATE Users 
                SET Name = @Name, IsPhoneVerified = @IsPhoneVerified, 
                    OtpCode = @OtpCode, OtpExpiry = @OtpExpiry, 
                    WalletBalance = @WalletBalance, UpdatedAt = GETUTCDATE() 
                WHERE Id = @Id";

            using var command = new SqlCommand(query, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Name", user.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsPhoneVerified", user.IsPhoneVerified);
            command.Parameters.AddWithValue("@OtpCode", user.OtpCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@OtpExpiry", user.OtpExpiry ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@WalletBalance", user.WalletBalance);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();
            await command.ExecuteNonQueryAsync();
        }


        public async Task UpdateFcmTokenAsync(int userId, string token)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Users SET FcmToken = @Token WHERE Id = @UserId";


            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@UserId", userId);


            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
