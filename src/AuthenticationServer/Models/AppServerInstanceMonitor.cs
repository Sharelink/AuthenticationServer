using NLog;
using ServerControlService.Model;
using ServerControlService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationServer
{
    public class AppServerInstanceMonitor:ServerControlService.BahamutAppInstanceMonitor
    {
        public IDictionary<string,HashSet<string>> OnlineAppInstances { get; set; }

        public async Task<BahamutAppInstance> GetInstanceForClientWithAppkeyAsync(string appkey)
        {
            var instance = await Startup.ServicesProvider.GetServerControlManagementService().GetAppInstanceAsync("");
            if (instance == null)
            {
                throw new NoAppInstanceException();
            }
            return instance;
        }

        public void OnInstanceRegisted(BahamutAppInstanceNotification state)
        {
            try
            {
                var set = OnlineAppInstances[state.Appkey];
                set.Add(state.InstanceId);
            }
            catch (Exception)
            {
                var set = new HashSet<string>();
                set.Add(state.InstanceId);
                OnlineAppInstances[state.Appkey] = set;
            }
        }

        public void OnInstanceHeartBeating(BahamutAppInstanceNotification state)
        {
            
        }

        public void OnInstanceOffline(BahamutAppInstanceNotification state)
        {
            
        }
    }

    public static class DIExtension
    {
        public static void UseAppServerInstanceMonitor(this ServerControlService.BahamutAppInsanceMonitorManager mgr)
        {
            var appkeys = Startup.Configuration.GetSection("Data:InterestedChannelAppKey").GetChildren();
            var monitor = new AppServerInstanceMonitor();
            foreach (var ak in appkeys)
            {
                var channel = Startup.Configuration[string.Format("AppChannel:{0}:channel", ak.Value)];
                if (string.IsNullOrWhiteSpace(channel))
                {
                    LogManager.GetLogger("Warning").Warn("No App Channel With Appkey:{0}", ak.Value);
                }
                else
                {
                    mgr.RegistMonitor(channel, monitor);
                }
            }
        }
    }
}
