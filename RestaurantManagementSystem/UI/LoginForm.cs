using System;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.DataAccess;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public class LoginForm : Form
    {
        private readonly TextBox _rfidTextBox;
        private readonly Label _statusLabel;

        public LoginForm()
        {
            Text = "Restaurant Login";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(440, 360);
            MinimumSize = new Size(440, 360);
            Font = UITheme.RegularFont();
            BackColor = UITheme.Background;

            Panel card = new Panel { Left = 54, Top = 38, Width = 332, Height = 274, BackColor = UITheme.Panel };

            Label icon = new Label { Text = "🍽️", Font = new Font("Segoe UI Emoji", 28F), TextAlign = ContentAlignment.MiddleCenter, Left = 0, Top = 18, Width = 332, Height = 48 };
            Label title = new Label { Text = "Restaurant System", Font = UITheme.HeaderFont(18F), TextAlign = ContentAlignment.MiddleCenter, Left = 0, Top = 70, Width = 332, Height = 32 };
            Label label = new Label { Text = "RFID Code", AutoSize = true, Left = 32, Top = 124, ForeColor = UITheme.MutedText };
            _rfidTextBox = new TextBox { Left = 32, Top = 150, Width = 268, Height = 28, Font = UITheme.RegularFont() };
            Button loginButton = UITheme.CreateButton("Login", true);
            loginButton.SetBounds(32, 192, 268, 44);
            _statusLabel = new Label { Left = 32, Top = 246, Width = 268, Height = 42, ForeColor = UITheme.Accent };

            loginButton.Click += LoginButtonClick;
            _rfidTextBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                    LoginButtonClick(sender, EventArgs.Empty);
            };

            card.Controls.AddRange(new Control[] { icon, title, label, _rfidTextBox, loginButton, _statusLabel });
            Controls.Add(card);
            UITheme.ApplyTheme(this);
            title.Font = UITheme.HeaderFont(18F);
            icon.Font = new Font("Segoe UI Emoji", 28F);
            UITheme.StyleButton(loginButton, true);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            string error;
            if (!Database.EnsureCreated(out error))
            {
                _statusLabel.Text = "Database setup failed.";
                MessageBox.Show(error, "Database setup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _statusLabel.Text = "Ready. Admin: ADMIN001 / Waiter: WAITER001";
                _statusLabel.ForeColor = UITheme.Success;
            }
            _rfidTextBox.Focus();
        }

        private void LoginButtonClick(object sender, EventArgs e)
        {
            string rfid = _rfidTextBox.Text.Trim();
            if (rfid.Length == 0)
            {
                _statusLabel.Text = "Enter or scan RFID code.";
                return;
            }

            try
            {
                User user = new UserRepository().GetByRFID(rfid);
                if (user == null)
                {
                    _statusLabel.Text = "Invalid RFID code.";
                    return;
                }

                Hide();
                using (MainMenuForm mainMenu = new MainMenuForm(user))
                    mainMenu.ShowDialog(this);
                _rfidTextBox.Clear();
                Show();
                _rfidTextBox.Focus();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Login failed: " + ex.Message;
            }
        }
    }
}
