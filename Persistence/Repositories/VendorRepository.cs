using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;

namespace Mart.Persistence.Repositories
{
    public class VendorRepository : IVendorRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public VendorRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

        public async Task<int> CreatePurchaseOrderAsync(int productId, int vendorId, decimal quantity, DateTime deliveryDate)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            const string checkSql = "SELECT COUNT(1) FROM Products WHERE Id = @PId AND IsDeleted = 0";
            using var checkCmd = new SqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("@PId", productId);

            var exists = (int)await checkCmd.ExecuteScalarAsync();
            if (exists == 0)
            {
                throw new Exception($"Product with ID {productId} does not exist.");
            }

            const string sql = @"
        INSERT INTO PurchaseOrders (ProductId, VendorId, Quantity, DeliveryDate, Status, CreatedAt)
        VALUES (@ProductId, @VendorId, @Qty, @DelDate, 'Pending', GETDATE());
        SELECT CAST(scope_identity() AS int);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@VendorId", vendorId);
            command.Parameters.AddWithValue("@Qty", quantity);
            command.Parameters.AddWithValue("@DelDate", deliveryDate);

            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<IEnumerable<object>> GetPendingOrdersAsync(int vendorId)
        {
            var orders = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
            SELECT po.Id, p.Name as ProductName, po.Quantity, po.DeliveryDate 
            FROM PurchaseOrders po
            JOIN Products p ON po.ProductId = p.Id
            WHERE po.VendorId = @VendorId AND po.Status = 'Pending'";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@VendorId", vendorId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    Id = reader["Id"],
                    Product = reader["ProductName"],
                    Qty = reader["Quantity"],
                    Delivery = reader["DeliveryDate"]
                });
            }
            return orders;
        }

        public async Task<bool> UpdatePOStatusAsync(int poId, string status)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE PurchaseOrders SET Status = @Status WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@Id", poId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }


        public async Task<object> GetVendorDashboardAsync(int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        SELECT 
            (SELECT COUNT(*) FROM PurchaseOrders WHERE VendorId = @VId AND Status = 'Pending') as TotalPendingOrders,
            
            (SELECT ISNULL(SUM(po.Quantity * p.PriceAmount), 0) 
             FROM PurchaseOrders po 
             JOIN Products p ON po.ProductId = p.Id 
             WHERE po.VendorId = @VId AND po.Status = 'Accepted') as TotalEarnings,
             
            (SELECT COUNT(*) FROM Products WHERE VendorId = @VId) as TotalProducts";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@VId", vendorId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new
                {
                    PendingOrders = reader["TotalPendingOrders"],
                    Earnings = reader["TotalEarnings"],
                    ProductCount = reader["TotalProducts"]
                };
            }
            return null;
        }


        public async Task<bool> UpdateInventoryAsync(int productId, decimal price, int stock)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE Products 
                SET PriceAmount = @Price, StockCount = @Stock, UpdatedAt = GETDATE()
                WHERE Id = @PId";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Price", price);
            command.Parameters.AddWithValue("@Stock", stock);
            command.Parameters.AddWithValue("@PId", productId);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }


        public async Task<IEnumerable<object>> GetOrderHistoryAsync(int vendorId)
        {
            var history = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                SELECT po.Id, p.Name, po.Quantity, po.Status, po.CreatedAt 
                FROM PurchaseOrders po
                JOIN Products p ON po.ProductId = p.Id
                WHERE po.VendorId = @VendorId AND po.Status != 'Pending'
                ORDER BY po.CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@VendorId", vendorId);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new
                {
                    Id = reader["Id"],
                    Product = reader["Name"],
                    Qty = reader["Quantity"],
                    Status = reader["Status"],
                    Date = reader["CreatedAt"]
                });
            }
            return history;
        }



        public async Task<int> AddProductAsync(ProductRequest product, int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string sql = @"
            INSERT INTO Products (Name, Description, PriceAmount, StockCount, CategoryId, VendorId, CreatedAt, IsDeleted)
            VALUES (@Name, @Desc, @Price, @Stock, @CatId, @VId, GETDATE(), 0);
            SELECT CAST(scope_identity() AS int);";

                using var cmd = new SqlCommand(sql, connection, transaction);
                cmd.Parameters.AddWithValue("@Name", product.Name);
                cmd.Parameters.AddWithValue("@Desc", product.Description);

               
                cmd.Parameters.AddWithValue("@Price", product.Price.Amount);

         
                cmd.Parameters.AddWithValue("@Stock", product.StockCount);

                cmd.Parameters.AddWithValue("@CatId", product.CategoryId);
                cmd.Parameters.AddWithValue("@VId", vendorId);

                int pId = (int)await cmd.ExecuteScalarAsync();

                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    const string imgSql = "INSERT INTO ProductMedia (ProductId, MediaUrl, IsPrimary) VALUES (@PId, @Url, 1)";
                    using var imgCmd = new SqlCommand(imgSql, connection, transaction);
                    imgCmd.Parameters.AddWithValue("@PId", pId);
                    imgCmd.Parameters.AddWithValue("@Url", product.ImageUrl);
                    await imgCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return pId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }



        //public async Task<bool> UpdateProductAsync(int productId, ProductRequest product, int vendorId)
        //{
        //    using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        //    const string sql = @"
        //        UPDATE Products 
        //        SET Name = @Name, Description = @Desc, PriceAmount = @Price, StockCount = @Stock, CategoryId = @CatId
        //        WHERE Id = @PId AND VendorId = @VId";

        //    using var cmd = new SqlCommand(sql, connection);
        //    cmd.Parameters.AddWithValue("@Name", product.Name);
        //    cmd.Parameters.AddWithValue("@Desc", product.Description);
        //    cmd.Parameters.AddWithValue("@Price", product.Price);
        //    cmd.Parameters.AddWithValue("@Stock", product.Stock);
        //    cmd.Parameters.AddWithValue("@CatId", product.CategoryId);
        //    cmd.Parameters.AddWithValue("@PId", productId);
        //    cmd.Parameters.AddWithValue("@VId", vendorId);

        //    await connection.OpenAsync();
        //    return await cmd.ExecuteNonQueryAsync() > 0;
        //}


        public async Task<bool> UpdateProductAsync(int productId, ProductRequest product, int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        UPDATE Products 
        SET Name = @Name, 
            Description = @Desc, 
            PriceAmount = @Price, 
            StockCount = @Stock, 
            CategoryId = @CatId
        WHERE Id = @PId AND VendorId = @VId";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Desc", product.Description);


            cmd.Parameters.AddWithValue("@Price", product.Price.Amount);


            cmd.Parameters.AddWithValue("@Stock", product.StockCount);

            cmd.Parameters.AddWithValue("@CatId", product.CategoryId);
            cmd.Parameters.AddWithValue("@PId", productId);
            cmd.Parameters.AddWithValue("@VId", vendorId);

            await connection.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }


        public async Task<IEnumerable<object>> GetMyProductsAsync(int vendorId)
        {
            var list = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                SELECT p.Id, p.Name, p.PriceAmount, p.StockCount, pm.MediaUrl 
                FROM Products p
                LEFT JOIN ProductMedia pm ON p.Id = pm.ProductId AND pm.IsPrimary = 1
                WHERE p.VendorId = @VId AND p.IsDeleted = 0";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@VId", vendorId);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["Id"],
                    Name = reader["Name"],
                    Price = reader["PriceAmount"],
                    Stock = reader["StockCount"],
                    Image = reader["MediaUrl"]
                });
            }
            return list;
        }


        public async Task<IEnumerable<object>> GetOrdersByStatusAsync(int vendorId, string status)
        {
            var orders = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                SELECT po.Id, p.Name as ProductName, po.Quantity, po.DeliveryDate, po.Status
                FROM PurchaseOrders po
                JOIN Products p ON po.ProductId = p.Id
                WHERE po.VendorId = @VId AND po.Status = @Status";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@VId", vendorId);
            cmd.Parameters.AddWithValue("@Status", status);

            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new
                {
                    Id = reader["Id"],
                    Product = reader["ProductName"],
                    Qty = reader["Quantity"],
                    Delivery = reader["DeliveryDate"],
                    Status = reader["Status"]
                });
            }
            return orders;
        }




        public async Task<bool> UpdateOrderStatusAsync(int poId, string status, int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // સ્ટેટસ અપડેટ કરો
                const string updateSql = "UPDATE PurchaseOrders SET Status = @Status WHERE Id = @Id AND VendorId = @VId";
                using var cmd = new SqlCommand(updateSql, connection, transaction);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Id", poId);
                cmd.Parameters.AddWithValue("@VId", vendorId);

                int rows = await cmd.ExecuteNonQueryAsync();

                // 💡 જો ઓર્ડર 'Delivered' થાય, તો વેન્ડરના વોલેટમાં પૈસા જમા કરો
                if (rows > 0 && status == "Delivered")
                {
                    const string walletSql = @"
                        DECLARE @Amount DECIMAL(18,2);
                        SELECT @Amount = (po.Quantity * p.PriceAmount) 
                        FROM PurchaseOrders po JOIN Products p ON po.ProductId = p.Id WHERE po.Id = @Id;

                        UPDATE VendorWallets SET TotalBalance = TotalBalance + @Amount, 
                               WithdrawableAmount = WithdrawableAmount + @Amount WHERE VendorId = @VId;
                        
                        INSERT INTO VendorTransactions (VendorId, Amount, Type, Description)
                        VALUES (@VId, @Amount, 'Credit', 'Earnings from Order #' + CAST(@Id AS NVARCHAR));";

                    using var walletCmd = new SqlCommand(walletSql, connection, transaction);
                    walletCmd.Parameters.AddWithValue("@Id", poId);
                    walletCmd.Parameters.AddWithValue("@VId", vendorId);
                    await walletCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return rows > 0;
            }
            catch { transaction.Rollback(); throw; }
        }





        public async Task<object> GetVendorWalletAsync(int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // બેલેન્સ મેળવો
            const string balanceSql = "SELECT TotalBalance, WithdrawableAmount FROM VendorWallets WHERE VendorId = @VId";
            using var bCmd = new SqlCommand(balanceSql, connection);
            bCmd.Parameters.AddWithValue("@VId", vendorId);

            decimal total = 0, withdrawable = 0;
            using (var bReader = await bCmd.ExecuteReaderAsync())
            {
                if (await bReader.ReadAsync())
                {
                    total = (decimal)bReader["TotalBalance"];
                    withdrawable = (decimal)bReader["WithdrawableAmount"];
                }
            }

            // ટ્રાન્ઝેક્શન મેળવો
            var transactions = new List<object>();
            const string txSql = "SELECT TOP 10 Amount, Type, Description, CreatedAt FROM VendorTransactions WHERE VendorId = @VId ORDER BY CreatedAt DESC";
            using var tCmd = new SqlCommand(txSql, connection);
            tCmd.Parameters.AddWithValue("@VId", vendorId);
            using (var tReader = await tCmd.ExecuteReaderAsync())
            {
                while (await tReader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        Amount = tReader["Amount"],
                        Type = tReader["Type"],
                        Desc = tReader["Description"],
                        Date = tReader["CreatedAt"]
                    });
                }
            }

            return new { TotalBalance = total, Withdrawable = withdrawable, History = transactions };
        }




        public async Task<object> GetVendorDashboardStatsAsync(int vendorId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM PurchaseOrders WHERE VendorId = @VId AND Status = 'Pending') as PendingOrders,
                    (SELECT COUNT(*) FROM PurchaseOrders WHERE VendorId = @VId AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)) as TodaysOrders,
                    (SELECT ISNULL(SUM(TotalBalance), 0) FROM VendorWallets WHERE VendorId = @VId) as TotalEarnings,
                    (SELECT COUNT(*) FROM Products WHERE VendorId = @VId AND StockCount < 5) as LowStockItems";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@VId", vendorId);
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new
                {
                    Pending = reader["PendingOrders"],
                    Today = reader["TodaysOrders"],
                    Earnings = reader["TotalEarnings"],
                    LowStock = reader["LowStockItems"]
                };
            }
            return null;
        }
    }

}

