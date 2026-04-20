using Mart.Domain.Common;
using Mart.Domain.ValueObjects;

namespace Mart.Domain.Entities
{
    public class OrderItem : BaseEntity
    {

        private OrderItem() { }

        public OrderItem(int productId, int quantity, Money priceAtPurchase)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity zero thi vadhare hovvi joie.");

            ProductId = productId;
            Quantity = quantity;
            PriceAtPurchase = priceAtPurchase;
        }



        public int OrderId { get; private set; } 
        public int ProductId { get; private set; }
        public int Quantity { get; private set; }


        public Money PriceAtPurchase { get; private set; }


        public decimal GetTotal() => PriceAtPurchase.Amount * Quantity;
    }
}
