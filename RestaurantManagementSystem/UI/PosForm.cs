using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public class PosForm : Form
    {
        private readonly User _waiter;
        private readonly OrderRepository _repository = new OrderRepository();
        private readonly ComboBox _tableCombo = new ComboBox();
        private readonly ListBox _productsList = new ListBox();
        private readonly FlowLayoutPanel _productsPanel = new FlowLayoutPanel();
        private readonly DataGridView _cartGrid = new DataGridView();
        private readonly Label _totalLabel = new Label();
        private readonly List<OrderItem> _cart = new List<OrderItem>();
        private int _lastOrderId;

        public PosForm(User waiter)
        {
            _waiter = waiter;
            Text = "POS - " + waiter.Name;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 620);
            ClientSize = new Size(1100, 680);
            Font = UITheme.RegularFont();
            BackColor = UITheme.Background;

            Panel header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = UITheme.Background };
            Label title = new Label { Text = "🍽️ Waiter POS", Left = 22, Top = 17, Width = 260, Height = 34, Font = UITheme.HeaderFont(18F), ForeColor = UITheme.Text };
            Label tableLabel = new Label { Text = "Table", Left = 720, Top = 24, AutoSize = true, ForeColor = UITheme.MutedText };
            _tableCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _tableCombo.SetBounds(770, 20, 250, 30);
            header.Controls.AddRange(new Control[] { title, tableLabel, _tableCombo });

            _productsList.Visible = false;
            _productsList.DisplayMember = "Name";
            _productsList.DoubleClick += delegate { AddSelectedProduct(); };

            Panel menuPanel = new Panel { Left = 22, Top = 88, Width = 560, Height = 520, BackColor = UITheme.Panel };
            Label menuTitle = new Label { Text = "🍔 Menu Items", Left = 18, Top = 16, Width = 220, Height = 28, Font = UITheme.HeaderFont(13F), ForeColor = UITheme.Text };
            _productsPanel.SetBounds(18, 58, 524, 440);
            _productsPanel.AutoScroll = true;
            _productsPanel.BackColor = UITheme.Panel;
            menuPanel.Controls.AddRange(new Control[] { menuTitle, _productsPanel, _productsList });

            Panel orderPanel = new Panel { Left = 604, Top = 88, Width = 464, Height = 520, BackColor = UITheme.Panel };
            Label orderTitle = new Label { Text = "🧾 Current Order", Left = 18, Top = 16, Width = 230, Height = 28, Font = UITheme.HeaderFont(13F), ForeColor = UITheme.Text };
            Button plusButton = UITheme.CreateButton("+", false);
            Button minusButton = UITheme.CreateButton("-", false);
            Button commentButton = UITheme.CreateButton("Comment", false);
            Button removeButton = UITheme.CreateButton("Remove", false);
            Button clearButton = UITheme.CreateButton("Clear", false);
            Button sendButton = UITheme.CreateButton("Send to Kitchen", true);
            Button closeButton = UITheme.CreateButton("Close / Pay", false);
            Button printButton = UITheme.CreateButton("Print", false);

            _cartGrid.SetBounds(18, 58, 428, 318);
            _cartGrid.ReadOnly = true;
            _cartGrid.AllowUserToAddRows = false;
            _cartGrid.AllowUserToDeleteRows = false;
            _cartGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _cartGrid.MultiSelect = false;
            _cartGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _totalLabel.SetBounds(248, 390, 198, 38);
            _totalLabel.Font = UITheme.HeaderFont(16F);
            _totalLabel.TextAlign = ContentAlignment.MiddleRight;
            _totalLabel.ForeColor = UITheme.Accent;

            plusButton.SetBounds(18, 388, 50, 42);
            minusButton.SetBounds(74, 388, 50, 42);
            commentButton.SetBounds(130, 388, 96, 42);
            removeButton.SetBounds(18, 444, 88, 42);
            clearButton.SetBounds(112, 444, 78, 42);
            sendButton.SetBounds(196, 444, 132, 42);
            closeButton.SetBounds(334, 444, 112, 42);
            printButton.SetBounds(18, 494, 88, 42);

            plusButton.Click += delegate { ChangeQuantity(1); };
            minusButton.Click += delegate { ChangeQuantity(-1); };
            commentButton.Click += delegate { AddComment(); };
            removeButton.Click += delegate { RemoveSelected(); };
            clearButton.Click += delegate { _cart.Clear(); BindCart(); };
            sendButton.Click += delegate { SendOrder(); };
            closeButton.Click += delegate { CloseAndPay(); };
            printButton.Click += delegate { PrintLastReceipt(); };

            orderPanel.Controls.AddRange(new Control[] { orderTitle, _cartGrid, _totalLabel, plusButton, minusButton, commentButton, removeButton, clearButton, sendButton, closeButton, printButton });
            Controls.AddRange(new Control[] { header, menuPanel, orderPanel });
            UITheme.ApplyTheme(this);
            title.Font = UITheme.HeaderFont(18F);
            menuTitle.Font = UITheme.HeaderFont(13F);
            orderTitle.Font = UITheme.HeaderFont(13F);
            _totalLabel.Font = UITheme.HeaderFont(16F);
            _totalLabel.ForeColor = UITheme.Accent;
            UITheme.StyleButton(sendButton, true);
            LoadData();
            BindCart();
        }

        private void LoadData()
        {
            try
            {
                _tableCombo.DataSource = _repository.GetTables();
                _tableCombo.DisplayMember = "Name";
                _tableCombo.ValueMember = "Id";
                ProductCache.Refresh();
                _productsList.DataSource = ProductCache.Products;
                BuildProductButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load POS data: " + ex.Message);
            }
        }

        private void BuildProductButtons()
        {
            _productsPanel.Controls.Clear();
            foreach (Product product in ProductCache.Products)
            {
                Button button = UITheme.CreateButton("🍔 " + product.Name + Environment.NewLine + product.Price.ToString("0.00"), false);
                button.Width = 160;
                button.Height = 76;
                button.Margin = new Padding(0, 0, 14, 14);
                button.Tag = product;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.Click += delegate(object sender, EventArgs e)
                {
                    AddProduct((Product)((Button)sender).Tag);
                };
                _productsPanel.Controls.Add(button);
            }
        }

        private void AddProduct(Product product)
        {
            if (product == null)
                return;

            OrderItem existing = _cart.FirstOrDefault(i => i.ProductId == product.Id && string.IsNullOrEmpty(i.Comment));
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    VAT = product.VAT,
                    Comment = string.Empty
                });
            }
            BindCart();
        }

        private void AddSelectedProduct()
        {
            Product product = _productsList.SelectedItem as Product;
            AddProduct(product);
        }

        private OrderItem SelectedItem()
        {
            if (_cartGrid.CurrentRow == null)
                return null;
            int productId = Convert.ToInt32(_cartGrid.CurrentRow.Cells["ProductId"].Value);
            string comment = Convert.ToString(_cartGrid.CurrentRow.Cells["Comment"].Value);
            return _cart.FirstOrDefault(i => i.ProductId == productId && i.Comment == comment);
        }

        private void ChangeQuantity(int delta)
        {
            OrderItem item = SelectedItem();
            if (item == null)
                return;
            item.Quantity += delta;
            if (item.Quantity <= 0)
                _cart.Remove(item);
            BindCart();
        }

        private void AddComment()
        {
            OrderItem item = SelectedItem();
            if (item == null)
                return;
            string comment = PromptForm.Ask("Item Comment", "Comment", item.Comment);
            if (comment != null)
            {
                item.Comment = comment;
                BindCart();
            }
        }

        private void RemoveSelected()
        {
            OrderItem item = SelectedItem();
            if (item == null)
                return;
            _cart.Remove(item);
            BindCart();
        }

        private void SendOrder()
        {
            if (_tableCombo.SelectedItem == null || _cart.Count == 0)
            {
                MessageBox.Show("Select table and add products first.");
                return;
            }

            try
            {
                RestaurantTable table = (RestaurantTable)_tableCombo.SelectedItem;
                _lastOrderId = _repository.CreateOrder(table.Id, _waiter.Id, _cart);
                MessageBox.Show("Order sent to kitchen.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not send order: " + ex.Message);
            }
        }

        private void CloseAndPay()
        {
            if (_lastOrderId == 0)
            {
                MessageBox.Show("Send the order to kitchen before closing payment.");
                return;
            }

            string method = PromptForm.Ask("Payment", "Payment method", "Cash");
            if (string.IsNullOrWhiteSpace(method))
                return;

            try
            {
                decimal total = CalculateTotal();
                _repository.CloseOrder(_lastOrderId, total, method);
                PrintLastReceipt();
                _cart.Clear();
                _lastOrderId = 0;
                BindCart();
                MessageBox.Show("Order closed and payment saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Payment failed: " + ex.Message);
            }
        }

        private void PrintLastReceipt()
        {
            if (_lastOrderId == 0 || _cart.Count == 0)
                return;

            try
            {
                RestaurantTable table = (RestaurantTable)_tableCombo.SelectedItem;
                new ReceiptPrinter(_lastOrderId, table.Name, new List<OrderItem>(_cart), CalculateTotal()).Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print failed: " + ex.Message);
            }
        }

        private void BindCart()
        {
            _cartGrid.DataSource = null;
            _cartGrid.DataSource = _cart.Select(i => new
            {
                i.ProductId,
                Product = i.ProductName,
                i.Quantity,
                Price = i.UnitPrice.ToString("0.00"),
                VAT = i.VAT.ToString("0.##") + "%",
                i.Comment,
                Total = i.LineTotal.ToString("0.00")
            }).ToList();
            if (_cartGrid.Columns.Contains("ProductId"))
                _cartGrid.Columns["ProductId"].Visible = false;
            _totalLabel.Text = "Total: " + CalculateTotal().ToString("0.00");
        }

        private decimal CalculateTotal()
        {
            return _cart.Sum(i => i.LineTotal);
        }
    }
}
