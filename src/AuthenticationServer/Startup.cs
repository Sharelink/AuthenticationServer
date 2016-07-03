using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BahamutService;
using ServerControlService.Service;
using ServiceStack.Redis;
using NLog;
using NLog.Config;
using Microsoft.Extensions.Configuration;
using BahamutCommon;
using BahamutAspNetCommon;
using System.IO;
using Newtonsoft.Json.Serialization;

namespace AuthenticationServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(configuration)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }

    public class Startup
    {

        public IConfiguration Configuration { get; private set; }
        public static IServiceProvider ServicesProvider { get; private set; }
        public static string Appkey { get; private set; }
        public static string Appname { get; private set; }
        public static IRedisClientsManager TokenServerClientManager { get; private set; }
        public static IRedisClientsManager ControlServerServiceClientManager { get; private set; }
        public static IHostingEnvironment HostingEnvironment { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            // Setup configuration sources.
            HostingEnvironment = env;
            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath);
            
            if (env.IsDevelopment())
            {
                builder.AddJsonFile("config_debug.json",true,true);
            }
            else
            {
                builder.AddJsonFile("/etc/bahamut/auth.json", true, true);
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
            services.AddMvc(config =>
            {
                config.Filters.Add(new LogExceptionFilter());
            }).AddJsonOptions(op =>
            {
                op.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            var bahamutDbConString = Configuration["Data:BahamutDBConnection:connectionString"];

            var svrControlDbConString = Configuration["Data:ServerControlDBConnection:connectionString"];

            var tokenServerUrl = Configuration["Data:TokenServer:url"].Replace("redis://", "");
            TokenServerClientManager = new PooledRedisClientManager(tokenServerUrl);

            var serverControlUrl = Configuration["Data:ControlServiceServer:url"].Replace("redis://", "");
            ControlServerServiceClientManager = new PooledRedisClientManager(serverControlUrl);
            services.AddSingleton(new AuthenticationService(bahamutDbConString));
            services.AddSingleton(new BahamutAccountService(bahamutDbConString));
            services.AddSingleton(new BahamutAppService(bahamutDbConString));
            services.AddSingleton(new ServerControlManagementService(ControlServerServiceClientManager));
            services.AddSingleton(new TokenService(TokenServerClientManager));

        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ServicesProvider = app.ApplicationServices;
            // Configure the HTTP request pipeline.

            //Log
            var logConfig = new LoggingConfiguration();
            LoggerLoaderHelper.LoadLoggerToLoggingConfig(logConfig, Configuration, "Data:Log:fileLoggers");

            if (env.IsDevelopment())
            {
                LoggerLoaderHelper.AddConsoleLoggerToLogginConfig(logConfig);
            }
            LogManager.Configuration = logConfig;

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

            LogManager.GetLogger("Main").Info("Server Started!");
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
