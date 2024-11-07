using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Npgsql;
using System.IO;
using System.ComponentModel;
using System.Threading;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for RedundancyWindow.xaml
    /// </summary>
    public partial class RedundancyWindow : Window, INotifyPropertyChanged
    {
        //Путь к CFG файлу
        private const String codesysControlCFG = "/etc/CODESYSControl.cfg";
        private NpgsqlConnection connection;
        public ObservableCollection<ModbusAreas> ModbusAreas { get; set; }
        public ObservableCollection<RedundancyOption> COMS { get; set; }
        //SSH клиент
        private CSSHClient sshClient = null;
        //CFG файл
        CIniFile iniCFG = null;
        //Список устройств
        private Dictionary<int[], string> devices = new Dictionary<int[], string>
        {
            {new int [2] {1,4},  "CPU"},
            {new int [2] {5,8},  "IM 1"},
            {new int [2] {9,12}, "IM 2"}
        };
        private Dictionary<string, int[]> limits = new Dictionary<string, int[]>
        {
            {"StandbyWaitTime", new int [2] {50, 1000} },
            {"Port", new int [2] {0, 65535 } },
            {"DataWaitTime", new int [2] {100, 65535 } },
            {"SyncWaitTime", new int [2] {100, 1000} },
            {"LockTimeout", new int [2] {10, 1000} },
            {"BootupWaitTime", new int [2] {1000, 60000} },
            {"TcpWaitTime", new int [2] {1000, 60000 } },
            {"ServiceWaitTime", new int [2] {1000, 10000} },
        };
        public RedundancyWindow(NpgsqlConnection connection, CSSHClient sshClient)
        {
            this.sshClient = sshClient;
            this.connection = connection;
            InitializeComponent();
            if (CGlobal.Handler.Auth.Account == "root")
            {
                LogsRectangle.Visibility = Visibility.Visible;
                LogsExpander.Visibility = Visibility.Visible;
            }
            if (!GetAreasDataFromDB())
            {
                this.Close();
            }
            LoadRedundancySettings();

            ModbusDG.ItemsSource = ModbusAreas;
            ListCollectionView collectionView = new ListCollectionView(COMS);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Type"));
            Interfaces.ItemsSource = collectionView;
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void LoadRedundancySettings()
        {
            try
            {
                Stream stream = sshClient.ReadFile(codesysControlCFG);
                if (stream == null)
                    stream = new MemoryStream();
                iniCFG = new CIniFile(stream);
                iniCFG.NewLine = "\n";
                //CPU role
                int plcId = Convert.ToInt32(iniCFG.ReadValue("cmpredundancy", "plcident"));
                Active.IsChecked = plcId == 1;
                Passive.IsChecked = plcId == 2;
                //connectionip settings
                GetConnectionParams(plcId, 1, ref ActiveIP, ref PassiveIP, ref Port);
                //services
                ServiceWaitTime.Text = iniCFG.ReadValue("cmpsrv", "service.waittime", "1000");
                CheckAssembly(plcId);
                LockTimeout.Text = iniCFG.ReadValue("cmpredundancy", "LockTimeout", "50");
                BootupWaitTime.Text = iniCFG.ReadValue("cmpredundancy", "BootupWaitTime", "5000");
                TcpWaitTime.Text = iniCFG.ReadValue("cmpredundancy", "TcpWaitTime", "2000");
                BootProject.Text = iniCFG.ReadValue("cmpredundancy", "BootProject");
                RedundancyTaskName.Text = iniCFG.ReadValue("cmpredundancy", "RedundancyTaskName");

                DataSyncAlways.IsChecked = Convert.ToBoolean(Convert.ToInt32(iniCFG.ReadValue("cmpredundancy", "datasyncalways", "0")));
                DebugMessages.IsChecked = Convert.ToBoolean(Convert.ToInt32(iniCFG.ReadValue("cmpredundancy", "debugmessages", "0")));
                DebugMessagesTaskTime.IsChecked = Convert.ToBoolean(Convert.ToInt32(iniCFG.ReadValue("cmpredundancy", "debugmessagestasktime", "0")));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось считать настройки Codesys! Ошибка \"{ex.Message}\"", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == "DUPLICATION_MODBUS") as CParam;
            if (param != null)
            {
                Duplication.Visibility = Visibility.Visible;
                Duplication.IsChecked = Convert.ToBoolean(Convert.ToInt32(param.ValueString));
            }
        }
        //Проверка сборки на совместимость
        private void CheckAssembly(int plcId)
        {
            //До сборки 5.2.0 был syncwaittime
            StandbyWaitTime.Text = iniCFG.ReadValue("cmpredundancy", "standbywaittime", "50");
            if (CGlobal.CurrState.AssemblyInt < 520)
            {
                ExtraIpSettings.Visibility = Visibility.Collapsed;
                DataWaitTime.Text = iniCFG.ReadValue("cmpredundancy", "syncwaittime", StandbyWaitTime.Text);
            }
            else
            {
                GetConnectionParams(plcId, 2, ref ExtraActiveIP, ref ExtraPassiveIP, ref ExtraPort);
                DataWaitTime.Text = iniCFG.ReadValue("cmpredundancy", "datawaittime", StandbyWaitTime.Text);
            }
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == "RUN_SWITCH_DIAG") as CParam;
            bool isRunSwitchOff = param.ValueString.ToLower() == "false";
            string msg = "Службы Codesys будут перезапущены. Вы уверены, что хотите сохранить настройки?";
            if (!isRunSwitchOff)
            {
                if (MessageBox.Show(msg, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }
            }

            if (!CheckValidIp(ActiveIP) || !CheckValidIp(PassiveIP) || !CheckValidIp(ExtraActiveIP) || !CheckValidIp(ExtraPassiveIP))
            {
                return;
            }

            CGlobal.Handler.UserLog(2, string.Format("Change Redundancy settings"));

            int sum = 0;
            var choosenAreas = ModbusAreas.Where(area => area.IsChoosen);
            foreach (var chosenArea in choosenAreas)
            {
                sum += (int)chosenArea.Key;
            }

            if (!UpdateRedSettings())
            {
                return;
            }

            UpdateDB(sum);

            if (!isRunSwitchOff)
            {
                Thread.Sleep(1000);
                CAuxil.SetTagValue("true", "CDS_RESTART");
                Thread.Sleep(1000);
            }

            MessageBox.Show("Настройки применены!", "Настройки резервирования", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool GetAreasDataFromDB()
        {
            const string sql = "select * from modbus_reservation_table order by num";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (!reader.HasRows)
                {
                    const string message = "Ошибка во время загрузки настроек. Данные в базе не найдены";
                    MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                ModbusAreas = new ObservableCollection<ModbusAreas>();
                COMS = new ObservableCollection<RedundancyOption>();
                int count = 0;
                while (reader.Read())
                {
                    count++;
                    if (count < 5)
                        SetModbus(reader, count);
                    else
                        SetCOM(reader, count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при работе базы данных." + ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                reader.Close();
            }

            return true;
        }

        private void SetModbus(NpgsqlDataReader reader, int num)
        {
            ModbusAreas modbusArea = new ModbusAreas();
            modbusArea.SetName(reader["name"].ToString());
            modbusArea.NumForWindow = num;
            modbusArea.IsChoosen = Convert.ToBoolean(reader["state"]);
            modbusArea.Key = Convert.ToInt32(reader["key"]);
            ModbusAreas.Add(modbusArea);
        }
        private void SetCOM(NpgsqlDataReader reader, int num)
        {
            //COM порты в базе начинаются с 5, а значит надо вычитать 4
            RedundancyOption com = new RedundancyOption();
            com.NumForWindow = num - 4;
            com.Name = "COM" + com.NumForWindow.ToString();
            com.RealNum = num;
            com.IsChoosen = Convert.ToBoolean(reader["state"]);
            com.Discription = "Вкл/выкл в Пассивном режиме";
            com.Type = devices.FirstOrDefault(device => device.Key[0] <= num - 4 && device.Key[1] >= num - 4).Value;
            COMS.Add(com);
        }
        private void GetConnectionParams(int plcId, int num, ref TextBox activeIp, ref TextBox passiveIp, ref TextBox port)
        {
            Dictionary<int, string[]> redSettings = new Dictionary<int, string[]>();
            string ipAddressLocal = iniCFG.ReadValue("cmpredundancyconnectionip", $"link{num}.ipaddresslocal");
            string ipAddressPeer = iniCFG.ReadValue("cmpredundancyconnectionip", $"link{num}.ipaddresspeer");
            port.Text = iniCFG.ReadValue("cmpredundancyconnectionip", $"link{num}.port");
            redSettings.Add(1, new string[] { ipAddressLocal, ipAddressPeer });
            redSettings.Add(2, new string[] { ipAddressPeer, ipAddressLocal });
            activeIp.Text = redSettings[plcId][0];
            passiveIp.Text = redSettings[plcId][1];

        }
        private void WriteConnectionParams(int plcId, int num, TextBox activeIp, TextBox passiveIp, TextBox port)
        {
            Dictionary<int, string[]> redSettings = new Dictionary<int, string[]>();
            redSettings[1] = new string[] { activeIp.Text, passiveIp.Text };
            redSettings[2] = new string[] { passiveIp.Text, activeIp.Text };
            iniCFG.WriteValue("cmpredundancyconnectionip", $"link{num}.ipaddresslocal", redSettings[plcId][0]);
            iniCFG.WriteValue("cmpredundancyconnectionip", $"link{num}.ipaddresspeer", redSettings[plcId][1]);
            iniCFG.WriteValue("cmpredundancyconnectionip", $"link{num}.port".ToLower(), port.Text);
        }
        private bool UpdateRedSettings()
        {
            if (CheckParams())
                return false;

            int redState = 1 + Convert.ToInt32(Passive.IsChecked == true);
            WriteConnectionParams(redState, 1, ActiveIP, PassiveIP, Port);
            iniCFG.WriteValue("cmpredundancy", "plcident", redState.ToString());
            iniCFG.WriteValue("cmpredundancy", "standbywaittime", StandbyWaitTime.Text);
            iniCFG.WriteValue("cmpredundancy", "locktimeout", LockTimeout.Text);
            iniCFG.WriteValue("cmpredundancy", "bootupwaittime", BootupWaitTime.Text);
            iniCFG.WriteValue("cmpredundancy", "tcpwaittime", TcpWaitTime.Text);
            iniCFG.WriteValue("cmpredundancy", "bootproject", BootProject.Text);
            //До сборки 5.2.0 был syncwaittime
            if (CGlobal.CurrState.AssemblyInt < 520)
            {
                iniCFG.WriteValue("cmpredundancy", "syncwaittime", DataWaitTime.Text);
            }
            else
            {
                WriteConnectionParams(redState, 2, ExtraActiveIP, ExtraPassiveIP, ExtraPort);
                iniCFG.WriteValue("cmpredundancy", "datawaittime", DataWaitTime.Text);
            }
            iniCFG.WriteValue("cmpredundancy", "redundancytaskname", RedundancyTaskName.Text);
            iniCFG.WriteValue("cmpredundancy", "datasyncalways", Convert.ToInt32(DataSyncAlways.IsChecked).ToString());
            if (CGlobal.Handler.Auth.Account != "root")
            {
                iniCFG.WriteValue("cmpredundancy", "debugmessages", "0");
                iniCFG.WriteValue("cmpredundancy", "debugmessagestasktime", "0");
            }
            else
            {
                iniCFG.WriteValue("cmpredundancy", "debugmessages", Convert.ToInt32(DebugMessages.IsChecked).ToString());
                iniCFG.WriteValue("cmpredundancy", "debugmessagestasktime", Convert.ToInt32(DebugMessagesTaskTime.IsChecked).ToString());
            }
            SetServices();
            iniCFG.ExtraSpace = false;
            sshClient.WriteFile("/etc/CODESYSControl.cfg", iniCFG.GetStream());
            return true;
        }
        private void SetServices()
        {
            iniCFG.ClearSection("cmpsrv");
            iniCFG.WriteValue("cmpsrv", "service.waittime", ServiceWaitTime.Text);
            iniCFG.WriteValue("cmpsrv", "Service.0.ServiceGroup", "1");
            iniCFG.WriteValue("cmpsrv", "Service.0.Service", "2");
            iniCFG.WriteValue("cmpsrv", "Service.1.ServiceGroup", "1");
            iniCFG.WriteValue("cmpsrv", "Service.1.Service", "3");
            iniCFG.WriteValue("cmpsrv", "Service.2.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.2.Service", "1");
            iniCFG.WriteValue("cmpsrv", "Service.3.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.3.Service", "2");
            iniCFG.WriteValue("cmpsrv", "Service.4.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.4.Service", "35");
            iniCFG.WriteValue("cmpsrv", "Service.5.ServiceGroup", "8");
            iniCFG.WriteValue("cmpsrv", "Service.5.Service", "2");
            iniCFG.WriteValue("cmpsrv", "Service.6.ServiceGroup", "8");
            iniCFG.WriteValue("cmpsrv", "Service.6.Service", "3");
            iniCFG.WriteValue("cmpsrv", "Service.7.ServiceGroup", "8");
            iniCFG.WriteValue("cmpsrv", "Service.7.Service", "4");
            iniCFG.WriteValue("cmpsrv", "Service.8.ServiceGroup", "8");
            iniCFG.WriteValue("cmpsrv", "Service.8.Service", "8");
            iniCFG.WriteValue("cmpsrv", "Service.9.ServiceGroup", "8");
            iniCFG.WriteValue("cmpsrv", "Service.9.Service", "9");
            iniCFG.WriteValue("cmpsrv", "Service.10.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.10.Service", "17");
            iniCFG.WriteValue("cmpsrv", "Service.11.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.11.Service", "16");
            iniCFG.WriteValue("cmpsrv", "Service.12.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.12.Service", "18");
            iniCFG.WriteValue("cmpsrv", "Service.13.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.13.Service", "34");
            iniCFG.WriteValue("cmpsrv", "Service.14.ServiceGroup", "27");
            iniCFG.WriteValue("cmpsrv", "Service.14.Service", "2");
            iniCFG.WriteValue("cmpsrv", "Service.15.ServiceGroup", "27");
            iniCFG.WriteValue("cmpsrv", "Service.15.Service", "3");
            iniCFG.WriteValue("cmpsrv", "Service.16.ServiceGroup", "2");
            iniCFG.WriteValue("cmpsrv", "Service.16.Service", "32");
        }
        private bool CheckParams()
        {
            bool error = false;
            foreach (var child in RedGrid.Children)
            {
                bool state;
                Expander expander = child as Expander;
                if (expander == null)
                {
                    continue;
                }
                Grid grid = expander.Content as Grid;
                foreach (var child2 in grid.Children)
                {
                    TextBox textBox = child2 as TextBox;
                    if (textBox == null)
                        continue;

                    if (!limits.ContainsKey(textBox.Name))
                        continue;

                    if (textBox.Text == "")
                        textBox.Text = limits[textBox.Name][0].ToString();

                    int low = limits[textBox.Name][0];
                    int high = limits[textBox.Name][1];
                    int value = Convert.ToInt32(textBox.Text);
                    state = value >= low && value <= high;
                    if (state)
                        textBox.ClearValue(Border.BorderBrushProperty);
                    else
                        textBox.BorderBrush = Brushes.Red;

                    if (state)
                        continue;

                    error = true;
                }
            }
            if (error)
                MessageBox.Show("Выход за границы допустимых значений", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            return error;
        }
        private void DataValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            TextBox textBox = sender as TextBox;
            if (textBox.Name.Contains("ActiveIP") || textBox.Name.Contains("PassiveIP"))
                range.Add(".");

            e.Handled = !range.Contains(e.Text);
        }

        private bool IsValidIPv4(string ip)
        {
            if (String.IsNullOrWhiteSpace(ip))
            {
                return false;
            }

            string[] splitValues = ip.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        private bool CheckValidIp(TextBox textBox)
        {
            if (textBox.Text == "")
            {
                textBox.Text = "0.0.0.0";
            }

            if (!IsValidIPv4(textBox.Text))
            {
                textBox.Focus();
                textBox.BorderBrush = Brushes.Red;
                MessageBox.Show(string.Format("Invalid IP: {0}!", textBox.Text), Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            textBox.ClearValue(BorderBrushProperty);

            return true;
        }
        private void UpdateDB(int sum)
        {
            string sqlCommand = "UPDATE modbus_reservation_table" +
                "\nSET state = CASE num";
            string sqlID = "1";
            int counter = ModbusAreas.Count() + COMS.Count();
            for (int i = 2; i < counter + 1; i++)
                sqlID += "," + i.ToString();
            foreach (ModbusAreas modbusArea in ModbusAreas)
                sqlCommand += $"\nWHEN {modbusArea.NumForWindow} THEN {modbusArea.IsChoosen}";
            foreach (RedundancyOption com in COMS)
                sqlCommand += $"\nWHEN {com.RealNum} THEN {com.IsChoosen}";
            sqlCommand += "\nend" +
                $"\nwhere num in ({sqlID})";

            NpgsqlCommand cmd = new NpgsqlCommand(sqlCommand, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();

            sqlCommand = String.Format("update fast_table set value='{0}', changed=true where tag='{1}'", sum, "MODBUS_RESERVATION");
            cmd = new NpgsqlCommand(sqlCommand, connection);
            cmd.ExecuteNonQuery();

            sqlCommand = String.Format("update fast_table set value='{0}', changed=true where tag='{1}'", Convert.ToInt16(Duplication.IsChecked), "DUPLICATION_MODBUS");
            cmd = new NpgsqlCommand(sqlCommand, connection);
            cmd.ExecuteNonQuery();
        }

        private void SV_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double data = SV.VerticalOffset;
            //if(data)
        }
    }

    public class RedundancyOption
    {
        public string Name { get; set; }
        //Номер для базы
        public int RealNum { get; set; }
        //Номер для окна
        public int NumForWindow { get; set; }
        public bool IsChoosen { get; set; }
        public string Discription { get; set; }
        //Ключ для ЦПУ
        public object Key { get; set; }

        public string Type { get; set; }
    }

    public class ModbusAreas : RedundancyOption
    {
        private Dictionary<string, string> localization = new Dictionary<string, string>
            {
                { "Real", "Реальное адресное пространство" },
                { "Virtual_1", "Первое виртуальное адресное пространство" },
                { "Virtual_2", "Второе виртуальное адресное пространство" },
                { "Virtual_3", "Третье виртуальное адресное пространство" },
            };

        public void SetName(string areaName)
        {
            Name = localization[areaName];
        }
    }

}