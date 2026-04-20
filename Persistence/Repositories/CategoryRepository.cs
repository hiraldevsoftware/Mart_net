using Mart.Domain.Entities;
using Mart.Domain.Interface;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;

namespace Mart.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CategoryRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Category>>GetAllCategoriesAsync()
        {
            var categories = new List<Category>();

            using var connection = _connectionFactory.CreateConnection();

            const string sql = "SELECT * FROM Categories WHERE IsDeleted = 0";


            using var command = new SqlCommand(sql, (SqlConnection)connection);
            await ((SqlConnection)connection).OpenAsync();

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    Id = (Guid)reader["Id"],
                    Name = reader["Name"].ToString()!,
                    ImageUrl = reader["ImageUrl"]?.ToString()
                });
            }
            return categories;
        }
        
    }
}
