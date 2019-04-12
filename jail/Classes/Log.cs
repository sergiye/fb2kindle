using System;
using NLog;
using Simpl.Extensions.Encryption;

namespace jail.Classes
{
    internal static class Logger
    {
        private static readonly ILogger _logger = LogManager.GetLogger("Log");

        private static void WriteCustom(LogLevel level, string message, string logCallerAddress = null, Exception ex = null, string calledBy = null)
        {
            if (CommonHelper.CurrentIdentityName.GetHash().Equals(CommonHelper.AdminLoginHash))
                return;

            var info = new LogEventInfo(level, _logger.Name, message);
            if (ex != null)
            {
                info.Exception = ex;
                info.Properties["Error"] = ex.Message;
            }
            info.Properties["CalledBy"] = string.IsNullOrWhiteSpace(calledBy) ? CommonHelper.CurrentIdentityName : calledBy;
            info.Properties["LogCallerAddress"] = logCallerAddress;
            _logger.Log(typeof(Logger), info);
        }

        public static void WriteWarning(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsWarnEnabled) return;
            WriteCustom(LogLevel.Warn, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, null, calledBy);
        }

        public static void WriteError(Exception ex, string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsErrorEnabled) return;
            WriteCustom(LogLevel.Error, message, logCallerAddress, ex, calledBy);
        }

        public static void WriteTrace(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsTraceEnabled) return;
            WriteCustom(LogLevel.Trace, message, logCallerAddress, null, calledBy);
        }

        public static void WriteDebug(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Debug, message, logCallerAddress, null, calledBy);
        }

        public static void WriteFatal(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsDebugEnabled) return;
            WriteCustom(LogLevel.Fatal, message, logCallerAddress, null, calledBy);
        }

        public static void WriteInfo(string message, string logCallerAddress = null, string calledBy = null)
        {
            if (!_logger.IsInfoEnabled) return;
            WriteCustom(LogLevel.Info, message, logCallerAddress, null, calledBy);
        }
    }
}