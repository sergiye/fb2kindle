using System;
using System.Configuration;
using System.Web.Configuration;

namespace jail.Classes
{
    /// <summary>
    /// App settings data manager
    /// </summary>
    public static class SettingsHelper
    {
        public static string ConverterName { get; set; }
        public static string DatabasePath { get; set; }
        public static string ArchivesPath { get; set; }
        public static long MaxRequestLength { get; set; }
        public static int MaxRecordsToShowAtOnce { get; set; }
       
        public static string SmtpServer { get; set; }
        public static int SmtpPort { get; set; }
        public static string SmtpLogin { get; set; }
        public static string SmtpPassword { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            ConverterName = ConfigurationManager.AppSettings["ConverterName"];
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            ArchivesPath = ConfigurationManager.AppSettings["ArchivesPath"];

            MaxRecordsToShowAtOnce = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRecordsToShowAtOnce"]);

            SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            SmtpPort =  Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
            SmtpLogin = ConfigurationManager.AppSettings["SmtpLogin"];
            SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            
            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            MaxRequestLength = section != null ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
        }
    }
}