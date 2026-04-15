using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{
    [Table("Order")]
    public class Order
    {
        [Key, Column("order_id")]
        public int OrderId { get; set; }

        [Column("customer_id")]
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [Column("pickup_point_id")]
        public int PickupPointId { get; set; }
        [ForeignKey("PickupPointId")]
        public virtual PickupPoint PickupPoint { get; set; } = null!;

        [Column("employee_id")]
        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column("status")]
        public string Status { get; set; } = "Processing";

        [Column("total_amount")] // Соответствие DDL
        public decimal TotalAmount { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
