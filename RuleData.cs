using AbakConfigurator.Properties;
using AbakConfigurator.Secure.Entry;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml;

namespace AbakConfigurator.Secure.Data
{
    internal class RuleEntry : EntryInfo
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

    internal class RuleGroup : INotifyPropertyChanged
    {
        string m_Name = "";
        List<RuleEntry> m_Rules = new List<RuleEntry>();
        RuleEntry m_SelectedRule = null;
        bool m_Changed = false;

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public List<RuleEntry> Rules
        {
            get => m_Rules;
            set
            {
                m_Rules = value;
                OnPropertyChanged(nameof(Rules));
            }
        }

        public RuleEntry SelectedRule
        {
            get => m_SelectedRule;
            set
            {
                m_SelectedRule = value;
                OnPropertyChanged(nameof(SelectedRule));
            }
        }

        public bool Changed
        {
            get => m_Changed;
            set
            {
                m_Changed = value;
                OnPropertyChanged(nameof(Changed));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    internal class RuleData : INotifyPropertyChanged, ISecureData
    {
        private RuleGroup m_SelectedGroup = null;
        public ObservableCollection<RuleGroup> Groups { get; set; } = new ObservableCollection<RuleGroup>();

        public RuleData()
        {

        }

        public void Init()
        {
            Groups.Clear();

            XmlDocument xml_doc = new XmlDocument();
            xml_doc.LoadXml(Resources.RuleScheme);

            foreach (XmlNode group_node in xml_doc.DocumentElement.ChildNodes)
            {
                RuleGroup group = new RuleGroup()
                {
                    Name = CGlobal.GetResourceValue(string.Format(group_node.Attributes["Title"].Value))
                };

                foreach (XmlNode rule_node in group_node.ChildNodes)
                {
                    RuleEntry rule = new RuleEntry()
                    {
                        Key = rule_node.Attributes["Key"].Value,
                        Title = CGlobal.GetResourceValue(rule_node.Attributes["Title"].Value),
                        Description = CGlobal.GetResourceValue(rule_node.Attributes["Description"].Value),
                        Type = (EntryType)Enum.Parse(typeof(EntryType), rule_node.Attributes["Type"].Value),
                        Value = rule_node.Attributes["Default"].Value
                    };

                    rule.State = EntryState.Inited;
                    rule.Changed = false;

                    group.Rules.Add(rule);
                }

                group.SelectedRule = group.Rules.ElementAt(0);
                group.Changed = false;

                Groups.Add(group);
            }
        }

        public void Load()
        {
            foreach (RuleGroup group in Groups)
            {
                foreach (RuleEntry rule in group.Rules)
                {
                    var command = new NpgsqlCommand("SELECT * FROM sec_rules WHERE key = @key", CGlobal.Handler.DBConnection);
                    command.Parameters.AddWithValue("key", NpgsqlDbType.Text, rule.Key);

                    var reader = command.ExecuteReader();

                    if (!reader.HasRows || !reader.Read())
                    {
                        reader.Close();

                        command = new NpgsqlCommand("INSERT INTO sec_rules (key, value) VALUES (@key, @value)", CGlobal.Handler.DBConnection);
                        command.Parameters.AddWithValue("key", NpgsqlDbType.Text, rule.Key);
                        command.Parameters.AddWithValue("value", NpgsqlDbType.Text, rule.Value);
                        command.ExecuteNonQuery();

                        continue;
                    }

                    SetRuleValue(reader["key"].ToString(), reader["value"].ToString());
                    reader.Close();
                }
            }
        }

        public void Save()
        {
            foreach (RuleGroup group in Groups)
            {
                if (group.Changed)
                {
                    foreach (RuleEntry rule in group.Rules)
                    {
                        if (rule.Changed)
                        {
                            CGlobal.Handler.UserLog(2, string.Format("Changed Rule '{0}' value to '{1}'", rule.Key, rule.Value));

                            var command = new NpgsqlCommand("UPDATE sec_rules SET value = @value WHERE key = @key", CGlobal.Handler.DBConnection);
                            command.Parameters.AddWithValue("key", NpgsqlDbType.Text, rule.Key);
                            command.Parameters.AddWithValue("value", NpgsqlDbType.Text, rule.Value);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public bool Loaded()
        {
            return false;
        }

        public bool Changed()
        {
            return false;
        }

        public string GetRuleValue(string key)
        {
            foreach (RuleGroup group in Groups)
            {
                foreach (RuleEntry rule in group.Rules)
                {
                    if (rule.Key == key)
                    {
                        return rule.Value;
                    }
                }
            }

            return null;
        }

        public int GetRuleValueInt(string key)
        {
            return int.Parse(GetRuleValue(key));
        }

        public Int64 GetRuleValueInt64(string key)
        {
            return Int64.Parse(GetRuleValue(key));
        }

        public double GetRuleValueDouble(string key)
        {
            return double.Parse(GetRuleValue(key));
        }

        public bool GetRuleValueBool(string key)
        {
            return bool.Parse(GetRuleValue(key));
        }

        void SetRuleValue(string key, string value)
        {
            foreach (RuleGroup group in Groups)
            {
                foreach (RuleEntry rule in group.Rules)
                {
                    if (rule.Key == key)
                    {
                        rule.Value = value;
                        rule.Changed = false;
                        return;
                    }
                }
            }
        }        

        public RuleGroup SelectedGroup
        {
            get => m_SelectedGroup;
            set
            {
                m_SelectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
