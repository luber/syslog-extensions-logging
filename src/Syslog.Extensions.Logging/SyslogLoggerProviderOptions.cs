using System;
using Microsoft.Extensions.Logging;

namespace Syslog.Extensions.Logging
{   
    public class SyslogLoggerProviderOptions
    {
        public string SyslogServerHost { get; set; }
        public int SyslogServerPort { get; set; }
        public Func<string, LogLevel, bool> Filter { get; set; }
    }
}