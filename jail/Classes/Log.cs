using System;
using NLog;

namespace jail.Classes
{
    internal static class Log
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Log");

        private static void WriteCustom(LogLevel level, string message, string logCallerAddress = null, Exception ex = null, string calledBy = null)
        {
            var info = new LogEventInfo(level, Logger.Name, message);
            if (ex != null)
            {
                info.Exception = ex;
                info.Properties["Error"] = ex.Message;
            }
            info.Properties["CalledBy"] = string.IsNullOrWhiteSpace(calledBy) ? Environment.UserName : calledBy;
            info.Properties["LogCallerAddress"] = logCallerAddress;
            Logger.Log(typeof(Log), info);
        }

        public static void WriteWarning(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsWarnEnabled) return;
            WriteCustom(LogLevel.Warn, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(Exception ex, string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, ex, calledBy);
        }

        public static void WriteTrace(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsTraceEnabled) return;
            WriteCustom(LogLevel.Trace, message, logCallerAddress, null, calledBy);
        }

        public static void WriteDebug(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Debug, message, logCallerAddress, null, calledBy);
        }

        public static void WriteFatal(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Fatal, message, logCallerAddress, null, calledBy);
        }

        public static void WriteInfo(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!Logger.IsInfoEnabled) return;
            WriteCustom(LogLevel.Info, message, logCallerAddress, null, calledBy);
        }
    }
}