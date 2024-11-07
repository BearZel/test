using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using Newtonsoft.Json.Linq;
namespace AbakConfigurator
{
    class CConfigLogic
    {
        //Ошибки на этапе т.з.
        private Dictionary<string, string> oldNewLines = new Dictionary<string, string>
        {
            {"sudo -u postgres psql -d abak -c \"TRUNCATE params_table\";" , ""},
            {"sudo -u postgres psql -d abak -c \"copy params_table from '/tmp/backup/DB/params_table.sql'\";",
            "/tmp/backup/DB/updateParams.sh;"},
            { "reboot;", ""}

        };
        public CConfigLogic()
        {

        }
        public bool Start()
        {
            if (!ChangeOldLines())
            {
                return false;
            }
            SetModulesLogic();
            SetTagsLogic();
            ChangeCodesysName();
            DeleteOldFiles();
            return true;
        }
        private void DeleteOldFiles()
        {
            CGlobal.Session.SSHClient.ExecuteCommand("rm /tmp/backup/abak_start");
        }
        private void SetModulesLogic()
        {
            //Не сохраняли...
            Stream stream = CGlobal.Session.SSHClient.ReadFile("/tmp/backup/DB/modules.sql");
            if (stream == null)
                return;

            ChangeLine(stream);
        }
        private void ChangeLine(Stream stream)
        {
            StreamReader streamReader = new StreamReader(stream);
            string cmd = "";
            while (streamReader.Peek() >= 0)
            {
                string line = streamReader.ReadLine();
                if (line == "")
                    continue;

                cmd += "\t 123\n";
            }
            CGlobal.Session.SSHClient.WriteFile("/tmp/backup/DB/modules.sql", CAuxil.StringToStream(cmd));
        }
        private bool ChangeOldLines()
        {
            Stream stream = CGlobal.Session.SSHClient.ReadFile("/tmp/backup/writeCmd.sh");
            if (stream == null)
            {
                return false;
            }
            StreamReader streamReader = new StreamReader(stream);
            string cmd = "";
            while (streamReader.Peek() >= 0)
            {
                string line = streamReader.ReadLine();
                if (line == "")
                    continue;

                if (oldNewLines.ContainsKey(line))
                    line = oldNewLines[line];

                cmd += line + "\n";
            }
            CGlobal.Session.SSHClient.WriteFile("/tmp/backup/writeCmd.sh", CAuxil.StringToStream(cmd));
            return true;
        }
        private void SetTagsLogic()
        {
            Stream stream = CGlobal.Session.SSHClient.ReadFile("/tmp/backup/DB/params_table.sql");
            if (stream == null)
                return;

            StreamReader streamReader = new StreamReader(stream);
            string cmd = "";
            while (streamReader.Peek() >= 0)
            {
                string line = streamReader.ReadLine();
                if (line == "")
                    continue;

                string[] str = line.Split('\t');

                if (str[6] == "\\N" || str[4].ToLower() == "t")
                    continue;

                cmd += $"sudo -u postgres psql -d abak -c \"update params_table set value = '{str[6]}' " +
                    $"where tag = '{str[1]}'\" &>/dev/null\n";
            }
            CGlobal.Session.SSHClient.WriteFile("/tmp/backup/DB/updateParams.sh", CAuxil.StringToStream(cmd));
            CGlobal.Session.SSHClient.ExecuteCommand($"chmod -R 755 /tmp/backup/DB/updateParams.sh");
        }
        private void ChangeCodesysName()
        {
            Stream stream = CGlobal.Session.SSHClient.ReadFile("/tmp/backup/CODESYSControl_User.cfg");
            if (stream == null)
            {
                return;
            }
            CIniFile ini = new CIniFile(stream);
            ini.NewLine = "\n";
            CParam param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "SERNUM") as CParam;
            ini.WriteValue("systarget", "nodename", "ABAKPLC" + param.Value.ToString());
            ini.ExtraSpace = false;
            CGlobal.Session.SSHClient.WriteFile("/etc/CODESYSControl.cfg", ini.GetStream());
        }
    }
}
