using Mart.Domain.Entities;

namespace Mart.Domain.Interface
{
    public interface IStoreRepository
    {
        Task<Store?>GetNearestStoreAsync(decimal userLat, decimal userLong);

        Task<IEnumerable<dynamic>> GetStorePendingOrdersAsync(int storeId);

        Task<IEnumerable<dynamic>> GetStoreInventoryAsync(int storeId);
        //Task<bool> UpdateStoreStockAsync(int storeId, int productId, int newQuantity);
        Task<bool> UpdateStoreStockAsync(int storeId, int productId, int newQuantity, int staffId, string reason);

        Task<bool> IsStoreManagerValidAsync(int storeId, int staffId);


        Task<bool> UpdateOrderStatusAsync(int orderId, int storeId, int newStatus);


        Task<IEnumerable<dynamic>> GetOrderItemsAsync(int orderId);

        Task<bool> UpdateProductLocationAsync(int productId, int storeId, string rack, string shelf);

        Task<bool> VerifyProductInOrderAsync(int orderId, string barcode);

        Task<IEnumerable<Store>> GetAllStoresAsync();

        Task<bool> CreateStockRequestAsync(int storeId, int productId, int qty, int staffId);
        Task<IEnumerable<dynamic>> GetStoreStockRequestsAsync(int storeId);


        Task<bool> PerformStockAuditAsync(int storeId, int productId, int physicalQty, int staffId, string remarks);



    }
}
