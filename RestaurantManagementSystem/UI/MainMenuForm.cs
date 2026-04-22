using System;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.UI
{
    public class MainMenuForm : Form
    {
        private readonly User _user;

        public MainMenuForm(User user)
        {
            _user = user;
            Text = "Restaurant Management - " + user.Name;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 340);
            MinimumSize = new Size(520, 340);
            Font = UITheme.RegularFont();
            BackColor = UITheme.Background;

            Panel card = new Panel { Left = 32, Top = 32, Width = 456, Height = 270, BackColor = UITheme.Panel };

            Label title = new Label
            {
                Text = "🍽️ Welcome, " + user.Name,
                Font = UITheme.HeaderFont(17F),
                Width = 408,
                Height = 34,
                Left = 24,
                Top = 24,
                ForeColor = UITheme.Text
            };
            Label role = new Label { Text = user.Role, Left = 26, Top = 60, Width = 160, Height = 24, ForeColor = UITheme.MutedText };

            Button posButton = MakeButton("🧾 POS", 24, 104);
            Button kitchenButton = MakeButton("👨‍🍳 Kitchen", 164, 104);
            Button adminButton = MakeButton("⚙ Admin", 304, 104);
            Button logoutButton = MakeButton("Logout", 304, 184);

            posButton.Click += delegate { using (PosForm form = new PosForm(_user)) form.ShowDialog(this); };
            kitchenButton.Click += delegate { using (KitchenForm form = new KitchenForm()) form.ShowDialog(this); };
            adminButton.Click += delegate { using (AdminForm form = new AdminForm()) form.ShowDialog(this); };
            logoutButton.Click += delegate { Close(); };

            bool isAdmin = string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            adminButton.Enabled = isAdmin;
            kitchenButton.Enabled = isAdmin;

            card.Controls.AddRange(new Control[] { title, role, posButton, kitchenButton, adminButton, logoutButton });
            Controls.Add(card);
            UITheme.ApplyTheme(this);
            title.Font = UITheme.HeaderFont(17F);
        }

        private static Button MakeButton(string text, int left, int top)
        {
            Button button = UITheme.CreateButton(text, false);
            button.SetBounds(left, top, 116, 56);
            return button;
        }
    }
}
