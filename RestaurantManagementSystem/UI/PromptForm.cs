using System;
using System.Drawing;
using System.Windows.Forms;

namespace RestaurantManagementSystem.UI
{
    public static class PromptForm
    {
        public static string Ask(string title, string label, string defaultValue)
        {
            using (Form form = new Form())
            using (Label labelControl = new Label())
            using (TextBox textBox = new TextBox())
            using (Button okButton = new Button())
            using (Button cancelButton = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(380, 150);
                form.BackColor = UITheme.Background;
                form.ForeColor = UITheme.Text;
                form.Font = UITheme.RegularFont();

                labelControl.Text = label;
                labelControl.SetBounds(18, 18, 344, 22);
                labelControl.ForeColor = UITheme.MutedText;
                textBox.Text = defaultValue ?? string.Empty;
                textBox.SetBounds(18, 48, 344, 28);
                textBox.BackColor = UITheme.Field;
                textBox.ForeColor = UITheme.Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.SetBounds(196, 94, 78, 38);
                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.SetBounds(284, 94, 78, 38);
                UITheme.StyleButton(okButton, true);
                UITheme.StyleButton(cancelButton, false);

                form.Controls.AddRange(new Control[] { labelControl, textBox, okButton, cancelButton });
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
            }
        }
    }
}
