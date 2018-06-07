using System;
using System.Net.Http;
using Backlog;
using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Pivotal.Discovery.Client;
using Steeltoe.Common.Discovery;
using Steeltoe.CircuitBreaker.Hystrix;
using Microsoft.AspNetCore.Authentication;

namespace BacklogServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            services.AddDbContext<StoryContext>(options => options.UseMySql(Configuration));
            services.AddScoped<IStoryDataGateway, StoryDataGateway>();

             services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IProjectClient>(sp =>
            {
                var handler = new DiscoveryHttpClientHandler(sp.GetService<IDiscoveryClient>());
                var httpClient = new HttpClient(handler, false)                
                {
                    BaseAddress = new Uri(Configuration.GetValue<string>("REGISTRATION_SERVER_ENDPOINT"))
                };

                var logger = sp.GetService<ILogger<ProjectClient>>();
                var contextAccessor = sp.GetService<IHttpContextAccessor>();

                return new ProjectClient(
                     httpClient, logger,
                     () => contextAccessor.HttpContext.GetTokenAsync("access_token")
                 );
            });

            services.AddHystrixMetricsStream(Configuration);

            services.AddDiscoveryClient(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();

            app.UseDiscoveryClient();

            app.UseHystrixMetricsStream();
            app.UseHystrixRequestContext();
        }
    }
}