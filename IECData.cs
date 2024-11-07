using AbakConfigurator.Properties;
using AbakConfigurator.Secure.Entry;
using AbakConfigurator.Secure.Toolbox;
using Newtonsoft.Json.Linq;
using Npgsql;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AbakConfigurator.Secure.Data
{
    internal class IECEntry : EntryInfo
    {
        private string m_Key = "";

        public string Key
        {
            get => m_Key;
            set
            {
                m_Key = value;
                OnPropertyChanged(nameof(Key));
            }
        }
    }

    internal class IECData : INotifyPropertyChanged, ISecureData
    {
        const string ConfigPath = "/opt/abak/A:/assembly/config/proxy_iec104.json";

        public ObservableDictionary<string, IECEntry> Entries { get; set; } = new ObservableDictionary<string, IECEntry>();

        public IECData()
        {

        }

        public void Init()
        {
            Entries.Clear();

            XmlDocument xml_doc = new XmlDocument();
            xml_doc.LoadXml(Resources.IECScheme);

            foreach (XmlNode entry_node in xml_doc.DocumentElement.ChildNodes)
            {
                IECEntry entry = new IECEntry()
                {
                    Key = entry_node.Attributes["Key"].Value,
                    Title = CGlobal.GetResourceValue(entry_node.Attributes["Title"].Value),
                    Description = CGlobal.GetResourceValue(entry_node.Attributes["Description"].Value),
                    Type = (EntryType)Enum.Parse(typeof(EntryType), entry_node.Attributes["Type"].Value),
                    Developer = bool.Parse(entry_node.Attributes["Developer"].Value)
                };

                entry.Value = (entry.Type == EntryType.Boolean) ? "False" : "0";
                entry.State = EntryState.Inited;
                entry.Changed = false;

                Entries.Add(entry.Key, entry);
            }
        }

        public void Load()
        {
            Stream stream = CGlobal.Handler.SSHClient.ReadFile(ConfigPath);

            if (stream == null)
            {
                // error
                return;
            }

            StreamReader reader = new StreamReader(stream);
            JObject json = JObject.Parse(reader.ReadToEnd());

            foreach (var property in json)
            {
                if (!Entries.ContainsKey(property.Key))
                {
                    continue;
                }

                var entry = Entries[property.Key];

                entry.Value = property.Value.ToString();
                entry.State = EntryState.Loaded;
                entry.Changed = false;
            }
        }

        public void Save()
        {
            JObject json = new JObject();

            foreach (var entry in Entries)
            {
                if (entry.Value.Changed)
                {
                    CGlobal.Handler.UserLog(2, string.Format("Changed IEC '{0}' value to '{1}'", entry.Value.Key, entry.Value.Value));
                }

                if (entry.Value.Type == EntryType.Boolean)
                {
                    // exceptional shit

                    if (entry.Value.Value == "True" || entry.Value.Value == "1")
                    {
                        json.Add(entry.Key, 1);
                    }
                    else if (entry.Value.Value == "False" || entry.Value.Value == "0")
                    {
                        json.Add(entry.Key, 0);
                    }
                    else
                    {
                        json.Add(entry.Key, Boolean.Parse(entry.Value.Value));
                    }
                }
                else if (entry.Value.Type == EntryType.Integer)
                {
                    json.Add(entry.Key, Int64.Parse(entry.Value.Value));
                }
                else
                {
                    json.Add(entry.Key, entry.Value.Value);
                }

                entry.Value.Changed = false;
            }

            string content = json.ToString();

            if (content.Length < 1)
            {
                // error
                return;
            }

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            CGlobal.Handler.SSHClient.WriteFile(ConfigPath, stream);
        }

        public bool Loaded()
        {
            return false;
        }

        public bool Changed()
        {
            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
