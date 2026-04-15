using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{
    [Table("Customer")]
    public class Customer
    {
        [Key, Column("customer_id")] // Явно указываем ID
        public int CustomerId { get; set; }

        [Column("full_name")] // В базе full_name
        public string FullName { get; set; } = null!;

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("address")]
        public string? Address { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
