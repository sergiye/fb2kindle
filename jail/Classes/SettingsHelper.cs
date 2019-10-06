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
        public static string StatisticDatabase { get; set; }
        public static string TimeTrackDatabase { get; set; }
        public static string ArchivesPath { get; set; }
        public static long MaxRequestLength { get; set; }
        public static int MaxRecordsToShowAtOnce { get; set; }
        public static bool TimeTrack { get; set; }
       
        public static string SmtpServer { get; set; }
        public static int SmtpPort { get; set; }
        public static string SmtpLogin { get; set; }
        public static string SmtpPassword { get; set; }
        
        public static string AdminDefaultEmail { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            TimeTrackDatabase = ConfigurationManager.ConnectionStrings["TimeTrack"].ConnectionString;

            ConverterName = ConfigurationManager.AppSettings["ConverterName"];
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            StatisticDatabase = ConfigurationManager.AppSettings["StatisticsDBPath"];
            ArchivesPath = ConfigurationManager.AppSettings["ArchivesPath"];

            MaxRecordsToShowAtOnce = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRecordsToShowAtOnce"]);
            TimeTrack = Convert.ToBoolean(ConfigurationManager.AppSettings["TimeTrack"]);

            SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            SmtpPort =  Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
            SmtpLogin = ConfigurationManager.AppSettings["SmtpLogin"];
            SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            
            AdminDefaultEmail = ConfigurationManager.AppSettings["AdminDefaultEmail"];
            
            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            MaxRequestLength = section != null ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
        }
    }
}