using System.Configuration;
using Fb2Kindle;

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

        public static DefaultOptions ConverterSettings;
        public static string ConverterCss;
        public static bool ConverterDetailedOutput;

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];

            //todo: customize from web.config later
            ConverterSettings = new DefaultOptions
                                {
                                    d = true
                                }; 
            ConverterCss = string.Empty;
            ConverterDetailedOutput = false;
        }
    }
}