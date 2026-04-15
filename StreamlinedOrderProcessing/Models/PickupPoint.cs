using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{

    [Table("Pickup_Point")]
    public class PickupPoint
    {
        [Key, Column("point_id")]
        public int PointId { get; set; }

        [Column("address")]
        public string Address { get; set; } = null!;

        [Column("manager_name")] // Добавлено согласно DDL
        public string? ManagerName { get; set; }

        [Column("opening_hours")] // Добавлено согласно DDL
        public string? OpeningHours { get; set; }
    }
}
