using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.IO;
using Npgsql;
using System.Globalization;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Diagnostics;
namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для CorrectTimeWindow.xaml
    /// </summary>
    public partial class CorrectTimeWindow : Window, INotifyPropertyChanged
    {
        private Dictionary<string, string> timeZones = new Dictionary<string, string>
        {
            {"Europe/Kaliningrad",  "Калининград (+2:00)"},
            { "Europe/Tallinn", "Таллин (+2:00)"},
            {"Europe/Riga", "Рига (+2:00)" },
            {"Europe/Vilnius", "Вильнюс (+2:00)"},
            {"Europe/Kiev", "Киев, Ужгород, Запорожье (+2:00)"},
            {"Europe/Chisinau", "Кишинёв (+2:00)"},
            {"Europe/Minsk", "Минск (+3:00)"},
            {"Europe/Moscow", "Москва, Симферополь (+3:00)"},
            {"Europe/Volgograd", "Астрахань, Волгоград, Самара, Саратов, Ульяновск (+4:00)"},
            {"Asia/Yerevan", "Ереван (+4:00)"},
            {"Asia/Tbilisi", "Тбилиси (+4:00)"},
            {"Asia/Baku", "Баку (+4:00)"},
            {"Asia/Qyzylorda", "Актау, Актобе, Атырау, Кызылорда, Уральск (+5:00)"},
            {"Asia/Yekaterinburg", "Екатеринбург (+5:00)"},
            {"Asia/Dushanbe", "Душанбе (+5:00)"},
            {"Asia/Ashgabat", "Ашхабад (+5:00)"},
            {"Asia/Tashkent", "Самарканд, Ташкент (+5:00)"},
            {"Asia/Bishkek", "Бишкек (+6:00)"},
            {"Asia/Almaty", "Алмата (+6:00)"},
            {"Asia/Omsk", "Омск (+6:00)"},
            {"Asia/Novosibirsk", "Барнаул, Новокузнецк, Новосибирск, Красноярск, Томск (+7:00)"},
            {"Asia/Irkutsk", "Иркутск (+8:00)"},
            {"Asia/Chita", "Чита, Якутск, Хандыга (+9:00)"},
            {"Asia/Vladivostok", "Владивосток, Усть-Нера (+10:00)"},
            {"Asia/Magadan", "Магадан, Сахалин, Среднеколымск (+11:00)"},
            {"Asia/Kamchatka", "Анадырь, Камчатка (+12:00)"},
        };

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<string> TimeZones { get; }
        //Текущий часовой пояс
        private string timeZone = "";
        public int TimeZoneIndex { get; set; }
        //Конфигурация контроллера
        CConfig config = null;
        //Таймер обновления данных на экране
        private DispatcherTimer timer = new DispatcherTimer();
        //Ручной ввод
        public bool ManualButton { get; set; }
        //Синхронизация с ПК
        public bool PCButton { get; set; }
        public CorrectTimeWindow(CConfig config)
        {
            CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == "TIME_ZONE") as CParam;
            timeZone = timeZones[param.ValueString];
            TimeZones = new ObservableCollection<string>(timeZones.Values);
            TimeZoneIndex = TimeZones.IndexOf(timeZone);
            PCButton = true;
            ManualButton = false;
            InitializeComponent();
            this.config = config;
            this.showCurrentTime();
            //Инициализация данных первоначальными значениями
            this.DayBox.Text = DateTime.Now.Day.ToString();
            this.MonthBox.Text = DateTime.Now.Month.ToString();
            this.YearBox.Text = DateTime.Now.Year.ToString();
            this.HourBox.Text = DateTime.Now.Hour.ToString();
            this.MinuteBox.Text = DateTime.Now.Minute.ToString();
            this.SecondBox.Text = DateTime.Now.Second.ToString();
            //Инициализация таймера обновляющего экран
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            this.timer.Tick += new EventHandler(timer_Tick);
            this.timer.Start();
        }

        public Boolean IsRunning
        {
            set
            {
                if (!value)
                    this.AbakTimeLabel.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void showCurrentTime()
        {
            if (!CGlobal.CurrState.IsRunning)
                return;

            try
            {
                //Год
                CParam param = this.config.FixedParamsList.Find(x => x.Tagname == "YEAR") as CParam;
                int Year = Convert.ToInt32(param.Value);
                //Месяц
                param = this.config.FixedParamsList.Find(x => x.Tagname == "MONTH") as CParam;
                int Month = Convert.ToInt32(param.Value);
                //День
                param = this.config.FixedParamsList.Find(x => x.Tagname == "DAY") as CParam;
                int Day = Convert.ToInt32(param.Value);
                string[] split_time = new string[10];
                if (CGlobal.Session.SSHClient == null)
                    return;

                split_time = CGlobal.Session.SSHClient.ExecuteCommand("date +%H:%M:%S:%3N").Split(':');
                DateTime dt = new DateTime(Year, Month, Day, Convert.ToInt32(split_time[0]),
                    Convert.ToInt32(split_time[1]), Convert.ToInt32(split_time[2]));

                TimeLabel.Content = $"{DateTime.Now}:{DateTime.Now.Millisecond}";

                this.AbakTimeLabel.Content = $"{dt}:{split_time[3]}";
            }
            catch
            {
                this.AbakTimeLabel.Content = "";
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.showCurrentTime();
        }

        private void writeTimeToController(Int32 Day, Int32 Month, Int32 Year, Int32 Hour, Int32 Minute, Int32 Second)
        {
            lock (this.config.VisibleParamsList)
            {
                //Час
                CParam param = this.config.FixedParamsList.Find(x => x.Tagname == "HOUR") as CParam;
                param.WriteValue = Hour.ToString();
                param.ManualChanged = true;
                //Минута
                param = this.config.FixedParamsList.Find(x => x.Tagname == "MINUTE") as CParam;
                param.WriteValue = Minute.ToString();
                param.ManualChanged = true;
                //Секунда
                param = this.config.FixedParamsList.Find(x => x.Tagname == "SECOND") as CParam;
                param.WriteValue = Second.ToString();
                param.ManualChanged = true;
                //Год
                param = this.config.FixedParamsList.Find(x => x.Tagname == "YEAR") as CParam;
                param.WriteValue = Year.ToString();
                param.ManualChanged = true;
                //Месяц
                param = this.config.FixedParamsList.Find(x => x.Tagname == "MONTH") as CParam;
                param.WriteValue = Month.ToString();
                param.ManualChanged = true;
                //День
                param = this.config.FixedParamsList.Find(x => x.Tagname == "DAY") as CParam;
                param.WriteValue = Day.ToString();
                param.ManualChanged = true;
            }
        }

        /// <summary>
        /// Корректировка времени в контроллере используя SSH
        /// </summary>
        /// <param name="dt"></param>
        void correctTimeThroughSSH()
        {
            DateTime now = DateTime.Now;
            //Сокращенное название месяца в английской локали
            string month = now.ToString("MMM", new CultureInfo("en-US"));
            //День
            string day = Convert.ToString(now.Day);
            //Год
            string year = Convert.ToString(now.Year);

            var sw = new Stopwatch();
            sw.Start();
            while ((now = DateTime.Now).Millisecond != 0 && sw.ElapsedMilliseconds < 5000) ;
            sw.Stop();

            string time = Convert.ToString($"{now.Hour}:{now.Minute}:{now.Second}." +
                $"{now.Millisecond}");
            CGlobal.Session.SSHClient.ExecuteCommand($"date --set \"{month} {day} {time} {year}\"");
            //Запись в микросхему
            CGlobal.Session.SSHClient.ExecuteCommand("hwclock -w");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CGlobal.Handler.UserLog(2, string.Format("Change time settings"));

            if (PCButton)
                SyncWithPC();
            else
                SetManual();

            CBaseParam ref_param;
            if (CConfig.ParamsList.TryGetValue("NTP_SERVER_STATE", out ref_param))
            {
                if (Convert.ToBoolean(ref_param.Value) == true)
                {
                    CAuxil.ExecuteSingleSSHCommand("systemctl stop chrony");
                    Thread.Sleep(100);
                    CAuxil.ExecuteSingleSSHCommand("systemctl start chrony");
                }
            }
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Если не цифра то запрещаем
            e.Handled = !CAuxil.CheckStringForInt(e.Text, true);
        }

        private void dataObjectPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = e.DataObject.GetData(typeof(String)) as String;
                if (!CAuxil.CheckStringForInt(text, true))
                    e.CancelCommand(); //Если не цифра то запрещаем
            }
            else
                e.CancelCommand();
        }

        //Синхронизация с ПК
        private void SyncWithPC()
        {
            CGlobal.Handler.UserLog(2, string.Format("Change time settings, sync with PC"));

            //Запись текущего времени
            DateTime now = DateTime.Now;
            //Новые версии начинаются с 5 и имеют три цифры
            bool newversion = CGlobal.CurrState.AssemblyHi == 5;
            //Старая несовместимая сборка ПЛК 2
            bool old_incompatible_vers = CGlobal.CurrState.AssemblyHi == 2 && CGlobal.CurrState.AssemblyLo < 16;
            //Старая совместимая сборка ПЛК 3
            bool old_compatible_vers = CGlobal.CurrState.AssemblyHi == 3 && CGlobal.CurrState.AssemblyLo >= 3;

            if (old_incompatible_vers)
            {
                //В прошивках версий ниже 2.16, время пишется только через fast_table
                this.writeTimeToController(now.Day, now.Month, now.Year, now.Hour, now.Minute, now.Second);
                return;
            }
            else if (old_compatible_vers || newversion)
            {
                //Начиная с версии 3.3 появилась возможность записи через SSH
                this.correctTimeThroughSSH();
            }
        }

        //Запись времени введенного вручную   
        private void SetManual()
        {
            Int32 day = Convert.ToInt32(this.DayBox.Text);
            //Сокращенное название месяца в английской локали
            Int32 month = Convert.ToInt32(this.MonthBox.Text);
            Int32 year = Convert.ToInt32(this.YearBox.Text);
            string monthName = new DateTime(year, month, day).ToString("MMM", new CultureInfo("en-US"));
            Int32 hour = Convert.ToInt32(this.HourBox.Text);
            Int32 minute = Convert.ToInt32(this.MinuteBox.Text);
            Int32 second = Convert.ToInt32(this.SecondBox.Text);
            //Время
            string time = Convert.ToString($"{hour}:{minute}:{second}");
            //Запись времени
            CAuxil.ExecuteSingleSSHCommand($"date --set \"{monthName} {day} {time} {year}\"");
            //Запись в микросхему
            CAuxil.ExecuteSingleSSHCommand("hwclock -w");
            this.writeTimeToController(day, month, year, hour, minute, second);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            timer.Stop();
        }

        public static DateTime GetNetworkTime()
        {
            //default Windows time server
            const string ntpServer = "ntp1.stratum2";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            PCTimeBox.IsEnabled = PCButton;
            ManualTimeBox.IsEnabled = ManualButton;
        }

        private void CurrentTimeZone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox timeZoneBox = sender as ComboBox;
            timeZone = timeZoneBox.SelectedItem.ToString();
        }

        private void TimeZoneChanging_Button_Click(object sender, RoutedEventArgs e)
        {
            string choosenTimeZone = timeZones.FirstOrDefault(zone => zone.Value == timeZone).Key;
            string sql = $"update fast_table set value='{choosenTimeZone}', changed=true where tag='TIME_ZONE'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();
            string linuxCMD = $"ln -s /usr/share/zoneinfo/{choosenTimeZone} /etc/localtime";
            CAuxil.ExecuteSingleSSHCommand("rm /etc/localtime");
            CAuxil.ExecuteSingleSSHCommand(linuxCMD);

            CBaseParam ref_param;
            if (CConfig.ParamsList.TryGetValue("NTP_SERVER_STATE", out ref_param))
            {
                if (Convert.ToBoolean(ref_param.Value) == true)
                {
                    CAuxil.ExecuteSingleSSHCommand("systemctl stop chrony");
                    Thread.Sleep(100);
                    CAuxil.ExecuteSingleSSHCommand("systemctl start chrony");
                }
            }
        }
    }
}
