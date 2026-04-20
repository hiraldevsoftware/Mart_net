using Mart.Domain.Enums;

namespace Mart.Domain.Interface
{
    public interface IAdminOrderRepository
    {
        Task<IEnumerable<object>> GetPendingOrdersAsync();


        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status);


        Task<bool> AssignRiderAsync(int orderId, int riderId);
        Task<IEnumerable<object>> GetAvailableRidersAsync();

        Task<object?> GetOrderDetailsByIdAsync(int orderId);


        Task<object> GetDashboardStatsAsync();
    }
}
