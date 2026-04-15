using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{
    [Table("Order_Items")]
    public class OrderItem
    {
        [Column("order_id")]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        [Column("product_id")]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("price_at_purchase")] // Соответствие DDL
        public decimal PriceAtPurchase { get; set; }
    }
}
