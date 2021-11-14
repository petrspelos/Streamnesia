using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Streamnesia.CommandProcessing;
using Streamnesia.Payloads;
using Streamnesia.Twitch;
using Streamnesia.WebApp.Hubs;

namespace Streamnesia.WebApp
{
    public class Startup
    {
        private const string StreamnesiaConfigFile = "streamnesia-config.json";

        private readonly IServerLogger _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public Startup(IConfiguration configuration, IServerLogger logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _logger.Log("Injecting services");
            services.AddSingleton(p => GetStreamnesiaConfig());

            services.AddSingleton<UpdateHub>();
            services.AddSingleton<StreamnesiaHub>();
            services.AddSingleton<Random>();
            services.AddSingleton<ICommandPoll, CommandPoll>();
            services.AddSingleton<CommandQueue>();
            services.AddSingleton<IPayloadLoader, LocalPayloadLoader>();
            services.AddSingleton<Bot>();
            services.AddSingleton<PollState>();
            services.AddSingleton(_logger);

            services.AddControllersWithViews();
            services.AddSignalR();
        }

        private StreamnesiaConfig GetStreamnesiaConfig()
        {
            if(!File.Exists(StreamnesiaConfigFile))
            {
                var config = new StreamnesiaConfig();
                File.WriteAllText(StreamnesiaConfigFile, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }

            return JsonConvert.DeserializeObject<StreamnesiaConfig>(File.ReadAllText(StreamnesiaConfigFile));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<UpdateHub>("/updatehub");
            });
        }
    }
}
