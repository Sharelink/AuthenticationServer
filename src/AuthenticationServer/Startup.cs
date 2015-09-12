using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using BahamutService;
using ServerControlService.Service;
using DataLevelDefines;
using Microsoft.Dnx.Runtime;

namespace AuthenticationServer
{

    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            // Setup configuration sources.
            var builder = new ConfigurationBuilder(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            Appkey = Configuration["Data:App:appkey"];
            Appname = Configuration["Data:App:appname"];
        }

        public IConfiguration Configuration { get; private set; }
        public static IServiceProvider ServicesProvider { get;private set; }
        public static string Appkey { get; private set; }
        public static string Appname { get; private set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            var bahamutDbConString = Configuration["Data:BahamutDBConnection:connectionString"];
            var svrControlDbConString = Configuration["Data:ServerControlDBConnection:connectionString"];
            IRedisServerConfig controlRedisServerConfig = new RedisServerConfig()
            {
                Db = long.Parse(Configuration["Data:ControlServiceServer:Db"]),
                Host = Configuration["Data:ControlServiceServer:Host"],
                Password = Configuration["Data:ControlServiceServer:Password"],
                Port = int.Parse(Configuration["Data:ControlServiceServer:Port"])
            };

            IRedisServerConfig tokenRedisServerConfig = new RedisServerConfig()
            {
                Db = long.Parse(Configuration["Data:TokenServer:Db"]),
                Host = Configuration["Data:TokenServer:Host"],
                Password = Configuration["Data:TokenServer:Password"],
                Port = int.Parse(Configuration["Data:TokenServer:Port"])
            };
            services.AddInstance(new AuthenticationService(bahamutDbConString));
            services.AddInstance(new BahamutAccountService(bahamutDbConString));
            services.AddInstance(new BahamutAppService(bahamutDbConString));
            services.AddInstance(new ServerControlManagementService(controlRedisServerConfig));
            services.AddInstance(new TokenService(tokenRedisServerConfig));
            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();

        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            ServicesProvider = app.ApplicationServices;

            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            // Configure the HTTP request pipeline.

            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseErrorPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseErrorHandler("/Home/Error");
            }

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Account}/{action=Login}");

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
            
        }

    }

    public static class IGetServerControlService
    {
        public static ServerControlManagementService GetServerControlManagementService(this IServiceProvider provider)
        {
            return provider.GetService<ServerControlManagementService>();
        }
    }

    public static class IGetBahamutServiceExtension
    {
        public static TokenService GetTokenService(this IServiceProvider provider)
        {
            return provider.GetService<TokenService>();
        }

        public static AuthenticationService GetAuthenticationService(this IServiceProvider provider)
        {
            return provider.GetService<AuthenticationService>();
        }

        public static BahamutAccountService GetBahamutAccountService(this IServiceProvider provider)
        {
            return provider.GetService<BahamutAccountService>();
        }

        public static BahamutAppService GetBahamutAppService(this IServiceProvider provider)
        {
            return provider.GetService<BahamutAppService>();
        }
    }
}
