using System.Configuration;

namespace jail.Classes
{
    /// <summary>
    /// App settings data manager
    /// </summary>
    public static class SettingsHelper
    {
        /// <summary>
        /// MobileCMSDB
        /// </summary>
        public static string DatabasePath { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            //DatabasePath = ConfigurationManager.ConnectionStrings["DatabasePath"].ConnectionString;
        }
    }
}