namespace StreamlinedOrderProcessing.Models
{
    public class Customer(string fullName, string email)
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = fullName;
        public string? Email { get; set; } = email;
        public string? Address { get; set; }
        public string? Phone { get; set; }

        // Navigation Property
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
