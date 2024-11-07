using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using Npgsql;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс описывающий отдельное правило
    /// </summary>
    public class FireWallRule
    {
        /// <summary>
        /// Порт с которым работае правило
        /// </summary>
        private String port = "";

        /// <summary>
        /// Флаг TCP порта, если сброшен то UDP порт
        /// </summary>
        private Boolean tcp = true;

        /// <summary>
        /// Адрес правила
        /// Адрес можно вводить как один, так и несколько через запятую
        /// </summary>
        private string ipAddress = "";
        private string macAddress = "";

        /// <summary>
        /// Описание на всякий случай
        /// </summary>
        private String description = "";

        public string Port { get => port; set => port = value; }
        public bool Tcp { get => tcp; set => tcp = value; }
        public string IpAddress { get => ipAddress; set => ipAddress = value; }
        public string MacAddress { get => macAddress; set => macAddress = value; }
        public string Description { get => description; set => description = value; }
        public String TypeString
        {
            get
            {
                if (this.tcp)
                    return "TCP";
                else
                    return "UDP";
            }
            set
            {
                if (value == "TCP")
                    this.tcp = true;
                else
                    this.tcp = false;
            }
        }
    }

    /// <summary>
    /// Логика взаимодействия для FirewallWindow.xaml
    /// </summary>
    public partial class FirewallWindow : Window, INotifyPropertyChanged
    {

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        const uint MF_DISABLED = 0x00000002;
        const uint SC_MINIMIZE = 0xF020;
        //статус firewall
        private bool fwState = true;
        //статус firewall для сравнения
        private bool savedFwState = true;
        private const String FIREWALL_SETTINGS_PATH = "/opt/abak/A:/assembly/firewall.xml";

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        public static extern bool ModifyMenu(IntPtr hMnu, uint uPosition, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")]
        public static extern bool DrawMenuBar(IntPtr hWnd);

        /// <summary>
        /// Список правил контроллера
        /// </summary>
        private ObservableCollection<FireWallRule> rules = new ObservableCollection<FireWallRule>();

        /// <summary>
        /// SSH клиент
        /// </summary>
        private CSSHClient sshClient = null;

        public ObservableCollection<FireWallRule> Rules
        {
            get
            {
                return this.rules;
            }
        }

        public bool FireWallState
        {
            get => fwState;
            set
            {
                fwState = value;
                OnPropertyChanged("FireWallState");
            }
        }
        public FirewallWindow(CSSHClient sshClient, bool windowMode)
        {
            int numb = CGlobal.CurrState.AssemblyHi * 1000
                + CGlobal.CurrState.AssemblyMid * 100 + CGlobal.CurrState.AssemblyLo;
            bool isAssembly510 = numb >= 5100;

            this.sshClient = sshClient;
            if (!windowMode)
                loadSettingsFromXml();
            else
                ActLikeWindow(isAssembly510);

            if (!isAssembly510)
                return;

            FireWallState = savedFwState = GetFirewallState();
        }
        private bool GetFirewallState()
        {            
            NpgsqlCommand cmd = new NpgsqlCommand("select * from fast_table where tag='FIREWALL_STATE'", 
                CGlobal.Session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            bool state = Convert.ToBoolean(reader["value"]);
            reader.Close();
            return state;
        }
        private void ActLikeWindow(bool isAssembly510)
        {
            InitializeComponent();
            if (!isAssembly510)
                EnableFW.Visibility = Visibility.Collapsed;
        }

        public void DeleteRule(FireWallRule rule)
        {
            CGlobal.Handler.UserLog(2, string.Format("Remove Firewall rule '{0}:{1}'", rule.IpAddress, rule.Port));
            rules.Remove(rule);
        }
        //Для редиректа
        public void AddRule(FireWallRule rule)
        {
            CGlobal.Handler.UserLog(2, string.Format("Add Firewall rule '{0}:{1}'", rule.IpAddress, rule.Port));
            this.rules.Add(rule);
        }
        private void SetMenuItem(uint vEI, uint vMF, UIntPtr newItemID, string lPNewItem)
        {
            Process p = Process.GetCurrentProcess();
            IntPtr winHandle = new WindowInteropHelper(this).Handle;
            IntPtr lHwnd = GetSystemMenu(winHandle, false);
            bool lResult = ModifyMenu(lHwnd, vEI, vMF, newItemID, lPNewItem);
            DrawMenuBar(winHandle);
        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            SetMenuItem(SC_MINIMIZE, MF_DISABLED, (UIntPtr)65533, "Minimize");
            this.loadSettingsFromXml();
        }

        private void NewButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.rules.Clear();
        }

        private void AddButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.rules.Add(new FireWallRule());
        }

        private void RemoveButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            if (this.dataGrid.SelectedItem == null)
                return;

            FireWallRule rule = this.dataGrid.SelectedItem as FireWallRule;
            DeleteRule(rule);
        }

        /// <summary>
        /// Настройки файервола сохраняются в контроллере в виде xml файла для того что бы их можно было загрузить
        /// </summary>
        public void saveSettingsAsXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(decl, root);
            XmlNode rootNode = doc.CreateNode("element", "rules", "");
            doc.AppendChild(rootNode);
            //Правила сохраняются в xml файл
            foreach (FireWallRule rule in this.rules)
            {
                XmlNode ruleNode = doc.CreateNode("element", "rule", "");
                //Порт
                XmlAttribute attr = doc.CreateAttribute("port");
                attr.Value = rule.Port;
                ruleNode.Attributes.Append(attr);
                //Тип порта
                attr = doc.CreateAttribute("type");
                attr.Value = rule.TypeString;
                ruleNode.Attributes.Append(attr);
                //Адреса
                attr = doc.CreateAttribute("address");
                attr.Value = rule.IpAddress;
                ruleNode.Attributes.Append(attr);
                //MAC
                attr = doc.CreateAttribute("mac");
                attr.Value = rule.MacAddress;
                ruleNode.Attributes.Append(attr);
                //Описание
                attr = doc.CreateAttribute("descr");
                attr.Value = rule.Description;
                ruleNode.Attributes.Append(attr);
                rootNode.AppendChild(ruleNode);
            }
            MemoryStream mem = new MemoryStream();
            doc.Save(mem);
            mem.Position = 0;
            this.sshClient.WriteFile(FIREWALL_SETTINGS_PATH, mem);
        }
        private void loadSettingsFromXml()
        {
            Stream stream = this.sshClient.ReadFile(FIREWALL_SETTINGS_PATH);
            XPathDocument doc = new XPathDocument(stream);
            XPathNavigator nav = doc.CreateNavigator();
            foreach (XPathNavigator ruleNode in nav.Select("//rule"))
            {
                FireWallRule rule = new FireWallRule();
                rule.Port = CXML.getAttributeValue(ruleNode, "port", "");
                rule.TypeString = CXML.getAttributeValue(ruleNode, "type", "TCP");
                rule.IpAddress = CXML.getAttributeValue(ruleNode, "address", "");
                rule.MacAddress = CXML.getAttributeValue(ruleNode, "mac", "");
                rule.Description = CXML.getAttributeValue(ruleNode, "descr", "");
                this.rules.Add(rule);
            }
        }
        public void SaveDataToPLC()
        {
            //Сохранение настроек в виде XML файла в контроллере
            this.saveSettingsAsXml();
        }

        bool IsValidRules()
        {
            foreach (FireWallRule rule in rules)
            {
                if (string.IsNullOrEmpty(rule.Port))
                {
                    MessageBox.Show(string.Format("The rule: 'port {0}, ip {1}, mac {2}, description {3}' must contain at least one port!", rule.Port, rule.IpAddress, rule.MacAddress, rule.Description));
                    return false;
                }
            }
            return true;
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            if (!IsValidRules())
            {
                return;
            }
            if (MessageBox.Show(String.Format("{0}\n{1}", CGlobal.GetResourceValue("l_warnfirewall"), CGlobal.GetResourceValue("l_continuequest")), CGlobal.GetResourceValue("l_firewallWindowName"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                return;

            if (fwState != savedFwState)
                UpdateFirewallState();

            try
            {
                if(fwState)
                    SaveDataToPLC();
            }
            catch
            {
                String mess = CGlobal.GetResourceValue("l_notconnected") + "\n";
                mess += CGlobal.GetResourceValue("l_nosettfirewall") + "\n";
                mess += CGlobal.GetResourceValue("l_settrestorefirewall");

                MessageBox.Show(mess, CGlobal.GetResourceValue("l_firewallWindowName"), MessageBoxButton.OK, MessageBoxImage.Error);
                this.DialogResult = true;
            }

            string msg = "Настройки применены";
            MessageBox.Show(msg, Title, MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
        }

        private void UpdateFirewallState()
        {
            string sql = String.Format("update fast_table set value='{0}', changed=true where tag='{1}'", FireWallState, "FIREWALL_STATE");
            NpgsqlCommand sqlCMD = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            sqlCMD.ExecuteNonQuery();
            sql = String.Format("update params_table set value='{0}' where tag='{1}'", FireWallState, "FIREWALL_STATE");
            sqlCMD = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            sqlCMD.ExecuteNonQuery();
            string linuxCMD = "systemctl start nftables; systemctl enable nftables;";
            if (!fwState)
                linuxCMD = "systemctl stop nftables; systemctl disable nftables;";

            this.sshClient.ExecuteCommand(linuxCMD);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            this.rules.Clear();
            this.loadSettingsFromXml();
        }
    }
}
