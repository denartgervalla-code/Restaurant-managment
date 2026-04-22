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
            Application.Run(new LoginForm());
        }
    }
}
