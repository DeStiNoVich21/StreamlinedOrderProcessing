using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamlinedOrderProcessing.Models
{
    [Table("Employee")]
    public class Employee
    {
        [Key, Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = null!;

        [Column("job_title")]
        public string? JobTitle { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }
    }
}
