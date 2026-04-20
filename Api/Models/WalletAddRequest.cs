namespace Mart.Api.Models
{
    public class WalletAddRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string RazorpayPaymentId { get; set; }
    }
}
