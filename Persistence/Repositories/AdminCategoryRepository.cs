using Mart.Api.Models;
using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Mart.Persistence.Repositories
{
    public class AdminCategoryRepository : IAdminCategoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminCategoryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public async Task<Guid> AddCategoryAsync(Category category)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            //const string sql = @"
            //    INSERT INTO Categories (Id, Name, ImageUrl, IsActive)
            //    OUTPUT INSERTED.Id
            //    VALUES (NEWID(), @Name, @ImageUrl, @IsActive)";
            const string sql = @"
    INSERT INTO Categories (Id, Name, ImageUrl, IsActive)
    OUTPUT INSERTED.Id
    VALUES (@Id, @Name, @ImageUrl, @IsActive)";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", category.Id);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@ImageUrl", category.ImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return (Guid)await command.ExecuteScalarAsync();
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var categories = new List<Category>();
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "SELECT Id, Name, ImageUrl, IsActive FROM Categories WHERE IsActive = 1";

            using var command = new SqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    ImageUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
               
                });
            }
            return categories;
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "UPDATE Categories SET Name = @Name, ImageUrl = @ImageUrl, IsActive = @IsActive WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", category.Id);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@ImageUrl", category.ImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", category.IsActive);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        //public async Task<bool> DeleteCategoryAsync(Guid id)
        //{
        //    using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        //    const string sql = "UPDATE Categories SET IsActive = 0 WHERE Id = @Id";
        //    using var command = new SqlCommand(sql, connection);
        //    command.Parameters.AddWithValue("@Id", id);

        //    if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        //    return await command.ExecuteNonQueryAsync() > 0;
        //}

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            using var connection = (SqlConnection)_connectionFactory.CreateConnection();
            const string sql = "DELETE FROM Categories WHERE Id = @Id"; 
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }





    }
}
