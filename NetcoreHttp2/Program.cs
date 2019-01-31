using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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
                    //factory.SetMinimumLevel(LogLevel.Trace);
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
                       listenOptions.UseConnectionLogging(); ;
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
            app.UseTimingMiddleware();
            app.Run(context =>
            {
                return context.Response.WriteAsync("Hello World! " + context.Request.Protocol);
            });
        }
    }


    public class TimingMiddleware
    {
        private readonly RequestDelegate _next;

        public TimingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Response.SupportsTrailers())
            {
                httpContext.Response.DeclareTrailer("Server-Timing");

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                await _next(httpContext);

                stopWatch.Stop();
                // Not yet supported in any browser dev tools
                httpContext.Response.AppendTrailer("Server-Timing", $"app;dur={stopWatch.ElapsedMilliseconds}.0");
            }
            else
            {
                // Works in chrome
                // httpContext.Response.Headers.Append("Server-Timing", $"app;dur=25.0");
                await _next(httpContext);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class TimingMiddlewareExtensions
    {
        public static IApplicationBuilder UseTimingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimingMiddleware>();
        }
    }


}
