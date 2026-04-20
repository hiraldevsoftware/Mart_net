using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Mart.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Product>>GetAllProductsAsync()
        {
            var products = new List<Product>();
            using var connection = _connectionFactory.CreateConnection();

            const string sql= "SELECT * FROM Products WHERE IsDeleted = 0";

            using var command = new SqlCommand(sql, (SqlConnection)connection);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            using var reader= await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(MapToProduct(reader));
            }
            return products;

        }

        Task<Product?> IProductRepository.GetProductByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId)
        {
            var products = new List<Product>();
            using var connection = _connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM Products WHERE CategoryId = @CategoryId AND IsDeleted = 0";


            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@CategoryId", categoryId);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapToProduct(reader));
            }
            return products;


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

        public async Task<IEnumerable<Product>> SearchProductsAsync(string term)
        {
            var products = new List<Product>();
            using var connection = _connectionFactory.CreateConnection();

        
            const string sql = @"
        SELECT * FROM Products 
        WHERE IsDeleted = 0 
        AND (Name LIKE @Term 
             OR Tags LIKE @Term 
             OR Description LIKE @Term)";

            using var command = new SqlCommand(sql, (SqlConnection)connection);

         
            command.Parameters.AddWithValue("@Term", $"%{term}%");

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapToProduct(reader));
            }
            return products;
        }

        //public async Task<int> GetStockAsync(int productId, int storeId)
        //{
        //    using var connection = _connectionFactory.CreateConnection();
        //    const string sql = "SELECT StockQuantity FROM ProductInventory WHERE ProductId = @ProductId AND StoreId = @StoreId";

        //    using var command = new SqlCommand(sql, (SqlConnection)connection);
        //    command.Parameters.AddWithValue("@ProductId", productId);
        //    command.Parameters.AddWithValue("@StoreId", storeId);

        //    await ((SqlConnection)connection).OpenAsync();
        //    var result = await command.ExecuteScalarAsync();
        //    return result != null ? (int)result : 0;
        //}

        public async Task<int> GetStockAsync(int productId, int storeId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = "SELECT * FROM Products WHERE Id = @ProductId";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@ProductId", productId);

            await ((SqlConnection)connection).OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
      
                var val = reader.GetValue(5);
                return val != DBNull.Value ? Convert.ToInt32(val) : 0;
            }
            return 0;
        }

        public async Task<bool> UpdateStockAsync(int productId, int storeId, int quantityChange)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
        UPDATE ProductInventory 
        SET StockQuantity = StockQuantity + @QuantityChange,
            LastUpdated = GETDATE()
        WHERE ProductId = @ProductId 
        AND StoreId = @StoreId 
        AND (StockQuantity + @QuantityChange >= 0)";

            using var command = new SqlCommand(sql, (SqlConnection)connection);

      
            command.Parameters.AddWithValue("@QuantityChange", quantityChange);
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@StoreId", storeId);

            if (connection.State != ConnectionState.Open) await ((SqlConnection)connection).OpenAsync();

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }



        public async Task<bool> AddToWishlistAsync(int userId, int productId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
        IF NOT EXISTS (SELECT 1 FROM Wishlist WHERE UserId = @UserId AND ProductId = @ProductId)
        INSERT INTO Wishlist (UserId, ProductId, CreatedAt) VALUES (@UserId, @ProductId, GETDATE())";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);

            await ((SqlConnection)connection).OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }


        public async Task<IEnumerable<Product>> GetWishlistAsync(int userId)
        {
            var products = new List<Product>();
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
        SELECT p.* FROM Products p
        INNER JOIN Wishlist w ON p.Id = w.ProductId
        WHERE w.UserId = @UserId";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@UserId", userId);
            await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(MapToProduct(reader));
            }
            return products;
        }

        public Task<bool> RemoveFromWishlistAsync(int userId, int productId)
        {
            throw new NotImplementedException();
        }



        //    public async Task<string> AddToCartAsync(int userId, int productId, int quantity, int storeId)
        //    {

        //        int currentStock = await GetStockAsync(productId, storeId);

        //        if (currentStock < quantity)
        //        {
        //            return "Insufficient stock!";
        //        }

        //        using var connection = _connectionFactory.CreateConnection();


        //        const string sql = @"
        //-- 1. Pehla check karo ke aa User mate aa Product pehla thi cart ma che?
        //IF EXISTS (SELECT 1 FROM UserCart WHERE UserId = @UserId AND ProductId = @ProductId)
        //BEGIN
        //    -- 2. Jo che, to khali Quantity vadharo
        //    UPDATE UserCart 
        //    SET Quantity = Quantity + @Quantity 
        //    WHERE UserId = @UserId AND ProductId = @ProductId
        //END
        //ELSE
        //BEGIN
        //    -- 3. Jo nathi, to navi entry nakho
        //    INSERT INTO UserCart (UserId, ProductId, Quantity, StoreId) 
        //    VALUES (@UserId, @ProductId, @Quantity, @StoreId)
        //END";

        //        using var command = new SqlCommand(sql, (SqlConnection)connection);
        //        command.Parameters.AddWithValue("@UserId", userId);
        //        command.Parameters.AddWithValue("@ProductId", productId);
        //        command.Parameters.AddWithValue("@Quantity", quantity);
        //        command.Parameters.AddWithValue("@StoreId", storeId);

        //        await ((SqlConnection)connection).OpenAsync();
        //        await command.ExecuteNonQueryAsync();

        //        return "Success";
        //    }


        public async Task<string> AddToCartAsync(int userId, int productId, int quantity, int storeId)
        {
            if (quantity <= 0) quantity = 1;

            using var connection = _connectionFactory.CreateConnection();
            await ((SqlConnection)connection).OpenAsync();
            using var transaction = ((SqlConnection)connection).BeginTransaction();

            try
            {

                string stockColumnName = "";
                int currentStock = 0;

                const string getProductSql = "SELECT * FROM Products WHERE Id = @ProductId";
                using (var checkCmd = new SqlCommand(getProductSql, (SqlConnection)connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@ProductId", productId);
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                 
                        stockColumnName = reader.GetName(5);
                        currentStock = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetValue(5));
                    }
                }

                if (currentStock < quantity)
                {
                    transaction.Rollback();
                    return "Insufficient stock!";
                }

                const string cartSql = @"
            IF EXISTS (SELECT 1 FROM UserCart WHERE UserId = @UserId AND ProductId = @ProductId AND StoreId = @StoreId)
            BEGIN
                UPDATE UserCart SET Quantity = Quantity + @Quantity 
                WHERE UserId = @UserId AND ProductId = @ProductId AND StoreId = @StoreId
            END
            ELSE
            BEGIN
                INSERT INTO UserCart (UserId, ProductId, Quantity, StoreId) 
                VALUES (@UserId, @ProductId, @Quantity, @StoreId)
            END";

                using (var cartCmd = new SqlCommand(cartSql, (SqlConnection)connection, transaction))
                {
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    cartCmd.Parameters.AddWithValue("@ProductId", productId);
                    cartCmd.Parameters.AddWithValue("@Quantity", quantity);
                    cartCmd.Parameters.AddWithValue("@StoreId", storeId);
                    await cartCmd.ExecuteNonQueryAsync();
                }

                string updateStockSql = $@"
            UPDATE Products 
            SET {stockColumnName} = {stockColumnName} - @Quantity 
            WHERE Id = @ProductId AND {stockColumnName} >= @Quantity";

                using (var stockCmd = new SqlCommand(updateStockSql, (SqlConnection)connection, transaction))
                {
                    stockCmd.Parameters.AddWithValue("@ProductId", productId);
                    stockCmd.Parameters.AddWithValue("@Quantity", quantity);
                    await stockCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return "Success";
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                Console.WriteLine("CRITICAL ERROR: " + ex.Message);
                return "Something went wrong!";
            }
        }


        public async Task<IEnumerable<object>> GetUserCartAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
        SELECT 
            uc.ProductId, 
            p.Name as ProductName, 
            uc.Quantity, 
            p.PriceAmount,  -- Ahiya tamara table ma je sachu naam hoy e lakho
            uc.StoreId 
        FROM UserCart uc
        INNER JOIN Products p ON uc.ProductId = p.Id
        WHERE uc.UserId = @UserId";

            using var command = new SqlCommand(sql, (SqlConnection)connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await ((SqlConnection)connection).OpenAsync();

            var items = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    ProductId = reader["ProductId"],
                    ProductName = reader["ProductName"],
                    Quantity = reader["Quantity"],
                    Price = reader["PriceAmount"],
                    StoreId = reader["StoreId"]
                });
            }
            return items;
        }


        public async Task RegisterForNotificationAsync(int userId, int productId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        IF NOT EXISTS (SELECT 1 FROM ProductNotifications WHERE UserId = @UserId AND ProductId = @ProductId AND IsNotified = 0)
        BEGIN
            INSERT INTO ProductNotifications (UserId, ProductId) VALUES (@UserId, @ProductId)
        END";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@ProductId", productId);


            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }


        public async Task<IEnumerable<object>> GetFrequentlyBoughtTogetherAsync(int productId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();


            const string sqlSuggestions = @"
        SELECT TOP 5 oi2.ProductId, p.Name AS ProductName, p.PriceAmount, p.Tags, COUNT(oi2.ProductId) as Frequency
        FROM OrderItems oi1
        JOIN OrderItems oi2 ON oi1.OrderId = oi2.OrderId
        JOIN Products p ON oi2.ProductId = p.Id
        WHERE oi1.ProductId = @ProductId AND oi2.ProductId <> @ProductId
        GROUP BY oi2.ProductId, p.Name, p.PriceAmount, p.Tags
        ORDER BY Frequency DESC";

            using var cmd1 = new SqlCommand(sqlSuggestions, connection);
            cmd1.Parameters.AddWithValue("@ProductId", productId);

            var suggestions = new List<object>();
            using (var reader = await cmd1.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    suggestions.Add(new
                    {
                        ProductId = (int)reader["ProductId"],
                        Name = reader["ProductName"].ToString(),
                        Price = (decimal)reader["PriceAmount"],
                        ImageUrl = reader["Tags"]?.ToString() ?? "",
                        Type = "Frequent"
                    });
                }
            }


            if (suggestions.Count == 0)
            {
                const string sqlTopSelling = @"
            SELECT TOP 5 p.Id, p.Name, p.PriceAmount, p.Tags
            FROM Products p
            LEFT JOIN OrderItems oi ON p.Id = oi.ProductId
            WHERE p.Id <> @ProductId AND p.IsDeleted = 0
            GROUP BY p.Id, p.Name, p.PriceAmount, p.Tags
            ORDER BY COUNT(oi.Id) DESC";

                using var cmd2 = new SqlCommand(sqlTopSelling, connection);
                cmd2.Parameters.AddWithValue("@ProductId", productId);
                using var reader2 = await cmd2.ExecuteReaderAsync();
                while (await reader2.ReadAsync())
                {
                    suggestions.Add(new
                    {
                        ProductId = (int)reader2["Id"],
                        Name = reader2["Name"].ToString(),
                        Price = (decimal)reader2["PriceAmount"],
                        ImageUrl = reader2["Tags"]?.ToString() ?? "",
                        Type = "TopSelling"
                    });
                }
            }

            return suggestions;
        }



        public async Task<IEnumerable<ProductMedia>> GetProductMediaAsync(int productId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        SELECT Id, ProductId, MediaUrl, MediaType, DisplayOrder, IsPrimary 
        FROM ProductMedia 
        WHERE ProductId = @ProductId 
        ORDER BY DisplayOrder ASC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductId", productId);

            await connection.OpenAsync();
            var mediaList = new List<ProductMedia>();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mediaList.Add(new ProductMedia
                {
                    Id = (int)reader["Id"],
                    ProductId = (int)reader["ProductId"],
                    MediaUrl = reader["MediaUrl"].ToString(),
                    MediaType = reader["MediaType"].ToString(),
                    DisplayOrder = (int)reader["DisplayOrder"],
                    IsPrimary = (bool)reader["IsPrimary"]
                });
            }
            return mediaList;
        }




        public async Task<string> RemoveFromCartAsync(int userId, int productId, int quantity, int storeId)
        {
            using var connection = _connectionFactory.CreateConnection();
            await ((SqlConnection)connection).OpenAsync();
            using var transaction = ((SqlConnection)connection).BeginTransaction();

            try
            {

                const string checkCartSql = @"SELECT Quantity FROM UserCart 
                                    WHERE UserId = @UserId 
                                    AND ProductId = @ProductId 
                                    AND StoreId = @StoreId";

                int cartQty = 0;
                using (var checkCmd = new SqlCommand(checkCartSql, (SqlConnection)connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@UserId", userId);
                    checkCmd.Parameters.AddWithValue("@ProductId", productId);
                    checkCmd.Parameters.AddWithValue("@StoreId", storeId);

                    var result = await checkCmd.ExecuteScalarAsync();
                    if (result == null || result == DBNull.Value)
                    {
                        return "Item not in cart"; 
                    }
                    cartQty = Convert.ToInt32(result);
                }


                string updateCartSql = "";
                if (cartQty > 1)
                {
                    updateCartSql = @"UPDATE UserCart SET Quantity = Quantity - @Quantity 
                             WHERE UserId = @UserId AND ProductId = @ProductId AND StoreId = @StoreId";
                }
                else
                {
                    updateCartSql = @"DELETE FROM UserCart 
                             WHERE UserId = @UserId AND ProductId = @ProductId AND StoreId = @StoreId";
                }

                using (var cartCmd = new SqlCommand(updateCartSql, (SqlConnection)connection, transaction))
                {
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    cartCmd.Parameters.AddWithValue("@ProductId", productId);
                    cartCmd.Parameters.AddWithValue("@Quantity", quantity);
                    cartCmd.Parameters.AddWithValue("@StoreId", storeId);
                    await cartCmd.ExecuteNonQueryAsync();
                }


                string stockCol = "";
                const string getColSql = "SELECT TOP 1 * FROM Products";
                using (var colCmd = new SqlCommand(getColSql, (SqlConnection)connection, transaction))
                {
                    using (var reader = await colCmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            stockCol = reader.GetName(5); 
                        }
                    } 
                }

                if (!string.IsNullOrEmpty(stockCol))
                {
                    string updateStockSql = $"UPDATE Products SET {stockCol} = {stockCol} + @Quantity WHERE Id = @ProductId";
                    using (var stockCmd = new SqlCommand(updateStockSql, (SqlConnection)connection, transaction))
                    {
                        stockCmd.Parameters.AddWithValue("@ProductId", productId);
                        stockCmd.Parameters.AddWithValue("@Quantity", quantity);
                        await stockCmd.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return "Success";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return "Error: " + ex.Message;
            }
        }




      

    }
}
