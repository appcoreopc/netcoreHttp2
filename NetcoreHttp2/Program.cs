using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;

namespace NetcoreHttp2
{
    class Program
    {
        static void Main(string[] args)
        {
                       

            var hostBuilder = new WebHostBuilder()
               .ConfigureLogging((_, factory) =>
               {
                    // Set logging to the MAX.
                    factory.SetMinimumLevel(LogLevel.Trace);
                   factory.AddConsole();
               })
               .UseKestrel()
               .ConfigureKestrel((context, options) =>
               {
                   var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                    // Run callbacks on the transport thread
                    options.ApplicationSchedulingMode = SchedulingMode.Inline;

                    // Http/1.1 endpoint for comparison
                    options.Listen(IPAddress.Any, basePort, listenOptions =>
                   {
                       listenOptions.Protocols = HttpProtocols.Http1;
                   });

                    // TLS Http/1.1 or HTTP/2 endpoint negotiated via ALPN
                    options.Listen(IPAddress.Any, basePort + 1, listenOptions =>
                   {
                       listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                       listenOptions.UseHttps();
                       // listenOptions.ConnectionAdapters.Add(new TlsFilterAdapter());
                   });

                    // Prior knowledge, no TLS handshake. WARNING: Not supported by browsers
                    // but useful for the h2spec tests
                    options.Listen(IPAddress.Any, basePort + 5, listenOptions =>
                   {
                       listenOptions.Protocols = HttpProtocols.Http2;
                   });
               })
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseStartup<Startup>();

            hostBuilder.Build().Run();


        }
    }
    
    public class Startup
    {

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // app.UseTimingMiddleware();
            app.Run(context =>
            {
                return context.Response.WriteAsync("Hello World! " + context.Request.Protocol);
            });
        }
    }
}
