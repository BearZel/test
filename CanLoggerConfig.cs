using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator
{
    class CanLoggerConfig
    {
        private const string _canLoggerCfgPath = "/etc/can_logger/can_logger.cfg";
        private CSSHClient sshClient = null;

        public Properties Props = new Properties();

        public class Trigger
        {
            public string CanName;
            public List<int> Modules;

            public bool HasModule(int id)
            {
                return Modules.Contains(id);
            }

            public void AddModule(int id)
            {
                Modules.Add(id);
            }
        }

        public class Properties
        {
            public string LogName;
            public int LogSizeMb;
            public int DelayRecoverySec;
            public int EmptyEventsDelaySec;
            public List<Trigger> Triggers;
        }


        public CanLoggerConfig(CSSHClient sshClient)
        {
            this.sshClient = sshClient;
            Load();
        }

        void Load()
        {
            var json = CAuxil.ParseJson(_canLoggerCfgPath);
            if (json == null)
            {
                return;
            }

            Props.LogName = json["log"].ToString();
            Props.LogSizeMb = json["log_size_megabytes"].Value<int>();
            Props.DelayRecoverySec = json["delay_recovery_seconds"].Value<int>();
            Props.EmptyEventsDelaySec = json["empty_events_delay_seconds"].Value<int>();
            Props.Triggers = new List<Trigger>();

            JObject triggers = json["triggers"].Value<JObject>();
            foreach (var item in triggers)
            {
                Trigger trigger = new Trigger();
                trigger.CanName = item.Key;
                trigger.Modules = item.Value.ToObject<List<int>>();

                Props.Triggers.Add(trigger);
            }
        }

        public void Save()
        {
            JObject o = new JObject();
            o["log"] = Props.LogName;
            o["log_size_megabytes"] = Props.LogSizeMb;
            o["delay_recovery_seconds"] = Props.DelayRecoverySec;
            o["empty_events_delay_seconds"] = Props.EmptyEventsDelaySec;

            JObject t = new JObject();
            Props.Triggers.ForEach(x =>
            {
                t[x.CanName] = new JArray(x.Modules);
            });

            o["triggers"] = t;

            Stream s = CAuxil.StringToStream(o.ToString());
            sshClient.WriteFile(_canLoggerCfgPath, s);
        }
    }
}
