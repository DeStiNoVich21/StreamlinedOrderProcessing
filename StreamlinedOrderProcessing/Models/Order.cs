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

        // Свойство навигации (FK связь)
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

        [Column("status")]
        public string Status { get; set; } = "New";

        // Связь с позициями заказа
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
