namespace RestaurantManagementSystem.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal VAT { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }

        public decimal PriceWithVat
        {
            get { return Price + (Price * VAT / 100m); }
        }
    }
}
