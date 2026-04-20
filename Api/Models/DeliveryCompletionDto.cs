namespace Mart.Api.Models
{
    public class DeliveryCompletionDto
    {
        public int OrderId { get; set; }

        public int RiderId { get; set; }

   
        public string Otp { get; set; } = string.Empty;


        public string PaymentMode { get; set; } = "COD";


        public decimal Amount { get; set; }

  
        public string? DeliveryPhotoUrl { get; set; }
    }
}
