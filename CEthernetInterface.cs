using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using AbakConfigurator.network;
using Newtonsoft.Json;
using System.Security.Principal;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс описывающий WiFi точку доступа
    /// </summary>
    public class CWifiInfo
    {
        //Название Wifi точки доступа
        private String name = "";
        //SSID точки
        private String ssid = "";
        //Открытая сеть
        private Boolean opened = false;
        public String Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public String SSID
        {
            get
            {
                return this.ssid;
            }
            set
            {
                this.ssid = value;
            }
        }

        public Boolean Opened
        {
            get
            {
                return this.opened;
            }
            set
            {
                this.opened = value;
            }
        }
    }

    /// <summary>
    /// Класс реализующий функционал по чтению и настройке Ethernet интерфейса
    /// </summary>
    public class CEthernetInterface : INotifyPropertyChanged
    {
        private Boolean wifi = false;
        private String netClass = "";
        private Boolean dhcp = false;
        private Boolean statIP = false;
        private String netMask = "";
        private String ip = "";
        private String globalIp = "";
        private string localGlobalIP = "";
        private string prefixLocalGlobalIP = "";
        private String prefixGlobalIp = "";
        private String gateway = "";
        private String accessPoint = "";
        private String ssid = "";
        private String password = "";
        private Boolean opened = false;
        private Boolean up = false;
        private Boolean changed = false;
        private bool autoStart = false;
        private String speed = "";
        private String duplex = "";
        private bool manualSpeed = false;
        private String name = "";
        private bool isGlobalPort = false;
        private bool isGlobalIpOn = false;
        private bool isPingOn = false;
        private string pingIP = "";
        private string minErrors = "0";
        private string timeOut = "0";
        private string externalNetwork = "";
        private string prefix = "";
        private Boolean isStatic = false;
        public ObservableCollection<DnsServers> dnsServers = new ObservableCollection<DnsServers>();
        public ObservableCollection<StaticRoute> staticRoutes = new ObservableCollection<StaticRoute>();


        private InterfaceSettings interfaceSettings = new InterfaceSettings();
        public InterfaceSettings InterfaceSettings
        {
            get
            {
                interfaceSettings.StaticRoutes.Clear();
                foreach (var item in staticRoutes)
                {
                    interfaceSettings.StaticRoutes.Add(new InterfaceSettings.Route(item.From, item.To));
                }
                return interfaceSettings;
            }
            set
            {
                interfaceSettings = value;
                int num = 1;
                staticRoutes.Clear();
                interfaceSettings.StaticRoutes.ForEach(r => staticRoutes.Add(new StaticRoute(num++, r.from, r.to)));
            }
        }

        /// <summary>
        /// Название интерфеса который прописан в /sys/class/net
        /// </summary>
        /// 
        public bool IsGlobalPort
        {
            get => this.isGlobalPort;
            set => this.isGlobalPort = value;
        }

        public String NetClass
        {
            get
            {
                return this.netClass;
            }
            set
            {
                this.netClass = value;
                this.wifi = this.netClass.Contains("wlan");
            }
        }

        /// <summary>
        /// MAC адрес интерфейса
        /// </summary>
        public String MacAddress
        {
            get;
            set;
        }

        /// <summary>
        /// DHCP флаг
        /// </summary>
        public Boolean DHCP
        {
            get
            {
                return this.dhcp;
            }
            set
            {
                if (this.dhcp == value)
                    return;

                this.dhcp = value;
                this.Changed = true;
                this.OnPropertyChanged("DHCP");
            }
        }

        /// <summary>
        /// DHCP флаг
        /// </summary>
        public Boolean StatIP
        {
            get
            {
                return this.statIP;
            }
            set
            {
                if (this.statIP == value)
                    return;

                this.statIP = value;
                this.Changed = true;
                this.OnPropertyChanged("StatIP");
            }
        }
        public Boolean IsStatic
        {
            get
            {
                return this.isStatic;
            }
            set
            {
                if (this.isStatic == value)
                    return;

                this.isStatic = value;
                this.Changed = true;
                this.OnPropertyChanged("IsStatic");
            }
        }
        public bool ManualSpeed
        {
            get
            {
                return this.manualSpeed;
            }
            set
            {
                this.manualSpeed = value;
                this.Changed = true;
                this.OnPropertyChanged("ManualSpeed");
            }
        }
        public string ExternalNetwork
        {
            get
            {
                return externalNetwork;
            }
            set
            {
                this.externalNetwork = value;
                this.Changed = true;
                this.OnPropertyChanged("ExternalNetwork");
            }
        }
        public string Prefix
        {
            get
            {
                return prefix;
            }
            set
            {
                this.prefix = value;
                this.Changed = true;
                this.OnPropertyChanged("Prefix");
            }
        }
        public string LocalGlobalIP
        {
            get
            {
                return localGlobalIP;
            }
            set
            {
                this.localGlobalIP = value;
                this.Changed = true;
                this.OnPropertyChanged("LocalGlobalIP");
            }
        }
        public bool IsGlobalIpOn
        {
            get
            {
                return this.isGlobalIpOn;
            }
            set
            {
                this.isGlobalIpOn = value;
                this.Changed = true;
                this.OnPropertyChanged("IsGlobalIpOn");
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
                this.Changed = true;
                this.OnPropertyChanged("Name");
            }
        }

        public String Speed
        {
            get
            {
                this.manualSpeed = (this.speed != "") || (this.duplex != "");
                return this.speed;
            }
            set
            {
                this.speed = value;
                this.Changed = true;
                this.OnPropertyChanged("Speed");
            }
        }

        public String Duplex
        {
            get
            {
                this.manualSpeed = (this.speed != "") || (this.duplex != "");
                return this.duplex.ToUpper();
            }
            set
            {
                this.duplex = value.ToLower();
                this.Changed = true;
                this.OnPropertyChanged("Duplex");
            }
        }

        /// <summary>
        /// Флаг того что интерфейс подключен
        /// </summary>
        public Boolean Up
        {
            get
            {
                return this.up;
            }
            set
            {
                if (this.up == value)
                    return;

                this.up = value;
            }
        }

        //Mask подсети
        public String NetMask
        {
            get
            {
                return this.netMask;
            }
            set
            {
                if (this.netMask == value)
                    return;

                this.netMask = value;
                this.Changed = true;
                this.OnPropertyChanged("NetMask");
            }
        }

        /// <summary>
        /// IP адрес
        /// </summary>
        public String IP
        {
            get
            {
                return this.ip;
            }
            set
            {
                if (this.ip == value)
                    return;

                this.ip = value;
                this.Changed = true;
                this.OnPropertyChanged("IP");
            }
        }

        /// <summary>
        /// Глобальный IP адрес
        /// </summary>
        public String GLobalIP
        {
            get
            {
                return this.globalIp;
            }
            set
            {
                if (this.globalIp == value)
                    return;

                this.globalIp = value;
                this.Changed = true;
                this.OnPropertyChanged("GLoablIP");
            }
        }
        /// <summary>
        /// Префикс глобального IP адрес
        /// </summary>
        public String PrefixGLobalIP
        {
            get
            {
                return this.prefixGlobalIp;
            }
            set
            {
                if (this.prefixGlobalIp == value)
                    return;

                this.prefixGlobalIp = value;
                this.Changed = true;
                this.OnPropertyChanged("PrefixGLobalIP");
            }
        }
        public String PrefixLocalGLobalIP
        {
            get
            {
                return this.prefixGlobalIp;
            }
            set
            {
                if (this.prefixGlobalIp == value)
                    return;

                this.prefixGlobalIp = value;
                this.Changed = true;
                this.OnPropertyChanged("PrefixGLobalIP");
            }
        }
        /// <summary>
        /// Шлюз
        /// </summary>
        public String Gateway
        {
            get
            {
                return this.gateway;
            }
            set
            {
                if (this.gateway == value)
                    return;

                this.gateway = value;
                this.Changed = true;
                this.OnPropertyChanged("Gateway");
            }
        }

        /// <summary>
        /// Название интерфейса
        /// </summary>
        public String AccessPoint
        {
            get
            {
                return this.accessPoint;
            }
            set
            {
                if (this.accessPoint == value)
                    return;

                this.accessPoint = value;
                this.Changed = true;
                this.OnPropertyChanged("AccessPoint");
            }
        }

        /// <summary>
        /// Режим подключения к WiFi точке
        /// </summary>
        public Boolean AutoStart
        {
            get
            {
                return this.autoStart;
            }
            set
            {
                if (this.autoStart == value)
                    return;

                this.autoStart = value;
                this.Changed = true;
                this.OnPropertyChanged("AutoStart");
            }
        }


        /// <summary>
        /// SSID интерфейса
        /// </summary>
        public String SSID
        {
            get
            {
                return this.ssid;
            }
            set
            {
                if (this.ssid == value)
                    return;

                this.ssid = value;
                this.Changed = true;
                this.OnPropertyChanged("SSID");
            }
        }

        /// <summary>
        /// Полное название WiFi сети
        /// </summary>
        public String FullWifiName
        {
            get
            {
                String s = String.Format("wifi_{0}_{1}_managed_", this.MacAddress.Replace(":", ""), this.ssid);
                if (!this.opened)
                    s = s + "psk";
                else
                    s = s + "open";

                return s;
            }
        }

        /// <summary>
        /// Пароль WiFi сети
        /// </summary>
        public String Password
        {
            get
            {
                return this.password;
            }
            set
            {
                if (this.password == value)
                    return;

                this.password = value;
                this.Changed = true;
                this.OnPropertyChanged("Password");
            }
        }

        /// <summary>
        /// Флаг того что испоьзуется открытяая wifi сеть
        /// </summary>
        public Boolean Opened
        {
            get
            {
                return this.opened;
            }
            set
            {
                if (this.opened == value)
                    return;

                this.opened = value;
                this.Changed = true;
            }
        }

        /// <summary>
        /// Флаг изменения настроек интерфейса
        /// </summary>
        public Boolean Changed
        {
            get
            {
                return this.changed;
            }
            set
            {
                if (this.changed == value)
                    return;

                this.changed = value;
                this.OnPropertyChanged("Changed");
            }
        }
        public bool IsPingOn
        {
            get => isPingOn;
            set
            {
                isPingOn = value;
                Changed = true;
                OnPropertyChanged("IsPingOn");
            }
        }
        public string PingIP
        {
            get => pingIP;
            set
            {
                pingIP = value;
                Changed = true;
                OnPropertyChanged("PingIP");
            }
        }
        public string MinErrors
        {
            get => minErrors;
            set
            {
                minErrors = value;
                Changed = true;
                OnPropertyChanged("MinErrors");
            }
        }
        public string TimeOut
        {
            get => timeOut;
            set
            {
                timeOut = value;
                Changed = true;
                OnPropertyChanged("TimeOut");
            }
        }
        /// <summary>
        /// Флаг того что здесь содержиться информация о WiFi сети
        /// </summary>
        public Boolean WiFi
        {
            get
            {
                return this.wifi;
            }
        }

        /// <summary>
        /// Человеческое описание сети
        /// </summary>
        public String Description
        {
            get
            {
                // Dictionary<String, String> description = new Dictionary<String, String>();
                String s = "";
                //description.TryGetValue("")
                if (this.NetClass.Contains("eth"))
                    s = String.Format("{0} ({1})", CGlobal.GetResourceValue("l_cableNet"), this.NetClass);
                else if (this.NetClass.Contains("wlan"))
                    s = String.Format("{0} ({1})", CGlobal.GetResourceValue("l_wifiNet"), this.NetClass);
                else
                    return "";

                if (this.Up)
                    s += "*";

                return s;
            }
        }

        /// <summary>
        /// Копирование параметро одного класса в другой
        /// </summary>
        /// <param name="ethInt">Из этого параметра надо скопировать</param>
        public void Assign(CEthernetInterface ethInt)
        {
            this.Name = ethInt.Name;
            IsGlobalPort = ethInt.IsGlobalPort;
            IsGlobalIpOn = ethInt.IsGlobalIpOn;
            this.dnsServers = ethInt.dnsServers;
            this.wifi = ethInt.WiFi;
            this.NetClass = ethInt.NetClass;
            this.DHCP = ethInt.DHCP;
            this.StatIP = ethInt.StatIP;
            this.NetMask = ethInt.NetMask;
            this.GLobalIP = ethInt.GLobalIP;
            this.PrefixGLobalIP = ethInt.PrefixGLobalIP;
            this.IP = ethInt.IP;
            this.Gateway = ethInt.Gateway;
            this.AccessPoint = ethInt.AccessPoint;
            this.SSID = ethInt.SSID;
            this.opened = ethInt.Opened;
            this.Password = ethInt.Password;
            this.MacAddress = ethInt.MacAddress;
            this.up = ethInt.Up;
            this.autoStart = ethInt.AutoStart;
            this.Speed = ethInt.Speed;
            this.Duplex = ethInt.Duplex;
            this.ManualSpeed = ethInt.ManualSpeed;
            isPingOn = ethInt.IsPingOn;
            pingIP = ethInt.PingIP;
            minErrors = ethInt.MinErrors;
            timeOut = ethInt.TimeOut;
            this.staticRoutes = ethInt.staticRoutes;
            this.interfaceSettings = ethInt.interfaceSettings;
            this.prefixLocalGlobalIP = ethInt.prefixLocalGlobalIP;
            this.PrefixLocalGLobalIP = ethInt.PrefixLocalGLobalIP;
            this.LocalGlobalIP = ethInt.LocalGlobalIP;
            this.ExternalNetwork = ethInt.ExternalNetwork;
            this.Prefix = ethInt.Prefix;
            this.isStatic = ethInt.IsStatic;
            this.Changed = false;
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Класс список Ethernet интерфейсов контроллера
    /// </summary>
    public class CEthernetIterfacesList
    {
        public JObject PingSettings = new JObject();
        private const string globalIPPath = "/opt/abak/A:/assembly/globalip.xml";
        //Путь к файлу с настройками интерфейсов (устарел и используется до сборок 5.2.0)
        private const String lanConfig = "/var/lib/connman/lan.config";
        //Путь к файлу с настройками интерфейса (используется для сборок 5.2.0 и выше)
        private const String ethConfig = "/var/lib/connman/ethernet_{0}_cable/settings";
        //Варианты для разных сборок. До 5.2.0 использовался файл lan.config.
        //После 5.2.0 перешли на команду connmanctl
        private Dictionary<bool, string[]> variantList = new Dictionary<bool, string[]>
        {
            {true, new string [] {"/var/lib/connman/ethernet_{0}_cable/settings",
                "ethernet_{0}_cable", "IPv4.method", "manual"}},
            {false, new string [] { "/var/lib/connman/lan.config",
                "service_{0}", "IPv4", "dhcp"}},
        };
        //SSH клиент
        private CSSHClient sshClient = null;
        //Список доступных интерфесов контроллера
        private List<CEthernetInterface> interfacesList = new List<CEthernetInterface>();
        //Список интерфейсов, которым можно назначить глобальный IP
        public Dictionary<string, string[]> GlobalEthsSettings = new Dictionary<string, string[]>();
        //speed and duplex
        CIniFile speedDuplex = null;
        /// <summary>
        /// Список интерфейсов контроллера
        /// </summary>
        public List<CEthernetInterface> InterfacesList
        {
            get
            {
                return this.interfacesList;
            }
        }

        public CEthernetIterfacesList(CSSHClient sshClient)
        {
            this.sshClient = sshClient;
        }

        /// <summary>
        /// Чтение настроек интерфейса из системы
        /// </summary>
        /// <param name="eth"></param>
        private void GetSettingsFromSystem(CEthernetInterface eth)
        {
            eth.IP = "";
            eth.NetMask = "";
            eth.Gateway = "";
            eth.DHCP = true;
            if (!eth.Up)
                return;

            //Чтение идет только у подключенного интерфейса
            String res = this.sshClient.ExecuteCommand(String.Format("ifconfig {0} | awk '/inet / {{print $0}}'", eth.NetClass));
            if (res == "")
                return;

            res = res.Trim();

            //Получение IP адреса
            int index = res.IndexOf(' ') + 1;
            res = res.Substring(index, res.Length - index);
            index = res.IndexOf(' ');
            eth.IP = res.Substring(0, index);

            //Получение маски подсети
            index = eth.IP.Length + 1;
            res = res.Substring(index, res.Length - index).TrimStart();
            index = res.IndexOf(' ') + 1;
            res = res.Substring(index, res.Length - index);
            index = res.IndexOf(' ');
            eth.NetMask = res.Substring(0, index);

            //Получение шлюза
            //Попытка №1
            res = this.sshClient.ExecuteCommand(String.Format("ip route | awk '/via.*{0}/ {{print $0}}'", eth.NetClass));
            if (res == "")
            {
                //Попытка №2
                res = this.sshClient.ExecuteCommand(String.Format("ip route | awk '/dev {0} scope link/ {{print $0}}'", eth.NetClass));
                if (res == "")
                    return;

                index = res.IndexOf(' ');
                res = res.Substring(0, index);

                if (res == "default")
                    eth.Gateway = "";
                else
                    eth.Gateway = res;
            }
            else
            {
                index = res.IndexOf("via ") + 4;
                res = res.Substring(index, res.Length - index);
                index = res.IndexOf(' ') + 1;
                res = res.Substring(0, index);
                if (res.Contains(" "))
                {
                    index = res.IndexOf(' ');
                    eth.Gateway = res.Remove(index, 1);
                }
                else
                    eth.Gateway = res;
            }
        }
        private CIniFile LoadLanSettings(string path)
        {
            Stream stream = this.sshClient.ReadFile(path);
            if (stream == null)
                stream = new MemoryStream();
            CIniFile ini = new CIniFile(stream);
            ini.NewLine = "\n";
            return ini;
        }
        private void LoadPingSettings()
        {
            PingSettings = CAuxil.ParseJson("/opt/abak/A:/assembly/configs/ping_service.config");
            if (PingSettings == null)
            {
                return;
            }
            foreach (JToken jToken in PingSettings.First.First.Children())
            {
                string netClass = jToken["interface"].ToString();
                CEthernetInterface eth = interfacesList.FirstOrDefault(x => x.NetClass == netClass);
                if (eth == null)
                    continue;

                eth.IsPingOn = jToken["enable"].ToString().ToLower() == "true";
                eth.PingIP = jToken["ip"].ToString();
                eth.MinErrors = jToken["errors"].ToString();
                eth.TimeOut = jToken["timeout"].ToString();
            }
        }
        private void LoadLanManualSettings(string path)
        {
            Stream stream = this.sshClient.ReadFile(path);
            if (stream == null)
                stream = new MemoryStream();
            speedDuplex = new CIniFile(stream);
            speedDuplex.NewLine = "\n";
        }
        /// <summary>
        /// Функция обновляет список интерфейсов с их настройками
        /// </summary>
        /// <param name="Host"></param>
        /// 

        //Подгрузка MAC и состояние интерфейса
        private bool GetMACAndState(ref CEthernetInterface eth)
        {
            string mac = this.sshClient.ExecuteCommand(String.Format("cat /sys/class/net/{0}/address", eth.NetClass));
            string operstate = this.sshClient.ExecuteCommand(String.Format("cat /sys/class/net/{0}/operstate", eth.NetClass));
            if (mac == "" || operstate == "")
                return false;

            eth.MacAddress = mac;
            //Флаг активности интерфейса 
            eth.Up = operstate == "up";
            return true;
        }

        private void SetEthSettings(string netClass)
        {
            int numb = CGlobal.CurrState.AssemblyInt;
            CEthernetInterface eth = new CEthernetInterface();
            eth.NetClass = netClass;
            //Подгрузка MAC и состояние интерфейса
            if (!GetMACAndState(ref eth))
                return;

            if (!LoadSettingsFromFile(ref eth, numb >= 520))
                GetSettingsFromSystem(eth);

            eth.Speed = speedDuplex.ReadValue(eth.NetClass, "speed");
            eth.Duplex = speedDuplex.ReadValue(eth.NetClass, "duplex");
            eth.LocalGlobalIP = speedDuplex.ReadValue(eth.NetClass, "localGlobalIP", "");
            string[] tmp = speedDuplex.ReadValue(eth.NetClass, "externalNetwork", "").Split('/');
            string test = speedDuplex.ReadValue(eth.NetClass, "isStatic", "0");
            eth.IsStatic = Convert.ToBoolean(speedDuplex.ReadValue(eth.NetClass, "isStatic", "false"));
            if (tmp.Length == 2)
            {
                eth.ExternalNetwork = tmp[0];
                eth.Prefix = tmp[1];
            }
            eth.StatIP = !eth.DHCP;
            SetGlobalIP(ref eth);

            this.interfacesList.Add(eth);
        }
        private void SetGlobalIP(ref CEthernetInterface eth)
        {
            eth.IsGlobalPort = GlobalEthsSettings.ContainsKey(eth.NetClass);
            if (!eth.IsGlobalPort)
                return;
            //Назначение настроек глобального IP
            string[] str = GlobalEthsSettings[eth.NetClass][0].Split('.');
            if (str.Length == 4)
            {
                if (str[3] != "0")
                {
                    eth.PrefixGLobalIP = $"{str[0]}.{str[1]}.{str[2]}.";
                    eth.GLobalIP = str[3];
                }
            }
            eth.IsGlobalIpOn = Convert.ToBoolean(GlobalEthsSettings[eth.NetClass][1]);
        }
        private bool LoadSettingsFromFile(ref CEthernetInterface eth, bool isNewAssembly)
        {
            //Поиск интерфейса по MAC адресу
            //из MAC адреса удаляются символы :
            String mac = eth.MacAddress.Replace(":", "");
            string path = String.Format(variantList[isNewAssembly][0], mac);
            string sectionName = String.Format(variantList[isNewAssembly][1], mac);
            //Подгрузка файла с настройками connman
            CIniFile config = this.LoadLanSettings(path);
            sectionName = String.Format(sectionName, mac);
            if (!config.IsSectionExists(sectionName))
                return false;

            string paramName = variantList[isNewAssembly][2];
            //Указанная секция существует, значит надо прочитать настройки
            String line = config.ReadValue(sectionName, paramName);
            if ((line == "") || (line == "dhcp") || (line == "auto")) //Интерфейс получает IP адрес по DHCP, значит параметры выдёргиваются из системы
                return false;
            //Ручная настройка IP адреса
            if (isNewAssembly)
                SetSettingsManually(ref eth, ref config, sectionName);
            else
                SetSettingsManually(ref eth, ref config, line, sectionName);

            return true;
        }

        private void LoadFromJson()
        {
            using (Stream file = sshClient.ReadFile("/opt/abak/A:/assembly/configs/network.json"))
            {
                StreamReader sr = new StreamReader(file);
                JObject json = JObject.Parse(sr.ReadToEnd());
                int counter = 0;
                foreach (JObject iface in json["interfaces"])
                {
                    InterfaceSettings settings = iface.ToObject<InterfaceSettings>();
                    CEthernetInterface cEthernetInterface = interfacesList.Find(i => i.NetClass == settings.Eth.ToString().ToLower());
                    if (cEthernetInterface != null)
                    {
                        cEthernetInterface.InterfaceSettings = settings;
                        ++counter;
                    }
                    if(counter == interfacesList.Count)
                    {
                        break;
                    }
                }
            }
        }

        public void SaveToJson(CEthernetInterface eth)
        {
            using (Stream file = sshClient.ReadFile("/opt/abak/A:/assembly/configs/network.json"))
            {
                StreamReader sr = new StreamReader(file);
                JObject json = JObject.Parse(sr.ReadToEnd());
                JToken ifaceToken = json["interfaces"].First(i => i["eth"].ToString().ToLower() == eth.NetClass.ToString().ToLower());
                JObject iface = JObject.FromObject(eth.InterfaceSettings);
                ifaceToken.Replace(iface);

                MemoryStream memoryStream = new MemoryStream();
                StreamWriter sw = new StreamWriter(memoryStream);
                var s = json.ToString();
                sw.Write(s);
                sw.Flush();
                sshClient.WriteFile("/opt/abak/A:/assembly/configs/network.json", memoryStream);
            }
        }

        private UInt32 GetPreffixLen(String networkMask)
        {
            UInt16[] d = new UInt16[] { 0, 0, 0, 0 };

            try
            {
                int i = 0;
                while (true)
                {
                    int index = networkMask.IndexOf('.');
                    if (index >= 0)
                    {
                        String s = networkMask.Substring(0, index);
                        d[i] = Convert.ToUInt16(s);
                        networkMask = networkMask.Remove(0, index + 1);
                        i++;
                    }
                    else
                    {
                        d[i] = Convert.ToUInt16(networkMask);
                        break;
                    }
                }

                UInt32 mask = 0;
                for (int j = 0; j < 4; j++)
                {
                    mask = mask | (UInt32)(d[3 - j] << j * 8);

                }
                mask = (mask ^ 0xFFFFFFFF) + 1;
                //Расчет величины prefix_len
                UInt32 pref = 0;
                for (int j = 0; j < 32; j++)
                {
                    mask = mask >> 1;
                    if ((mask & 0x00000001) != 0)
                        pref = (UInt32)(j + 1);
                }
                pref = 32 - pref;


                return pref;
            }
            catch
            {
                return 0;
            }

        }
        private string GetNetMaskByPrefixLen(string prefixLen)
        {
            if (prefixLen == "")
                return "";

            if (prefixLen == "32")
                return "255.255.255.255";

            string[] netMask = new string[4] { "255", "255", "255", "255" };
            int len = Convert.ToInt32(prefixLen);
            //Надо у каких октетов включены все биты
            int octet = len / 8;
            //Надо узнать сколько бит включено у октета, с которым работаем
            //Единица вычитается, чтобы подстроить число под массив
            int bitsOn = len - octet * 8 - 1;
            StringBuilder sb = new StringBuilder("11111111");
            for (int i = 7; i > bitsOn; i--)
                sb[i] = '0';

            string bits = sb.ToString();
            for (int i = octet; i < 4; i++)
                netMask[i] = "0";

            netMask[octet] = Convert.ToInt32(bits, 2).ToString();
            return netMask[0] + "." + netMask[1] + "." +
                netMask[2] + "." + netMask[3];
        }
        //Ручная настройка IP адреса
        private void SetSettingsManually(ref CEthernetInterface eth, ref CIniFile config, String sectionName)
        {
            eth.IP = config.ReadValue(sectionName, "IPv4.local_address", "");
            string prefixLen = config.ReadValue(sectionName, "IPv4.netmask_prefixlen", "");
            eth.NetMask = GetNetMaskByPrefixLen(prefixLen);
            eth.Gateway = config.ReadValue(sectionName, "IPv4.gateway", "");
            FillServers(ref eth, config.ReadValue(sectionName, "Nameservers", ""), ';');
        }
        private void SetSettingsManually(ref CEthernetInterface eth, ref CIniFile config, String line, String sectionName)
        {
            //IP адрес
            int index = line.IndexOf('/');
            String ss = line.Substring(0, index);
            eth.IP = ss;
            line = line.Remove(0, index + 1);
            //Маска подсети
            index = line.IndexOf('/');
            if (index != -1)
            {
                ss = line.Substring(0, index);
                eth.NetMask = ss;
                line = line.Remove(0, index + 1);
                //Шлюз
                eth.Gateway = line;
            }
            else
                eth.NetMask = line;
            //DNS сервера
            FillServers(ref eth, config.ReadValue(sectionName, "Nameservers"), ',');
        }
        private void FillServers(ref CEthernetInterface eth, string nameServers, char splitter)
        {
            //Нужно для старых сбол
            string[] servers = nameServers.Split(splitter);
            for (int i = 0; i < servers.Length; i++)
            {
                eth.dnsServers.Add(new DnsServers());
                eth.dnsServers[i].Num = i + 1;
                eth.dnsServers[i].IP = servers[i];
            }
        }
        public void LoadInterfaces()
        {
            if (this.sshClient == null)
                return;

            this.interfacesList.Clear();
            //Получение списка Ethernet и WiFi интерфейсов
            String res = this.sshClient.ExecuteCommand("ls /sys/class/net | awk '/eth|wlan/ {print $0}'");
            String[] sList = res.Split(null);
            Thread.Sleep(100);
            //Получение настроек глобального IP
            GlobalEthsSettings = GetGLobalIP();
            //Дополнительные настройки
            LoadLanManualSettings("/opt/abak/A:/assembly/Speed_Duplex.config");
            foreach (string s in sList)
            {
                SetEthSettings(s);
            }

            LoadFromJson();

            //Пинг адресов
            LoadPingSettings();
        }
        private Dictionary<string, string[]> GetGLobalIP()
        {
            Dictionary<string, string[]> ethsSettings = new Dictionary<string, string[]>();
            Stream stream = this.sshClient.ReadFile(globalIPPath);
            if (stream == null)
                return ethsSettings;
            XPathDocument doc = new XPathDocument(stream);
            XPathNavigator nav = doc.CreateNavigator();

            foreach (XPathNavigator ruleNode in nav.Select("//eth"))
            {
                string[] ethSettings = new string[2];
                string name = $"eth{CXML.getAttributeValue(ruleNode, "num", "")}";
                ethSettings[0] = CXML.getAttributeValue(ruleNode, "address", "");
                ethSettings[1] = CXML.getAttributeValue(ruleNode, "status", "");
                ethsSettings.Add(name, ethSettings);
            }
            return ethsSettings;
        }
        private void UpdateXML(CEthernetInterface curEth)
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(decl, root);
            XmlNode rootNode = doc.CreateNode("element", "rules", "");
            doc.AppendChild(rootNode);
            List<CEthernetInterface> globalEths = new List<CEthernetInterface>();
            globalEths = interfacesList.Where(eth => GlobalEthsSettings.ContainsKey(eth.NetClass)
                         && eth.NetClass != curEth.NetClass).ToList();
            globalEths.Add(curEth);
            foreach (CEthernetInterface eth in globalEths)
            {
                //Правила сохраняются в xml файл
                XmlNode ruleNode = doc.CreateNode("element", "eth", "");
                //Номер eth порта
                CXML.SetAttributeValue(doc, ruleNode, "num", eth.NetClass.Remove(0, 3));
                //Адрес
                CXML.SetAttributeValue(doc, ruleNode, "address", eth.PrefixGLobalIP + eth.GLobalIP);
                //Маска
                CXML.SetAttributeValue(doc, ruleNode, "netmask", "255.255.255.255");
                //Вкл/Выкл
                CXML.SetAttributeValue(doc, ruleNode, "status", eth.IsGlobalIpOn.ToString());
                rootNode.AppendChild(ruleNode);
            }
            string command = $"ip addr del {GlobalEthsSettings[curEth.NetClass][0]}/32 dev {curEth.NetClass}";
            CAuxil.ExecuteSingleSSHCommand(command);
            Thread.Sleep(100);
            MemoryStream mem = new MemoryStream();
            doc.Save(mem);
            mem.Position = 0;
            const string path = "/tmp/globalip.xml";
            this.sshClient.WriteFile(path, mem);
        }
        /// <summary>
        /// Функция сканирует доступные WiFi сети
        /// </summary>
        public List<CWifiInfo> ScanWifi(CEthernetInterface eth)
        {
            List<CWifiInfo> wifiList = new List<CWifiInfo>();

            if (this.sshClient == null)
                return wifiList;

            //Отключение интерфейса
            this.sshClient.ExecuteCommand(String.Format("ifconfig {0} down", eth.NetClass));
            //Включение интерфейса
            this.sshClient.ExecuteCommand(String.Format("ifconfig {0} up", eth.NetClass));
            //Сканирование WiFi сетей
            this.sshClient.ExecuteCommand("connmanctl scan wifi");
            //Немного подождём
            Thread.Sleep(1000);
            //Получение списка WiFi сетей
            String services = this.sshClient.ExecuteCommand("connmanctl services | awk '/wifi_/ {{print $0}}'").Trim();

            //Формирование списка wifi сетей
            while (services.Length > 0)
            {
                //Здесь хитрожопый парсинг
                int index = services.IndexOf("wifi");

                CWifiInfo info = new CWifiInfo();

                //Получение имени wifi сети
                String name = services.Substring(0, index).TrimEnd();
                if (name.IndexOf('*') == 0)
                {
                    //Надо удалить запись типа *A или *AR перед именем
                    name = name.Remove(0, name.IndexOf(' ') + 1).Trim();
                }
                info.Name = name;

                //Получение строки в которой содержится SSID 
                services = services.Remove(0, index);
                index = services.IndexOf(' ');
                if (index == -1)
                    index = services.Length;
                String wifiString = services.Substring(0, index);
                //Из этой строки удаляется 'wifi_'
                wifiString = wifiString.Remove(0, wifiString.IndexOf('_') + 1);
                //Из этой строки удаляется 'mac_'
                wifiString = wifiString.Remove(0, wifiString.IndexOf('_') + 1);
                info.SSID = wifiString.Substring(0, wifiString.IndexOf('_'));
                //Определяется открытая сеть или закрытая
                if (wifiString.Contains("psk"))
                    info.Opened = false;
                else
                    info.Opened = true;

                //Переход к следующей сетке
                services = services.Remove(0, index).TrimStart();

                wifiList.Add(info);
            }

            return wifiList;
        }

        public void ResetInterfaceSettings(CEthernetInterface eth)
        {
            if (this.sshClient == null)
                return;

            CIniFile ini = this.LoadLanSettings(eth.MacAddress.Replace(":", ""));
            //Дополнительные настройки
            LoadLanManualSettings("/opt/abak/A:/assembly/Speed_Duplex.config");
            String section_name = String.Format("service_{0}", eth.MacAddress.Replace(":", ""));
            ini.RemoveSection(section_name);
            //Сохранение настроек в контроллер
            this.sshClient.WriteFile(lanConfig, ini.GetStream());
        }

        /// <summary>
        /// Функция дает команду на подключение к wifi сети
        /// </summary>
        public void ConnectWifi(CEthernetInterface eth)
        {
            if (this.sshClient == null)
                return;

            this.ChangeEthernetSettings(eth);

            //Команда на установку соединения
            String cmd = String.Format("connmanctl connect {0}", eth.FullWifiName);
            String res = this.sshClient.ExecuteCommand(cmd);
            if ((res == "") && (this.sshClient.LastError != ""))
                MessageBox.Show(this.sshClient.LastError, CGlobal.GetResourceValue("l_wifiConnectError"), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Функция дает команду на отключение от wifi сети
        /// </summary>
        public void DisconnectWifi(CEthernetInterface eth)
        {
            if (this.sshClient == null)
                return;

            //Команда на отключение от WiFi точки
            String cmd = String.Format("connmanctl disconnect {0}", eth.FullWifiName);
            String res = this.sshClient.ExecuteCommand(cmd);
            if ((res == "") && (this.sshClient.LastError != ""))
                MessageBox.Show(this.sshClient.LastError, CGlobal.GetResourceValue("l_wifiConnectError"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public void UpdateBackupSettings(CEthernetInterface eth)
        {
            const string path = "/opt/abak/A:/assembly/";
            //Тут настройки выбранного порта
            string cable = String.Format("ethernet_{0}_cable", eth.MacAddress.Replace(":", ""));
            string manualSettings = $"sudo ethtool -s {eth.NetClass} autoneg on speed {eth.Speed} " +
                $"duplex {eth.Duplex.ToLower()};\n";
            speedDuplex.WriteValue(eth.NetClass, "speed", eth.Speed);
            speedDuplex.WriteValue(eth.NetClass, "duplex", eth.Duplex);
            speedDuplex.WriteValue(eth.NetClass, "localGlobalIP", eth.LocalGlobalIP);
            speedDuplex.WriteValue(eth.NetClass, "externalNetwork", $"{eth.ExternalNetwork}/{eth.Prefix}");
            speedDuplex.WriteValue(eth.NetClass, "isStatic", eth.IsStatic.ToString());
            //Тут все остальные
            var notSelectedEthList = interfacesList.Where(ethInList => eth.NetClass != ethInList.NetClass).ToList();
            foreach (CEthernetInterface ethernet in notSelectedEthList)
            {
                speedDuplex.WriteValue(ethernet.NetClass, "speed", ethernet.Speed);
                speedDuplex.WriteValue(ethernet.NetClass, "duplex", ethernet.Duplex);
                speedDuplex.WriteValue(ethernet.NetClass, "localGlobalIP", ethernet.LocalGlobalIP);
                speedDuplex.WriteValue(ethernet.NetClass, "isStatic", ethernet.IsStatic.ToString());
                speedDuplex.WriteValue(ethernet.NetClass, "externalNetwork", $"{ethernet.ExternalNetwork}/{ethernet.Prefix}");
                cable = String.Format("ethernet_{0}_cable", ethernet.MacAddress.Replace(":", ""));
                manualSettings += $"sudo ethtool -s {ethernet.NetClass} autoneg on speed {ethernet.Speed} " +
                $"duplex {ethernet.Duplex.ToLower()};\n";
            }
            var stream = CAuxil.StringToStream(manualSettings);
            sshClient.WriteFile($"{path}writeEth.sh", stream);
            sshClient.WriteFile($"{path}Speed_Duplex.config", speedDuplex.GetStream());
            sshClient.ExecuteCommand($"chmod -R 755 {path}writeEth.sh;");
            UpdateLocalGlobalIP(eth);
        }
        private void UpdateRoute(CEthernetInterface selectedEth)
        {
            //Ссылка для понимания почему 101
            //https://timeweb.com/ru/community/articles/nastroyka-neskolkih-tablic-marshrutizacii-na-odnom-servere
            const string path = "/opt/abak/A:/assembly/";
            sshClient.ExecuteCommand($"mkdir -p {path}/route");
            foreach (CEthernetInterface eth in interfacesList)
            {
                if (eth.NetClass == selectedEth.NetClass)
                {
                    eth.Gateway = selectedEth.Gateway;
                    eth.GLobalIP = selectedEth.GLobalIP;
                    eth.ExternalNetwork = selectedEth.ExternalNetwork;
                    eth.IsGlobalIpOn = selectedEth.IsGlobalIpOn;
                    eth.IsStatic = selectedEth.IsStatic;
                    eth.Prefix = selectedEth.Prefix;
                }
                string masterRoute = "";
                if (eth.NetClass == "eth1")
                {
                    var eth2 = interfacesList.FirstOrDefault(x => x.NetClass == "eth2");
                    if (eth2 != null && eth2.ExternalNetwork != "")
                    {
                        masterRoute += $"ip route del {eth2.ExternalNetwork}/{eth2.Prefix} via {eth2.Gateway} dev eth2 table 10;\n";
                    }
                }

                if (eth.NetClass == "eth2")
                {
                    var eth1 = interfacesList.First(x => x.NetClass == "eth1");
                    if (eth1.ExternalNetwork != "")
                    {
                        masterRoute += $"ip route del {eth1.ExternalNetwork}/{eth1.Prefix} via {eth1.Gateway} dev eth1 table 10;\n";
                    }
                }
                if (eth.ExternalNetwork != "")
                {
                    masterRoute += $"ip route add {eth.ExternalNetwork}/{eth.Prefix} via {eth.Gateway} dev {eth.NetClass} table 10;\n";
                    masterRoute += $"ip rule add from all table 10;\n";
                }
                var mStream = CAuxil.StringToStream(masterRoute);
                sshClient.WriteFile($"{path}route/{eth.NetClass}", mStream);
                sshClient.ExecuteCommand($"chmod -R 755 {path}route/{selectedEth.NetClass}");

            }

        }
        private void UpdateLocalGlobalIP(CEthernetInterface selectedEth)
        {
            const string path = "/opt/abak/A:/assembly/";
            string globalIpUp = "";
            string globalIpDown = "";
            foreach (CEthernetInterface eth in interfacesList)
            {
                if (eth.NetClass == selectedEth.NetClass)
                {
                    eth.Gateway = selectedEth.Gateway;
                    eth.GLobalIP = selectedEth.GLobalIP;
                    eth.LocalGlobalIP = selectedEth.LocalGlobalIP;
                }
                if (eth.IsGlobalIpOn && eth.LocalGlobalIP != "")
                {
                    string localGlobalIP = eth.PrefixGLobalIP + eth.LocalGlobalIP;
                    globalIpUp += $"ifconfig {eth.NetClass}:1 {localGlobalIP} netmask 255.255.255.255;\n";
                    globalIpUp += $"arping -c 2 -b -S {localGlobalIP} {localGlobalIP};";
                    globalIpDown += $"ip addr del {localGlobalIP}/32 dev {eth.NetClass}:1;\n";
                }
            }
            var stream = CAuxil.StringToStream(globalIpUp);
            sshClient.WriteFile($"{path}writeGlobalIP.sh", stream);
            sshClient.ExecuteCommand($"chmod -R 755 {path}writeGlobalIP.sh;");

            stream = CAuxil.StringToStream(globalIpDown);
            sshClient.WriteFile($"{path}DeleteGlobalIP.sh", stream);
            sshClient.ExecuteCommand($"chmod -R 755 {path}DeleteGlobalIP.sh;");
        }
        public string UpdateDirectly(ref CEthernetInterface eth, string path)
        {
            string manualSettings = $"sudo ethtool -s {eth.NetClass} autoneg on speed {eth.Speed} " +
                $"duplex {eth.Duplex.ToLower()};";
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.NewLine = "\n";
            String line;
            //Заголовок файла
            string cable = String.Format("ethernet_{0}_cable", eth.MacAddress.Replace(":", ""));
            line = $"[{cable}]";
            writer.WriteLine(line);
            //Название сети
            writer.WriteLine("Name=Wired");
            //Авто подключение
            writer.WriteLine("AutoConnect=true");
            //DHCP
            if (eth.DHCP)
                writer.WriteLine("IPv4.method=dhcp");
            else
            //Статический IP адрес
            {
                writer.WriteLine("IPv4.method=manual");
                //IP адрес
                if (eth.IP != "")
                {
                    line = String.Format("IPv4.local_address={0}", eth.IP);
                    writer.WriteLine(line);
                }
                //Маска подсети
                if (eth.NetMask != "")
                {
                    UInt32 mask = this.GetPreffixLen(eth.NetMask);
                    line = String.Format("IPv4.netmask_prefixlen={0}", mask);
                    writer.WriteLine(line);
                }
                //Шлюз
                if (eth.Gateway != "")
                {
                    line = String.Format("IPv4.gateway={0}", eth.Gateway);
                    writer.WriteLine(line);
                }
                //Nameservers
                string dnsCMD = "";
                foreach (var dnsServer in eth.dnsServers)
                {
                    if (dnsServer.IP == "")
                        continue;

                    dnsCMD += dnsServer.IP + ";";
                }
                if (dnsCMD != "")
                    writer.WriteLine($"Nameservers={dnsCMD}");
            }
            writer.Flush();
            this.sshClient.WriteFile(path + eth.NetClass, writer.BaseStream);
            string cmd = $"\nsleep 5;\nifconfig {eth.NetClass} down;\nsleep 1;" +
                $"\ncp -r {path + eth.NetClass} /var/lib/connman/{cable}/settings;" +
                $"\nifconfig {eth.NetClass} up;\n{manualSettings}\nsleep 1;";
            return cmd;
        }

        private CIniFile GetDataForFile(ref CEthernetInterface eth)
        {
            CIniFile ini = this.LoadLanSettings("/var/lib/connman/lan.config");

            String section_name = String.Format("service_{0}", eth.MacAddress.Replace(":", ""));
            ini.RemoveSection(section_name);

            ini.WriteValue(section_name, "MAC", eth.MacAddress);
            //Настройки IP адреса
            if (eth.DHCP)
                ini.WriteValue(section_name, "IPv4", "dhcp");
            else
            {
                if (eth.Gateway != "")
                    ini.WriteValue(section_name, "IPv4", String.Format("{0}/{1}/{2}", eth.IP, eth.NetMask, eth.Gateway));
                else
                    ini.WriteValue(section_name, "IPv4", String.Format("{0}/{1}", eth.IP, eth.NetMask));
                //ДНС сервера
                //Если DNS сервер не задан то секция Nameservers удаляется
                if (eth.dnsServers.Count() != 0)
                    SetDNS(ref eth, ref ini, section_name);
            }
            //Тип интерфейса
            if (eth.WiFi)
                SetWifi(ref eth, ref ini, section_name);
            else
                ini.WriteValue(section_name, "Type", "ethernet");

            //Режим управления скростью
            if (eth.ManualSpeed && (eth.Speed != "") && (eth.Duplex != ""))
            {
                ini.WriteValue(section_name, "speed", eth.Speed);
                ini.WriteValue(section_name, "duplex", eth.Duplex);
            }

            return ini;
        }
        private string UpdateThroughFile(ref CEthernetInterface eth)
        {
            CIniFile ini = GetDataForFile(ref eth);

            const string tmpLanConfig = "/tmp/lan.config";
            string cmd = $"cat {tmpLanConfig} > /var/lib/connman/lan.config;" +
                $"\nifconfig {eth.NetClass} {eth.IP} " +
                $"netmask {eth.NetMask} broadcast {eth.Gateway};";
            this.sshClient.WriteFile(tmpLanConfig, ini.GetStream());
            if (!eth.DHCP)
                cmd += $"ifconfig {eth.NetClass} {eth.IP} " +
                    $"netmask {eth.NetMask} broadcast {eth.Gateway};";

            return cmd;
        }
        //Special file for dns for connman
        public void ChangeResolveFile(CEthernetInterface selectedEth)
        {
            var notSelectedEthList = interfacesList.Where(ethInList => selectedEth.NetClass != ethInList.NetClass).ToList();
            sshClient.ExecuteCommand("> /run/connman/resolv.conf");
            string dnsCMD = "";
            List<string> dnsServersList = new List<string>();
            //DNS изменяемого порта
            foreach (DnsServers dnsServer in selectedEth.dnsServers)
            {
                if (dnsServersList.Contains(dnsServer.IP) || dnsServer.IP == "")
                    continue;

                dnsServersList.Add(dnsServer.IP);
                dnsCMD += "nameserver " + dnsServer.IP + "\n";
            }
            //DNS остальных
            foreach (CEthernetInterface eth in notSelectedEthList)
            {
                foreach (DnsServers dnsServer in eth.dnsServers)
                {
                    if (dnsServersList.Contains(dnsServer.IP) || dnsServer.IP == "")
                        continue;

                    dnsServersList.Add(dnsServer.IP);
                    dnsCMD += "nameserver " + dnsServer.IP + "\n";
                }
            }
            if (dnsCMD.Length == 0)
                return;

            var stream = CAuxil.StringToStream(dnsCMD);
            sshClient.WriteFile("/run/connman/resolv.conf", stream);
        }
        public int ChangeEthernetSettings(CEthernetInterface eth)
        {
            if (this.sshClient == null)
                return -1;

            string cmd;
            if (CGlobal.CurrState.AssemblyInt >= 520)
                cmd = UpdateDirectly(ref eth, $"/tmp/");
            else
                cmd = UpdateThroughFile(ref eth);

            //Нужно отключать, иначе начинаются сбои
            if (GlobalEthsSettings.ContainsKey(eth.NetClass))
                UpdateXML(eth);

            cmd += "\ncp -r /tmp/globalip.xml /opt/abak/A:/assembly/;";
            bool isTheSame = IsEthForConnection(ref eth);
            UpdateRoute(eth);
            var stream = CAuxil.StringToStream(cmd);
            this.sshClient.WriteFile("/tmp/writeEth.sh", stream);
            cmd = "chmod -R 755 /tmp/writeEth.sh";
            if (!isTheSame)
                cmd += ";/tmp/writeEth.sh;";

            this.sshClient.ExecuteCommand(cmd);

            return Convert.ToInt32(isTheSame);
        }
        //DNS
        private void SetDNS(ref CEthernetInterface eth, ref CIniFile ini, string section_name)
        {
            string dnsServers = "";
            List<DnsServers> servers = eth.dnsServers.Where(x => x.IP != ""
                && x.IP != null).ToList();
            foreach (DnsServers server in servers)
                dnsServers += server.IP + ",";

            if (dnsServers == "")
                return;
            //Удаляется последняя запятая
            dnsServers = dnsServers.Remove(dnsServers.Length - 1);
            ini.WriteValue(section_name, "Nameservers", dnsServers);
        }
        //Wifi
        private void SetWifi(ref CEthernetInterface eth, ref CIniFile ini, string section_name)
        {
            ini.WriteValue(section_name, "Type", "wifi");
            ini.WriteValue(section_name, "Security", "psk");
            ini.WriteValue(section_name, "Name", eth.AccessPoint);
            ini.WriteValue(section_name, "SSID", eth.SSID);
            ini.WriteValue(section_name, "Passphrase", eth.Password);
            if (eth.AutoStart)
                ini.WriteValue(section_name, "Mode", "auto");
            else
                ini.WriteValue(section_name, "Mode", "");
        }
        public void UpdateSettingsAfterClosing()
        {
            this.sshClient.ExecuteCommand("/tmp/writeEth.sh &");
        }

        //Проверяет если через этот порт подключились к ЦПУ
        private bool IsEthForConnection(ref CEthernetInterface curEth)
        {
            if (SaveIP.GetUSB() == true)
                return false;

            foreach (CEthernetInterface eth in interfacesList)
            {
                bool isTheSame = eth.NetClass == curEth.NetClass && eth.IP == SaveIP.GetIP();
                if (isTheSame)
                    return true;
            }
            return false;
        }
    }
    public class DnsServers
    {
        private bool isChanged = false;
        private int num;
        private string ip;
        public int Num { get => num; set => num = value; }
        public bool IsChanged { get => isChanged; set => isChanged = value; }
        public string IP
        {
            get => ip;
            set
            {
                isChanged = ip != value;
                ip = value;
            }
        }
    }

    public class StaticRoute
    {
        private bool isChanged = false;
        private int num;
        private string from;
        private string to;

        public StaticRoute(int num, string from, string to)
        {
            this.num = num;
            this.from = from;
            this.to = to;
        }

        public int Num { get => num; set => num = value; }
        public bool IsChanged { get => isChanged; set => isChanged = value; }
        public string From
        {
            get => from;
            set
            {
                isChanged = from != value;
                from = value;
            }
        }
        public string To
        {
            get => to;
            set
            {
                isChanged = to != value;
                to = value;
            }
        }
    }
}
