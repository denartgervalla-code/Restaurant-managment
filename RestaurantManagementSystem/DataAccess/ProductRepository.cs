using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.DataAccess
{
    public class ProductRepository
    {
        public List<Product> GetAll()
        {
            const string sql = @"
SELECT p.Id, p.Name, p.Price, p.VAT, p.CategoryId, ISNULL(c.Name, '') AS CategoryName
FROM Products p
LEFT JOIN Categories c ON c.Id = p.CategoryId
ORDER BY c.Name, p.Name";

            List<Product> products = new List<Product>();
            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = Convert.ToString(reader["Name"]),
                            Price = Convert.ToDecimal(reader["Price"]),
                            VAT = Convert.ToDecimal(reader["VAT"]),
                            CategoryId = Convert.ToInt32(reader["CategoryId"]),
                            CategoryName = Convert.ToString(reader["CategoryName"])
                        });
                    }
                }
            }
            return products;
        }
    }
}
