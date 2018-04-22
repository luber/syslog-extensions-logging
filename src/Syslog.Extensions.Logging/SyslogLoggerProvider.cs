using System;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Syslog.Extensions.Logging
{
    [ProviderAlias("Syslog")]
    public class SyslogLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, SyslogLogger> _loggers = new ConcurrentDictionary<string, SyslogLogger>();

        private readonly IDisposable _optionsReloadToken;

        private SyslogLoggerProcessor _messageProcessor;
        private string _serverHost;
        private int _serverPort;

        public SyslogLoggerProvider(IOptionsMonitor<SyslogLoggerProviderOptions> options)
        {
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
            ReloadLoggerOptions(options.CurrentValue);
        }

        public bool IsEnabled { get; private set; } // TODO

        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, CreateLoggerImplementation);
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
            _messageProcessor.Dispose();
        }

        private SyslogLogger CreateLoggerImplementation(string name)
        {
            _messageProcessor = new SyslogLoggerProcessor(_serverHost, _serverPort);

            return new SyslogLogger(name, Dns.GetHostName(),_messageProcessor);
        }

        private void ReloadLoggerOptions(SyslogLoggerProviderOptions providerOptions)
        {
            _serverHost = providerOptions.SyslogServerHost;
            _serverPort = providerOptions.SyslogServerPort;

            foreach (var logger in _loggers.Values)
            {
                logger.SysLogServerHost = providerOptions.SyslogServerHost;
                logger.SysLogServerPort = providerOptions.SyslogServerPort;
            }
        }
    }
}