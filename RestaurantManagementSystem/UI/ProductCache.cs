using System.Collections.Generic;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public static class ProductCache
    {
        private static List<Product> _products;

        public static List<Product> Products
        {
            get
            {
                if (_products == null)
                    Refresh();
                return _products;
            }
        }

        public static void Refresh()
        {
            _products = new ProductRepository().GetAll();
        }
    }
}
