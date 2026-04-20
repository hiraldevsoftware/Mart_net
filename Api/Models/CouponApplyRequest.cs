namespace Mart.Api.Models
{
    public class CouponApplyRequest
    {
        public string CouponCode { get; set; }
        public decimal CartTotal { get; set; }
    }
}
