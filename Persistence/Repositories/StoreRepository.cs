using Dapper;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class StoreRepository : IStoreRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public StoreRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Store?> GetNearestStoreAsync(decimal userLat, decimal userLong)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
            SELECT TOP 1 *, 
            (geography::Point(Latitude, Longitude, 4326).STDistance(geography::Point(@UserLat, @UserLong, 4326)) / 1000) AS DistanceInKm
            FROM Stores
            WHERE IsActive = 1
            ORDER BY (geography::Point(Latitude, Longitude, 4326).STDistance(geography::Point(@UserLat, @UserLong, 4326))) ASC";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@UserLat", userLat);
            command.Parameters.AddWithValue("@UserLong", userLong);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Store
                {
                    Id = (int)reader["Id"],
                    StoreName = reader["StoreName"].ToString()!,
                    Latitude = (decimal)reader["Latitude"],
                    Longitude = (decimal)reader["Longitude"],
                    ServiceRadiusInKm = (int)reader["ServiceRadiusInKm"],
                    IsActive = (bool)reader["IsActive"],
                    DistanceInKm = Convert.ToDouble(reader["DistanceInKm"])
                };
            }
            return null;

        }

        public async Task<IEnumerable<Store>> GetAllStoresAsync()
        {
            var stores = new List<Store>();
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Stores";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                stores.Add(new Store
                {
                    Id = (int)reader["Id"],
                    StoreName = reader["StoreName"].ToString()!,
                    Latitude = (decimal)reader["Latitude"],
                    Longitude = (decimal)reader["Longitude"],
                    ServiceRadiusInKm = (int)reader["ServiceRadiusInKm"],
                    IsActive = (bool)reader["IsActive"]
                });
            }
            return stores;
        }

        public async Task<IEnumerable<dynamic>> GetStorePendingOrdersAsync(int storeId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        SELECT Id, UserId, TotalAmount, OrderStatus, CreatedAt, DeliveryAddressId 
        FROM Orders 
        WHERE StoreId = @StoreId 
        AND OrderStatus = 1  -- 1 = Pending
        AND (IsDeleted = 0 OR IsDeleted IS NULL)
        ORDER BY CreatedAt DESC";

            return await conn.QueryAsync(sql, new { StoreId = storeId });
        }


        public async Task<IEnumerable<dynamic>> GetStoreInventoryAsync(int storeId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT 
            P.Id as ProductId, 
            P.Name, 
            PI.StockQuantity, 
            PI.RackNumber, 
            PI.ShelfLevel,
            PI.LastUpdated
        FROM ProductInventory PI
        INNER JOIN Products P ON PI.ProductId = P.Id
        WHERE PI.StoreId = @StoreId 
        ORDER BY PI.RackNumber ASC"; 

            return await conn.QueryAsync(sql, new { StoreId = storeId });
        }





        //public async Task<bool> UpdateStoreStockAsync(int storeId, int productId, int newQuantity, int staffId, string reason)
        //{
        //    using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        //    const string sql = @"
        //UPDATE ProductInventory 
        //SET StockQuantity = @NewQuantity, 
        //    StaffId = @StaffId, 
        //    Reason = @Reason, 
        //    LastUpdated = GETDATE()
        //WHERE StoreId = @StoreId AND ProductId = @ProductId";

        //    int rows = await conn.ExecuteAsync(sql, new
        //    {
        //        NewQuantity = newQuantity,
        //        StaffId = staffId,
        //        Reason = reason,
        //        StoreId = storeId,
        //        ProductId = productId
        //    });
        //    return rows > 0;
        //}

        public async Task<bool> UpdateStoreStockAsync(int storeId, int productId, int newQuantity, int staffId, string reason)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        UPDATE ProductInventory 
        SET StockQuantity = @NewQuantity, 
            LastUpdated = GETDATE(),
            -- જો આ કોલમ્સ હોય તો જ રાખવી:
            StaffId = @StaffId, 
            Reason = @Reason
        WHERE StoreId = @StoreId AND ProductId = @ProductId";

            int rows = await conn.ExecuteAsync(sql, new
            {
                NewQuantity = newQuantity,
                StaffId = staffId,
                Reason = reason,
                StoreId = storeId,
                ProductId = productId
            });

            return rows > 0;
        }


        public async Task<bool> IsStoreManagerValidAsync(int storeId, int userId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

  
            const string sql = "SELECT COUNT(1) FROM Users WHERE Id = @UserId AND StoreId = @StoreId";

            var count = await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, StoreId = storeId });
            return count > 0;
        }




        //public async Task<bool> UpdateOrderStatusAsync(int orderId, int storeId, int newStatus)
        //{
        //    using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        //    await conn.OpenAsync();
        //    using var transaction = conn.BeginTransaction();

        //    try
        //    {

        //        const string sqlOrder = @"UPDATE Orders SET OrderStatus = @Status, UpdatedAt = SYSDATETIMEOFFSET() 
        //                         WHERE Id = @OrderId AND StoreId = @StoreId";

        //        var affected = await conn.ExecuteAsync(sqlOrder, new { Status = newStatus, OrderId = orderId, StoreId = storeId }, transaction);


        //        if (newStatus == 2)
        //        {
        //            const string sqlStock = @"
        //        UPDATE PI SET PI.StockQuantity = PI.StockQuantity - OI.Quantity
        //        FROM ProductInventory PI
        //        INNER JOIN OrderItems OI ON PI.ProductId = OI.ProductId
        //        WHERE OI.OrderId = @OrderId AND PI.StoreId = @StoreId";

        //            await conn.ExecuteAsync(sqlStock, new { OrderId = orderId, StoreId = storeId }, transaction);
        //        }

        //        transaction.Commit();
        //        return affected > 0;
        //    }
        //    catch
        //    {
        //        transaction.Rollback();
        //        throw;
        //    }
        //}



        public async Task<bool> UpdateOrderStatusAsync(int orderId, int storeId, int newStatus)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                // ૧. ઓર્ડરનું સ્ટેટસ અપડેટ કરવું
                // અહીં ખાતરી કરો કે 'Id' અને 'OrderStatus' કોલમના નામ તારા ડેટાબેઝ મુજબ જ છે
                const string sqlOrder = @"
            UPDATE Orders 
            SET OrderStatus = @Status, 
                UpdatedAt = SYSDATETIMEOFFSET() 
            WHERE Id = @OrderId AND StoreId = @StoreId";

                var affected = await conn.ExecuteAsync(sqlOrder,
                    new { Status = newStatus, OrderId = orderId, StoreId = storeId },
                    transaction);

                // ૨. જો કોઈ રો અપડેટ ન થાય (affected == 0), તો એનો અર્થ એ કે:
                // ઓર્ડર આ સ્ટોરનો નથી અથવા ઓર્ડર આઈડી ખોટો છે.
                if (affected == 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // ૩. જો ઓર્ડર એક્સેપ્ટ થયો હોય (Status 2), તો જ સ્ટોક કાપવો
                if (newStatus == 2)
                {
                    const string sqlStock = @"
                UPDATE PI 
                SET PI.StockQuantity = PI.StockQuantity - OI.Quantity
                FROM ProductInventory PI
                INNER JOIN OrderItems OI ON PI.ProductId = OI.ProductId
                WHERE OI.OrderId = @OrderId AND PI.StoreId = @StoreId";

                    await conn.ExecuteAsync(sqlStock,
                        new { OrderId = orderId, StoreId = storeId },
                        transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public async Task<IEnumerable<dynamic>> GetOrderItemsAsync(int orderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT OI.ProductId, P.Name, OI.Quantity, P.PriceAmount, (OI.Quantity * P.PriceAmount) as SubTotal
        FROM OrderItems OI
        INNER JOIN Products P ON OI.ProductId = P.Id
        WHERE OI.OrderId = @OrderId";
            return await conn.QueryAsync(sql, new { OrderId = orderId });
        }



        public async Task<bool> UpdateProductLocationAsync(int productId, int storeId, string rack, string shelf)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        UPDATE ProductInventory 
        SET RackNumber = @Rack, 
            ShelfLevel = @Shelf, 
            LastUpdated = SYSDATETIMEOFFSET()
        WHERE ProductId = @ProductId AND StoreId = @StoreId";

            var rows = await conn.ExecuteAsync(sql, new
            {
                Rack = rack,
                Shelf = shelf,
                ProductId = productId,
                StoreId = storeId
            });

            return rows > 0;
        }


        public async Task<bool> VerifyProductInOrderAsync(int orderId, string barcode)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        SELECT COUNT(1) 
        FROM OrderItems OI
        INNER JOIN Products P ON OI.ProductId = P.Id
        INNER JOIN Orders O ON OI.OrderId = O.Id
        WHERE O.Id = @OrderId 
        AND P.Barcode = @Barcode 
        AND O.IsDeleted = 0";

            var count = await conn.ExecuteScalarAsync<int>(sql, new { OrderId = orderId, Barcode = barcode });
            return count > 0;
        }



        public async Task<bool> CreateStockRequestAsync(int storeId, int productId, int qty, int staffId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        INSERT INTO StockRequests (StoreId, ProductId, RequestedQuantity, StaffId, Status)
        VALUES (@StoreId, @ProductId, @Qty, @StaffId, 1)";

            var rows = await conn.ExecuteAsync(sql, new { StoreId = storeId, ProductId = productId, Qty = qty, StaffId = staffId });
            return rows > 0;
        }


        public async Task<IEnumerable<dynamic>> GetStoreStockRequestsAsync(int storeId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT SR.*, P.Name as ProductName 
        FROM StockRequests SR
        INNER JOIN Products P ON SR.ProductId = P.Id
        WHERE SR.StoreId = @StoreId
        ORDER BY SR.CreatedAt DESC";
            return await conn.QueryAsync(sql, new { StoreId = storeId });
        }

        public async Task<bool> PerformStockAuditAsync(int storeId, int productId, int physicalQty, int staffId, string remarks)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {

                const string getQtySql = "SELECT StockQuantity FROM ProductInventory WHERE StoreId = @StoreId AND ProductId = @ProductId";
                int systemQty = await conn.ExecuteScalarAsync<int>(getQtySql, new { StoreId = storeId, ProductId = productId }, transaction);

           
                const string auditSql = @"
            INSERT INTO InventoryAudit (StoreId, ProductId, SystemQuantity, PhysicalQuantity, StaffId, Remarks)
            VALUES (@StoreId, @ProductId, @SystemQty, @PhysicalQty, @StaffId, @Remarks)";

                await conn.ExecuteAsync(auditSql, new
                {
                    StoreId = storeId,
                    ProductId = productId,
                    SystemQty = systemQty,
                    PhysicalQty = physicalQty,
                    StaffId = staffId,
                    Remarks = remarks
                }, transaction);


                const string updateInvSql = @"
            UPDATE ProductInventory 
            SET StockQuantity = @PhysicalQty, LastUpdated = GETDATE(), Reason = 'Audit Adjustment'
            WHERE StoreId = @StoreId AND ProductId = @ProductId";

                await conn.ExecuteAsync(updateInvSql, new { PhysicalQty = physicalQty, StoreId = storeId, ProductId = productId }, transaction);

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
    }                                                                     
}
                                                                                                              