namespace RestaurantManagementSystem.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
    }
}
