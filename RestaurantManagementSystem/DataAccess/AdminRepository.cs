using System.Data;
using System.Data.SqlClient;

namespace RestaurantManagementSystem.DataAccess
{
    public class AdminRepository
    {
        public DataTable GetTable(string tableName)
        {
            string sql = "SELECT * FROM " + SafeTable(tableName) + " ORDER BY Id";
            DataTable table = new DataTable();

            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                adapter.Fill(table);
            }

            return table;
        }

        public void SaveCategory(int id, string name)
        {
            ExecuteUpsert(id, "Categories", "Name", name);
        }

        public void SaveHall(int id, string name)
        {
            ExecuteUpsert(id, "Halls", "Name", name);
        }

        public void SaveTable(int id, string name, int hallId)
        {
            const string insertSql = "INSERT INTO [Tables] (Name, HallId) VALUES (@Name, @HallId)";
            const string updateSql = "UPDATE [Tables] SET Name=@Name, HallId=@HallId WHERE Id=@Id";
            ExecuteNonQuery(id == 0 ? insertSql : updateSql,
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name),
                new SqlParameter("@HallId", hallId));
        }

        public void SaveUser(int id, string name, string role, string rfidCode)
        {
            const string insertSql = "INSERT INTO Users (Name, Role, RFIDCode) VALUES (@Name, @Role, @RFIDCode)";
            const string updateSql = "UPDATE Users SET Name=@Name, Role=@Role, RFIDCode=@RFIDCode WHERE Id=@Id";
            ExecuteNonQuery(id == 0 ? insertSql : updateSql,
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name),
                new SqlParameter("@Role", role),
                new SqlParameter("@RFIDCode", rfidCode));
        }

        public void SaveProduct(int id, string name, decimal price, decimal vat, int categoryId)
        {
            const string insertSql = "INSERT INTO Products (Name, Price, VAT, CategoryId) VALUES (@Name, @Price, @VAT, @CategoryId)";
            const string updateSql = "UPDATE Products SET Name=@Name, Price=@Price, VAT=@VAT, CategoryId=@CategoryId WHERE Id=@Id";
            ExecuteNonQuery(id == 0 ? insertSql : updateSql,
                new SqlParameter("@Id", id),
                new SqlParameter("@Name", name),
                new SqlParameter("@Price", price),
                new SqlParameter("@VAT", vat),
                new SqlParameter("@CategoryId", categoryId));
        }

        public void Delete(string tableName, int id)
        {
            string sql = "DELETE FROM " + SafeTable(tableName) + " WHERE Id=@Id";
            ExecuteNonQuery(sql, new SqlParameter("@Id", id));
        }

        private static void ExecuteUpsert(int id, string tableName, string columnName, string value)
        {
            string table = SafeTable(tableName);
            string sql = id == 0
                ? "INSERT INTO " + table + " (" + columnName + ") VALUES (@Value)"
                : "UPDATE " + table + " SET " + columnName + "=@Value WHERE Id=@Id";
            ExecuteNonQuery(sql, new SqlParameter("@Id", id), new SqlParameter("@Value", value));
        }

        private static void ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = Database.CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static string SafeTable(string tableName)
        {
            switch (tableName)
            {
                case "Users": return "Users";
                case "Products": return "Products";
                case "Categories": return "Categories";
                case "Tables": return "[Tables]";
                case "Halls": return "Halls";
                default: throw new System.ArgumentException("Invalid table name.");
            }
        }
    }
}
