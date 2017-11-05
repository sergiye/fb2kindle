using System.Configuration;
using System.Web.Configuration;

namespace jail.Classes
{
    /// <summary>
    /// App settings data manager
    /// </summary>
    public static class SettingsHelper
    {
        public static string DatabasePath { get; set; }
        public static string ArchivesPath { get; set; }
        public static long MaxRequestLength { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            ArchivesPath = ConfigurationManager.AppSettings["ArchivesPath"];

            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            MaxRequestLength = section != null ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
        }
    }
}