using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public class ReceiptPrinter
    {
        private readonly int _orderId;
        private readonly string _tableName;
        private readonly List<OrderItem> _items;
        private readonly decimal _total;

        public ReceiptPrinter(int orderId, string tableName, List<OrderItem> items, decimal total)
        {
            _orderId = orderId;
            _tableName = tableName;
            _items = items;
            _total = total;
        }

        public void Print()
        {
            using (PrintDocument document = new PrintDocument())
            {
                document.PrintPage += PrintPage;
                document.Print();
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 15;
            Font titleFont = new Font("Arial", 12, FontStyle.Bold);
            Font font = new Font("Arial", 9);
            Font boldFont = new Font("Arial", 9, FontStyle.Bold);

            e.Graphics.DrawString(SettingsRepository.RestaurantName, titleFont, Brushes.Black, 10, y);
            y += 24;
            e.Graphics.DrawString("Order #" + _orderId + "  Table: " + _tableName, font, Brushes.Black, 10, y);
            y += 18;
            e.Graphics.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), font, Brushes.Black, 10, y);
            y += 24;

            e.Graphics.DrawString("Item", boldFont, Brushes.Black, 10, y);
            e.Graphics.DrawString("Qty", boldFont, Brushes.Black, 150, y);
            e.Graphics.DrawString("VAT", boldFont, Brushes.Black, 190, y);
            e.Graphics.DrawString("Total", boldFont, Brushes.Black, 235, y);
            y += 18;

            foreach (OrderItem item in _items)
            {
                e.Graphics.DrawString(Trim(item.ProductName, 22), font, Brushes.Black, 10, y);
                e.Graphics.DrawString(item.Quantity.ToString(), font, Brushes.Black, 150, y);
                e.Graphics.DrawString(item.VAT.ToString("0.##") + "%", font, Brushes.Black, 190, y);
                e.Graphics.DrawString(item.LineTotal.ToString("0.00"), font, Brushes.Black, 235, y);
                y += 18;
            }

            y += 8;
            e.Graphics.DrawString("Total: " + _total.ToString("0.00"), titleFont, Brushes.Black, 10, y);
        }

        private static string Trim(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text;
            return text.Substring(0, max - 3) + "...";
        }
    }
}
