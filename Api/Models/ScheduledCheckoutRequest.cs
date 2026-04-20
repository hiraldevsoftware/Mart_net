namespace Mart.Api.Models
{
    public class ScheduledCheckoutRequest
    {

        public int UserId { get; set; }

        public int StoreId { get; set; }


        public int DeliverySlotId { get; set; }

        public DateTime ScheduledDate { get; set; }


        public string RazorpayOrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpaySignature { get; set; }


        public Guid DeliveryAddressId { get; set; } 


        public decimal TotalAmount { get; set; }

      
        public List<OrderItemRequest> Items
        {
            get; set;
        }


        public class OrderItemRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
