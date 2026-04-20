using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Mart.Domain.ValueObjects;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminProductRepository : IAdminProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConnectionMultiplexer _redis;

        public AdminProductRepository(IDbConnectionFactory connectionFactory, IConnectionMultiplexer redis)
        {
            _connectionFactory = connectionFactory;
            _redis = redis;
        }
        public async Task<int> AddProductAsync(ProductRequest product)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            //const string sql = @"
            //    INSERT INTO Products (Name, Description, PriceAmount, Currency, CategoryId, StockCount, MinStockAlert, IsDeleted)
            //    OUTPUT INSERTED.Id
            //    VALUES (@Name, @Description, @Price, @Curr, @CatId, @Stock, @Min, 0)";

            const string sql = @"
    INSERT INTO Products (Name, Description, PriceAmount, Currency, CategoryId, StockCount, MinStockAlert, IsDeleted, Barcode, QRCodeBase64)
    OUTPUT INSERTED.Id
    VALUES (@Name, @Description, @Price, @Curr, @CatId, @Stock, @Min, 0, @Barcode, @QR)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price.Amount);
            command.Parameters.AddWithValue("@Curr", product.Price.Currency);
            command.Parameters.AddWithValue("@CatId", product.CategoryId);
            command.Parameters.AddWithValue("@Stock", product.StockCount);
            command.Parameters.AddWithValue("@Min", product.MinStockAlert);

            command.Parameters.AddWithValue("@Barcode", product.Barcode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@QR", product.QRCodeBase64 ?? (object)DBNull.Value);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            int productId = (int)await command.ExecuteScalarAsync();

      
            await SyncSingleStockToRedis(productId, 1, product.StockCount);

            return productId;
        }

        public async Task<bool> UpdateProductAsync(ProductRequest product)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE Products 
                SET Name = @Name, Description = @Description, PriceAmount = @Price, 
                    CategoryId = @CatId, MinStockAlert = @Min
                WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", product.Id);
            command.Parameters.AddWithValue("@Name", product.Name);
            command.Parameters.AddWithValue("@Description", product.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price.Amount);
            command.Parameters.AddWithValue("@CatId", product.CategoryId);
            command.Parameters.AddWithValue("@Min", product.MinStockAlert);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateStockAsync(int productId, int storeId, int newQuantity)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        IF EXISTS (SELECT 1 FROM ProductInventory WHERE ProductId = @Pid AND StoreId = @Sid)
        BEGIN
            UPDATE ProductInventory 
            SET StockQuantity = @Qty, LastUpdated = GETDATE()
            WHERE ProductId = @Pid AND StoreId = @Sid
        END
        ELSE
        BEGIN
            INSERT INTO ProductInventory (ProductId, StoreId, StockQuantity, LastUpdated)
            VALUES (@Pid, @Sid, @Qty, GETDATE())
        END";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Qty", newQuantity);
            command.Parameters.AddWithValue("@Pid", productId);
            command.Parameters.AddWithValue("@Sid", storeId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            int rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {

                var db = _redis.GetDatabase();
                string redisKey = $"stock:{storeId}:{productId}";
                await db.StringSetAsync(redisKey, newQuantity);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Products SET IsDeleted = 1 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", productId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> AddProductMediaAsync(ProductMedia media)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO ProductMedia (ProductId, MediaUrl, MediaType, DisplayOrder, IsPrimary)
                VALUES (@Pid, @Url, @Type, @Order, @IsPrimary)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Pid", media.ProductId);
            command.Parameters.AddWithValue("@Url", media.MediaUrl);
            command.Parameters.AddWithValue("@Type", media.MediaType);
            command.Parameters.AddWithValue("@Order", media.DisplayOrder);
            command.Parameters.AddWithValue("@IsPrimary", media.IsPrimary);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> BulkSyncToRedisAsync(int storeId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT ProductId, StockQuantity FROM ProductInventory WHERE StoreId = @Sid";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Sid", storeId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            var db = _redis.GetDatabase();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string key = $"stock:{storeId}:{reader["ProductId"]}";
                await db.StringSetAsync(key, (int)reader["StockQuantity"]);
            }
            return true;
        }


        private async Task SyncSingleStockToRedis(int productId, int storeId, int qty)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"stock:{storeId}:{productId}", qty);
        }

        public async Task<bool> AddVariantAsync(ProductVariantRequest variant)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        INSERT INTO ProductVariants (ProductId, VariantName, Price, StockQuantity)
        VALUES (@ProductId, @Name, @Price, @Stock)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductId", variant.ProductId);
            command.Parameters.AddWithValue("@Name", variant.VariantName);
            command.Parameters.AddWithValue("@Price", variant.Price);
            command.Parameters.AddWithValue("@Stock", variant.StockQuantity);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IEnumerable<object>> GetVariantsByProductIdAsync(int productId)
        {
            var variants = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM ProductVariants WHERE ProductId = @Pid";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Pid", productId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                variants.Add(new
                {
                    Id = reader["Id"],
                    Name = reader["VariantName"],
                    Price = reader["Price"],
                    Stock = reader["StockQuantity"],
                    SKU = reader["SKU"]
                });
            }
            return variants;
        }

        public async Task<IEnumerable<object>> GetExpiryAlertsAsync(int daysThreshold)
        {
            var alerts = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
            SELECT b.Id, p.Name as ProductName, b.BatchNumber, b.ExpiryDate, b.Quantity
            FROM ProductBatches b
            JOIN Products p ON b.ProductId = p.Id
            WHERE b.ExpiryDate <= DATEADD(day, @Days, GETDATE()) 
            AND b.IsWaste = 0 AND b.Quantity > 0
            ORDER BY b.ExpiryDate ASC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Days", daysThreshold);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                alerts.Add(new
                {
                    BatchId = reader["Id"],
                    Product = reader["ProductName"],
                    Batch = reader["BatchNumber"],
                    Expiry = Convert.ToDateTime(reader["ExpiryDate"]).ToString("yyyy-MM-dd"),
                    Qty = reader["Quantity"]
                });
            }
            return alerts;
        }

        public async Task<bool> MarkBatchAsWasteAsync(int batchId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE ProductBatches SET IsWaste = 1, Quantity = 0 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", batchId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }



        public async Task<Product?> GetProductByBarcodeAsync(string barcode)
        {
            using var connection = _connectionFactory.CreateConnection();

            string cleanBarcode = barcode.Trim();

            const string sql = @"SELECT * FROM Products 
                         WHERE (Barcode = @Barcode OR CAST(Id AS NVARCHAR) = @Barcode) 
                         AND IsDeleted = 0";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Barcode", cleanBarcode);

            if (connection.State != ConnectionState.Open)
                await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return MapToProduct(reader);
            }

            return null;
        }



        private Product MapToProduct(SqlDataReader reader)
        {
            var product = (Product)Activator.CreateInstance(typeof(Product), true)!;


            typeof(Product).GetProperty("Id")?.SetValue(product, (int)reader["Id"]);
            typeof(Product).GetProperty("Name")?.SetValue(product, reader["Name"].ToString());
            typeof(Product).GetProperty("Description")?.SetValue(product, reader["Description"].ToString());
            typeof(Product).GetProperty("StockCount")?.SetValue(product, (int)reader["StockCount"]);
            typeof(Product).GetProperty("MinStockAlert")?.SetValue(product, (int)reader["MinStockAlert"]);
            typeof(Product).GetProperty("CategoryId")?.SetValue(product, (Guid)reader["CategoryId"]);
            typeof(Product).GetProperty("Tags")?.SetValue(product, reader["Tags"]?.ToString());

            typeof(Product).GetProperty("Barcode")?.SetValue(product, reader["Barcode"]?.ToString());


            var amount = (decimal)reader["PriceAmount"];
            var currency = reader["Currency"].ToString() ?? "INR";
            var price = new Money(amount, currency);
            typeof(Product).GetProperty("Price")?.SetValue(product, price);


            return product;
        }


        public async Task<Product?> GetProductByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();

        
            const string sql = "SELECT * FROM Products WHERE Id = @Id AND IsDeleted = 0";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@Id", id);

            if (connection.State != ConnectionState.Open)
                await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {

                return MapToProduct(reader);
            }

            return null;
        }


        public async Task<object> GetDashboardSummaryAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT 
            (SELECT COUNT(*) FROM Products WHERE IsDeleted = 0) AS TotalProducts,
            (SELECT COUNT(*) FROM Products WHERE StockCount <= 0 AND IsDeleted = 0) AS OutOfStock,
            (SELECT COUNT(*) FROM Products WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE) AND IsDeleted = 0) AS NewProductsToday";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new
                {
                    totalProducts = (int)reader["TotalProducts"],
                    outOfStock = (int)reader["OutOfStock"],
                    newProductsToday = (int)reader["NewProductsToday"]
                };
            }
            return new { totalProducts = 0, outOfStock = 0, newProductsToday = 0 };
        }



        public async Task<int> BulkUpdatePriceAsync(List<int> productIds, decimal percentageChange)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            string idList = string.Join(",", productIds);

            string sql = $@"
        UPDATE Products 
        SET PriceAmount = PriceAmount + (PriceAmount * (@Percentage / 100)) 
        WHERE Id IN ({idList}) AND IsDeleted = 0";

            using var command = new SqlCommand(sql, connection);

     
            command.Parameters.AddWithValue("@Percentage", percentageChange);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            return await command.ExecuteNonQueryAsync();
        }



        public async Task<IEnumerable<object>> GetLowStockAlertsAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT Id, Name, StockCount, MinStockAlert, QRCodeBase64 
        FROM Products 
        WHERE StockCount <= MinStockAlert 
        AND IsDeleted = 0";

            var alerts = new List<object>();

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                alerts.Add(new
                {
                    id = reader["Id"],
                    name = reader["Name"]?.ToString(),
                    currentStock = reader["StockCount"],
                    minAlert = reader["MinStockAlert"],
                    qrCode = reader["QRCodeBase64"] != DBNull.Value ? reader["QRCodeBase64"].ToString() : null
                });
            }



            return alerts;
        }

    }
}
