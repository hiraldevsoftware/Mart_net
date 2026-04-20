namespace Mart.Domain.Interface
{
    public interface IAdminMarketingRepository
    {
        Task<decimal> GetReferralBonusAsync();
        Task<bool> UpdateReferralBonusAsync(decimal amount);


        Task<IEnumerable<object>> GetTopReferrersAsync();
    }
}
