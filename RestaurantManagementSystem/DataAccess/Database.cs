using System;
using System.Configuration;
using System.Data.SqlClient;

namespace RestaurantManagementSystem.DataAccess
{
    public static class Database
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["RestaurantDb"].ConnectionString;

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool EnsureCreated(out string error)
        {
            try
            {
                if (RequiredTablesExist())
                {
                    error = null;
                    return true;
                }

                CreateDatabaseIfMissing();
                CreateTablesIfMissing();
                SeedDemoDataIfEmpty();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (RequiredTablesExist())
                    {
                        error = null;
                        return true;
                    }
                }
                catch
                {
                    // Return the original setup error below; it is more useful to the user.
                }

                error = BuildFriendlyError(ex);
                return false;
            }
        }

        public static bool TestConnection(out string error)
        {
            try
            {
                using (SqlConnection connection = CreateConnection())
                {
                    connection.Open();
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = BuildFriendlyError(ex);
                return false;
            }
        }

        private static void CreateDatabaseIfMissing()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString);
            string databaseName = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            using (SqlCommand command = new SqlCommand("IF DB_ID(@DatabaseName) IS NULL SELECT 0 ELSE SELECT 1", connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", databaseName);
                connection.Open();
                bool exists = Convert.ToInt32(command.ExecuteScalar()) == 1;
                if (exists)
                    return;
            }

            string safeDatabaseName = databaseName.Replace("]", "]]");
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            using (SqlCommand command = new SqlCommand("CREATE DATABASE [" + safeDatabaseName + "]", connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static bool RequiredTablesExist()
        {
            const string sql = @"
SELECT COUNT(*)
FROM sys.tables
WHERE name IN ('Users', 'Products', 'Categories', 'Tables', 'Halls', 'Orders', 'OrderItems', 'Payments')";

            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) == 8;
            }
        }

        private static void CreateTablesIfMissing()
        {
            string sql = @"
IF OBJECT_ID('Users', 'U') IS NULL
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    RFIDCode NVARCHAR(100) NOT NULL UNIQUE
);

IF OBJECT_ID('Categories', 'U') IS NULL
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

IF OBJECT_ID('Halls', 'U') IS NULL
CREATE TABLE Halls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

IF OBJECT_ID('[Tables]', 'U') IS NULL
CREATE TABLE [Tables] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    HallId INT NOT NULL,
    CONSTRAINT FK_Tables_Halls FOREIGN KEY (HallId) REFERENCES Halls(Id)
);

IF OBJECT_ID('Products', 'U') IS NULL
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    VAT DECIMAL(5,2) NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

IF OBJECT_ID('Orders', 'U') IS NULL
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TableId INT NOT NULL,
    WaiterId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Orders_Tables FOREIGN KEY (TableId) REFERENCES [Tables](Id),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (WaiterId) REFERENCES Users(Id)
);

IF OBJECT_ID('OrderItems', 'U') IS NULL
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    Comment NVARCHAR(250) NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

IF OBJECT_ID('Payments', 'U') IS NULL
CREATE TABLE Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_RFIDCode')
CREATE INDEX IX_Users_RFIDCode ON Users(RFIDCode);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_Status_CreatedAt')
CREATE INDEX IX_Orders_Status_CreatedAt ON Orders(Status, CreatedAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId')
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_CategoryId')
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);";

            ExecuteNonQuery(sql);
        }

        private static void SeedDemoDataIfEmpty()
        {
            string sql = @"
IF NOT EXISTS (SELECT 1 FROM Users)
BEGIN
    INSERT INTO Users (Name, Role, RFIDCode) VALUES
    ('Administrator', 'Admin', 'ADMIN001'),
    ('Waiter One', 'Waiter', 'WAITER001');
END

IF NOT EXISTS (SELECT 1 FROM Categories)
BEGIN
    INSERT INTO Categories (Name) VALUES ('Food'), ('Drinks');
END

IF NOT EXISTS (SELECT 1 FROM Halls)
BEGIN
    INSERT INTO Halls (Name) VALUES ('Main Hall');
END

IF NOT EXISTS (SELECT 1 FROM [Tables])
BEGIN
    INSERT INTO [Tables] (Name, HallId) VALUES ('Table 1', 1), ('Table 2', 1), ('Table 3', 1);
END

IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    INSERT INTO Products (Name, Price, VAT, CategoryId) VALUES
    ('Burger', 4.50, 18, 1),
    ('Pizza', 6.00, 18, 1),
    ('Water', 1.00, 8, 2),
    ('Coffee', 1.20, 8, 2);
END";

            ExecuteNonQuery(sql);
        }

        private static void ExecuteNonQuery(string sql)
        {
            using (SqlConnection connection = CreateConnection())
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 30;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static string BuildFriendlyError(Exception ex)
        {
            return ex.Message +
                   Environment.NewLine + Environment.NewLine +
                   "Kontrollo connection string ne App.config. Per kete PC perdor LocalDB; per LAN vendos emrin/IP e SQL Server-it kryesor.";
        }
    }
}
