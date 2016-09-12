﻿using System;
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
using DataLevelDefines;

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
        public static IRedisClientsManager TokenServerClientManager { get; private set; }
        public static IRedisClientsManager ControlServerServiceClientManager { get; private set; }
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
            builder.AddJsonFile(configFile, true, true);
            builder.AddJsonFile(logConfig, true, true);
            HostingEnvironment = env;
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
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

            TokenServerClientManager = DBClientManagerBuilder.GenerateRedisClientManager(Configuration.GetSection("Data:TokenServer"));
            ControlServerServiceClientManager = DBClientManagerBuilder.GenerateRedisClientManager(Configuration.GetSection("Data:ControlServiceServer"));
            
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
            LoggerLoaderHelper.LoadLoggerToLoggingConfig(logConfig, Configuration, "Logger:fileLoggers");

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
