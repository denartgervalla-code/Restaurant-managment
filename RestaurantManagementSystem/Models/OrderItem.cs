namespace RestaurantManagementSystem.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Comment { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VAT { get; set; }

        public decimal LineTotal
        {
            get { return Quantity * (UnitPrice + (UnitPrice * VAT / 100m)); }
        }
    }
}
