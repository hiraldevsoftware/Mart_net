using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;

namespace Mart.Persistence.Repositories
{
    public class CouponRepository : ICouponRepository
    {

        private readonly IDbConnectionFactory _connectionFactory;

        public  CouponRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<Coupon> GetCouponByCodeAsync(string code)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Coupons WHERE Code = @Code AND IsActive = 1 AND ExpiryDate > GETDATE()";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Code", code);

            await ((SqlConnection)connection).OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Coupon
                {
                    Code = reader["Code"].ToString(),
                    DiscountAmount = (decimal)reader["DiscountAmount"],
                    DiscountPercentage = reader["DiscountPercentage"] != DBNull.Value ? (int)reader["DiscountPercentage"] : 0,
                    MinOrderValue = (decimal)reader["MinOrderValue"]
                };
            }
            return null;
        }


        public async Task<IEnumerable<Coupon>> GetAvailableCouponsAsync(decimal cartTotal)
        {
            using var connection = _connectionFactory.CreateConnection();


            const string sql = @"
        SELECT * FROM Coupons 
        WHERE IsActive = 1 
        AND ExpiryDate > GETDATE() 
        AND MinOrderValue <= @CartTotal";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@CartTotal", cartTotal);

            await ((SqlConnection)connection).OpenAsync();

            var coupons = new List<Coupon>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                coupons.Add(new Coupon
                {
                    Id = (int)reader["Id"],
                    Code = reader["Code"].ToString(),
                    DiscountAmount = (decimal)reader["DiscountAmount"],
                    DiscountPercentage = reader["DiscountPercentage"] != DBNull.Value ? (int)reader["DiscountPercentage"] : 0,
                    MinOrderValue = (decimal)reader["MinOrderValue"],
                    ExpiryDate = (DateTime)reader["ExpiryDate"]
                });
            }
            return coupons;
        }
    }
}
