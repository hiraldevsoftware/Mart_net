using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(Guid categoryId);

        Task<IEnumerable<Product>> SearchProductsAsync(string term);

        Task<int> GetStockAsync(int productId, int storeId);
        Task<bool> UpdateStockAsync(int productId, int storeId, int quantity);


     
        Task<bool> AddToWishlistAsync(int userId, int productId);
        Task<bool> RemoveFromWishlistAsync(int userId, int productId);
        Task<IEnumerable<Product>> GetWishlistAsync(int userId);

        Task<string> AddToCartAsync(int userId, int productId, int quantity, int storeId);
        Task<IEnumerable<object>> GetUserCartAsync(int userId);

        Task RegisterForNotificationAsync(int userId, int productId);

        Task<string> RemoveFromCartAsync(int userId, int productId, int quantity, int storeId);

        Task<IEnumerable<object>> GetFrequentlyBoughtTogetherAsync(int productId);

        Task<IEnumerable<ProductMedia>> GetProductMediaAsync(int productId);

    }
}
