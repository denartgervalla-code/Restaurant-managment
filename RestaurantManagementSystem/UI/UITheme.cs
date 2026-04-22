using System.Drawing;
using System.Windows.Forms;

namespace RestaurantManagementSystem.UI
{
    public static class UITheme
    {
        public static readonly Color Background = Color.FromArgb(30, 30, 46);
        public static readonly Color Panel = Color.FromArgb(42, 42, 61);
        public static readonly Color Accent = Color.FromArgb(255, 107, 53);
        public static readonly Color AccentHover = Color.FromArgb(232, 88, 36);
        public static readonly Color Text = Color.White;
        public static readonly Color MutedText = Color.FromArgb(210, 214, 224);
        public static readonly Color Field = Color.FromArgb(36, 36, 52);
        public static readonly Color Border = Color.FromArgb(64, 64, 88);
        public static readonly Color Danger = Color.FromArgb(196, 73, 73);
        public static readonly Color Success = Color.FromArgb(72, 168, 95);
        public static readonly Color Warning = Color.FromArgb(246, 190, 76);

        public static Font RegularFont()
        {
            return new Font("Segoe UI", 10F);
        }

        public static Font HeaderFont(float size)
        {
            return new Font("Segoe UI", size, FontStyle.Bold);
        }

        public static void ApplyTheme(Control parent)
        {
            if (parent == null)
                return;

            parent.Font = RegularFont();

            if (parent is Form)
            {
                Form form = (Form)parent;
                form.BackColor = Background;
                form.ForeColor = Text;
                form.StartPosition = FormStartPosition.CenterScreen;
            }
            else if (parent is Panel || parent is FlowLayoutPanel || parent is TableLayoutPanel)
            {
                parent.BackColor = Panel;
                parent.ForeColor = Text;
            }
            else if (parent is Label)
            {
                parent.BackColor = Color.Transparent;
                parent.ForeColor = Text;
            }
            else if (parent is Button)
            {
                StyleButton((Button)parent, true);
            }
            else if (parent is TextBox)
            {
                TextBox textBox = (TextBox)parent;
                textBox.BackColor = Field;
                textBox.ForeColor = Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (parent is ComboBox)
            {
                ComboBox comboBox = (ComboBox)parent;
                comboBox.BackColor = Field;
                comboBox.ForeColor = Text;
                comboBox.FlatStyle = FlatStyle.Flat;
            }
            else if (parent is ListBox)
            {
                ListBox listBox = (ListBox)parent;
                listBox.BackColor = Field;
                listBox.ForeColor = Text;
                listBox.BorderStyle = BorderStyle.None;
                listBox.ItemHeight = 32;
            }
            else if (parent is DataGridView)
            {
                StyleGrid((DataGridView)parent);
            }

            foreach (Control child in parent.Controls)
                ApplyTheme(child);
        }

        public static Button CreateButton(string text, bool accent)
        {
            Button button = new Button
            {
                Text = text,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                Font = RegularFont(),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            StyleButton(button, accent);
            return button;
        }

        public static Label CreateHeader(string text, float size)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = HeaderFont(size),
                ForeColor = Text,
                BackColor = Color.Transparent
            };
        }

        public static void StyleButton(Button button, bool accent)
        {
            button.BackColor = Accent;
            button.ForeColor = Text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentHover;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 28, 40);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Panel;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.RowHeadersVisible = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Accent;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = HeaderFont(10F);
            grid.DefaultCellStyle.BackColor = Panel;
            grid.DefaultCellStyle.ForeColor = MutedText;
            grid.DefaultCellStyle.SelectionBackColor = Accent;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(38, 38, 55);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = MutedText;
            grid.RowTemplate.Height = 34;
        }
    }
}
