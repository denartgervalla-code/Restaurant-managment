CREATE DATABASE RestaurantManagement;
GO

USE RestaurantManagement;
GO

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    RFIDCode NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Halls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE [Tables] (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    HallId INT NOT NULL,
    CONSTRAINT FK_Tables_Halls FOREIGN KEY (HallId) REFERENCES Halls(Id)
);

CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(120) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    VAT DECIMAL(5,2) NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TableId INT NOT NULL,
    WaiterId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    CONSTRAINT FK_Orders_Tables FOREIGN KEY (TableId) REFERENCES [Tables](Id),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (WaiterId) REFERENCES Users(Id)
);

CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    Comment NVARCHAR(250) NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE INDEX IX_Users_RFIDCode ON Users(RFIDCode);
CREATE INDEX IX_Orders_Status_CreatedAt ON Orders(Status, CreatedAt);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
GO

INSERT INTO Users (Name, Role, RFIDCode) VALUES
('Administrator', 'Admin', 'ADMIN001'),
('Waiter One', 'Waiter', 'WAITER001');

INSERT INTO Categories (Name) VALUES ('Food'), ('Drinks');
INSERT INTO Halls (Name) VALUES ('Main Hall');
INSERT INTO [Tables] (Name, HallId) VALUES ('Table 1', 1), ('Table 2', 1), ('Table 3', 1);
INSERT INTO Products (Name, Price, VAT, CategoryId) VALUES
('Burger', 4.50, 18, 1),
('Pizza', 6.00, 18, 1),
('Water', 1.00, 8, 2),
('Coffee', 1.20, 8, 2);
GO
