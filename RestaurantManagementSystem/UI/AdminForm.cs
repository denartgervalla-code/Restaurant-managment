using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RestaurantManagementSystem.DataAccess;

namespace RestaurantManagementSystem.UI
{
    public class AdminForm : Form
    {
        private readonly AdminRepository _repository = new AdminRepository();
        private readonly ComboBox _entityCombo = new ComboBox();
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _nameText = new TextBox();
        private readonly TextBox _priceText = new TextBox();
        private readonly TextBox _vatText = new TextBox();
        private readonly TextBox _categoryIdText = new TextBox();
        private readonly TextBox _hallIdText = new TextBox();
        private readonly ComboBox _roleCombo = new ComboBox();
        private readonly TextBox _rfidText = new TextBox();
        private readonly Panel _fieldPanel = new Panel();

        public AdminForm()
        {
            Text = "Admin Panel";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 560);
            ClientSize = new Size(1040, 640);
            Font = UITheme.RegularFont();
            BackColor = UITheme.Background;

            _entityCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _entityCombo.Items.AddRange(new object[] { "Products", "Categories", "Tables", "Halls", "Users" });
            _entityCombo.SelectedIndex = 0;
            _entityCombo.Visible = false;
            _entityCombo.SelectedIndexChanged += delegate { LoadData(); UpdateFieldVisibility(); };

            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 190, BackColor = UITheme.Panel, Padding = new Padding(16) };
            Label sideTitle = new Label { Text = "🍽️ Admin", Left = 16, Top = 18, Width = 158, Height = 34, Font = UITheme.HeaderFont(15F), ForeColor = UITheme.Text };
            Button productsButton = NavButton("🍔 Products", 70, "Products");
            Button categoriesButton = NavButton("🧾 Categories", 124, "Categories");
            Button tablesButton = NavButton("🪑 Tables", 178, "Tables");
            Button hallsButton = NavButton("🏛️ Halls", 232, "Halls");
            Button usersButton = NavButton("👤 Users", 286, "Users");
            sidebar.Controls.AddRange(new Control[] { sideTitle, productsButton, categoriesButton, tablesButton, hallsButton, usersButton });

            Panel header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = UITheme.Background };
            Label title = new Label { Text = "Restaurant Admin Panel", Left = 220, Top = 18, Width = 420, Height = 34, Font = UITheme.HeaderFont(18F), ForeColor = UITheme.Text };
            Button refreshButton = UITheme.CreateButton("Refresh", false);
            refreshButton.SetBounds(900, 16, 110, 40);
            refreshButton.Click += delegate { LoadData(); };
            header.Controls.AddRange(new Control[] { title, refreshButton });

            _grid.SetBounds(220, 92, 520, 500);
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.SelectionChanged += delegate { FillFieldsFromSelection(); };

            _fieldPanel.SetBounds(760, 92, 250, 500);
            _fieldPanel.BackColor = UITheme.Panel;
            _fieldPanel.Padding = new Padding(20);
            Label editorTitle = new Label { Text = "Record Details", Left = 20, Top = 20, Width = 210, Height = 28, Font = UITheme.HeaderFont(13F), ForeColor = UITheme.Text };
            _fieldPanel.Controls.Add(editorTitle);

            AddLabel("Name", 20, 64);
            _nameText.SetBounds(20, 90, 210, 28);
            AddLabel("Price", 20, 132);
            _priceText.SetBounds(20, 158, 96, 28);
            AddLabel("VAT %", 134, 132);
            _vatText.SetBounds(134, 158, 96, 28);
            AddLabel("Category Id", 20, 200);
            _categoryIdText.SetBounds(20, 226, 96, 28);
            AddLabel("Hall Id", 134, 200);
            _hallIdText.SetBounds(134, 226, 96, 28);
            AddLabel("Role", 20, 268);
            _roleCombo.Items.AddRange(new object[] { "Admin", "Waiter" });
            _roleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _roleCombo.SetBounds(20, 294, 96, 28);
            AddLabel("RFID Code", 134, 268);
            _rfidText.SetBounds(134, 294, 96, 28);

            Button newButton = UITheme.CreateButton("New", false);
            Button saveButton = UITheme.CreateButton("Save", true);
            Button deleteButton = UITheme.CreateButton("Delete", false);
            newButton.SetBounds(20, 360, 66, 42);
            saveButton.SetBounds(92, 360, 66, 42);
            deleteButton.SetBounds(164, 360, 66, 42);
            newButton.Click += delegate { ClearFields(); };
            saveButton.Click += delegate { Save(); };
            deleteButton.Click += delegate { Delete(); };

            _fieldPanel.Controls.AddRange(new Control[] { _nameText, _priceText, _vatText, _categoryIdText, _hallIdText, _roleCombo, _rfidText, newButton, saveButton, deleteButton });
            Controls.AddRange(new Control[] { sidebar, header, _entityCombo, _grid, _fieldPanel });
            UITheme.ApplyTheme(this);
            title.Font = UITheme.HeaderFont(18F);
            sideTitle.Font = UITheme.HeaderFont(15F);
            editorTitle.Font = UITheme.HeaderFont(13F);
            UITheme.StyleButton(saveButton, true);
            LoadData();
            UpdateFieldVisibility();
        }

        private void AddLabel(string text, int left, int top)
        {
            _fieldPanel.Controls.Add(new Label { Text = text, Left = left, Top = top, AutoSize = true, ForeColor = UITheme.MutedText });
        }

        private Button NavButton(string text, int top, string entity)
        {
            Button button = UITheme.CreateButton(text, false);
            button.SetBounds(16, top, 158, 44);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Click += delegate { _entityCombo.SelectedItem = entity; };
            return button;
        }

        private string Entity { get { return Convert.ToString(_entityCombo.SelectedItem); } }

        private int SelectedId
        {
            get
            {
                if (_grid.CurrentRow == null)
                    return 0;
                return Convert.ToInt32(_grid.CurrentRow.Cells["Id"].Value);
            }
        }

        private void LoadData()
        {
            try
            {
                _grid.DataSource = _repository.GetTable(Entity);
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load data: " + ex.Message);
            }
        }

        private void UpdateFieldVisibility()
        {
            bool products = Entity == "Products";
            bool tables = Entity == "Tables";
            bool users = Entity == "Users";
            _priceText.Enabled = products;
            _vatText.Enabled = products;
            _categoryIdText.Enabled = products;
            _hallIdText.Enabled = tables;
            _roleCombo.Enabled = users;
            _rfidText.Enabled = users;
        }

        private void FillFieldsFromSelection()
        {
            if (_grid.CurrentRow == null)
                return;

            DataGridViewRow row = _grid.CurrentRow;
            _nameText.Text = Convert.ToString(row.Cells["Name"].Value);
            SetTextIfColumn(row, "Price", _priceText);
            SetTextIfColumn(row, "VAT", _vatText);
            SetTextIfColumn(row, "CategoryId", _categoryIdText);
            SetTextIfColumn(row, "HallId", _hallIdText);
            if (HasColumn(row, "Role")) _roleCombo.Text = Convert.ToString(row.Cells["Role"].Value);
            SetTextIfColumn(row, "RFIDCode", _rfidText);
        }

        private static bool HasColumn(DataGridViewRow row, string column)
        {
            return row.DataGridView.Columns.Contains(column);
        }

        private static void SetTextIfColumn(DataGridViewRow row, string column, TextBox textBox)
        {
            textBox.Text = HasColumn(row, column) ? Convert.ToString(row.Cells[column].Value) : string.Empty;
        }

        private void ClearFields()
        {
            _grid.ClearSelection();
            _nameText.Clear();
            _priceText.Clear();
            _vatText.Clear();
            _categoryIdText.Clear();
            _hallIdText.Clear();
            _rfidText.Clear();
            _roleCombo.SelectedIndex = _roleCombo.Items.Count > 0 ? 1 : -1;
        }

        private void Save()
        {
            string name = _nameText.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show("Name is required.");
                return;
            }

            try
            {
                int id = SelectedId;
                if (Entity == "Categories") _repository.SaveCategory(id, name);
                if (Entity == "Halls") _repository.SaveHall(id, name);
                if (Entity == "Tables") _repository.SaveTable(id, name, ParseInt(_hallIdText.Text, "Hall Id"));
                if (Entity == "Users") _repository.SaveUser(id, name, _roleCombo.Text, _rfidText.Text.Trim());
                if (Entity == "Products") _repository.SaveProduct(id, name, ParseDecimal(_priceText.Text, "Price"), ParseDecimal(_vatText.Text, "VAT"), ParseInt(_categoryIdText.Text, "Category Id"));

                ProductCache.Refresh();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message);
            }
        }

        private void Delete()
        {
            int id = SelectedId;
            if (id == 0)
                return;
            if (MessageBox.Show("Delete selected record?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                _repository.Delete(Entity, id);
                ProductCache.Refresh();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Delete failed: " + ex.Message);
            }
        }

        private static int ParseInt(string value, string name)
        {
            int result;
            if (!int.TryParse(value, out result) || result <= 0)
                throw new InvalidOperationException(name + " must be a valid positive number.");
            return result;
        }

        private static decimal ParseDecimal(string value, string name)
        {
            decimal result;
            if (!decimal.TryParse(value, out result) || result < 0)
                throw new InvalidOperationException(name + " must be a valid positive number.");
            return result;
        }
    }
}
