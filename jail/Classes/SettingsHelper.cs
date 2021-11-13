using System;
using System.Configuration;
using System.IO;
using System.Web.Configuration;

namespace jail.Classes
{
    /// <summary>
    /// App settings data manager
    /// </summary>
    public static class SettingsHelper
    {
        public static string ConverterPath { get; set; }
        public static string DatabasePath { get; set; }
        public static string StatisticDatabase { get; set; }
        public static string TimeTrackDatabase { get; set; }
        public static string ArchivesPath { get; set; }
        public static string TempDataFolder { get; }
        public static long MaxRequestLength { get; set; }
        public static int MaxRecordsToShowAtOnce { get; set; }
        public static bool TimeTrack { get; set; }
        public static bool GenerateBookDetails { get; set; }
        public static int GenerateBookTimeout { get; set; }
       
        public static string SmtpServer { get; set; }
        public static int SmtpPort { get; set; }
        public static string SmtpLogin { get; set; }
        public static string SmtpPassword { get; set; }
        
        public static string AdminDefaultEmail { get; set; }
        public static string SiteRemotePath { get; set; }
        public static string FlibustaLink { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            TimeTrackDatabase = ConfigurationManager.ConnectionStrings["TimeTrack"].ConnectionString;

            ConverterPath = ConfigurationManager.AppSettings["ConverterPath"];
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            StatisticDatabase = ConfigurationManager.AppSettings["StatisticsDBPath"];
            ArchivesPath = ConfigurationManager.AppSettings["ArchivesPath"];
            TempDataFolder = ConfigurationManager.AppSettings["TempDataFolder"];
            Directory.CreateDirectory(TempDataFolder); //to ensure folder exists

            MaxRecordsToShowAtOnce = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRecordsToShowAtOnce"]);
            TimeTrack = Convert.ToBoolean(ConfigurationManager.AppSettings["TimeTrack"]);
            GenerateBookDetails = Convert.ToBoolean(ConfigurationManager.AppSettings["GenerateBookDetails"]);
            GenerateBookTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["GenerateBookTimeout"]);

            SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
            SmtpPort =  Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
            SmtpLogin = ConfigurationManager.AppSettings["SmtpLogin"];
            SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            
            AdminDefaultEmail = ConfigurationManager.AppSettings["AdminDefaultEmail"];
            SiteRemotePath = ConfigurationManager.AppSettings["SiteRemotePath"];
            FlibustaLink = ConfigurationManager.AppSettings["FlibustaLink"];

            MaxRequestLength = ConfigurationManager.GetSection("system.web/httpRuntime") is HttpRuntimeSection section 
                ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
        }
    }
}