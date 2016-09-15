using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using BahamutService;
using ServerControlService.Service;
using NLog;
using NLog.Config;
using Microsoft.Extensions.Configuration;
using BahamutCommon;
using BahamutAspNetCommon;
using System.IO;
using Newtonsoft.Json.Serialization;
using DataLevelDefines;
using ServerControlService;
using Newtonsoft.Json;

namespace AuthenticationServer
{
    public class Program
    {
        public static IConfiguration ArgsConfig { get; private set; }
        public static void Main(string[] args)
        {
            ArgsConfig = new ConfigurationBuilder().AddCommandLine(args).Build();
            var configFile = ArgsConfig["config"];
            if (string.IsNullOrEmpty(configFile))
            {
                Console.WriteLine("No Config File");
            }
            else
            {
                var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(ArgsConfig)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();

                var appConfig = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile(configFile).Build();
                var urls = appConfig["Data:App:urls"].Split(new char[] { ';', ',', ' ' });
                hostBuilder.UseUrls(urls);
                hostBuilder.Build().Run();
            }
        }
    }

    public class Startup
    {

        public static IConfiguration Configuration { get; private set; }
        public static IServiceProvider ServicesProvider { get; private set; }
        public static string Appkey { get { return Configuration["Data:App:appkey"]; } }
        public static string Appname { get { return Configuration["Data:App:appname"]; } }
        public static AppServerInstanceMonitor AppServerInstanceMonitor { get; private set; }
        public static IHostingEnvironment HostingEnvironment { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            ReadConfig(env);
        }

        private void ReadConfig(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);
            var configFile = Program.ArgsConfig["config"];
            var baseConfig = builder.AddJsonFile(configFile, true, true).Build();
            var logConfig = baseConfig["Data:LogConfig"];
            var appChannelConfig = baseConfig["Data:AppChannelConfig"];
            builder.AddJsonFile(configFile, true, true);
            builder.AddJsonFile(logConfig, true, true);
            builder.AddJsonFile(appChannelConfig, true, true);
            HostingEnvironment = env;
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            //Log
            var logConfig = new LoggingConfiguration();
            LoggerLoaderHelper.LoadLoggerToLoggingConfig(logConfig, Configuration, "Logger:fileLoggers");
#if DEBUG
            LoggerLoaderHelper.AddConsoleLoggerToLogginConfig(logConfig);
#endif
            LogManager.Configuration = logConfig;

            // Add MVC services to the services container.
            services.AddMvc(config =>
            {
                config.Filters.Add(new LogExceptionFilter());
            }).AddJsonOptions(op =>
            {
                op.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                op.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                op.SerializerSettings.Formatting = Formatting.None;
            });

            var bahamutDbConString = Configuration["Data:BahamutDBConnection:connectionString"];
            services.AddSingleton(new AuthenticationService(bahamutDbConString));
            services.AddSingleton(new BahamutAccountService(bahamutDbConString));

            try
            {
                var tokenRedis = DBClientManagerBuilder.GenerateRedisConnectionMultiplexer(Configuration.GetSection("Data:TokenServer"));
                var redis = DBClientManagerBuilder.GenerateRedisConnectionMultiplexer(Configuration.GetSection("Data:ControlServiceServer"));
                BahamutAppInsanceMonitorManager.Instance.InitManager(redis);
                services.AddSingleton(new ServerControlManagementService(redis));
                services.AddSingleton(new TokenService(tokenRedis));
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("Error").Fatal(ex, "Redis Error:{0}", ex.Message);
                throw;
            }
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ServicesProvider = app.ApplicationServices;

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

            //Watch Api Servers Online
            AppServerInstanceMonitor = BahamutAppInsanceMonitorManager.Instance.UseAppServerInstanceMonitor();

            LogManager.GetLogger("Main").Info("Server Started!");
        }
    }
}
