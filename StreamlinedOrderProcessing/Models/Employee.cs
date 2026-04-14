namespace StreamlinedOrderProcessing.Models
{
    public class Employee(string fullName, string position)
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = fullName;
        public string Position { get; set; } = position;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
