using Mart.Api.Models;
using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface IAdminCategoryRepository
    {
        Task<Guid> AddCategoryAsync(Category category);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(Guid id);
    }
}
