﻿using System.Configuration;
using System.Web.Configuration;
using Fb2Kindle;

namespace jail.Classes
{
    /// <summary>
    /// App settings data manager
    /// </summary>
    public static class SettingsHelper
    {
        public static string DatabasePath { get; set; }
        public static string ConvertedBooksPath { get; set; }
        public static long MaxRequestLength { get; set; }

        public static DefaultOptions ConverterSettings;
        public static string ConverterCss;
        public static bool ConverterDetailedOutput;

        /// <summary>
        /// constructor
        /// </summary>
        static SettingsHelper()
        {
            DatabasePath = ConfigurationManager.AppSettings["DatabasePath"];
            ConvertedBooksPath = ConfigurationManager.AppSettings["ConvertedBooksPath"];

            var section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            MaxRequestLength = section != null ? (long)section.MaxRequestLength * 1024 : 4096 * 1024;
            
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