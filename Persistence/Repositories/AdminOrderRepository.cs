using Mart.Domain.Enums;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminOrderRepository : IAdminOrderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminOrderRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<IEnumerable<object>> GetPendingOrdersAsync()
        {
            var orders = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        SELECT 
            o.Id, 
            u.Name AS CustomerName, 
            o.TotalAmount, 
            o.OrderStatus, 
            o.CreatedAt
        FROM Orders o
        JOIN Users u ON o.UserId = u.Id
        WHERE o.OrderStatus = @Status AND o.IsDeleted = 0
        ORDER BY o.CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);

  
            command.Parameters.AddWithValue("@Status", (int)OrderStatus.Placed);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    OrderId = reader["Id"],
                    OrderNo = "ORD-" + reader["Id"], 
                    Customer = reader["CustomerName"],
                    Amount = reader["TotalAmount"],
                    Status = ((OrderStatus)(int)reader["OrderStatus"]).ToString(),
                    Date = reader["CreatedAt"]
                });
            }
            return orders;
        }
        //public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        //{
        //    using var connection = (SqlConnection)_connectionFactory.CreateConnection();

        //    const string sql = "UPDATE Orders SET OrderStatus = @Status WHERE Id = @Id";

        //    using var command = new SqlCommand(sql, connection);
        //    command.Parameters.AddWithValue("@Status", (int)status);
        //    command.Parameters.AddWithValue("@Id", orderId);

        //    if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        //    return await command.ExecuteNonQueryAsync() > 0;
        //}

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {

                const string updateOrderSql = "UPDATE Orders SET OrderStatus = @Status WHERE Id = @Id";
                using var cmd1 = new SqlCommand(updateOrderSql, connection, transaction);
                cmd1.Parameters.AddWithValue("@Status", (int)status);
                cmd1.Parameters.AddWithValue("@Id", orderId);
                await cmd1.ExecuteNonQueryAsync();

                if (status == OrderStatus.Delivered)
                {
                    const string walletSql = @"
                DECLARE @RiderId INT;
                SELECT @RiderId = RiderId FROM Orders WHERE Id = @OrderId;

                IF @RiderId IS NOT NULL
                BEGIN
                    -- Jo wallet na hoy to insert karo, hoy to update karo
                    IF NOT EXISTS (SELECT 1 FROM RiderWallets WHERE RiderId = @RiderId)
                        INSERT INTO RiderWallets (RiderId, TotalEarnings, PendingPayout, LastSettlementDate)
                        VALUES (@RiderId, 25, 25, GETDATE());
                    ELSE
                        UPDATE RiderWallets 
                        SET TotalEarnings = TotalEarnings + 25, 
                            PendingPayout = PendingPayout + 25 
                        WHERE RiderId = @RiderId;
                END";

                    using var cmd2 = new SqlCommand(walletSql, connection, transaction);
                    cmd2.Parameters.AddWithValue("@OrderId", orderId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                transaction.Rollback();
                return false;
            }
        }


        public async Task<IEnumerable<object>> GetAvailableRidersAsync()
        {
            var riders = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT Id, Name, PhoneNumber FROM Riders WHERE IsAvailable = 1";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                riders.Add(new
                {
                    Id = reader["Id"],
                    Name = reader["Name"],
                    Phone = reader["PhoneNumber"]
                });
            }
            return riders;
        }



        public async Task<bool> AssignRiderAsync(int orderId, int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        UPDATE Orders 
        SET RiderId = @RiderId, 
            OrderStatus = @Status 
        WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RiderId", riderId);
            command.Parameters.AddWithValue("@Status", (int)OrderStatus.OutForDelivery);
            command.Parameters.AddWithValue("@Id", orderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }



        public async Task<object?> GetOrderDetailsByIdAsync(int orderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            // Query to get Order Info + Items
            const string sql = @"
        SELECT 
            o.Id, o.TotalAmount, o.OrderStatus, o.CreatedAt,
            oi.ProductId, p.Name as ProductName, oi.Quantity, oi.UnitPrice
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        JOIN Products p ON oi.ProductId = p.Id
        WHERE o.Id = @Id";

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", orderId);

            var items = new List<object>();
            object? orderHeader = null;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (orderHeader == null)
                {
                    orderHeader = new
                    {
                        OrderId = reader["Id"],
                        Total = reader["TotalAmount"],
                        Status = ((OrderStatus)(int)reader["OrderStatus"]).ToString(),
                        Date = reader["CreatedAt"]
                    };
                }
                items.Add(new
                {
                    Product = reader["ProductName"],
                    Qty = reader["Quantity"],
                    Price = reader["UnitPrice"]
                });
            }

            return orderHeader == null ? null : new { Order = orderHeader, Items = items };
        }

        public async Task<object> GetDashboardStatsAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"

        SELECT COUNT(Id) FROM Orders 
        WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND IsDeleted = 0;

  
        SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders 
        WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) 
        AND OrderStatus != 7 -- 7 means Cancelled (Mara enum pramane check karjo)
        AND IsDeleted = 0;


        SELECT COUNT(Id) FROM Orders 
        WHERE OrderStatus = 2 -- 2 means Confirmed/Accepted
        AND RiderId IS NULL 
        AND IsDeleted = 0;


        SELECT COUNT(Id) FROM Riders WHERE IsAvailable = 1;";

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            int todayOrders = 0;
            decimal todayRevenue = 0;
            int pendingAssignments = 0;
            int activeRiders = 0;


            if (await reader.ReadAsync()) todayOrders = reader.GetInt32(0);


            await reader.NextResultAsync();
            if (await reader.ReadAsync()) todayRevenue = reader.GetDecimal(0);


            await reader.NextResultAsync();
            if (await reader.ReadAsync()) pendingAssignments = reader.GetInt32(0);


            await reader.NextResultAsync();
            if (await reader.ReadAsync()) activeRiders = reader.GetInt32(0);

            return new
            {
                TodayOrders = todayOrders,
                TodayRevenue = todayRevenue,
                PendingAssignments = pendingAssignments,
                ActiveRiders = activeRiders,
                LastUpdated = DateTime.Now
            };
        }





        public async Task<bool> SendAssignmentRequestAsync(int orderId, int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

          
            const string checkSql = "SELECT COUNT(1) FROM OrderAssignments WHERE OrderId = @OrderId AND RiderId = @RiderId AND Status = 'Pending'";
            using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@OrderId", orderId);
                checkCmd.Parameters.AddWithValue("@RiderId", riderId);
                int existing = (int)await checkCmd.ExecuteScalarAsync();
                if (existing > 0) return true; 
            }

         
            const string sql = @"
        INSERT INTO OrderAssignments (OrderId, RiderId, Status, AssignmentTime)
        VALUES (@OrderId, @RiderId, 'Pending', SYSDATETIMEOFFSET())";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@RiderId", riderId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
