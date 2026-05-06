using Mart.Api.Models;

namespace Mart.Domain.Interface
{
    public interface IAdminStoreRepository
    {

        Task<bool> AssignManagerToStoreAsync(int staffId, int storeId);


        Task<IEnumerable<dynamic>> GetAllStoresSummaryAsync();
        Task<bool> AssignOrderToStoreAsync(int orderId, int storeId);



        Task<IEnumerable<dynamic>> GetPendingStockRequestsAsync();
        Task<bool> ProcessStockRequestAsync(StockApprovalDto dto);


        Task<IEnumerable<dynamic>> GetInventoryAuditReportsAsync();

        Task<IEnumerable<dynamic>> GetAssignableUsersAsync();
    }
}
