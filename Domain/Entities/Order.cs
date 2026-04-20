using Mart.Domain.Common;
using Mart.Domain.Enums;
using Mart.Domain.ValueObjects;

namespace Mart.Domain.Entities
{
    public class Order : BaseEntity
    {
        // Private constructor for EF Core or Activator
        private Order() { }

        public Order(int userId, int storeId, Money totalAmount)
        {
            UserId = userId;
            StoreId = storeId; 
            TotalAmount = totalAmount;
            Status = OrderStatus.Placed ;
            PaymentStatus = PaymentStatus.Pending;
        }

        public int UserId { get; set; }
        public int StoreId { get; private set; } 
        public Money TotalAmount { get; private set; }
        public OrderStatus Status { get;  set; }
        public PaymentStatus PaymentStatus { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    
        public void AddItem(int productId, int quantity, Money unitPrice)
        {
            var item = new OrderItem(productId, quantity, unitPrice);
            _items.Add(item);
        }

        public void UpdateStatus(OrderStatus newStatus)
        {
            if (Status == OrderStatus.Delivered && newStatus == OrderStatus.Cancelled)
                throw new Exception("Cannot cancel a delivered order.");

            Status = newStatus;                                          
        }                                                                
                                                                        
        public void UpdatePaymentStatus(PaymentStatus newPaymentStatus)     
        {
            PaymentStatus = newPaymentStatus;                                    
        }
    }                                                                    
}
                                                                    