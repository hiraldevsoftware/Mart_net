using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;

namespace Mart.Persistence.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AddressRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool>AddAddressAsync(UserAddress address)
        {
            using var connection = _connectionFactory.CreateConnection();
            await ((SqlConnection)connection).OpenAsync();
            using var transaction = ((SqlConnection)connection).BeginTransaction();

            try
            {
                if (address.IsDefault)
                {
                    const string updateSql = "UPDATE UserAddresses SET IsDefault = 0 WHERE UserId = @UserId";
                    using var updateCmd = new SqlCommand(updateSql, (SqlConnection)connection, transaction);
                    updateCmd.Parameters.AddWithValue("@UserId", address.UserId);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                const string sql = @"
                INSERT INTO UserAddresses (Id, UserId, FullAddress, Landmark, Latitude, Longitude, Tag, IsDefault)
                VALUES (@Id, @UserId, @FullAddress, @Landmark, @Latitude, @Longitude, @Tag, @IsDefault)";

                using var command = new SqlCommand(sql, (SqlConnection)connection, transaction);

                command.Parameters.AddWithValue("@Id", address.Id);
                command.Parameters.AddWithValue("@UserId", address.UserId);
                command.Parameters.AddWithValue("@FullAddress", address.FullAddress);
                command.Parameters.AddWithValue("@Landmark", (object?)address.Landmark ?? DBNull.Value);
                command.Parameters.AddWithValue("@Latitude", address.Latitude);
                command.Parameters.AddWithValue("@Longitude", address.Longitude);
                command.Parameters.AddWithValue("@Tag", address.Tag);
                command.Parameters.AddWithValue("@IsDefault", address.IsDefault);


                var result = await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

     

        public async Task<IEnumerable<UserAddress>>GetUserAddressesAsync(int userId)
        {
            var addresses = new List<UserAddress>();
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM UserAddresses WHERE UserId = @UserId ORDER BY IsDefault DESC";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@UserId", userId);
            await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                addresses.Add(new UserAddress
                {
                    Id = (Guid)reader["Id"],
                    UserId = (int)reader["UserId"],
                    FullAddress = reader["FullAddress"].ToString()!,
                    Landmark = reader["Landmark"]?.ToString(),
                    Latitude = (decimal)reader["Latitude"],
                    Longitude = (decimal)reader["Longitude"],
                    Tag = reader["Tag"].ToString()!,
                    IsDefault = (bool)reader["IsDefault"]
                });
            }
            return addresses;
        }

        public async Task<bool>SetDefaultAddressAsync(int userId, Guid addressId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await((SqlConnection)connection).OpenAsync();
            using var transaction = ((SqlConnection)connection).BeginTransaction();


            try
            {
                const string resetSql = "UPDATE UserAddresses SET IsDefault = 0 WHERE UserId = @UserId";
                using var resetCmd = new SqlCommand(resetSql, (SqlConnection)connection, transaction);
                resetCmd.Parameters.AddWithValue("@UserId", userId);
                await resetCmd.ExecuteNonQueryAsync();

                const string setSql = "UPDATE UserAddresses SET IsDefault = 1 WHERE Id = @Id AND UserId = @UserId";
                using var setCmd = new SqlCommand(setSql, (SqlConnection)connection, transaction);
                setCmd.Parameters.AddWithValue("@Id", addressId);
                setCmd.Parameters.AddWithValue("@UserId", userId);


                var result = await setCmd.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                return result > 0;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<bool> DeleteAddressAsync(int userId, Guid addressId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "DELETE FROM UserAddresses WHERE Id = @Id AND UserId = @UserId";
            using var command = new SqlCommand(sql, (SqlConnection)connection);

            command.Parameters.AddWithValue("@Id", addressId);
            command.Parameters.AddWithValue("@UserId", userId);
            await ((SqlConnection)connection).OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
