using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator
{
    public class CodesysConfig
    {
        private const string codesysControlCFG = "/etc/CODESYSControl.cfg";
        private CSSHClient sshClient = null;

        private LogSettings _log = new LogSettings();
        public LogSettings Log
        {
            get { return _log; }
            set { _log = value; }
        }

        public struct LogSettings
        {
            public string Name {  get; set; }
        }

        public CodesysConfig(CSSHClient sshClient)
        {
            this.sshClient = sshClient;
            Load();
        }

        void Load()
        {
            Stream stream = sshClient.ReadFile(codesysControlCFG);
            if (stream == null)
            {
                return;
            }

            CIniFile ini = new CIniFile(stream);
            ini.NewLine = "\n";

            ParseLogSettings(ini);
        }

        void ParseLogSettings(CIniFile ini)
        {
            _log.Name = ini.ReadValue("cmplog", "logger.0.name", "/var/log/codesyscontrol.log");
        }
    }
}
