using System;
using System.Net.Http;
using Allocations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Steeltoe.Security.Authentication.CloudFoundry;

namespace AllocationsServer
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

            services.AddDbContext<AllocationContext>(options => options.UseMySql(Configuration));
            services.AddScoped<IAllocationDataGateway, AllocationDataGateway>();

            services.AddSingleton<IProjectClient>(sp =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(Configuration.GetValue<string>("REGISTRATION_SERVER_ENDPOINT"))
                };

                return new ProjectClient(httpClient);
            });
                    
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                 .AddCloudFoundryJwtBearer(Configuration);

            services.AddAuthorization(options =>
                options.AddPolicy("pal-tracker", policy => policy.RequireClaim("scope", "uaa.resource")));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseAuthentication();   
            app.UseMvc();
        }
    }
}