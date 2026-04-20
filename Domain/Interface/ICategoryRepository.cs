using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>>GetAllCategoriesAsync();
    }
}
