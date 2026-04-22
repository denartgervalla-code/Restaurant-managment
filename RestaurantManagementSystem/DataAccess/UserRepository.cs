using System;
using System.Data.SqlClient;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.DataAccess
{
    public class UserRepository
    {
        public User GetByRFID(string rfidCode)
        {
            const string sql = "SELECT Id, Name, Role, RFIDCode FROM Users WHERE RFIDCode = @RFIDCode";

            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RFIDCode", rfidCode);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return new User
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = Convert.ToString(reader["Name"]),
                        Role = Convert.ToString(reader["Role"]),
                        RFIDCode = Convert.ToString(reader["RFIDCode"])
                    };
                }
            }
        }
    }
}
