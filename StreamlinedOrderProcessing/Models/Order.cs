namespace StreamlinedOrderProcessing.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal PriceTotal { get; set; }
        public string Status { get; set; } = "Processing"; // Default status
        public string PaymentStatus { get; set; } = "Pending"; // e.g., Cash or Credit Card

        // Foreign Keys
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; } = null!;

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;

        public int? PickupPointId { get; set; }
        public virtual PickupPoint? PickupPoint { get; set; }

        // Navigation Property for items
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
