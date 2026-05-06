using Dapper;
using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminStoreRepository : IAdminStoreRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public AdminStoreRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;


        //public async Task<bool> AssignManagerToStoreAsync(int staffId, int storeId)
        //{
        //    using var conn = (SqlConnection)_connectionFactory.CreateConnection();


        //    const string sql = @"
        //        UPDATE Users 
        //        SET StoreId = @StoreId, Role = 'Manager' 
        //        WHERE Id = @UserId";

        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.AddWithValue("@StoreId", storeId);
        //    cmd.Parameters.AddWithValue("@UserId", staffId);

        //    if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        //    int rows = await cmd.ExecuteNonQueryAsync();
        //    return rows > 0;
        //}


        public async Task<bool> AssignManagerToStoreAsync(int userId, int storeId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
        UPDATE Users 
        SET Role = 'Manager', StoreId = @StoreId, UpdatedAt = SYSDATETIMEOFFSET()
        WHERE Id = @UserId";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StoreId", storeId);
            cmd.Parameters.AddWithValue("@UserId", userId);

            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync() > 0;
        }


        public async Task<bool> AssignOrderToStoreAsync(int orderId, int storeId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

            const string sql = @"
        UPDATE Orders 
        SET StoreId = @StoreId, 
            UpdatedAt = SYSDATETIMEOFFSET() 
        WHERE Id = @OrderId";

            int rows = await conn.ExecuteAsync(sql, new { OrderId = orderId, StoreId = storeId });
            return rows > 0;
        }

        public async Task<IEnumerable<dynamic>> GetAssignableUsersAsync()
        {
            var list = new List<dynamic>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = "SELECT Id, Name, Email FROM Users WHERE Role = 'Customer' AND IsDeleted = 0";

            using var cmd = new SqlCommand(sql, conn);
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["Id"],
                    Name = reader["Name"].ToString(),
                    Email = reader["Email"].ToString()
                });
            }
            return list;
        }


        public async Task<IEnumerable<dynamic>> GetPendingStockRequestsAsync()
        {
            var list = new List<dynamic>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();

  
            const string sql = @"
        SELECT sr.Id, sr.StoreId, sr.ProductId, sr.RequestedQuantity, 
               s.StoreName, p.Name as ProductName 
        FROM StockRequests sr
        JOIN Stores s ON sr.StoreId = s.Id
        JOIN Products p ON sr.ProductId = p.Id
        WHERE sr.Status = 1"; 

            using var cmd = new SqlCommand(sql, conn);
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["Id"],
                    StoreId = reader["StoreId"],
                    ProductId = reader["ProductId"],
                    Qty = reader["RequestedQuantity"],
                    StoreName = reader["StoreName"].ToString(),
                    ProductName = reader["ProductName"].ToString()
                });
            }
            return list;
        }


        public async Task<bool> ProcessStockRequestAsync(StockApprovalDto dto)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var trans = conn.BeginTransaction();

            try
            {
                int statusId = dto.IsApproved ? 2 : 3;


                const string sqlUpdateReq = @"
            UPDATE StockRequests 
            SET Status = @Status
            WHERE Id = @Id";

                using var cmd1 = new SqlCommand(sqlUpdateReq, conn, trans);
                cmd1.Parameters.AddWithValue("@Status", statusId);
                cmd1.Parameters.AddWithValue("@Id", dto.RequestId);

                int rowsAffected = await cmd1.ExecuteNonQueryAsync();
                if (rowsAffected == 0) throw new Exception("Request ID not found.");


                if (dto.IsApproved)
                {
        
                    const string sqlUpdateInv = @"
                MERGE INTO ProductInventory AS Target
                USING (SELECT StoreId, ProductId, RequestedQuantity FROM StockRequests WHERE Id = @Id) AS Source
                ON (Target.StoreId = Source.StoreId AND Target.ProductId = Source.ProductId)
                WHEN MATCHED THEN
                    UPDATE SET Target.StockQuantity = Target.StockQuantity + Source.RequestedQuantity,
                               Target.LastUpdated = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (StoreId, ProductId, StockQuantity, LastUpdated, Reason)
                    VALUES (Source.StoreId, Source.ProductId, Source.RequestedQuantity, GETDATE(), 'Stock Approved');";

                    using var cmd2 = new SqlCommand(sqlUpdateInv, conn, trans);
                    cmd2.Parameters.AddWithValue("@Id", dto.RequestId);
                    await cmd2.ExecuteNonQueryAsync();
                }

                await trans.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                throw new Exception("SQL Error: " + ex.Message);
            }
        }


        public async Task<IEnumerable<dynamic>> GetAllStoresSummaryAsync()
        {
            var list = new List<dynamic>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = @"
                SELECT s.Id, s.StoreName, 
                (SELECT COUNT(*) FROM ProductInventory WHERE StoreId = s.Id) as TotalProducts,
                (SELECT COUNT(*) FROM Orders WHERE StoreId = s.Id AND OrderStatus = 3) as ActiveOrders
                FROM Stores s";

            using var cmd = new SqlCommand(sql, conn);
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["Id"],
                    StoreName = reader["StoreName"].ToString(),
                    TotalProducts = reader["TotalProducts"],
                    ActiveOrders = reader["ActiveOrders"]
                });
            }
            return list;
        }


        public async Task<IEnumerable<dynamic>> GetInventoryAuditReportsAsync()
        {
            var list = new List<dynamic>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();


            const string sql = @"
                SELECT a.*, s.StoreName, p.Name as ProductName, (a.PhysicalQuantity - a.SystemQuantity) as Mismatch
                FROM InventoryAudit a
                JOIN Stores s ON a.StoreId = s.Id
                JOIN Products p ON a.ProductId = p.Id
                WHERE a.PhysicalQuantity <> a.SystemQuantity";

            using var cmd = new SqlCommand(sql, conn);
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader["Id"],
                    StoreName = reader["StoreName"].ToString(),
                    ProductName = reader["ProductName"].ToString(),
                    PhysicalQty = reader["PhysicalQuantity"],
                    SystemQty = reader["SystemQuantity"],
                    Mismatch = reader["Mismatch"]
                });
            }
            return list;
        }
    }
}