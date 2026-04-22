using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public class KitchenForm : Form
    {
        private readonly OrderRepository _repository = new OrderRepository();
        private readonly Timer _timer = new Timer();
        private readonly DataGridView _ordersGrid = new DataGridView();
        private readonly TextBox _itemsText = new TextBox();

        public KitchenForm()
        {
            Text = "Kitchen Display";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 560);
            ClientSize = new Size(980, 620);
            Font = UITheme.RegularFont();
            BackColor = UITheme.Background;

            Label title = new Label { Text = "👨‍🍳 Kitchen Screen", Left = 22, Top = 18, Width = 300, Height = 34, Font = UITheme.HeaderFont(18F), ForeColor = UITheme.Text };
            Label ordersTitle = new Label { Text = "Active Orders", Left = 28, Top = 82, Width = 180, Height = 28, Font = UITheme.HeaderFont(13F), ForeColor = UITheme.Text };
            Label detailTitle = new Label { Text = "Order Items", Left = 528, Top = 82, Width = 180, Height = 28, Font = UITheme.HeaderFont(13F), ForeColor = UITheme.Text };

            _ordersGrid.SetBounds(28, 118, 470, 400);
            _ordersGrid.ReadOnly = true;
            _ordersGrid.AllowUserToAddRows = false;
            _ordersGrid.AllowUserToDeleteRows = false;
            _ordersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _ordersGrid.MultiSelect = false;
            _ordersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _ordersGrid.SelectionChanged += delegate { ShowSelectedOrderItems(); };

            _itemsText.SetBounds(528, 118, 410, 400);
            _itemsText.Multiline = true;
            _itemsText.ReadOnly = true;
            _itemsText.ScrollBars = ScrollBars.Vertical;
            _itemsText.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            Button pendingButton = UITheme.CreateButton("Pending", false);
            Button preparingButton = UITheme.CreateButton("Preparing", false);
            Button readyButton = UITheme.CreateButton("Ready", false);
            Button refreshButton = UITheme.CreateButton("Refresh", false);
            pendingButton.SetBounds(528, 536, 96, 44);
            preparingButton.SetBounds(636, 536, 110, 44);
            readyButton.SetBounds(758, 536, 86, 44);
            refreshButton.SetBounds(856, 536, 82, 44);

            pendingButton.Click += delegate { SetStatus("Pending"); };
            preparingButton.Click += delegate { SetStatus("Preparing"); };
            readyButton.Click += delegate { SetStatus("Ready"); };
            refreshButton.Click += delegate { LoadOrders(); };

            _timer.Interval = 3000;
            _timer.Tick += delegate { LoadOrders(); };

            Controls.AddRange(new Control[] { title, ordersTitle, detailTitle, _ordersGrid, _itemsText, pendingButton, preparingButton, readyButton, refreshButton });
            UITheme.ApplyTheme(this);
            title.Font = UITheme.HeaderFont(18F);
            ordersTitle.Font = UITheme.HeaderFont(13F);
            detailTitle.Font = UITheme.HeaderFont(13F);
            _itemsText.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            pendingButton.BackColor = UITheme.Accent;
            preparingButton.BackColor = UITheme.Warning;
            preparingButton.ForeColor = Color.FromArgb(35, 35, 35);
            readyButton.BackColor = UITheme.Success;
            LoadOrders();
            _timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        private void LoadOrders()
        {
            int selectedOrderId = SelectedOrderId();
            try
            {
                _ordersGrid.DataSource = null;
                _ordersGrid.DataSource = _repository.GetActiveOrders().Select(o => new
                {
                    o.Id,
                    Table = o.TableName,
                    Waiter = o.WaiterName,
                    o.Status,
                    Created = o.CreatedAt.ToString("HH:mm")
                }).ToList();

                foreach (DataGridViewRow row in _ordersGrid.Rows)
                {
                    string status = Convert.ToString(row.Cells["Status"].Value);
                    row.DefaultCellStyle.BackColor = StatusColor(status);
                    row.DefaultCellStyle.ForeColor = status == "Preparing" ? Color.FromArgb(35, 35, 35) : UITheme.Text;
                    if (Convert.ToInt32(row.Cells["Id"].Value) == selectedOrderId)
                    {
                        row.Selected = true;
                        _ordersGrid.CurrentCell = row.Cells[0];
                        break;
                    }
                }
                ShowSelectedOrderItems();
            }
            catch (Exception ex)
            {
                _timer.Stop();
                MessageBox.Show("Kitchen refresh failed: " + ex.Message);
                _timer.Start();
            }
        }

        private static Color StatusColor(string status)
        {
            if (status == "Preparing")
                return UITheme.Warning;
            if (status == "Ready")
                return UITheme.Success;
            return UITheme.Accent;
        }

        private int SelectedOrderId()
        {
            if (_ordersGrid.CurrentRow == null)
                return 0;
            return Convert.ToInt32(_ordersGrid.CurrentRow.Cells["Id"].Value);
        }

        private void ShowSelectedOrderItems()
        {
            int orderId = SelectedOrderId();
            if (orderId == 0)
            {
                _itemsText.Clear();
                return;
            }

            try
            {
                StringBuilder builder = new StringBuilder();
                foreach (OrderItem item in _repository.GetOrderItems(orderId))
                {
                    builder.AppendLine(item.Quantity + " x " + item.ProductName);
                    if (!string.IsNullOrWhiteSpace(item.Comment))
                        builder.AppendLine("  COMMENT: " + item.Comment);
                    builder.AppendLine();
                }
                _itemsText.Text = builder.ToString();
            }
            catch (Exception ex)
            {
                _itemsText.Text = "Could not load items: " + ex.Message;
            }
        }

        private void SetStatus(string status)
        {
            int orderId = SelectedOrderId();
            if (orderId == 0)
                return;

            try
            {
                _repository.UpdateStatus(orderId, status);
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update status: " + ex.Message);
            }
        }
    }
}
