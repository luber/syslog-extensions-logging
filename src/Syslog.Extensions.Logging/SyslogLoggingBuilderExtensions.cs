using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Syslog.Extensions.Logging
{
    public static class SyslogLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds a Syslog logger named 'Syslog' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddSyslog(this ILoggingBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, SyslogLoggerProvider>();

            return builder;
        }

        /// <summary>
        /// Adds a Syslog logger named 'Syslog' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure"></param>
        public static ILoggingBuilder AddSyslog(this ILoggingBuilder builder, Action<SyslogLoggerProviderOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddSyslog();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}