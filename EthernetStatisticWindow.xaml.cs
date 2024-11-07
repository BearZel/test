using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для EthernetStatisticWindow.xaml
    /// </summary>
    public partial class EthernetStatisticWindow : Window, INotifyPropertyChanged
    {
        //Указатель на класс с открытой SSH сессией
        CSSHClient sshClient;
        //Таймер обновления данных
        private DispatcherTimer timer = new DispatcherTimer();
        //Название интерфейса
        private String interfaceName;
        //Контекст потока для работы с пользовательским интерфейсом приложения из других потоков
        private SynchronizationContext uiContext;

        //Принято пакетов
        private UInt64 recPackets = 0;
        //Принято байт
        private UInt64 recBytes = 0;
        //Количество ошибок при приёме
        private UInt64 recErrors = 0;
        //Отправлено пакетов
        private UInt64 sentPackets = 0;
        //Отправлено байт
        private UInt64 sentBytes = 0;
        //Количество ошибок при отправке
        private UInt64 sendErrors = 0;
        //Заголовок окна
        private String windowTitle = "";
        //Текущая скорость
        private String speed = "";
        //Дуплекс
        private String duplex = "";

        public EthernetStatisticWindow(CSSHClient sshClient, String interfaceName)
        {
            InitializeComponent();

            this.uiContext = SynchronizationContext.Current;
            this.sshClient = sshClient;
            //Инициализация таймера
            this.timer.IsEnabled = false;
            this.timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            this.timer.Tick += new EventHandler(this.timer_Tick);

            this.interfaceName = interfaceName;
            this.WindowTitle = String.Format("{0} ({1})", CGlobal.GetResourceValue("l_ethStatWindowName"), interfaceName);
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            this.timer.IsEnabled = false;

            await Task.Run(() => updateStatistic());
        }

        private void enableTimer(object state)
        {
            this.timer.IsEnabled = Convert.ToBoolean(state);
        }

        private String prepareStringForSplit(String str)
        {
            str = str.Trim();
            while (true)
            {
                int length = str.Length;
                str = str.Replace("  ", " ");
                if (length == str.Length)
                    break;
            }

            return str;
        }

        /// <summary>
        /// Функция обновляет статистику о интерфейсе
        /// </summary>
        private void updateStatistic()
        {
            try
            {
                String res = this.sshClient.ExecuteCommand(String.Format("ip -s link show {0}", this.interfaceName));
                if (this.sshClient.LastError != "")
                {
                    //String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerUnzipErr"), this.session.SSHClient.LastError);
                    //this.uiContext.Post(this.enableTimer, false);
                    return;
                }

                String[] list = res.Split('\n');
                if (list.Length != 6)
                    return;

                //Работа с переменными по приёму
                String[] values = this.prepareStringForSplit(list[3]).Split(' ');
                this.RecBytes = Convert.ToUInt64(values[0]);
                this.RecPackets = Convert.ToUInt64(values[1]);
                this.RecErrors = Convert.ToUInt64(values[2]);

                //Работа с переменными по отправке
                values = this.prepareStringForSplit(list[5]).Split(' ');
                this.SentBytes = Convert.ToUInt64(values[0]);
                this.SentPackets = Convert.ToUInt64(values[1]);
                this.SendErrors = Convert.ToUInt64(values[2]);

                res = this.sshClient.ExecuteCommand(String.Format("ethtool {0} | awk '/Speed|Duplex/ {{print $0}}'", this.interfaceName));
                if (this.sshClient.LastError != "")
                {
                    return;
                }

                list = res.Split('\n');
                if (list.Length != 2)
                    return;
                foreach(String s in list)
                {
                    if (s.Contains("Speed"))
                        this.Speed = s.Trim();
                    else
                        this.Duplex = s.Trim();
                }

                if (!this.IsVisible)
                  return;

                this.uiContext.Post(this.enableTimer, true);
            }
            catch
            {

            }
        }

        public UInt64 RecPackets
        {
            get
            {
                return this.recPackets;
            }
            set
            {
                this.recPackets = value;
                this.OnPropertyChanged("RecPackets");
            }
        }

        public UInt64 RecBytes
        {
            get
            {
                return this.recBytes;
            }
            set
            {
                this.recBytes = value;
                this.OnPropertyChanged("RecBytes");
            }
        }

        public UInt64 RecErrors
        {
            get
            {
                return this.recErrors;
            }
            set
            {
                this.recErrors = value;
                this.OnPropertyChanged("RecErrors");
            }
        }

        public UInt64 SentPackets
        {
            get
            {
                return this.sentPackets;
            }
            set
            {
                this.sentPackets = value;
                this.OnPropertyChanged("SentPackets");
            }
        }

        public UInt64 SentBytes
        {
            get
            {
                return this.sentBytes;
            }
            set
            {
                this.sentBytes = value;
                this.OnPropertyChanged("SentBytes");
            }
        }

        public UInt64 SendErrors
        {
            get
            {
                return this.sendErrors;
            }
            set
            {
                this.sendErrors = value;
                this.OnPropertyChanged("SendErrors");
            }
        }

        public String Speed
        {
            get
            {
                return this.speed;
            }
            set
            {
                this.speed = value;
                this.OnPropertyChanged("Speed");
            }
        }

        public String Duplex
        {
            get
            {
                return this.duplex;
            }
            set
            {
                this.duplex = value;
                this.OnPropertyChanged("Duplex");
            }
        }

        public String WindowTitle
        {
            get
            {
                return this.windowTitle;
            }
            set
            {
                this.windowTitle = value;
                this.OnPropertyChanged("WindowTitle");
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => updateStatistic());
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
