using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using BahamutService;
using ServerControlService.Service;
using Microsoft.Dnx.Runtime;
using ServiceStack.Redis;
using BahamutService.Model;
using NLog;
using NLog.Config;

namespace AuthenticationServer
{
    
    public class Startup
    {

        public IConfiguration Configuration { get; private set; }
        public static IServiceProvider ServicesProvider { get; private set; }
        public static string Appkey { get; private set; }
        public static string Appname { get; private set; }
        public static IRedisClientsManager TokenServerClientManager { get; private set; }
        public static IRedisClientsManager ControlServerServiceClientManager { get; private set; }
        public static IHostingEnvironment HostingEnvironment { get; private set; }
        public static IApplicationEnvironment AppEnvironment { get; private set; }

        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            // Setup configuration sources.
            HostingEnvironment = env;
            AppEnvironment = appEnv;
            var builder = new ConfigurationBuilder().SetBasePath(appEnv.ApplicationBasePath);
            if (env.IsDevelopment())
            {
                builder.AddJsonFile("config_debug.json");
            }
            else
            {
                builder.AddJsonFile("config.json");
            }
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            Appkey = Configuration["Data:App:appkey"];
            Appname = Configuration["Data:App:appname"];

        }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            var bahamutDbConString = Configuration["Data:BahamutDBConnection:connectionString"];

            var svrControlDbConString = Configuration["Data:ServerControlDBConnection:connectionString"];

            var tokenServerUrl = Configuration["Data:TokenServer:url"].Replace("redis://", "");
            TokenServerClientManager = new PooledRedisClientManager(tokenServerUrl);

            var serverControlUrl = Configuration["Data:ControlServiceServer:url"].Replace("redis://", "");
            ControlServerServiceClientManager = new PooledRedisClientManager(serverControlUrl);
            services.AddInstance(new AuthenticationService(bahamutDbConString));
            services.AddInstance(new BahamutAccountService(bahamutDbConString));
            services.AddInstance(new BahamutAppService(bahamutDbConString));
            services.AddInstance(new ServerControlManagementService(ControlServerServiceClientManager));
            services.AddInstance(new TokenService(TokenServerClientManager));

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();

        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ServicesProvider = app.ApplicationServices;
            // Configure the HTTP request pipeline.

            //Log
            var logConfig = new LoggingConfiguration();
            var fileTarget = new NLog.Targets.FileTarget();
            fileTarget.FileName = Configuration["Data:Log:logFile"];
            fileTarget.Name = "FileLogger";
            fileTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger}:${message}";
            logConfig.AddTarget(fileTarget);
            logConfig.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, fileTarget));
            LogManager.Configuration = logConfig;

            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                var consoleLogger = new NLog.Targets.ColoredConsoleTarget();
                consoleLogger.Name = "ConsoleLogger";
                consoleLogger.Layout = @"${date:format=HH\:mm\:ss} ${logger}:${message}";
                logConfig.AddTarget(consoleLogger);
                logConfig.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, consoleLogger));

                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
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

            LogManager.GetCurrentClassLogger().Info("Server Started!");
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
