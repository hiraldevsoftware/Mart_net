namespace Mart.Domain.Interface
{
    public interface IRiderRepository
    {
        Task<bool> UpdateRiderAvailabilityAsync(int riderId, bool isAvailable);


        Task<int> CreateAssignmentAsync(int orderId, int riderId);

    
        Task<bool> RespondToAssignmentAsync(int orderId, int riderId, string status);

  
        Task HandleExpiredAssignmentsAsync();

       
        Task UpdateLocationAsync(int riderId, decimal lat, decimal lng);


        Task<bool> VerifyDeliveryOTPAsync(int orderId, string otp);


        Task<bool> StartWaitingTimerAsync(int orderId, decimal riderLat, decimal riderLng);


        Task<bool> AdjustInventoryAsync(int productId, int storeId, int quantity, string reason, int staffId);


        Task<bool> UpdateRiderEarningsAsync(int riderId, decimal amount, decimal tip);


        Task<decimal> GetTotalEarningsAsync(int riderId);


        Task<bool> UpdateRiderProfileAsync(int riderId, string licenseNo, string rcBookUrl);


        Task<bool> MarkOrderAsPickedUpAsync(int orderId);

        Task<string> GetCustomerPhoneForOrderAsync(int orderId);


        Task<IEnumerable<dynamic>> GetAvailableRidersInRangeAsync(int storeId, double radiusInMeters);


        Task<bool> CollectCashAsync(int riderId, int orderId, decimal amount);
        Task<bool> IsRiderUnderLimitAsync(int riderId);


        Task<bool> UpdateOrderStageAsync(int orderId, string stage, decimal lat, decimal lng);



        Task<string> RejectOrderAsync(int riderId, int orderId);
    }
}
