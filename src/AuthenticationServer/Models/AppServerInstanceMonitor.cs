using NLog;
using ServerControlService.Model;
using ServerControlService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuthenticationServer
{
    public class AppServerInstanceMonitor:ServerControlService.BahamutAppInstanceMonitor
    {
        public IDictionary<string,HashSet<string>> OnlineAppInstances { get; private set; }
        public IDictionary<string,DateTime> AppInstancesExpire { get; set; }

        public AppServerInstanceMonitor()
        {
            OnlineAppInstances = new Dictionary<string, HashSet<string>>();
            AppInstancesExpire = new Dictionary<string, DateTime>();
            var ts = ServerControlManagementService.AppInstanceExpireTime;
            new Timer(OnCleanOfflineAppinstance, null, ts, ts);
        }

        private void OnCleanOfflineAppinstance(object state)
        {
            var now = DateTime.UtcNow;
            var ts = ServerControlManagementService.AppInstanceExpireTime;
            foreach (var set in OnlineAppInstances)
            {
                foreach (var ins in set.Value.ToArray())
                {
                    try
                    {
                        var dt = AppInstancesExpire[ins];
                        if (now - dt > ts)
                        {
                            set.Value.Remove(ins);
                            AppInstancesExpire.Remove(ins);
                        }
                    }
                    catch (Exception)
                    {
                        set.Value.Remove(ins);
                    }
                }
            }
        }

        public static string GetAppChannelId(string appkey)
        {
            return Startup.Configuration[string.Format("AppChannel:{0}:channel", appkey)];
        }

        public async Task<BahamutAppInstance> GetInstanceForClientWithAppkeyAsync(string appkey)
        {
            try
            {
                foreach (var instanceId in OnlineAppInstances[appkey].ToArray())
                {
                    var instance = await Startup.ServicesProvider.GetServerControlManagementService().GetAppInstanceAsync(instanceId);
                    if (instance != null)
                    {
                        return instance;
                    }
                }
                throw new NoAppInstanceException();

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
            AppInstancesExpire[state.InstanceId] = DateTime.UtcNow;

#if DEBUG
            if (state.NotifyType == BahamutAppInstanceNotification.TYPE_REGIST_APP_INSTANCE)
            {
                Console.WriteLine("{0} Instance Online:{1}", GetAppChannelId(state.Appkey), state.InstanceId);
            }
            else if (state.NotifyType == BahamutAppInstanceNotification.TYPE_INSTANCE_HEART_BEAT)
            {
                Console.WriteLine("{0} Instance Heart Beat:{1}", GetAppChannelId(state.Appkey), state.InstanceId);
            }
#endif
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
                AppInstancesExpire.Remove(state.InstanceId);
            }
            catch (Exception)
            {
            }
        }
    }

    public static class DIExtension
    {
        public static AppServerInstanceMonitor UseAppServerInstanceMonitor(this ServerControlService.BahamutAppInsanceMonitorManager mgr)
        {
            var appkeys = Startup.Configuration.GetSection("Data:InterestedChannelAppKey").GetChildren();
            var monitor = new AppServerInstanceMonitor();
            foreach (var ak in appkeys)
            {
                var channel = AppServerInstanceMonitor.GetAppChannelId(ak.Value);
                if (string.IsNullOrWhiteSpace(channel))
                {
                    LogManager.GetLogger("Warning").Warn("No App Channel With Appkey:{0}", ak.Value);
                }
                else
                {
                    mgr.RegistMonitor(channel, monitor);
                }
            }
            return monitor;
        }
    }
}
