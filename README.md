# syslog-extensions-logging
Syslog provider for .NET Core 2.0 [logging subsystem](https://github.com/aspnet/Logging).
This provider will send log messages to a Syslog server using via UDP protocol.

Note: For now it supports .Net Core 2.0 and later (versions before 2.0 are not supported yet)

### Instructions

**Package**: `Syslog.Extensions.Logging`
NuGet (`master`): [![](http://img.shields.io/nuget/v/Syslog.Extensions.Logging.svg?style=flat-square)](http://www.nuget.org/packages/Syslog.Extensions.Logging) [![](http://img.shields.io/nuget/dt/Syslog.Extensions.Logging.svg?style=flat-square)](http://www.nuget.org/packages/Syslog.Extensions.Logging)

Configure your app settings by adding next settings to appsettings.json:

    {
      "SyslogSettings": {
        "ServerHost": "127.0.0.1",
        "ServerPort": 514
      }
    }

Tell your application's logging subsystem that you want to you Syslog by adding next line to Program.cs:

```csharp
        public static IWebHost BuildWebHost(string[] args) {
            return WebHost.CreateDefaultBuilder(args) 
                .UseConfiguration(hostingConfig)
                .UseStartup<Startup>()
                .ConfigureLogging(builder => builder.AddSyslog()) // <- Add this line
                .Build();
  
```

And let Syslog provider know which configuration to use by adding next line in your Startup.cs:

```
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddOptions();

            services.Configure<SyslogLoggerProviderOptions>(Configuration.GetSection("SyslogSettings")); // <- Add this line
			...
        }
```

Also you can configure Syslog provider during adding like this:

```
		public static IWebHost BuildWebHost(string[] args) {
            return WebHost.CreateDefaultBuilder(args) 
                .UseConfiguration(hostingConfig)
                .UseStartup<Startup>()
                .ConfigureLogging((ctx, builder) => 
					builder.AddSyslog(options => {
						options.SyslogServerHost = "127.0.0.1"; // IP of your Syslog Server
						options.SyslogServerPort = 514; // Port on which Syslog Server is listening
					})
				 )
                .Build();
```

## Building from source
Open Syslog.Logging.sln in Visual Studio 2017 and build.
See [developer documentation](https://github.com/aspnet/Home/wiki) for more details.