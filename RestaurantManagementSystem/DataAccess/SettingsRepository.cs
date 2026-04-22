using System.Configuration;

namespace RestaurantManagementSystem.DataAccess
{
    public static class SettingsRepository
    {
        public static string RestaurantName
        {
            get
            {
                string value = ConfigurationManager.AppSettings["RestaurantName"];
                return string.IsNullOrWhiteSpace(value) ? "Restaurant" : value;
            }
        }
    }
}
