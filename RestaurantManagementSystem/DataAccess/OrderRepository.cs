using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.DataAccess
{
    public class OrderRepository
    {
        public List<RestaurantTable> GetTables()
        {
            const string sql = @"
SELECT t.Id, t.Name, t.HallId, ISNULL(h.Name, '') AS HallName
FROM [Tables] t
LEFT JOIN Halls h ON h.Id = t.HallId
ORDER BY h.Name, t.Name";
            List<RestaurantTable> tables = new List<RestaurantTable>();

            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(new RestaurantTable
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = Convert.ToString(reader["Name"]),
                            HallId = Convert.ToInt32(reader["HallId"]),
                            HallName = Convert.ToString(reader["HallName"])
                        });
                    }
                }
            }
            return tables;
        }

        public int CreateOrder(int tableId, int waiterId, List<OrderItem> items)
        {
            const string orderSql = @"
INSERT INTO Orders (TableId, WaiterId, Status, CreatedAt)
VALUES (@TableId, @WaiterId, 'Pending', GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS int);";
            const string itemSql = @"
INSERT INTO OrderItems (OrderId, ProductId, Quantity, Comment)
VALUES (@OrderId, @ProductId, @Quantity, @Comment)";

            using (SqlConnection connection = Database.CreateConnection())
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int orderId;
                        using (SqlCommand command = new SqlCommand(orderSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@TableId", tableId);
                            command.Parameters.AddWithValue("@WaiterId", waiterId);
                            orderId = (int)command.ExecuteScalar();
                        }

                        foreach (OrderItem item in items)
                        {
                            using (SqlCommand command = new SqlCommand(itemSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                command.Parameters.AddWithValue("@Comment", (object)item.Comment ?? DBNull.Value);
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return orderId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<Order> GetActiveOrders()
        {
            const string sql = @"
SELECT o.Id, o.TableId, t.Name AS TableName, o.WaiterId, u.Name AS WaiterName, o.Status, o.CreatedAt
FROM Orders o
INNER JOIN [Tables] t ON t.Id = o.TableId
INNER JOIN Users u ON u.Id = o.WaiterId
WHERE o.Status IN ('Pending', 'Preparing', 'Ready')
ORDER BY o.CreatedAt DESC";
            List<Order> orders = new List<Order>();

            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new Order
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            TableId = Convert.ToInt32(reader["TableId"]),
                            TableName = Convert.ToString(reader["TableName"]),
                            WaiterId = Convert.ToInt32(reader["WaiterId"]),
                            WaiterName = Convert.ToString(reader["WaiterName"]),
                            Status = Convert.ToString(reader["Status"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        });
                    }
                }
            }

            foreach (Order order in orders)
                order.Items = GetOrderItems(order.Id);

            return orders;
        }

        public List<OrderItem> GetOrderItems(int orderId)
        {
            const string sql = @"
SELECT oi.Id, oi.OrderId, oi.ProductId, p.Name AS ProductName, oi.Quantity, ISNULL(oi.Comment, '') AS Comment,
       p.Price AS UnitPrice, p.VAT
FROM OrderItems oi
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = @OrderId
ORDER BY oi.Id";
            List<OrderItem> items = new List<OrderItem>();

            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@OrderId", orderId);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new OrderItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            OrderId = Convert.ToInt32(reader["OrderId"]),
                            ProductId = Convert.ToInt32(reader["ProductId"]),
                            ProductName = Convert.ToString(reader["ProductName"]),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Comment = Convert.ToString(reader["Comment"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            VAT = Convert.ToDecimal(reader["VAT"])
                        });
                    }
                }
            }
            return items;
        }

        public void UpdateStatus(int orderId, string status)
        {
            const string sql = "UPDATE Orders SET Status=@Status WHERE Id=@OrderId";
            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@OrderId", orderId);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void CloseOrder(int orderId, decimal total, string paymentMethod)
        {
            const string paymentSql = "INSERT INTO Payments (OrderId, Total, PaymentMethod) VALUES (@OrderId, @Total, @PaymentMethod)";
            const string orderSql = "UPDATE Orders SET Status='Closed' WHERE Id=@OrderId";

            using (SqlConnection connection = Database.CreateConnection())
            {
                connection.Open();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand command = new SqlCommand(paymentSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@OrderId", orderId);
                            command.Parameters.AddWithValue("@Total", total);
                            command.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                            command.ExecuteNonQuery();
                        }
                        using (SqlCommand command = new SqlCommand(orderSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@OrderId", orderId);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
