using System;
using System.Collections.Generic;

namespace RestaurantManagementSystem.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public string TableName { get; set; }
        public int WaiterId { get; set; }
        public string WaiterName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItem> Items { get; set; }

        public Order()
        {
            Items = new List<OrderItem>();
        }
    }
}
