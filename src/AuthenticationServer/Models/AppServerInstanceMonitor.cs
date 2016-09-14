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
            try
            {
                var instanceId = OnlineAppInstances[appkey].First();
                var instance = await Startup.ServicesProvider.GetServerControlManagementService().GetAppInstanceAsync(instanceId);
                if (instance == null)
                {
                    throw new NoAppInstanceException();
                }
                return instance;
            }
            catch (Exception)
            {
                throw new NoAppInstanceException();
            }
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
            OnInstanceRegisted(state);
        }

        public void OnInstanceOffline(BahamutAppInstanceNotification state)
        {
            try
            {
                OnlineAppInstances[state.Appkey].Remove(state.InstanceId);
            }
            catch (Exception)
            {
            }
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
