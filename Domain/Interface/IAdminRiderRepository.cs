using Mart.Api.Models;

namespace Mart.Domain.Interface
{
    public interface IAdminRiderRepository
    {
        Task<int> CreateRiderAsync(RiderRequest rider);
        Task<IEnumerable<object>> GetAllRidersAsync();
        Task<IEnumerable<object>> GetAvailableRidersAsync();
        Task<bool> UpdateRiderStatusAsync(int riderId, bool isAvailable);
        Task<bool> DeleteRiderAsync(int riderId);


        Task<bool> ApproveRiderAsync(int riderId);
        Task<IEnumerable<object>> GetLiveRiderLocationsAsync();
        Task<bool> SettleRiderCashAsync(int riderId);
        Task<bool> MarkPayoutAsPaidAsync(int riderId);


        Task<bool> SendAssignmentRequestAsync(int orderId, int riderId);

        Task<object> GetRiderWalletAsync(int riderId);
        Task<bool> SettleRiderPayoutAsync(int riderId, decimal amount);


        Task<bool> AssignShiftAsync(int riderId, string shiftType, TimeSpan start, TimeSpan end);
        Task<IEnumerable<object>> GetRiderShiftsAsync(int riderId);
    }
}
