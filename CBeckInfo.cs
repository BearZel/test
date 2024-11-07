using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Xml.XPath;
using System.IO;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс содержащий информацию о контроллере в сети
    /// </summary>
    public class CAbakInfo : INotifyPropertyChanged
    {
        private String serial;
        private String ip = "";
        private String description;
        private String netmask;
        private String macAddress;
        private String iface;
        private String ping = "";
        private String hostname = "";
        private String plcVersion = "";
        private String assembly;
        private Ping pingSender;

        public async Task SendPing()
        {
            Ping ping = new Ping();

            try
            {
                PingOptions options = new PingOptions(64, true);
                byte[] MESSAGE = { 0x86, 0x83, 0x74, 0x8F, 0x91, 0xDE, 0x4C, 0xE8, 0x80, 0x14, 0x41, 0x0A, 0x26, 0x7C, 0xB9, 0x3E };
                await this.pingSender.SendPingAsync(this.ip, 1000, MESSAGE, options);
            }
            catch
            {
                this.Ping = "NOT AVAILABLE";
            }
        }

        private void initPing()
        {
            this.pingSender = new Ping();
            this.pingSender.PingCompleted += new PingCompletedEventHandler(this.pingCompleted);
        }

        private void pingCompleted(object sender, PingCompletedEventArgs e)
        {
            PingReply reply = e.Reply;
            if (reply == null)
                return;

            if (reply.Status == IPStatus.Success)
            {
                this.Ping = String.Format("PING OK, {0} ms", reply.RoundtripTime);
            }
            else
            {
                this.Ping = "PING FAILED";
            }
        }

        public CAbakInfo()
        {
        }
        public String Assembly
        {
            get
            {
                return this.assembly;
            }
            set
            {
                this.assembly = value;
                this.OnPropertyChanged("Assembly");
            }
        }
        public String PlcVersion
        {
            get
            {
                return this.plcVersion;
            }
            set
            {
                this.plcVersion = value;
                this.OnPropertyChanged("PlcVersion");
            }
        }
        public String Serial
        {
            get
            {
                return this.serial;
            }
            set
            {
                this.serial = value;
                this.OnPropertyChanged("Serial");
            }
        }

        public String IP
        {
            get
            {
                return this.ip;
            }
            set
            {
                bool ip_change = this.ip != value;
                this.ip = value;
                this.OnPropertyChanged("IP");

                if (ip_change)
                    this.initPing();
            }
        }

        public String Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
                this.OnPropertyChanged("Description");
            }
        }

        public String Netmask
        {
            get
            {
                return this.netmask;
            }
            set
            {
                this.netmask = value;
                this.OnPropertyChanged("Netmask");
            }
        }

        public String MacAddress
        {
            get
            {
                return this.macAddress;
            }
            set
            {
                this.macAddress = value;
                this.OnPropertyChanged("MacAddress");
            }
        }

        public String Hostname
        {
            get
            {
                return this.hostname;
            }
            set
            {
                this.hostname = value;
                this.OnPropertyChanged("Hostname");
            }
        }

        public String Interface
        {
            get
            {
                return this.iface;
            }
            set
            {
                this.iface = value;
                this.OnPropertyChanged("Interface");
            }
        }

        public DateTime LastUpdateTime
        {
            get;
            set;
        }

        public String UniqueName
        {
            get
            {
                return String.Format("{0}_{1}", this.Serial, this.Interface);
            }
        }

        public String Ping
        {
            get
            {
                return this.ping;
            }
            set
            {
                this.ping = value;
                this.OnPropertyChanged("Ping");
            }
        }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }


    /// <summary>
    /// Класс для поиска контроллеров и вычислителей Абак
    /// </summary>
    public class CBeckFinder
    {
        //UDP порт в контроллер для приёма широковещательных посылок
        private const int DEVICE_PORT = 8310;
        private int APP_PORT = DEVICE_PORT + 1;

        //Флаг активности приёмника
        private bool alive = false;

        //Широковещательный пакет, с помощью которого ищутся абаки
        //В первые два байта размещается порт который был использован приёмником
        private byte[] MESSAGE = { 0x00, 0x00, 0x86, 0x83, 0x74, 0x8F, 0x91, 0xDE, 0x4C, 0xE8, 0x80, 0x14, 0x41, 0x0A, 0x26, 0x7C, 0xB9, 0x3E };

        //UDP клиент для отправки широковещательных сообщений
        private UdpClient udpClient = null;
        //UDP клиент для приёма ответов от контроллеров
        private UdpClient udpServer = null;
        //Список контроллеров
        private List<CAbakInfo> controllersList = new List<CAbakInfo>();
        private object _synchObj = new object();

        /// <summary>
        /// Запуск процесса поиска контроллеров
        /// </summary>
        public void Start()
        {
            //Инициализация UDP клиента и UDP сервера
            try
            {
                this.udpClient = new UdpClient();
                //this.udpClient.EnableBroadcast = true;
                while (true)
                {
                    try
                    {
                        this.udpServer = new UdpClient(this.APP_PORT);
                        // запускаем задачу на прием сообщений
                        Task receiveTask = new Task(this.receiveMessages);
                        this.alive = true;
                        receiveTask.Start();
                        break;
                    }
                    catch (SocketException)
                    {
                        this.APP_PORT++;
                    }
                }
            }
            catch
            {
                this.udpClient = null;
                this.udpServer = null;
            }
        }

        public void Stop()
        {
            this.alive = false;
            APP_PORT = DEVICE_PORT + 1;
            lock (_synchObj)
            {
                this.controllersList.Clear();
            }
            
            try
            {
                if (this.udpClient != null)
                {
                    this.udpClient.Close();
                    this.udpClient = null;
                }
            }
            catch
            {
                this.udpClient = null;
            }

            try
            {
                if (this.udpServer != null)
                {
                    this.udpServer.Close();
                    this.udpServer = null;
                }
            }
            catch
            {
                this.udpServer = null;
            }
        }

        private void receiveMessages()
        {
            while (this.alive)
            {
                try
                {
                    IPEndPoint endpoint = null;
                    byte[] message = this.udpServer != null ? this.udpServer.Receive(ref endpoint) : new byte[0];

                    MemoryStream stream = new MemoryStream(message);
                    stream.Position = 0;
                    String s = Encoding.ASCII.GetString(message);
                    XPathDocument doc = new XPathDocument(stream);
                    XPathNavigator nav = doc.CreateNavigator();

                    XPathNavigator root = nav.SelectSingleNode("root");

                    //Серийный номер
                    String ss = CXML.getAttributeValue(root, "serial", "");
                    //Описание
                    String Description = CXML.getAttributeValue(root, "description", "");
                    string plcVersion = CXML.getAttributeValue(root, "plc_version", "");
                    string assembly = CXML.getAttributeValue(root, "assembly_version", "");
                    foreach (XPathNavigator ifa in root.Select("interface"))
                    {
                        if (!alive)
                            return;

                        String Interface = CXML.getAttributeValue(ifa, "name", "");
                        if (Interface == "")
                            continue;

                        lock (_synchObj)
                        {
                            Boolean newController = false;
                            CAbakInfo abakInfo = this.controllersList.Find(x => x.UniqueName == String.Format("{0}_{1}", ss, Interface));
                            if (abakInfo == null)
                            {
                                abakInfo = new CAbakInfo();
                                newController = true;
                            }
                            abakInfo.Serial = ss;
                            abakInfo.Description = Description;
                            abakInfo.Interface = Interface;
                            abakInfo.IP = CXML.getAttributeValue(ifa, "address", "");
                            abakInfo.Hostname = $"ABAKPLC{ss}";
                            abakInfo.Netmask = CXML.getAttributeValue(ifa, "netmask", "");
                            abakInfo.MacAddress = CXML.getAttributeValue(ifa, "mac", "");
                            abakInfo.Assembly = assembly;
                            abakInfo.PlcVersion = plcVersion;
                            abakInfo.LastUpdateTime = DateTime.Now;
                            if (newController)
                                this.controllersList.Add(abakInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Какая то ошибка при разборе пакета, возможно пакет битый или мусор
                    String s = ex.Message;

                }
            }
        }

        public void FindControllers()
        {
            if (this.udpClient == null)
                return;

            try
            {
                //куда надо запрос слать
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Broadcast, DEVICE_PORT);
                //В сообщение вставляется порт используемый приложением для приёма сообщений от контроллеров
                this.MESSAGE[0] = (byte)this.APP_PORT;
                this.MESSAGE[1] = (byte)(this.APP_PORT >> 8);
                //Отправка запроса
                this.udpClient.Send(this.MESSAGE, this.MESSAGE.Length, endpoint);
                Thread.Sleep(100);

                //Убираем те абаки что уже не отвечают
                int index = 0;
                Task[] tasks;
                lock (_synchObj)
                {
                    while (index < this.controllersList.Count)
                    {
                        CAbakInfo info = this.controllersList[index];
                        TimeSpan span = DateTime.Now - info.LastUpdateTime;
                        if (span.Seconds > 5)
                        {
                            this.controllersList.Remove(info);
                            continue;
                        }

                        index++;
                    }
                    
                    //Засылает всем контроллерам PING
                    tasks = new Task[this.controllersList.Count];
                    for (int i = 0; i < this.controllersList.Count; i++)
                    {
                        CAbakInfo abakInfo = this.controllersList[i];
                        Task task = abakInfo.SendPing();
                        tasks[i] = task;
                    }
                }

                if(tasks.Length > 0)
                    Task.WaitAll(tasks);
            }
#if DEBUG
            catch(Exception ex)
            {
                //MessageBox.Show(ex.Message);
                this.Stop();
                //throw;
            }
#endif
#if (!DEBUG)
            catch
            {
                this.Stop();
                throw;
            }
#endif


        }

        public async Task IsControllerOnline()
        {
            string ip = "192.168.7.2";
            Ping ping = new Ping();
            try
            {
                var loop1Task = Task.Run(async () => {
                    while (true)
                    {
                        PingReply pingreply = ping.Send(ip, 10000);
                        if (pingreply.Status != IPStatus.Success)
                            MessageBox.Show("");
                        await Task.Delay(5000);

                    }
                });
                await Task.Delay(1);
            }
            catch
            {

            }
        }
        /// <summary>
        /// Список контроллеров 
        /// </summary>
        public List<CAbakInfo> ControllersList
        {
            get
            {
                List<CAbakInfo> cloneList;
                lock (_synchObj)
                {
                    cloneList = new List<CAbakInfo>(this.controllersList);
                }

                return cloneList;
            }
        }
    }
}
