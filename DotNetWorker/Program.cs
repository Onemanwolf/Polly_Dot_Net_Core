using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Polly;
using Polly.Extensions.Http;

namespace DotNetWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {

                    var circuitBreakerPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .CircuitBreakerAsync(6, TimeSpan.FromSeconds(30));

                    var retryPolicy = HttpPolicyExtensions
                        .HandleTransientHttpError() 
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // HttpRequestException, 5XX and 408
                        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(5));

               
                        services.AddHttpClient("worker")
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                        .AddPolicyHandler(retryPolicy)
                        .AddPolicyHandler(circuitBreakerPolicy);
                        services.AddHostedService<Worker>();
                });
    }
}
