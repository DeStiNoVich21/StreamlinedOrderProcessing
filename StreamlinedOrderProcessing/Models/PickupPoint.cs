namespace StreamlinedOrderProcessing.Models
{
    public class PickupPoint(string address)
    {
        public int PickupPointId { get; set; }
        public string Address { get; set; } = address;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
