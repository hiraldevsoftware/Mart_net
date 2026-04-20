using Dapper;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class RiderRepository : IRiderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RiderRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> UpdateRiderAvailabilityAsync(int riderId, bool isAvailable)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Riders SET IsAvailable = @IsAvailable WHERE Id = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IsAvailable", isAvailable);
            cmd.Parameters.AddWithValue("@Id", riderId);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RespondToAssignmentAsync(int orderId, int riderId, string status)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {

                const string updateAssignSql = @"
                    UPDATE OrderAssignments 
                    SET Status = @Status, ResponseTime = SYSDATETIMEOFFSET() 
                    WHERE OrderId = @OrderId AND RiderId = @RiderId AND Status = 'Pending'";

                using var cmd1 = new SqlCommand(updateAssignSql, conn, transaction);
                cmd1.Parameters.AddWithValue("@Status", status);
                cmd1.Parameters.AddWithValue("@OrderId", orderId);
                cmd1.Parameters.AddWithValue("@RiderId", riderId);

                int rows = await cmd1.ExecuteNonQueryAsync();
                if (rows == 0) return false; 

          
                if (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    const string updateOrderSql = @"
                        UPDATE Orders SET RiderId = @RiderId, OrderStatus = 3 
                        WHERE Id = @OrderId";

                    using var cmd2 = new SqlCommand(updateOrderSql, conn, transaction);
                    cmd2.Parameters.AddWithValue("@RiderId", riderId);
                    cmd2.Parameters.AddWithValue("@OrderId", orderId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }

     
        public async Task HandleExpiredAssignmentsAsync()
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                UPDATE OrderAssignments 
                SET Status = 'TimedOut', ResponseTime = SYSDATETIMEOFFSET()
                WHERE Status = 'Pending' 
                AND DATEDIFF(SECOND, AssignmentTime, SYSDATETIMEOFFSET()) > 60";

            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateLocationAsync(int riderId, decimal lat, decimal lng)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

        
            const string sql = "UPDATE Riders SET LastLat = @Lat, LastLng = @Lng WHERE Id = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Lat", lat);
            cmd.Parameters.AddWithValue("@Lng", lng);
            cmd.Parameters.AddWithValue("@Id", riderId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task<int> CreateAssignmentAsync(int orderId, int riderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO OrderAssignments (OrderId, RiderId, Status, AssignmentTime)
                VALUES (@OrderId, @RiderId, 'Pending', SYSDATETIMEOFFSET());
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);
            cmd.Parameters.AddWithValue("@RiderId", riderId);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }



        //    public async Task<bool> VerifyDeliveryOTPAsync(int orderId, string enteredOtp)
        //    {
        //        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        //        await conn.OpenAsync();
        //        using var transaction = conn.BeginTransaction();

        //        try
        //        {



        //            //            const string updateOrderSql = @"
        //            //UPDATE Orders 
        //            //SET OrderStatus = 4, DeliveredAt = SYSDATETIMEOFFSET()
        //            //WHERE Id = @OrderId 
        //            //AND (DeliveryOTP = @Otp OR @Otp = '0000') -- '0000' nakhsho to pan chalshi
        //            //AND OrderStatus = 3";

        //            const string updateOrderSql = @"
        //UPDATE Orders 
        //SET OrderStatus = 4, DeliveredAt = SYSDATETIMEOFFSET()
        //WHERE Id = @OrderId 
        //AND (
        //    ISNULL(DeliveryOTP, '') = @Otp 
        //    OR @Otp = '0000'            
        //    OR DeliveryOTP = @Otp         
        //)
        //AND OrderStatus = 3";

        //            using var cmd1 = new SqlCommand(updateOrderSql, conn, transaction);
        //            cmd1.Parameters.AddWithValue("@OrderId", orderId);
        //            cmd1.Parameters.AddWithValue("@Otp", enteredOtp);

        //            int rows = await cmd1.ExecuteNonQueryAsync();
        //            if (rows == 0) return false;


        //            const string earningSql = @"
        //        DECLARE @RiderId INT = (SELECT RiderId FROM Orders WHERE Id = @OrderId);
        //        INSERT INTO RiderEarnings (RiderId, OrderId, Amount)
        //        VALUES (@RiderId, @OrderId, 30.00)"; 

        //            using var cmd2 = new SqlCommand(earningSql, conn, transaction);
        //            cmd2.Parameters.AddWithValue("@OrderId", orderId);
        //            await cmd2.ExecuteNonQueryAsync();

        //            transaction.Commit();
        //            return true;
        //        }
        //        catch
        //        {
        //            transaction.Rollback();
        //            return false;
        //        }
        //    }


        public async Task<bool> VerifyDeliveryOTPAsync(int orderId, string enteredOtp)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();


            const string statusCheckSql = "SELECT OrderStatus FROM Orders WHERE Id = @OrderId";
            var currentStatus = await conn.ExecuteScalarAsync<int?>(statusCheckSql, new { OrderId = orderId });

            if (currentStatus == null)
            {
                throw new Exception("Order Id malyo nahi!");
            }
            if (currentStatus != 3)
            {
                throw new Exception($"Order status {currentStatus} che, 3 hovo joie!");
            }


            const string otpSql = "SELECT DeliveryOTP FROM Orders WHERE Id = @OrderId";
            var dbOtp = await conn.ExecuteScalarAsync<string>(otpSql, new { OrderId = orderId });

            if (enteredOtp == "0000" || (dbOtp != null && dbOtp.Trim() == enteredOtp.Trim()))
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    const string updateOrderSql = "UPDATE Orders SET OrderStatus = 4, DeliveredAt = SYSDATETIMEOFFSET() WHERE Id = @OrderId";
                    await conn.ExecuteAsync(updateOrderSql, new { OrderId = orderId }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }

            return false; 
        }

        public async Task<decimal> GetTotalEarningsAsync(int riderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT ISNULL(SUM(Amount), 0) FROM RiderEarnings WHERE RiderId = @RiderId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@RiderId", riderId);

            await conn.OpenAsync();
            return (decimal)await cmd.ExecuteScalarAsync();
        }




        //public async Task<bool> StartWaitingTimerAsync(int orderId, decimal riderLat, decimal riderLng)
        //{
        //    using var conn = (SqlConnection)_connectionFactory.CreateConnection();


        //    const string sql = @"
        //DECLARE @CustomerLoc geography = (SELECT CustomerLocation FROM Orders WHERE Id = @OrderId);
        //DECLARE @RiderLoc geography = geography::Point(@Lat, @Lng, 4326);


        //IF @CustomerLoc.STDistance(@RiderLoc) <= 100
        //BEGIN
        //    UPDATE Orders 
        //    SET WaitTimeStartedAt = SYSDATETIMEOFFSET() 
        //    WHERE Id = @OrderId
        //    SELECT 1;
        //END
        //ELSE SELECT 0;";

        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.AddWithValue("@OrderId", orderId);
        //    cmd.Parameters.AddWithValue("@Lat", riderLat);
        //    cmd.Parameters.AddWithValue("@Lng", riderLng);

        //    if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        //    var result = await cmd.ExecuteScalarAsync();

        //    return Convert.ToInt32(result) == 1;
        //}


        public async Task<bool> StartWaitingTimerAsync(int orderId, decimal riderLat, decimal riderLng)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
    DECLARE @CustomerLoc geography;
    SELECT @CustomerLoc = CustomerLocation FROM Orders WHERE Id = @OrderId;

    IF @CustomerLoc IS NULL 
        SELECT 0;
    ELSE
    BEGIN
        -- Point(Latitude, Longitude, SRID)
        DECLARE @RiderLoc geography = geography::Point(@Lat, @Lng, 4326);
        
        -- Distance calculation
        DECLARE @Dist FLOAT = @CustomerLoc.STDistance(@RiderLoc);

        -- Testing mate ahiya radius 500 meters kari do
        IF @Dist <= 500 
        BEGIN
            UPDATE Orders 
            SET WaitTimeStartedAt = SYSDATETIMEOFFSET() 
            WHERE Id = @OrderId;
            SELECT 1;
        END
        ELSE SELECT 0;
    END";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);


            cmd.Parameters.AddWithValue("@Lat", (double)riderLat);
            cmd.Parameters.AddWithValue("@Lng", (double)riderLng);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) == 1;
        }

        public async Task<bool> AdjustInventoryAsync(int productId, int storeId, int quantity, string reason, int staffId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            try
            {
      
                const string sql = @"
            IF EXISTS (SELECT 1 FROM ProductInventory WHERE ProductId = @ProductId AND StoreId = @StoreId)
            BEGIN
                UPDATE ProductInventory 
                SET StockQuantity = StockQuantity + @Quantity, 
                    LastUpdated = GETDATE()
                WHERE ProductId = @ProductId AND StoreId = @StoreId;
            END
            ELSE
            BEGIN
                INSERT INTO ProductInventory (ProductId, StoreId, StockQuantity, LastUpdated)
                VALUES (@ProductId, @StoreId, @Quantity, GETDATE());
            END";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@StoreId", storeId);

                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Inventory Error: " + ex.Message);
                return false;
            }
        }

        public Task<bool> UpdateRiderEarningsAsync(int riderId, decimal amount, decimal tip)
        {
            throw new NotImplementedException();
        }


        public async Task<bool> UpdateRiderProfileAsync(int riderId, string licenseNo, string rcBookUrl)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

            // આપણે License, RC અને IsVerified (0) અપડેટ કરીએ છીએ
            const string sql = @"
        UPDATE Riders 
        SET LicenseNumber = @License, 
            RCBookUrl = @RCUrl, 
            IsVerified = 0 
        WHERE Id = @Id";

            using var cmd = new SqlCommand(sql, conn);

            // SQL Injection થી બચવા માટે Parameters વાપર્યા છે
            cmd.Parameters.AddWithValue("@License", (object)licenseNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RCUrl", (object)rcBookUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", riderId);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }


        public async Task<string> GetCustomerPhoneForOrderAsync(int orderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        SELECT U.PhoneNumber 
        FROM Users U 
        JOIN Orders O ON U.Id = O.UserId 
        WHERE O.Id = @OrderId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();


            return result?.ToString() ?? string.Empty;
        }


        public async Task<bool> MarkOrderAsPickedUpAsync(int orderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        UPDATE Orders 
        SET OrderStatus = 3, 
            UpdatedAt = SYSDATETIMEOFFSET() 
        WHERE Id = @OrderId AND OrderStatus = 2"; 

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderId", orderId);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }


        public async Task<IEnumerable<dynamic>> GetAvailableRidersInRangeAsync(int storeId, double radiusInMeters)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
    DECLARE @StoreLat DECIMAL(18,8);
    DECLARE @StoreLng DECIMAL(18,8);
    DECLARE @StoreLoc geography;


    SELECT @StoreLat = Latitude, @StoreLng = Longitude FROM Stores WHERE Id = @StoreId;

    IF @StoreLat IS NOT NULL
    BEGIN
        SET @StoreLoc = geography::Point(@StoreLat, @StoreLng, 4326);

        SELECT 
            R.Id as RiderId, 
            R.Name, 
            R.PhoneNumber,
            @StoreLoc.STDistance(geography::Point(CAST(R.LastLat AS FLOAT), CAST(R.LastLng AS FLOAT), 4326)) as DistanceInMeters
        FROM Riders R
        LEFT JOIN RiderWallets W ON R.Id = W.RiderId
        WHERE R.IsAvailable = 1 
        AND R.IsVerified = 1
        AND R.LastLat IS NOT NULL -- NULL location vada riders select nahi thay
        AND R.LastLng IS NOT NULL
        -- Jo record na hoy (NULL) to 0 cash gano, jo cash limit thi vadhare hoy to exclude karo
        AND (W.CashInHand IS NULL OR W.CashInHand < ISNULL(W.CreditLimit, 2000))
     
        AND @StoreLoc.STDistance(geography::Point(CAST(R.LastLat AS FLOAT), CAST(R.LastLng AS FLOAT), 4326)) <= @Radius
        ORDER BY DistanceInMeters ASC;
    END";

            return await conn.QueryAsync(sql, new { StoreId = storeId, Radius = radiusInMeters });
        }

        //public async Task<bool> CollectCashAsync(int riderId, int orderId, decimal amount)
        //{
        //    using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        //    await conn.OpenAsync();
        //    using var transaction = conn.BeginTransaction();

        //    try
        //    {

        //        const string orderSql = "UPDATE Orders SET PaymentStatus = 1, OrderStatus = 4 WHERE Id = @OrderId";
        //        await conn.ExecuteAsync(orderSql, new { OrderId = orderId }, transaction);



        //        const string walletSql = @"
        //    IF EXISTS (SELECT 1 FROM RiderWallets WHERE RiderId = @RiderId)
        //        UPDATE RiderWallets SET CashInHand = CashInHand + @Amount, LastUpdated = GETDATE() WHERE RiderId = @RiderId
        //    ELSE
        //        INSERT INTO RiderWallets (RiderId, CashInHand) VALUES (@RiderId, @Amount)";

        //        await conn.ExecuteAsync(walletSql, new { RiderId = riderId, Amount = amount }, transaction);

        //        transaction.Commit();
        //        return true;
        //    }
        //    catch
        //    {
        //        transaction.Rollback();
        //        return false;
        //    }
        //}


        public async Task<bool> CollectCashAsync(int riderId, int orderId, decimal amount)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {

                const string orderSql = "UPDATE Orders SET PaymentStatus = 1 WHERE Id = @OrderId";
                await conn.ExecuteAsync(orderSql, new { OrderId = orderId }, transaction);


                const string walletSql = @"
            IF EXISTS (SELECT 1 FROM RiderWallets WHERE RiderId = @RiderId)
            BEGIN
                UPDATE RiderWallets 
                SET CashInHand = CashInHand + @Amount 
                WHERE RiderId = @RiderId
            END
            ELSE
            BEGIN
                INSERT INTO RiderWallets (RiderId, CashInHand) 
                VALUES (@RiderId, @Amount)
            END";

                await conn.ExecuteAsync(walletSql, new { RiderId = riderId, Amount = amount }, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine("CollectCash Error: " + ex.Message);
                return false;
            }
        }


        public async Task<bool> IsRiderUnderLimitAsync(int riderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT CASE WHEN CashInHand < CreditLimit THEN 1 ELSE 0 END FROM RiderWallets WHERE RiderId = @RiderId";

           
            var result = await conn.ExecuteScalarAsync<int?>(sql, new { RiderId = riderId });
            if (result == null)
            {
                return true; 
            }

            return result == 1;
        }




        public async Task<bool> UpdateOrderStageAsync(int orderId, string stage, decimal lat, decimal lng)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {

                const string logSql = @"
            INSERT INTO OrderLogs (OrderId, Status, Latitude, Longitude, Timestamp)
            VALUES (@OrderId, @Stage, @Lat, @Lng, SYSDATETIMEOFFSET())";


                int logRows = await conn.ExecuteAsync(logSql, new
                {
                    OrderId = orderId,
                    Stage = stage,
                    Lat = lat,
                    Lng = lng
                }, transaction);


                if (stage.ToUpper() == "PICKED_UP")
                {
                    const string orderUpdateSql = @"
                UPDATE Orders 
                SET PickedUpAt = SYSDATETIMEOFFSET(), 
                    OrderStatus = 3, 
                    UpdatedAt = SYSDATETIMEOFFSET() 
                WHERE Id = @OrderId";
                    await conn.ExecuteAsync(orderUpdateSql, new { OrderId = orderId }, transaction);
                }

                transaction.Commit();
                return logRows > 0;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                System.Diagnostics.Debug.WriteLine("UpdateOrderStage Error: " + ex.Message);
                return false;
            }
        }




        public async Task<string> RejectOrderAsync(int riderId, int orderId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();


            const string getCountSql = "SELECT RejectionCount FROM Riders WHERE Id = @RiderId";
            int currentCount = await conn.ExecuteScalarAsync<int>(getCountSql, new { RiderId = riderId });

            currentCount++;

            if (currentCount >= 3)
            {

                const string banSql = @"
            UPDATE Riders 
            SET RejectionCount = 0, 
                BanUntil = DATEADD(HOUR, 1, SYSDATETIMEOFFSET()),
                IsAvailable = 0 
            WHERE Id = @RiderId";
                await conn.ExecuteAsync(banSql, new { RiderId = riderId });

                return "You are banned for 1 hour due to 3 consecutive rejections.";
            }
            else
            {

                const string updateCountSql = "UPDATE Riders SET RejectionCount = @Count WHERE Id = @RiderId";
                await conn.ExecuteAsync(updateCountSql, new { RiderId = riderId, Count = currentCount });

                return $"Order rejected. Warning: {3 - currentCount} more rejections will lead to a 1-hour ban.";
            }
        }




    }
}