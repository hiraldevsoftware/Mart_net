using Mart.Api.Models;

namespace Mart.Domain.Interface
{
    public interface IVendorRepository
    {
        Task<int> CreatePurchaseOrderAsync(int productId, int vendorId, decimal quantity, DateTime deliveryDate);


        Task<IEnumerable<object>> GetPendingOrdersAsync(int vendorId);
        Task<bool> UpdatePOStatusAsync(int poId, string status);


        Task<object> GetVendorDashboardAsync(int vendorId);

        Task<bool> UpdateInventoryAsync(int productId, decimal price, int stock);

        Task<IEnumerable<object>> GetOrderHistoryAsync(int vendorId);


        Task<int> AddProductAsync(ProductRequest product, int vendorId); 
        Task<bool> UpdateProductAsync(int productId, ProductRequest product, int vendorId); 
        Task<IEnumerable<object>> GetMyProductsAsync(int vendorId);


        Task<IEnumerable<object>> GetOrdersByStatusAsync(int vendorId, string status);
        Task<bool> UpdateOrderStatusAsync(int poId, string status, int vendorId); 


        Task<object> GetVendorWalletAsync(int vendorId); 
        Task<object> GetVendorDashboardStatsAsync(int vendorId);
    }
}
