using Mart.Api.Models;
using Mart.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Domain.Interface
{
    public interface IOrderRepository
    {
        Task<int> PlaceOrderAsync(Order order);
        Task<IEnumerable<object>> GetOrderHistoryAsync(int userId);
        Task<object> GetOrderTrackingAsync(int orderId);

        Task<int> PlaceOneTapOrderAsync(int userId, int storeId, int? productId);

        Task<int> PlaceScheduledOrderAsync(ScheduledCheckoutRequest request);
        Task HandleExpiredAssignmentsAsync();

        Task<IEnumerable<dynamic>> GetAvailableSlotsAsync(DateTime date);

        Task<string> InitiateRefundAsync(int orderId, string reason);
        Task<bool> UpdateRiderAssignmentAsync(int orderId, int riderId, string response);
    }
}
