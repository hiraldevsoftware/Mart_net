using Mart.Domain.Entities;
using static Azure.Core.HttpHeader;

namespace Mart.Domain.Interface
{
    public interface ICouponRepository
    {
        Task<Coupon> GetCouponByCodeAsync(string code);
        Task<IEnumerable<Coupon>> GetAvailableCouponsAsync(decimal cartTotal);
    }
}
