using System;
using System.Windows.Forms;
using RestaurantManagementSystem.UI;

namespace RestaurantManagementSystem
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (WebApiServer apiServer = new WebApiServer())
            {
                try
                {
                    apiServer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Web frontend API could not start on http://localhost:5055/." + Environment.NewLine + ex.Message,
                        "Frontend API", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                Application.Run(new LoginForm());
            }
        }
    }
}
