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
        private Func<string, LogLevel, bool> _filter;

        public SyslogLoggerProvider(IOptionsMonitor<SyslogLoggerProviderOptions> options)
        {
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
            ReloadLoggerOptions(options.CurrentValue);
        }

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
            return new SyslogLogger(name, Dns.GetHostName(), _filter, _messageProcessor);
        }

        private void ReloadLoggerOptions(SyslogLoggerProviderOptions providerOptions)
        {
            _filter = providerOptions.Filter;

            // all new loogers will use this instance
            _messageProcessor = new SyslogLoggerProcessor(providerOptions.SyslogServerHost, providerOptions.SyslogServerPort);

            // update all existing loggers to use new processor
            foreach (var logger in _loggers.Values)
            {
                // dispose previous
                logger.MessageProcessor.Dispose();

                // set new
                logger.MessageProcessor = _messageProcessor;

                logger.Filter = providerOptions.Filter;
            }
        }
    }
}