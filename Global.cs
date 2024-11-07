using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using AbakConfigurator.Secure;
using AbakConfigurator.Secure.Data;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс описывающий сессию
    /// </summary>


    public class CSession : IDisposable
    {
        private bool isFirstStart = true;
        private String user = "";
        private String password = "";
        private string sshUser = "";
        private string sshPassword = "";
        private String ip = "192.168.3.100";
        //Флаг сброса сессии из меню 
        public bool flagResetSession = false;
        //Флаг 
        public bool flagStartSession = false;
        //Соединение с базой
        private NpgsqlConnection connection = null;
        //SSH клиент
        private CSSHClient sshClient = null;
        //Cписок для хранения сессий контроллеров
        private List<Autentification> sessions = new List<Autentification>();
      
        /// <summary>
        /// Функция возвращает список сессий данного контроллера
        /// </summary>
        public List<Autentification> GetSessions
        {
            get
            {
                return this.sessions;
            }

        }

        /// <summary>
        /// Функция устанавливает параметры подключения
        /// </summary>
        /// <param name="login"></param>
        /// <param name="pass"></param>
        /// <param name="ip"></param>
        public void setConnectParams(string login = "", string pass = "", string ip = "192.168.3.100")
        {
            this.user = login;
            this.password = pass;
            this.ip = ip;

        }
        /// <summary>
        /// Удаление сессий контроллера
        /// </summary>
        public void DeleteCurentSession()
        {
            List<Autentification> tempList = new List<Autentification>();
            this.sessions = tempList;
        }

        ///<summary>
        ///Функция запоминает параметры авторизации в классе
        ///</summary>
        public void RememberSessions(Autentification cs, CSettings settings, String user, String password)
        {
            cs.ip = this.ip = settings.ConnectIP;
            cs.user = this.user = user;
            cs.password = this.password = password;
            if (CGlobal.Settings.USB)
                cs.usb = "USB";
            else
                cs.usb = "Ethernet";
        }

        /// <summary>
        /// Функция проверяет на совпадение параметров сессии в списке. 
        /// </summary>
        /// <param name="sessions"></param>
        /// <returns></returns>
        public bool IsSameSessions(List<Autentification> sessions, CSettings settings, string user = "")
        {
            bool flagFind = false;

            foreach (Autentification s in sessions)
            {
                if (s.ip == settings.ConnectIP & string.Equals(s.user, user))
                {
                    flagFind = true;
                    ip = s.ip;
                    user = s.user;
                    password = s.password;
                }
            }

            return flagFind;
        }

        /// <summary>
        /// Сброс сессии для того, чтобы показать окно с паролем.
        /// </summary>
        public void ResetSession()
        {
            this.password = "";
            this.user = "";
        }
        public string SshPassword
        {
            get => sshPassword;
        }
        public string SshUser
        {
            get => sshUser;
        }
        public String Password
        {
            get
            {
                return this.password;
            }
        }

        public String User
        {
            get
            {
                return this.user;
            }
        }

        /// <summary>
        /// Функция создаёт и открывает SQL соединение
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public NpgsqlConnection CreateSQLConnection(String user, String password, 
            bool silent = false, bool isFirtstAttempt = false, int commandTimeout = 30)
        {           
            //Нсатройка соединения
            NpgsqlConnectionStringBuilder connBuilder = new NpgsqlConnectionStringBuilder();
            connBuilder.Database = "abak";
            if (CheckPing() != "")
                return null;

            connBuilder.Host = CSettings.GetSettings().ConnectIP;
            connBuilder.Port = CSettings.GetSettings().ConnectDBPort;
            connBuilder.Username = user;
            connBuilder.Password = password;
            connBuilder.Pooling = false;
            connBuilder.CommandTimeout = commandTimeout;
            connBuilder.Timeout = 5;
            //connBuilder.conne
            NpgsqlConnection conn = new NpgsqlConnection(connBuilder.ConnectionString);
            try
            {
                conn.Open();
                PrepareSqlFunctions(conn);
                return conn;
            }
            catch (Exception e)
            {
                this.password = "admin";
                if (isFirtstAttempt)
                    return CreateSQLConnection("admin", "admin");

                if (!silent)
                {
                    String mess = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_dbConnectError"), e.Message);
                    MessageBox.Show(mess, "createConnection", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return null;
            }
        }
        
        public string CheckPing()
        {
            string ip = SaveIP.GetIP();
            Ping ping = new Ping();
            try
            {
                PingReply pingreply = ping.Send(ip, 1000);
                if (pingreply.Status == IPStatus.Success)
                    return "";

                string message;
                if (ip == "192.168.7.2")
                    message = "Не удалось подключиться к контроллеру через USB-порт";
                else
                    message = $"Контроллер с адресом '{ip}' не найден";

                return message;
            }
            catch
            {
                return "Не удалось подключиться к контроллеру";
            }
        }


        /// <summary>
        /// Закрывает SQl сеодинение
        /// </summary>
        public void CloseSQL()
        {
            if (this.connection != null)
            {
                this.connection.Close();
                this.connection = null;

            }
        }

        /// <summary>
        /// Открывает соединение используя текущие имя пользователя и пароль
        /// </summary>
        /// <returns></returns>
        public Boolean OpenSQL()
        {
            this.connection = this.CreateSQLConnection(user, password);
            return this.connection != null;
        }

        /// <summary>
        /// Закрывает SQL и SSH соединения с контроллером
        /// </summary>
        public void CloseSession()
        {
            if (this.sshClient != null)
            {
                this.sshClient.Disconnect();
                this.sshClient = null;
            }
            this.CloseSQL();
        }

        /// <summary>
        /// Открывает SSH соединение с контроллером
        /// Логин и пароль пользователя Linux берутся из базы контроллера
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// 

        public CSSHClient OpenSSHConnection(NpgsqlConnection connection)
        {
            String sql = String.Format("select r.login, r.password from user_roles r inner join users u on r.code=u.role where (u.name='{0}' and u.password='{1}') limit 1", CGlobal.DBUser, CGlobal.DBPassword);
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                reader.Close();
                this.CloseSession();
                return null;
            }

            //Вроде удачно, получение имени пользователя и пароль linux
            reader.Read();
            sshUser = reader["login"].ToString();
            sshPassword = reader["password"].ToString();
            reader.Close();

            //Пароль для SSH получили, открывается SSH канал
            CSSHClient sshClient = new CSSHClient(CSettings.GetSettings().ConnectIP, CSettings.GetSettings().ConnectSSHport, sshUser, sshPassword);
            //if (!sshClient.Connect())
            //    return null;

            return sshClient;
        }

        /// <summary>
        /// Функция открывает сессию с контроллером
        /// </summary>
        /// <returns>буль</returns>
        public bool OpenSession(bool showMsg = false, bool isTheSame = false)
        {
            isFirstStart = true;
            string message = "Ошибка при подключении к базе данных: ";
            //Получение пароля SSH
            try
            {
                this.connection = this.CreateSQLConnection(CGlobal.DBUser, CGlobal.DBPassword, false, true);
                if (this.connection == null)
                    return false;
                
                Thread.Sleep(100);
                message = "Ошибка при подключении к системе: ";
                this.sshClient = this.OpenSSHConnection(this.connection);
                if (this.sshClient == null)
                {
                    this.CloseSession();
                    this.ResetSession();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!showMsg)
                    return false;

                MessageBox.Show(message + ex.Message, "OpenSession", MessageBoxButton.OK, MessageBoxImage.Error);
                this.CloseSession();
                this.ResetSession();
                return false;
            }
        }

        public void Dispose()
        {
            this.CloseSession();
        }
        private void PrepareSqlFunctions(NpgsqlConnection connection)
        {
            try
            {
                string s = "prepare update_fast(text, int) as update fast_table set value=$1, changed=true where id=$2";
                NpgsqlCommand cmd = new NpgsqlCommand(s, connection);
                cmd.CommandTimeout = 2;
                cmd.ExecuteNonQuery();

                s = "prepare select_fast(int) as select tag, value from fast_table where id=$1 limit 1";
                cmd = new NpgsqlCommand(s, connection);
                cmd.CommandTimeout = 2;
                cmd.ExecuteNonQuery();

                cmd = new NpgsqlCommand("LISTEN EMPTY_MODULES_UPDATED; " +
                    "LISTEN EMPTY_MODULES; LISTEN ADD_MODULE; LISTEN REMOVE_MODULE; " +
                    "LISTEN UPDATE_MODULE_STATE; LISTEN MODULE_VALUES_CHANGE; " +
                    "LISTEN MODULE_UPDATE_IMAGE_STATE", connection);
                cmd.ExecuteNonQuery();

                cmd = new NpgsqlCommand("prepare write_cmd(text, text, int) " +
                    "as insert into fast_can_write_tag_table(tag, value, node_id) values($1, $2, $3)",
                    connection);
                cmd.ExecuteNonQuery();
            }
            catch
            {

            }
        }
        public NpgsqlConnection Connection
        {
            get
            {
                if (connection == null)
                    connection = CreateSQLConnection(CGlobal.DBUser, CGlobal.DBPassword, true, false, 5);
                
                return this.connection;
            }
        }
        public CSSHClient SSHClient
        {
            get
            {
                if (this.sshClient == null)
                {
                    this.sshClient = this.OpenSSHConnection(Connection);
                    if (this.sshClient == null)
                    {
                        this.CloseSession();
                        this.ResetSession();

                        return null;
                    }
                }
                return this.sshClient;
            }
        }
    }

    /// <summary>
    /// Класс в котором производятся действия не относящиеся к конкретному классу
    /// </summary>
    public class CGlobal
    {
        /// <summary>
        /// Языки прилоржения
        /// </summary>
        //Русский язык
        public const String RU = "ru-RU";
        //Английский язык
        public const String EU = "en-US";
        //Режим наладчика
        private static bool naladchikStyle = false;
        //Путь к папке abak в контроллере
        public const String AbakPath = "/opt/abak/A:";
        //Класс текущего состояния
        private static CCurrentState currState;
        //Класс для поиска контроллеров в сети
        private static CBeckFinder beckFinder = new CBeckFinder();
        //Ресурсы приложения
        private static ResourceDictionary resources = null;
        //Сессия приложения
        private static CSession session = new CSession();
        //Конфигурация прибора
        private static CConfig config = new CConfig();

        public const string DBUser = "admin";
        public const string DBPassword = "amigoCOLORADOS";

        public static SecureHandler Handler { get; set; }

        /// <summary>
        /// Класс поиска контроллеров в сети Ethernet
        /// </summary>

        /// <summary>
        /// Режим наладчика
        /// </summary>
        public static Boolean NaladchikStyle
        {
            get => naladchikStyle;
            set => naladchikStyle = value;
        }
        public static CBeckFinder BeckFinder
        {
            get
            {
                return beckFinder;
            }
        }
        /// <summary>
        /// Конфигурация прибора
        /// </summary>
        public static CConfig Config => config;


        /// <summary>
        /// Настройки приложения
        /// </summary>
        public static CSettings Settings
        {
            get
            {
                return CSettings.GetSettings();
            }
        }

        /// <summary>
        /// Описание текущей сесси приложения
        /// </summary>
        public static CSession Session
        {
            get
            {
                return session;
            }
        }

        public static CCurrentState CurrState
        {
            get
            {
                return currState;
            }
            set
            {
                currState = value;
            }
        }


        /// <summary>
        /// Флаг работы с ИВК Абак+
        /// </summary>
        public static Boolean IsIVKAbak
        {
            get;
            set;
        }

        public static ResourceDictionary Resources
        {
            get
            {
                if (resources == null)
                {
                    resources = new ResourceDictionary();
                    switch (CGlobal.Settings.Language)
                    {
                        case CGlobal.RU:
                            resources.Source = new Uri("Resources/lang.ru-RU.xaml", UriKind.Relative);
                            break;

                        //По умолчанию используется английский
                        default:
                            resources.Source = new Uri("Resources/lang.xaml", UriKind.Relative);
                            break;
                    }
                }
                return resources;
            }
            set
            {
                resources = null;
            }
        }

        public static String GetResourceValue(String resName)
        {
            String str = (String)CGlobal.Resources[resName];
            return str.Replace("\\n\\r", "\n\r");
        }
    }

    /// <summary>
    /// Класс список доступных скоростей обмена
    /// </summary>
    public class CBaudRates
    {
        private List<UInt32> baudRates = new List<UInt32>();

        public CBaudRates()
        {
            this.baudRates.Add(1200);
            this.baudRates.Add(2400);
            this.baudRates.Add(4800);
            this.baudRates.Add(9600);
            this.baudRates.Add(19200);
            this.baudRates.Add(38400);
            this.baudRates.Add(57600);
            this.baudRates.Add(115200);
        }

        public List<UInt32> BaudRates
        {
            get
            {
                return this.baudRates;
            }
        }
    }

    public class MenuState : INotifyPropertyChanged
    {
        bool m_Management = false;
        bool m_Control = false;
        bool m_RemoteStart = false;
        bool m_RemoteShutdown = false;
        bool m_RemoteRestart = false;
        bool m_RemoteReboot = false;
        bool m_RemoteCodesysApp = false;
        bool m_RemoteCodesysIDE = false;

        public void Refresh()
        {
            if (CGlobal.Handler == null || !CGlobal.Handler.Auth.Authorized)
            {
                Management = false;
                Control = false;

                return;
            }

            var rule_repo = (RuleData)CGlobal.Handler.Repo["Rule"];

            Management = CGlobal.Handler.Auth.GroupType <= GroupTypeEnum.Administrator;
            Control = CGlobal.Handler.Auth.GroupType <= GroupTypeEnum.Moderator;

            RemoteRestart = rule_repo.GetRuleValueBool("remote_restart");
            RemoteReboot = rule_repo.GetRuleValueBool("remote_reboot");
            RemoteCodesysApp = rule_repo.GetRuleValueBool("remote_codesys_app");
        }

        public bool Management
        {
            get => m_Management;
            set
            {
                m_Management = value;
                OnPropertyChanged(nameof(Management));
            }
        }

        public bool Control
        {
            get => m_Control;
            set
            {
                m_Control = value;
                OnPropertyChanged(nameof(Control));
            }
        }

        public bool RemoteStart
        {
            get => m_RemoteStart;
            set
            {
                m_RemoteStart = value;
                OnPropertyChanged(nameof(RemoteStart));
            }
        }

        public bool RemoteShutdown
        {
            get => m_RemoteShutdown;
            set
            {
                m_RemoteShutdown = value;
                OnPropertyChanged(nameof(RemoteShutdown));
            }
        }

        public bool RemoteRestart
        {
            get => m_RemoteRestart;
            set
            {
                m_RemoteRestart = value;
                OnPropertyChanged(nameof(RemoteRestart));
            }
        }

        public bool RemoteReboot
        {
            get => m_RemoteReboot;
            set
            {
                m_RemoteReboot = value;
                OnPropertyChanged(nameof(RemoteReboot));
            }
        }

        public bool RemoteCodesysApp
        {
            get => m_RemoteCodesysApp;
            set
            {
                m_RemoteCodesysApp = value;
                OnPropertyChanged(nameof(RemoteCodesysApp));
            }
        }

        public bool RemoteCodesysIDE
        {
            get => m_RemoteCodesysIDE;
            set
            {
                m_RemoteCodesysIDE = value;
                OnPropertyChanged(nameof(RemoteCodesysIDE));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CCurrentState : INotifyPropertyChanged, IValueConverter
    {
        /// <summary>
        /// Типы доступных ролей
        /// none = 0
        /// connected = 1
        /// running = 2
        /// </summary>
        [Flags]
        public enum StateFlags
        {
            none = 0,
            connected = 1,
            running = connected << 1,
        };

        private Boolean isConnected = false;
        private Boolean isRunning = false;
        private String configPath = "";
        private SolidColorBrush greenColor = Brushes.Gray;
        private SolidColorBrush redColor = Brushes.Red;
        private StateFlags currentStateFlags = StateFlags.none;
        private String serial = "";
        private String caption = "";
        private string modulesCollectionVer = "";
        private Boolean? wdt_on = null;
        private Boolean update_on = false;
        private String ip = "";
        //Два поля по которым определяется версия сборки
        private UInt16 assembly_hi = 0;
        private UInt16 assembly_lo = 0;
        private UInt16 assembly_mid = 0;
        private UInt16 assemblyInt = 0;
        //Флаг авторизации на контроллере через окно сессий
        public bool flagSessionWindows = false;
        public bool assembly_for_usb = false;
        private string updatedmodules = "";
        private bool isRockChip = false;
        private float plcversioninfo = 0;
        public double AbakIoVers = 0;
        private int modulesamount = 0;
        public MenuState Menu { get; set; } = new MenuState();

        public void ProcessAssemblyString(String assemblyString)
        {
            Array s = assemblyString.Split('.');
            Dictionary<int, Delegate> SetAssembly = new Dictionary<int, Delegate>();
            SetAssembly.Add(2, new Action<Array>(OldVersion));
            SetAssembly.Add(3, new Action<Array>(NewVersion));
            try
            {
                SetAssembly[s.Length].DynamicInvoke(s);
            }
            catch
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_InvalidAssemblyString"), "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void NewVersion(Array s)
        {
            string numb = "";
            foreach (string c in s)
            {
                if (c.Length == 1)
                {
                    numb += c;
                }

                if (c.Length > 1)
                {
                    numb += c.Remove(1, c.Length - 1);
                }
            }  
            assemblyInt = System.Convert.ToUInt16(numb);
            this.AssemblyHi = System.Convert.ToUInt16(s.GetValue(0));
            this.AssemblyMid = System.Convert.ToUInt16(s.GetValue(1));
            this.AssemblyLo = System.Convert.ToUInt16(s.GetValue(2));
        }

        private void OldVersion(Array s)
        {
            assemblyInt = System.Convert.ToUInt16((string)s.GetValue(0)
                + (string)s.GetValue(1));
            this.AssemblyHi = System.Convert.ToUInt16(s.GetValue(0));
            this.AssemblyLo = System.Convert.ToUInt16(s.GetValue(1));
        }
        public bool IsRockChip => isRockChip;
        public float PLCVersionInfo
        {
            get
            {
                return this.plcversioninfo;
            }
            set
            {
                if (this.plcversioninfo != value)
                {
                    this.plcversioninfo = value;
                    this.OnPropertyChanged("PLCVersionInfo");
                }
            }
        }

        public int ModulesAmount
        {
            get => modulesamount;
            set
            {
                if (this.modulesamount != value)
                {
                    this.modulesamount = value;
                    this.OnPropertyChanged("ModulesAmount");
                }
            }
        }

        public bool PlcTypeReader()
        {

            try
            {
                Stream stream = CGlobal.Session.SSHClient.ReadFile("/opt/abak/A:/assembly/abak_version.config");
                if (stream == null)
                {
                    return false;
                }
                StreamReader sr = new StreamReader(stream);
                JObject jObject = JObject.Parse(sr.ReadToEnd());
                string assembly = jObject["assembly"].ToString();
                string cpuType = jObject["cpu_type"].ToString();
                string configuratorVer = jObject["confrigurator_version"].ToString();
                if (!ConfiguratorCheck(configuratorVer))
                {
                    return false;
                }
                AbakIoCheck(assembly);
                isRockChip = cpuType.Contains("k31r") ? true : false;
            }
            catch 
            {
                return false;
            }
            return true;
        }

        private void AbakIoCheck(string version)
        {
            string[] str = version.Split('.');
            if (str.Length == 3)
                AbakIoVers = System.Convert.ToDouble(CAuxil.AdaptFloat(str[0] + str[1] + str[2]));
        }

        private bool ConfiguratorCheck(string version)
        {
            List<int> ver_in_file = new List<int>();
            foreach (string str in version.Split('.'))
                ver_in_file.Add(System.Convert.ToInt32(str));

            if (ver_in_file.Count != 3)
                return true;

            List<int> cgr_ver = new List<int>();
            foreach (string str in FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion.Split('.'))
                cgr_ver.Add(System.Convert.ToInt32(str));

            bool is_compatible = cgr_ver[0] >= ver_in_file[0] && cgr_ver[1] >= ver_in_file[1] && cgr_ver[2] >= ver_in_file[2];
            if (is_compatible)
                return true;

            string message = $"Для использования контроллера необходим конфигуратор версии '{version}' или выше";
            MessageBox.Show(message, "Ошибка версии!", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }
   
        /// <summary>
        /// Флаг наличия связи с контроллером
        /// </summary>
        public Boolean IsConnected
        {
            get
            {
                return this.isConnected;
            }
            set
            {
                if (this.isConnected != value)
                {
                    this.isConnected = value;
                    if (this.isConnected)
                        this.CurrentStateFlags |= StateFlags.connected;
                    else
                        this.CurrentStateFlags &= ~StateFlags.connected;
                    this.OnPropertyChanged("IsConnected");
                }
            }
        }

        /// <summary>
        /// Флаг того что запущен опрос
        /// </summary>
        public Boolean IsRunning
        {
            get
            {
                return this.isRunning;
            }
            set
            {
                if (this.isRunning != value)
                {
                    this.isRunning = value;
                    if (this.isConnected)
                        this.CurrentStateFlags |= StateFlags.running;
                    else
                        this.CurrentStateFlags &= ~StateFlags.running;
                    this.OnPropertyChanged("IsRunning");
                }
            }
        }
        public StateFlags CurrentStateFlags
        {
            get
            {
                return this.currentStateFlags;
            }
            set
            {
                if (this.currentStateFlags != value)
                {
                    this.currentStateFlags = value;
                    this.OnPropertyChanged("CurrentStateFlags");
                }
            }
        }

        public String ConfigPath
        {
            get
            {
                return this.configPath;
            }
            set
            {
                if (this.configPath != value)
                {
                    this.configPath = value;
                    this.OnPropertyChanged("ConfigPath");
                }
            }
        }

        public SolidColorBrush GreenColor
        {
            get
            {
                return this.greenColor;
            }
            set
            {
                this.greenColor = value;
                this.OnPropertyChanged("GreenColor");
            }
        }

        public String IP
        {
            get
            {
                if (flagSessionWindows == true)
                {
                    if (this.ip == "192.168.7.2")
                        return "USB";
                    else
                        return "IP:" + this.ip;
                }
                if (CGlobal.Settings.USB)
                    return "USB";
                else
                    return "IP:" + this.ip;
            }
            set
            {
                this.ip = value;
                this.OnPropertyChanged("IP");
            }
        }

        public SolidColorBrush RedColor
        {
            get
            {
                return this.redColor;
            }
            set
            {
                this.redColor = value;
                this.OnPropertyChanged("RedColor");
            }
        }

        /// <summary>
        /// Серийный номер контроллера
        /// </summary>
        public String Serial
        {
            get
            {
                return String.Format("{0}: {1}", CGlobal.GetResourceValue("l_serialColumn"), this.serial);
            }
            set
            {
                if (this.serial != value)
                {
                    this.serial = value;
                    this.OnPropertyChanged("Serial");
                }
            }
        }

        public String UpdatedModules
        {
            get
            {
                return this.updatedmodules;
            }
            set
            {
                if (this.updatedmodules != value)
                {
                    this.updatedmodules = value;
                    this.OnPropertyChanged("UpdatedModules");
                }
            }
        }

        /// <summary>
        /// Описание контроллера
        /// </summary>
        public String Caption
        {
            get
            {
                return String.Format("{0}: {1}", CGlobal.GetResourceValue("l_description"), this.caption);
            }
            set
            {
                if (this.caption != value)
                {
                    this.caption = value;
                    this.OnPropertyChanged("Caption");
                }
            }
        }
        public string ModulesCollectionVersion
        {
            get
            {
                return "Версия коллекции прошивок: " + modulesCollectionVer;
            }
            set
            {
                this.modulesCollectionVer = value;
                this.OnPropertyChanged("ModulesCollectionVersion");
            }
        }
        public Boolean Update_On
        {
            get
            {
                return this.update_on;
            }
            set
            {

                this.update_on = value;
                this.OnPropertyChanged("Update_On");
            }
        }

        public Boolean? WDT_On
        {
            get
            {
                return this.wdt_on;
            }
            set
            {
                if (this.wdt_on != value)
                {
                    this.wdt_on = value;
                    this.OnPropertyChanged("WDT_On");
                }
            }
        }

        public UInt16 AssemblyHi
        {
            get
            {
                return this.assembly_hi;
            }
            set
            {
                if (this.assembly_hi != value)
                {
                    this.assembly_hi = value;
                    this.OnPropertyChanged("AssemblyHi");
                    this.OnPropertyChanged("AssemblyString");
                }
            }
        }

        public UInt16 AssemblyMid
        {
            get
            {
                return this.assembly_mid;
            }
            set
            {
                if (this.assembly_mid != value)
                {
                    this.assembly_mid = value;
                    this.OnPropertyChanged("AssemblyMid");
                    this.OnPropertyChanged("AssemblyString");
                }
            }
        }


        public UInt16 AssemblyLo
        {
            get
            {
                return this.assembly_lo;
            }
            set
            {
                if (this.assembly_lo != value)
                {
                    this.assembly_lo = value;
                    this.OnPropertyChanged("AssemblyLo");
                    this.OnPropertyChanged("AssemblyString");
                }
            }
        }
        public int AssemblyInt { get => assemblyInt; }
        public String AssemblyString
        {
            get
            {
                if(this.assembly_hi >= 5)
                    return String.Format("{0}: {1}", CGlobal.GetResourceValue("l_assemblyVersion"), String.Format("{0}.{1}.{2}", this.assembly_hi, this.assembly_mid, this.assembly_lo));
                else
                    return String.Format("{0}: {1}", CGlobal.GetResourceValue("l_assemblyVersion"), String.Format("{0}.{1}", this.assembly_hi, this.assembly_lo));
            }
        }

        /// <summary>
        /// Флаг записи конфигурации в контроллер
        /// </summary>
        public Boolean IsWritingConfig
        {
            get;
            set;
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter.ToString() == "IsConnected")
            {
                if (this.isConnected)
                    return CGlobal.GetResourceValue("l_connected");
                else
                    return CGlobal.GetResourceValue("l_disconnected");
            }
            else if (parameter.ToString() == "Visible")
            {
                if (this.isRunning)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            else if (parameter.ToString() == "Collapsed")
            {
                if (this.isRunning)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
            else if (parameter.ToString() == "Background")
            {
                //Фон контроллера
                if (this.IsRunning)
                {
                    if (this.isConnected) //if ((Boolean)value)
                        return new SolidColorBrush(Color.FromRgb(139, 229, 0));
                    else
                        return new SolidColorBrush(Color.FromRgb(248, 86, 86));
                }
                return new SolidColorBrush(Colors.Transparent);
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UpdateModulesVisibilityConverter : INotifyPropertyChanged, IValueConverter
    {
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter.ToString() == "Visible")
            {
                if (CGlobal.CurrState != null)
                {
                    if (CGlobal.CurrState.IsRunning && CGlobal.CurrState.Update_On)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PLCVersionVisibilityConverter : INotifyPropertyChanged, IValueConverter
    {
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter.ToString() == "Visible")
            {
                if (CGlobal.CurrState != null)
                {
                    if (CGlobal.CurrState.PLCVersionInfo == 3 && CGlobal.CurrState.IsRunning)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Класс отвечающий за видимость элемента индикации WDT
    /// </summary>
    public class WDTVisibilityConverter : INotifyPropertyChanged, IValueConverter
    {
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter.ToString() == "Visible")
            {
                if (CGlobal.CurrState != null)
                {
                        if (CGlobal.CurrState.IsRunning && (CGlobal.CurrState.WDT_On == false))
                            return Visibility.Visible;
                        else
                            return Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Класс для работы с настройками устройства
    /// </summary>
    public class CSettings : INotifyPropertyChanged
    {
        //Порт SSH по умолчанию
        public const UInt16 DEFAULT_SSH_PORT = 22;
        //Порт базы данных по умолчанию
        public const UInt16 DEFAULT_DB_PORT = 5432;

        private String ip = "192.168.3.100";
        private Boolean usb = true;
        private UInt16 sshPort = CSettings.DEFAULT_SSH_PORT;
        private UInt16 dbPort = CSettings.DEFAULT_DB_PORT;

        //Список ранее использованных IP адресов
        private List<String> ip_list = new List<string>();

        /// <summary>
        /// Локализация
        /// </summary>
        private String language = "en-US";

        /// <summary>
        /// Отображаемые поля
        /// </summary>
        private Boolean nameColumn = true;
        private Boolean valueColumn = true;
        private Boolean typeColumn = true;
        private Boolean tagColumn = true;
        //Флаг авторизации с окна выбора сессий контроллера
        public bool flagSessionWindow = false;
        //Список контроллеров доступных по сети
        private ObservableCollection<CAbakInfo> _controllersList;

        private static CSettings settings = null;

        public CSettings()
        {
        }

        /// <summary>
        /// Путь к файлу с настройками приложения
        /// </summary>
        public static String SettingsPath { get => CAuxil.AppDataPath + "\\settings.xml"; }

        public List<String> IPList { get => this.ip_list; }

        /// <summary>
        /// Функция загружает файл с настройками и читает параметры связи
        /// </summary>
        /// <param name="path"></param>
        public void Load(String path)
        {
            XPathDocument doc;
            XPathNavigator channel;
            try
            {
                doc = new XPathDocument(path);
                channel = doc.CreateNavigator().SelectSingleNode("//channel");
            }
            catch
            {
                doc = null;
                channel = null;
            }
            //Нсатройки канала связи

            if (channel != null)
            {
                //ip
                this.ip = CXML.getAttributeValue(channel, "ip", "192.168.55.127");
                //Порт SSH
                try
                {
                    this.sshPort = Convert.ToUInt16(CXML.getAttributeValue(channel, "ssh", CSettings.DEFAULT_SSH_PORT.ToString()));
                }
                catch
                {
                    this.sshPort = CSettings.DEFAULT_SSH_PORT;
                }
                //Порт базы данных
                try
                {
                    this.dbPort = Convert.ToUInt16(CXML.getAttributeValue(channel, "postgre", CSettings.DEFAULT_DB_PORT.ToString()));
                }
                catch
                {
                    this.dbPort = 5432;
                }
                //usb
                try
                {
                    this.usb = Convert.ToBoolean(CXML.getAttributeValue(channel, "usb", "true"));
                }
                catch
                {
                    this.usb = true;
                }
                //Получение списка ранее использованных ip адресов
                try
                {
                    XPathNavigator iplistNode = channel.SelectSingleNode("iplist");
                    if (iplistNode != null)
                    {
                        foreach (XPathNavigator ipNode in iplistNode.Select("ip"))
                            this.ip_list.Add(ipNode.InnerXml);
                    }
                }
                catch
                {

                }
            }
            //Настройки локализации
            try
            {
                XPathNavigator localization = doc.CreateNavigator().SelectSingleNode("//localization");
                if (localization != null)
                {
                    //language
                    this.language = CXML.getAttributeValue(localization, "language", "en-US");
                }
            }
            catch
            {
                this.language = "ru-RU";
            }
            try
            { //Настройки интерфейса программы
                XPathNavigator options = doc.CreateNavigator().SelectSingleNode("//options");
                if (options != null)
                {
                    this.nameColumn = Convert.ToBoolean(CXML.getAttributeValue(options, "namecol", this.nameColumn.ToString()));
                    this.valueColumn = Convert.ToBoolean(CXML.getAttributeValue(options, "valuecol", this.valueColumn.ToString()));
                    this.typeColumn = Convert.ToBoolean(CXML.getAttributeValue(options, "typecol", this.typeColumn.ToString()));
                    this.tagColumn = Convert.ToBoolean(CXML.getAttributeValue(options, "tagcol", this.tagColumn.ToString()));
                }
            }
            catch
            {
                this.nameColumn = this.valueColumn = this.typeColumn = this.tagColumn = true;
            }
        }

        public void Load()
        {

            this.Load(CSettings.SettingsPath);
        }

        /// <summary>
        /// Предварительная инициализация файла с настройками
        /// </summary>
        private static void initSettingsFile(Boolean force = false)
        {
            try
            {
                String path = CSettings.SettingsPath;
                if (!File.Exists(path) || force)
                {
                    //Предварительная настройка структуры файла с настройками
                    //Корень
                    XElement element = new XElement("settings");
                    element.Save(path);
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Функция сохраняет параметры связи с контроллером в файл с настройками
        /// </summary>
        /// <param name="path"></param>
        public void Save(String path)
        {
            CSettings.initSettingsFile(true);

            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNode settings = doc.SelectSingleNode("settings");

            //Настройки канала связи
            XmlNode channel = doc.CreateElement("channel");
            settings.AppendChild(channel);
            //ip
            CXML.appendAttributeToNode(channel, "ip", this.ip);
            //Порт SSH
            CXML.appendAttributeToNode(channel, "ssh", this.sshPort.ToString());
            //Порт базы данных
            CXML.appendAttributeToNode(channel, "postgre", this.dbPort.ToString());
            //usb
            CXML.appendAttributeToNode(channel, "usb", this.usb.ToString());
            //Список ранее использованных IP адресов
            XmlNode ipList = doc.CreateElement("iplist");
            channel.AppendChild(ipList);
            foreach (String ip in this.ip_list)
            {
                XmlNode ipNode = doc.CreateElement("ip");
                ipNode.InnerText = ip;
                ipList.AppendChild(ipNode);
            }

            //Настройки локализации
            XmlNode localization = doc.CreateElement("localization");
            settings.AppendChild(localization);
            //language
            CXML.appendAttributeToNode(localization, "language", this.language);

            //Настройки интерфейса
            XmlNode options = doc.CreateElement("options");
            settings.AppendChild(options);
            CXML.appendAttributeToNode(options, "namecol", this.nameColumn.ToString());
            CXML.appendAttributeToNode(options, "valuecol", this.valueColumn.ToString());
            CXML.appendAttributeToNode(options, "typecol", this.typeColumn.ToString());
            CXML.appendAttributeToNode(options, "tagcol", this.tagColumn.ToString());

            doc.Save(path);
        }

        public void Save()
        {
            try
            {
                this.Save(CSettings.SettingsPath);
            }
            catch
            {

            }
        }

        public static CSettings GetSettings()
        {
            if (settings != null)
                return settings;

            //Предварительная инциализация файла с настройками
            CSettings.initSettingsFile();
            //Создание экземпляра класса с настройками
            settings = new CSettings();
            settings.Load();
            return settings;
        }

        /// <summary>
        /// Функция копирует настройки из другого экземпляра
        /// </summary>
        /// <param name="Settings"></param>
        public void Assign(CSettings Settings)
        {
            this.IP = Settings.IP;
            this.USB = Settings.USB;
            this.SSHPort = Settings.SSHPort;
            this.DBPort = Settings.DBPort;

            this.NameColumnVisible = Settings.NameColumnVisible;
            this.ValueColumnVisible = Settings.ValueColumnVisible;
            this.TypeColumnVisible = Settings.TypeColumnVisible;
            this.TagColumnVisible = Settings.TagColumnVisible;

            this.ip_list.Clear();
            this.ip_list.AddRange(Settings.IPList);
            this.ip_list.Sort();
        }

        /// <summary>
        /// Функция проверяет на совпадение настроек связи
        /// </summary>
        /// <param name="settings">Указатель на структуру с настройками с которой надо сравнить</param>
        /// <returns></returns>
        public bool IsSameSettings(CSettings settings)
        {
            if (this.IP != settings.IP)
                return false;

            return true;
        }


        /// <summary>
        /// IP адрес устройств в сети
        /// </summary>
        public String IP
        {
            get
            {
                return this.ip;
            }
            set
            {
                this.ip = value;
                this.OnPropertyChanged("IP");
            }
        }

        public UInt16 SSHPort
        {
            get
            {
                return this.sshPort;
            }
            set
            {
                this.sshPort = value;
                this.OnPropertyChanged("SSHPort");
            }
        }

        public UInt16 DBPort
        { 
            get
            {
                return this.dbPort;
            }
            set
            {
                this.dbPort = value;
                this.OnPropertyChanged("DBPort");
            }
        }

        /// <summary>
        /// IP адрес для установки соединения
        /// </summary>
        public String ConnectIP
        {
            get
            {
                //Если авторизация проходит через окно сессий то возвращаем IP из глобальных переменных (подгруженная из списка)
                if (flagSessionWindow == true)
                {
                    return this.ip;
                }

                if (this.usb)
                    return "192.168.7.2";
                return this.ip;
            }
        }

        public UInt16 ConnectSSHport
        {
            get
            {
                if (this.usb)
                    return CSettings.DEFAULT_SSH_PORT;

                return this.sshPort;
            }
        }

        public UInt16 ConnectDBPort
        {
            get
            {
                if (this.usb)
                    return CSettings.DEFAULT_DB_PORT;

                return this.dbPort;
            }
        }

        /// <summary>
        /// Режим работы по USB кабелю
        /// </summary>
        public Boolean USB
        {
            get
            {
                return this.usb;
            }
            set
            {
                if (this.usb == value)
                    return;

                this.usb = value;
                this.OnPropertyChanged("USB");
                this.OnPropertyChanged("NotUSB");
            }
        }

        public Boolean NotUSB
        {
            get
            {
                return !this.usb;
            }
            set
            {
                if (this.usb != value)
                    return;

                this.usb = !value;
                this.OnPropertyChanged("USB");
                this.OnPropertyChanged("NotUSB");
            }
        }

        public String Language
        {
            get
            {
                return this.language;
            }
            set
            {
                this.language = value;
            }
        }

        public Boolean NameColumnVisible
        {
            get
            {
                return this.nameColumn;
            }
            set
            {
                this.nameColumn = value;
                this.OnPropertyChanged("NameColumnVisible");
            }
        }

        public Boolean ValueColumnVisible
        {
            get
            {
                return this.valueColumn;
            }
            set
            {
                this.valueColumn = value;
                this.OnPropertyChanged("ValueColumnVisible");
            }
        }

        public Boolean TypeColumnVisible
        {
            get
            {
                return this.typeColumn;
            }
            set
            {
                this.typeColumn = value;
                this.OnPropertyChanged("TypeColumnVisible");
            }
        }



        public Boolean TagColumnVisible
        {
            get
            {
                return this.tagColumn;
            }
            set
            {
                this.tagColumn = value;
                this.OnPropertyChanged("TagColumnVisible");
            }
        }

        //Список контроллеров доступных по сети
        public ObservableCollection<CAbakInfo> ControllersList
        {
            get => _controllersList ?? (_controllersList = new ObservableCollection<CAbakInfo>());
            set
            {
                _controllersList = value;
                this.OnPropertyChanged("ControllersList");
                this.OnPropertyChanged("CountControllers");
            }
        }

        public int CountControllers => ControllersList.Count;

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }

    public class ProtocolType
    {
        public ProtocolType(bool isCheckType, string protocolTypeName)
        {
            IsCheckType = isCheckType;
            ProtocolTypeName = protocolTypeName;
        }
        public bool IsCheckType { get; set; }
        public string ProtocolTypeName { get; set; }


    }

    /// <summary>
    /// Класс для сохранения параметров сессии.Применяется при аутентификации
    /// </summary>
    public class Autentification
    {
        public string user;

        public string password;

        public string ip;

        public string usb;

        public UInt16 sshPort;

        public UInt16 dbPort;
    }



}


