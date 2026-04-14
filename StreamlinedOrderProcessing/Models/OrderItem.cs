namespace StreamlinedOrderProcessing.Models
{
    public class OrderItem
    {
        // Composite Key (Configured in DbContext)
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}
