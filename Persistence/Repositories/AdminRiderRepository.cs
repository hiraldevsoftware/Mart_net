using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminRiderRepository : IAdminRiderRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminRiderRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> AssignShiftAsync(int riderId, string shiftType, TimeSpan start, TimeSpan end)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        IF EXISTS (SELECT 1 FROM RiderShifts WHERE RiderId = @Id)
            UPDATE RiderShifts SET ShiftType = @Type, StartTime = @Start, EndTime = @End WHERE RiderId = @Id
        ELSE
            INSERT INTO RiderShifts (RiderId, ShiftType, StartTime, EndTime) VALUES (@Id, @Type, @Start, @End)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);
            command.Parameters.AddWithValue("@Type", shiftType);
            command.Parameters.AddWithValue("@Start", start);
            command.Parameters.AddWithValue("@End", end);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<int>CreateRiderAsync(RiderRequest rider)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                INSERT INTO Riders (Name, PhoneNumber, IsAvailable, CreatedAt)
                VALUES (@Name, @Phone, 1, GETDATE());
                SELECT CAST(scope_identity() AS int);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", rider.Name);
            command.Parameters.AddWithValue("@Phone", rider.PhoneNumber);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<bool>DeleteRiderAsync(int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "DELETE FROM Riders WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IEnumerable<object>> GetAllRidersAsync()
        {
            var riders = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT Id, Name, PhoneNumber, IsAvailable FROM Riders ORDER BY CreatedAt DESC";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                riders.Add(new
                {
                    Id = reader["Id"],
                    Name = reader["Name"],
                    Phone = reader["PhoneNumber"],
                    IsAvailable = reader["IsAvailable"]
                });
            }
            return riders;
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
                riders.Add(new { Id = reader["Id"], Name = reader["Name"], Phone = reader["PhoneNumber"] });
            }
            return riders;
        }

        public async Task<IEnumerable<object>> GetRiderShiftsAsync(int riderId)
        {
            var shifts = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        SELECT Id, ShiftType, StartTime, EndTime, IsActive 
        FROM RiderShifts 
        WHERE RiderId = @Id 
        ORDER BY IsActive DESC";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                shifts.Add(new
                {
                    ShiftId = reader["Id"],
                    Type = reader["ShiftType"],
                    Start = reader["StartTime"].ToString(), 
                    End = reader["EndTime"].ToString(),
                    Status = (bool)reader["IsActive"] ? "Active" : "Inactive"
                });
            }
            return shifts;
        }

        public async Task<object> GetRiderWalletAsync(int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT * FROM RiderWallets WHERE RiderId = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new
                {
                    Total = reader["TotalEarnings"],
                    Pending = reader["PendingPayout"],
                    LastSettled = reader["LastSettlementDate"]
                };
            }
            return new { Total = 0, Pending = 0 };
        }

        public async Task<bool> SendAssignmentRequestAsync(int orderId, int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync();

            // Safety Check: Jo aa rider ne aa order mate pehla thi j request mokli hoy to dubara na jay
            const string checkSql = "SELECT COUNT(1) FROM OrderAssignments WHERE OrderId = @OrderId AND RiderId = @RiderId AND Status = 'Pending'";
            using (var checkCmd = new SqlCommand(checkSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@OrderId", orderId);
                checkCmd.Parameters.AddWithValue("@RiderId", riderId);
                int existing = (int)await checkCmd.ExecuteScalarAsync();
                if (existing > 0) return true; // Already sent
            }

            // Main Insert Query
            const string sql = @"
        INSERT INTO OrderAssignments (OrderId, RiderId, Status, AssignmentTime)
        VALUES (@OrderId, @RiderId, 'Pending', SYSDATETIMEOFFSET())";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@RiderId", riderId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> SettleRiderPayoutAsync(int riderId, decimal amount)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
        UPDATE RiderWallets 
        SET PendingPayout = PendingPayout - @Amount, 
            LastSettlementDate = GETDATE() 
        WHERE RiderId = @Id AND PendingPayout >= @Amount";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateRiderStatusAsync(int riderId, bool isAvailable)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Riders SET IsAvailable = @Status WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Status", isAvailable);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;                  
        }

        //New

        public async Task<bool> ApproveRiderAsync(int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
   
            const string sql = "UPDATE Riders SET IsVerified = 1, IsAvailable = 1 WHERE Id = @Id";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<IEnumerable<object>> GetLiveRiderLocationsAsync()
        {
            var locations = new List<object>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = "SELECT Id, Name, LastLat, LastLng, PhoneNumber FROM Riders WHERE LastLat IS NOT NULL";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                locations.Add(new
                {
                    RiderId = reader["Id"],
                    Name = reader["Name"],
                    Lat = reader["LastLat"],
                    Lng = reader["LastLng"],
                    Phone = reader["PhoneNumber"]
                });
            }
            return locations;
        }


        public async Task<bool> SettleRiderCashAsync(int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        IF EXISTS (SELECT 1 FROM RiderWallets WHERE RiderId = @Id)
        BEGIN
            UPDATE RiderWallets 
            SET CashInHand = 0, 
                LastSettlementDate = SYSDATETIMEOFFSET() 
            WHERE RiderId = @Id
        END
        ELSE
        BEGIN
            INSERT INTO RiderWallets (RiderId, CashInHand, TotalEarnings, PendingPayout, LastSettlementDate)
            VALUES (@Id, 0, 0, 0, SYSDATETIMEOFFSET())
        END";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

 
            await command.ExecuteNonQueryAsync();
            return true;
        }

        public async Task<bool> MarkPayoutAsPaidAsync(int riderId)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        IF EXISTS (SELECT 1 FROM RiderWallets WHERE RiderId = @Id)
        BEGIN
            UPDATE RiderWallets 
            SET PendingPayout = 0, 
                LastSettlementDate = SYSDATETIMEOFFSET() 
            WHERE RiderId = @Id
        END
        ELSE
        BEGIN
            INSERT INTO RiderWallets (RiderId, CashInHand, TotalEarnings, PendingPayout, LastSettlementDate)
            VALUES (@Id, 0, 0, 0, SYSDATETIMEOFFSET())
        END";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", riderId);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            await command.ExecuteNonQueryAsync();
            return true; 
        }
    }                                                         
}                                                                   
                                                                                       