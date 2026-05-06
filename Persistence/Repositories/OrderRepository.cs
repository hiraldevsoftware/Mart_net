using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Razorpay.Api;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public OrderRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        //        public async Task<int> PlaceOrderAsync(Domain.Entities.Order order)
        //        {
        //            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        //            await connection.OpenAsync();
        //            using var transaction = connection.BeginTransaction();

        //            try
        //            {
        //                const string orderSql = @"
        //            INSERT INTO Orders (UserId, StoreId, TotalAmount, Currency, OrderStatus, PaymentStatus, CreatedAt) 
        //            OUTPUT INSERTED.Id
        //            VALUES (@UserId, @StoreId, @Amount, @Curr, 0, 0, SYSDATETIMEOFFSET());";

        //                using var cmdOrder = new SqlCommand(orderSql, connection, transaction);

        //                cmdOrder.Parameters.AddWithValue("@UserId", order.UserId);
        //                cmdOrder.Parameters.AddWithValue("@StoreId", order.StoreId);

        //                // AA LINE CHECK KARO: Object mathi value kadhvi padse
        //                cmdOrder.Parameters.AddWithValue("@Amount", order.TotalAmount.Amount);
        //                cmdOrder.Parameters.AddWithValue("@Curr", order.TotalAmount.Currency ?? "INR");

        //                int orderId = Convert.ToInt32(await cmdOrder.ExecuteScalarAsync());

        //                foreach (var item in order.Items)
        //                {
        //                    const string itemSql = @"
        //INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, Currency) 
        //VALUES (@OrderId, @ProductId, @Quantity, @Price, @ItemCurr)";

        //                    using var cmdItem = new SqlCommand(itemSql, connection, transaction);
        //                    cmdItem.Parameters.AddWithValue("@OrderId", orderId);
        //                    cmdItem.Parameters.AddWithValue("@ProductId", item.ProductId);
        //                    cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);

        //                    // Ahiya pan object mathi value lo
        //                    cmdItem.Parameters.AddWithValue("@Price", item.PriceAtPurchase.Amount);
        //                    cmdItem.Parameters.AddWithValue("@ItemCurr", "INR");

        //                    await cmdItem.ExecuteNonQueryAsync();

        //                    // Stock update logic (Same as before)
        //                    const string stockSql = @"
        //                UPDATE ProductInventory SET StockQuantity = StockQuantity - @Qty 
        //                WHERE ProductId = @Pid AND StoreId = @Sid AND StockQuantity >= @Qty";

        //                    using var cmdStock = new SqlCommand(stockSql, connection, transaction);
        //                    cmdStock.Parameters.AddWithValue("@Qty", item.Quantity);
        //                    cmdStock.Parameters.AddWithValue("@Pid", item.ProductId);
        //                    cmdStock.Parameters.AddWithValue("@Sid", order.StoreId);

        //                    if (await cmdStock.ExecuteNonQueryAsync() == 0)
        //                        throw new Exception($"Stock nathi Product ID: {item.ProductId}");
        //                }

        //                transaction.Commit();
        //                return orderId;
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                throw new Exception("Order fail: " + ex.Message);
        //            }
        //        }



        public async Task<int> PlaceOrderAsync(Domain.Entities.Order order)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Orders ટેબલમાં એન્ટ્રી કરો
                const string orderSql = @"
            INSERT INTO Orders (UserId, StoreId, TotalAmount, Currency, OrderStatus, PaymentStatus, CreatedAt) 
            OUTPUT INSERTED.Id
            VALUES (@UserId, @StoreId, @Amount, @Curr, 0, 0, SYSDATETIMEOFFSET());";

                using var cmdOrder = new SqlCommand(orderSql, connection, transaction);
                cmdOrder.Parameters.AddWithValue("@UserId", order.UserId);
                cmdOrder.Parameters.AddWithValue("@StoreId", order.StoreId);
                cmdOrder.Parameters.AddWithValue("@Amount", order.TotalAmount.Amount);
                cmdOrder.Parameters.AddWithValue("@Curr", order.TotalAmount.Currency ?? "INR");

                int orderId = Convert.ToInt32(await cmdOrder.ExecuteScalarAsync());

                foreach (var item in order.Items)
                {
                    // 2. OrderItems ટેબલમાં એન્ટ્રી કરો
                    const string itemSql = @"
                INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, Currency) 
                VALUES (@OrderId, @ProductId, @Quantity, @Price, @ItemCurr)";

                    using var cmdItem = new SqlCommand(itemSql, connection, transaction);
                    cmdItem.Parameters.AddWithValue("@OrderId", orderId);
                    cmdItem.Parameters.AddWithValue("@ProductId", item.ProductId);
                    cmdItem.Parameters.AddWithValue("@Quantity", item.Quantity);
                    cmdItem.Parameters.AddWithValue("@Price", item.PriceAtPurchase.Amount);
                    cmdItem.Parameters.AddWithValue("@ItemCurr", "INR");
                    await cmdItem.ExecuteNonQueryAsync();

                    // --- 3. SMART STOCK LOGIC (અહીં ફેરફાર છે) ---

                    // પહેલા ચેક કરો કે આ પ્રોડક્ટ ઇન્વેન્ટરીમાં છે કે નહીં, જો ન હોય તો એડ કરો
                    const string ensureInventorySql = @"
    IF NOT EXISTS (SELECT 1 FROM ProductInventory WHERE ProductId = @Pid AND StoreId = @Sid)
    BEGIN
        -- અહિયાંથી UpdatedAt કાઢી નાખ્યું છે
        INSERT INTO ProductInventory (ProductId, StoreId, StockQuantity)
        VALUES (@Pid, @Sid, 100); 
    END";
                    using var cmdEnsure = new SqlCommand(ensureInventorySql, connection, transaction);
                    cmdEnsure.Parameters.AddWithValue("@Pid", item.ProductId);
                    cmdEnsure.Parameters.AddWithValue("@Sid", order.StoreId);
                    await cmdEnsure.ExecuteNonQueryAsync();

                    // હવે સ્ટોક અપડેટ કરો
                    const string stockSql = @"
                UPDATE ProductInventory 
                SET StockQuantity = StockQuantity - @Qty 
                WHERE ProductId = @Pid AND StoreId = @Sid AND StockQuantity >= @Qty";

                    using var cmdStock = new SqlCommand(stockSql, connection, transaction);
                    cmdStock.Parameters.AddWithValue("@Qty", item.Quantity);
                    cmdStock.Parameters.AddWithValue("@Pid", item.ProductId);
                    cmdStock.Parameters.AddWithValue("@Sid", order.StoreId);

                    if (await cmdStock.ExecuteNonQueryAsync() == 0)
                        throw new Exception($"Stock nathi Product ID: {item.ProductId}");
                }

                transaction.Commit();
                return orderId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Order fail: " + ex.Message);
            }
        }


        public async Task<IEnumerable<object>> GetOrderHistoryAsync(int userId)
        {
            var orderDict = new Dictionary<int, dynamic>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        SELECT o.Id AS OrderId, o.CreatedAt, o.TotalAmount, o.OrderStatus,
               oi.ProductId, p.Name AS ProductName, oi.Quantity, oi.UnitPrice
        FROM Orders o
        INNER JOIN OrderItems oi ON o.Id = oi.OrderId
        INNER JOIN Products p ON oi.ProductId = p.Id
        WHERE o.UserId = @UserId
        ORDER BY o.CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int orderId = (int)reader["OrderId"];

                if (!orderDict.ContainsKey(orderId))
                {
                    orderDict[orderId] = new
                    {
                        OrderId = orderId,
                        OrderDate = reader["CreatedAt"], 
                        TotalAmount = (decimal)reader["TotalAmount"],
                        Status = (int)reader["OrderStatus"],
                        Items = new List<object>()
                    };
                }

                ((List<object>)orderDict[orderId].Items).Add(new
                {
                    ProductId = (int)reader["ProductId"],
                    ProductName = reader["ProductName"].ToString(),
                    Quantity = (int)reader["Quantity"],
                    Price = (decimal)reader["UnitPrice"] 
                });
            }

            return orderDict.Values;
        }


        public async Task<object> GetOrderTrackingAsync(int orderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            //    const string sql = @"
            //SELECT o.Id, o.OrderStatus, t.Latitude, t.Longitude, t.LastUpdated
            //FROM Orders o
            //LEFT JOIN OrderTracking t ON o.Id = t.OrderId
            //WHERE o.Id = @OrderId";

            const string sql = @"
SELECT o.Id, o.OrderStatus, t.Latitude, t.Longitude, t.LastUpdated
FROM Orders o WITH (NOLOCK)
LEFT JOIN OrderTracking t WITH (NOLOCK) ON o.Id = t.OrderId
WHERE o.Id = @OrderId";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new
                {
                    OrderId = (int)reader["Id"],
                    Status = (int)reader["OrderStatus"],

                    RiderLocation = reader["Latitude"] != DBNull.Value ? new
                    {
                        Lat = (decimal)reader["Latitude"],
                        Lng = (decimal)reader["Longitude"],
                        LastUpdated = (DateTime)reader["LastUpdated"]
                    } : null
                };
            }
            return null;
        }

        public async Task<int> PlaceOneTapOrderAsync(int userId, int storeId, int? productId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {

                const string addrSql = @"
            SELECT TOP 1 Id FROM UserAddresses 
            WHERE UserId = @UserId AND IsDefault = 1 
            ORDER BY CreatedAt DESC";

                Guid addressId;
                using (var addrCmd = new SqlCommand(addrSql, connection, transaction))
                {
                    addrCmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await addrCmd.ExecuteScalarAsync();
                    if (result == null) throw new Exception("Tame koi default address set nathi karyu!");
                    addressId = (Guid)result;
                }

                const string userSql = "SELECT WalletBalance FROM Users WHERE Id = @UserId";
                decimal walletBalance = 0;
                using (var userCmd = new SqlCommand(userSql, connection, transaction))
                {
                    userCmd.Parameters.AddWithValue("@UserId", userId);
                    var balance = await userCmd.ExecuteScalarAsync();
                    walletBalance = balance != DBNull.Value ? (decimal)balance : 0;
                }

                var itemsToOrder = new List<OrderItemDetail>();
                if (productId.HasValue)
                {
                    const string prodSql = "SELECT Id, Name, PriceAmount FROM Products WHERE Id = @Pid";
                    using var prodCmd = new SqlCommand(prodSql, connection, transaction);
                    prodCmd.Parameters.AddWithValue("@Pid", productId.Value);
                    using var reader = await prodCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        itemsToOrder.Add(new OrderItemDetail
                        {
                            ProductId = (int)reader["Id"],
                            Quantity = 1,
                            UnitPrice = (decimal)reader["PriceAmount"]
                        });
                    }
                }
                else
                {
                    const string cartSql = "SELECT ProductId, Quantity, UnitPrice FROM Cart WHERE UserId = @UserId AND StoreId = @StoreId";
                    using var cartCmd = new SqlCommand(cartSql, connection, transaction);
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    cartCmd.Parameters.AddWithValue("@StoreId", storeId);
                    using var reader = await cartCmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        itemsToOrder.Add(new OrderItemDetail
                        {
                            ProductId = (int)reader["ProductId"],
                            Quantity = (int)reader["Quantity"],
                            UnitPrice = (decimal)reader["UnitPrice"]
                        });
                    }
                }

                if (!itemsToOrder.Any()) throw new Exception("Cart khali che!");

                decimal totalAmount = itemsToOrder.Sum(x => x.UnitPrice * x.Quantity);


                const string insertOrderSql = @"
    INSERT INTO Orders (
        UserId, 
        StoreId, 
        TotalAmount, 
        Currency, 
        DeliveryAddressId, 
        OrderStatus, 
        PaymentStatus, 
        PaymentMethod, 
        CreatedAt, 
        IsDeleted
    )
    OUTPUT INSERTED.Id
    VALUES (
        @UserId, 
        @StoreId, 
        @Total, 
        'INR', 
        @AddrId, 
        0, -- OrderStatus: 0 (Placed)
        0, -- PaymentStatus: 0 (Pending)
        @PayMethod, 
        SYSDATETIMEOFFSET(), 
        0
    )";

                int orderId;
                using (var orderCmd = new SqlCommand(insertOrderSql, connection, transaction))
                {
                    orderCmd.Parameters.AddWithValue("@UserId", userId);
                    orderCmd.Parameters.AddWithValue("@StoreId", storeId);
                    orderCmd.Parameters.AddWithValue("@Total", totalAmount);
                    orderCmd.Parameters.AddWithValue("@AddrId", addressId); 
                    orderCmd.Parameters.AddWithValue("@PayMethod", "COD");
                    orderId = (int)await orderCmd.ExecuteScalarAsync();
                }



                transaction.Commit();
                return orderId;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }



        public async Task<int> PlaceScheduledOrderAsync(ScheduledCheckoutRequest request)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Check Slot Availability (JOINing with DeliverySlots table)
                const string checkSlotSql = @"
            SELECT (S.MaxOrders - COUNT(O.Id)) 
            FROM DeliverySlots S
            LEFT JOIN Orders O ON S.Id = O.DeliverySlotId 
                AND O.ScheduledDate = @SDate 
                AND O.IsDeleted = 0
            WHERE S.Id = @SlotId
            GROUP BY S.MaxOrders";

                using (var checkCmd = new SqlCommand(checkSlotSql, connection, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@SlotId", request.DeliverySlotId);
                    checkCmd.Parameters.AddWithValue("@SDate", request.ScheduledDate.Date);

                    var result = await checkCmd.ExecuteScalarAsync();

                    if (result == null)
                        throw new Exception("Bhai, aa Delivery Slot database ma nathi!");

                    int remaining = Convert.ToInt32(result);
                    if (remaining <= 0)
                        throw new Exception("Bhai, aa slot full thai gayo che. Bijo select karo!");
                }

                // 2. Insert the Scheduled Order
                const string sql = @"
            INSERT INTO Orders (
                UserId, StoreId, TotalAmount, Currency, OrderStatus, 
                PaymentStatus, PaymentMethod, DeliveryAddressId, 
                DeliverySlotId, ScheduledDate, RazorpayPaymentId, CreatedAt
            )
            OUTPUT INSERTED.Id
            VALUES (
                @UserId, @StoreId, @Total, 'INR', 1, 
                1, 'Razorpay', @AddrId, 
                @SlotId, @SDate, @RPayId, SYSDATETIMEOFFSET()
            )";

                int orderId;
                using (var cmd = new SqlCommand(sql, connection, transaction))
                {
                    // Explicitly defining types to avoid Guid/Int clash errors
                    cmd.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = request.UserId;
                    cmd.Parameters.Add("@StoreId", System.Data.SqlDbType.Int).Value = request.StoreId;
                    cmd.Parameters.Add("@Total", System.Data.SqlDbType.Decimal).Value = request.TotalAmount;
                    cmd.Parameters.Add("@AddrId", System.Data.SqlDbType.UniqueIdentifier).Value = request.DeliveryAddressId;
                    cmd.Parameters.Add("@SlotId", System.Data.SqlDbType.Int).Value = request.DeliverySlotId;
                    cmd.Parameters.Add("@SDate", System.Data.SqlDbType.Date).Value = request.ScheduledDate.Date;
                    cmd.Parameters.Add("@RPayId", System.Data.SqlDbType.NVarChar).Value = (object)request.RazorpayPaymentId ?? DBNull.Value;

                    orderId = (int)await cmd.ExecuteScalarAsync();
                }

                transaction.Commit();
                return orderId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Scheduled Order Fail: " + ex.Message);
            }
        }

        public async Task<IEnumerable<dynamic>> GetAvailableSlotsAsync(DateTime date)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();



            const string sql = @"
        SELECT 
            s.Id, 
            s.SlotName, 
            s.StartTime, 
            s.EndTime,
            (s.MaxOrders - COUNT(o.Id)) AS RemainingCapacity
        FROM DeliverySlots s
        LEFT JOIN Orders o ON s.Id = o.DeliverySlotId 
            AND CAST(o.ScheduledDate AS DATE) = @SelectedDate 
            AND o.IsDeleted = 0
        WHERE s.IsActive = 1
          -- Jo date 'Today' hoy, to khali future na slots batavo
          AND (@SelectedDate > CAST(GETDATE() AS DATE) 
               OR s.StartTime > CAST(GETDATE() AS TIME))
        GROUP BY s.Id, s.SlotName, s.StartTime, s.EndTime, s.MaxOrders
        HAVING (s.MaxOrders - COUNT(o.Id)) > 0
        ORDER BY s.StartTime";

            await connection.OpenAsync();

 
            var slots = new List<dynamic>();
            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@SelectedDate", date.Date);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    slots.Add(new
                    {
                        Id = (int)reader["Id"],
                        SlotName = reader["SlotName"].ToString(),
                        Remaining = (int)reader["RemainingCapacity"]
                    });
                }
            }
            return slots;
        }



        public async Task<string> InitiateRefundAsync(int orderId, string reason)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();


            const string sql = "SELECT RazorpayPaymentId, TotalAmount FROM Orders WHERE Id = @Id AND IsDeleted = 0";

            string paymentId = "";
            decimal totalAmount = 0;

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@Id", orderId);
                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    paymentId = reader["RazorpayPaymentId"]?.ToString();
                    totalAmount = (decimal)reader["TotalAmount"];
                }
                else
                {
                    return "Order not found!";
                }
            }

            if (string.IsNullOrEmpty(paymentId)) return "Payment ID missing. Refund not possible.";

            try
            {

                string key = "rzp_test_YourKey";
                string secret = "YourSecret";
                RazorpayClient client = new RazorpayClient(key, secret);


                long amountInPaise = Convert.ToInt64(totalAmount * 100);

                Dictionary<string, object> options = new Dictionary<string, object>();
                options.Add("amount", amountInPaise);
                options.Add("speed", "normal");
                options.Add("notes", new Dictionary<string, string>() { { "reason", reason }, { "orderId", orderId.ToString() } });

                Refund refund = client.Payment.Fetch(paymentId).Refund(options);

                if (refund["id"] != null)
                {

                    const string updateSql = "UPDATE Orders SET OrderStatus = 0, PaymentStatus = 2 WHERE Id = @Id";


                    using (var updateCmd = new SqlCommand(updateSql, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@Id", orderId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return "Refund Processed Successfully. Refund ID: " + refund["id"];
                }

                return "Refund failed at Razorpay side.";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }


        public async Task<bool> UpdateRiderAssignmentAsync(int orderId, int riderId, string response)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
       
                const string checkSql = @"
            SELECT Id FROM OrderAssignments 
            WHERE OrderId = @OrderId AND RiderId = @RiderId 
            AND Status = 'Pending' 
            AND DATEDIFF(SECOND, AssignmentTime, SYSDATETIMEOFFSET()) <= 60";

                using var checkCmd = new SqlCommand(checkSql, connection, transaction);
                checkCmd.Parameters.AddWithValue("@OrderId", orderId);
                checkCmd.Parameters.AddWithValue("@RiderId", riderId);

                var assignmentExists = await checkCmd.ExecuteScalarAsync();
                if (assignmentExists == null) return false; 

                const string updateAssignSql = @"
            UPDATE OrderAssignments 
            SET Status = @Response, ResponseTime = SYSDATETIMEOFFSET() 
            WHERE OrderId = @OrderId AND RiderId = @RiderId";

                using var updateCmd = new SqlCommand(updateAssignSql, connection, transaction);
                updateCmd.Parameters.AddWithValue("@Response", response);
                updateCmd.Parameters.AddWithValue("@OrderId", orderId);
                updateCmd.Parameters.AddWithValue("@RiderId", riderId);
                await updateCmd.ExecuteNonQueryAsync();


                if (response.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    const string updateOrderSql = @"
                UPDATE Orders 
                SET RiderId = @RiderId, OrderStatus = 3, UpdatedAt = SYSDATETIMEOFFSET() 
                WHERE Id = @OrderId";

                    using var orderCmd = new SqlCommand(updateOrderSql, connection, transaction);
                    orderCmd.Parameters.AddWithValue("@RiderId", riderId);
                    orderCmd.Parameters.AddWithValue("@OrderId", orderId);
                    await orderCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }


        public async Task HandleExpiredAssignmentsAsync()
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();


            const string sql = @"
        UPDATE OrderAssignments 
        SET Status = 'TimedOut', 
            ResponseTime = SYSDATETIMEOFFSET()
        WHERE Status = 'Pending' 
        AND DATEDIFF(SECOND, AssignmentTime, SYSDATETIMEOFFSET()) > 60";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

    }                                                                  
}
                                                                       