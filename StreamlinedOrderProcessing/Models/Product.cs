namespace StreamlinedOrderProcessing.Models
{
    public class Product(string title, decimal price)
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = title;
        public string? Description { get; set; }
        public decimal Price { get; set; } = price;
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
