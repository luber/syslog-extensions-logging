using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Syslog.Extensions.Logging
{
    public class SyslogLogger : ILogger
    {
        private static readonly string _loglevelPadding = ": ";

        private readonly SyslogLoggerProcessor _queueProcessor;
        private Func<string, LogLevel, bool> _filter;

        public enum FacilityType
        {
            Kern, User, Mail, Daemon, Auth, Syslog, LPR, News, UUCP, Cron, AuthPriv, FTP,
            NTP, Audit, Audit2, CRON2, Local0, Local1, Local2, Local3, Local4, Local5, Local6, Local7
        }

        public enum SyslogLogLevel
        {
            Emergency,
            Alert,
            Critical,
            Error,
            Warn,
            Notice,
            Info,
            Debug
        }

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        internal SyslogLogger(string name, string hostName, Func<string, LogLevel, bool> filter, SyslogLoggerProcessor loggerProcessor)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            HostName = hostName;
            _filter = filter ?? ((category, logLevel) => true);

            _queueProcessor = loggerProcessor;
        }

        public string HostName { get; set; }

        public Func<string, LogLevel, bool> Filter
        {
            get => _filter;
            set => _filter = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string SysLogServerHost { get; set; }
        public int SysLogServerPort { get; set; }

        public string Name { get; }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && Filter(Name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return NoopDisposable.Instance;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            var severity = MapToSyslogLevel(logLevel);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(severity, Name, eventId.Id, message, exception);
            }
        }

        private static SyslogLogLevel MapToSyslogLevel(LogLevel level)
        {
            if (level == LogLevel.Critical)
                return SyslogLogLevel.Critical;
            if (level == LogLevel.Debug)
                return SyslogLogLevel.Debug;
            if (level == LogLevel.Error)
                return SyslogLogLevel.Error;
            if (level == LogLevel.Information)
                return SyslogLogLevel.Info;
            if (level == LogLevel.None)
                return SyslogLogLevel.Info;
            if (level == LogLevel.Trace)
                return SyslogLogLevel.Info;
            if (level == LogLevel.Warning)
                return SyslogLogLevel.Warn;
            return SyslogLogLevel.Info;
        }

        public virtual void WriteMessage(SyslogLogLevel severity, string logName, int eventId, string message, Exception exception)
        {
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            var priority = (int)FacilityType.Local0 * 8 + (int)severity; // (Facility * 8) + Severity = Priority
            
            logBuilder.Append("<");
            logBuilder.Append(priority);
            logBuilder.Append(">");
            logBuilder.Append(DateTime.Now.ToString("MMM dd HH:mm:ss", CultureInfo.InvariantCulture));
            logBuilder.Append(" ");
            logBuilder.Append(HostName);
            logBuilder.Append(" ");
            logBuilder.Append(logName);
            logBuilder.Append(_loglevelPadding);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.Append("]");
            
            if (!string.IsNullOrEmpty(message))
            {
                // message
                logBuilder.Append(" ");

                var len = logBuilder.Length;
                logBuilder.AppendLine(message);
                logBuilder.Replace(Environment.NewLine, Environment.NewLine + " ", len, message.Length);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logBuilder.AppendLine(exception.ToString());
            }

            if (logBuilder.Length > 0)
            {
                // Queue log message
                _queueProcessor.EnqueueMessage(logBuilder.ToString());
            }

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

    }

    internal class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance = new NoopDisposable();
        public void Dispose()
        {
        }
    }
}