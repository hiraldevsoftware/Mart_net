using Mart.Api.Models;
using Mart.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Mart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController: ControllerBase
    {
        private readonly ICouponRepository _repo;
        public CouponController(ICouponRepository repo) => _repo = repo;

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyCoupon([FromBody] CouponApplyRequest request)
        {
            var coupon = await _repo.GetCouponByCodeAsync(request.CouponCode);

            if (coupon == null)
                return BadRequest(new { message = "This Coupon Is Not Valid!" });

            if (request.CartTotal < coupon.MinOrderValue)
            {
                return BadRequest(new { message = $"Aa coupon mate minimum {coupon.MinOrderValue} no order joie." });
            }

            decimal discount = 0;


            if (coupon.DiscountPercentage > 0)
            {
                discount = (request.CartTotal * coupon.DiscountPercentage.Value) / 100;
            }
            else
            {
                discount = coupon.DiscountAmount;
            }

            return Ok(new
            {
                couponCode = coupon.Code,
                discountAmount = discount,
                finalAmount = request.CartTotal - discount,
                message = "Coupon applied successfully!"
            });
        }




        [HttpGet("available/{cartTotal}")]
        public async Task<IActionResult> GetAvailableCoupons(decimal cartTotal)
        {
            try
            {
                var coupons = await _repo.GetAvailableCouponsAsync(cartTotal);

                if (coupons == null || !coupons.Any())
                {
                    return Ok(new { message = "Not Any Offer Available", coupons = new List<object>() });
                }

                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error fetching coupons", details = ex.Message });
            }
        }
    }
}
