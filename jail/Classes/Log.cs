using System;
using jail.Models;
using NLog;

namespace jail.Classes {
    internal static class Logger {
        private static readonly ILogger logger = LogManager.GetLogger("Log");

        static Logger() {
            BaseRepository.CheckDatabaseInitialized();
        }

        private static void WriteCustom(LogLevel level, string message, string logCallerAddress = null,
            Exception ex = null, string calledBy = null) {
            if ((!string.IsNullOrEmpty(calledBy) && calledBy.ToLower().GetHash().Equals(CommonHelper.AdminLoginHash)) ||
                CommonHelper.CurrentIdentityName.ToLower().GetHash().Equals(CommonHelper.AdminLoginHash) ||
                "::1".Equals(logCallerAddress) ||
                CommonHelper.CurrentUserType == UserType.Administrator)
                return;

            var info = new LogEventInfo(level, logger.Name, message);
            if (ex != null) {
                info.Exception = ex;
                info.Properties["Error"] = ex.Message;
            }

            info.Properties["CalledBy"] =
                string.IsNullOrWhiteSpace(calledBy) ? CommonHelper.CurrentIdentityName : calledBy;
            info.Properties["LogCallerAddress"] = logCallerAddress;
            logger.Log(typeof(Logger), info);
        }

        public static void WriteWarning(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsWarnEnabled) return;
            WriteCustom(LogLevel.Warn, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(Exception ex, string message, string logCallerAddress = null,
            string calledBy = null) {
            if (!logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, ex, calledBy);
        }

        public static void WriteTrace(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsTraceEnabled) return;
            WriteCustom(LogLevel.Trace, message, logCallerAddress, null, calledBy);
        }

        public static void WriteDebug(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Debug, message, logCallerAddress, null, calledBy);
        }

        public static void WriteFatal(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Fatal, message, logCallerAddress, null, calledBy);
        }

        public static void WriteInfo(string message, string logCallerAddress = null, string calledBy = null) {
            if (!logger.IsInfoEnabled) return;
            WriteCustom(LogLevel.Info, message, logCallerAddress, null, calledBy);
        }
    }
}