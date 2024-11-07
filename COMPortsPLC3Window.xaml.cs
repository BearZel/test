using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AbakConfigurator
{
    /// <summary>
    /// Окошка в котором реализована логика управления последовательными портами
    /// </summary>
    public partial class COMPortsPLC3Window : Window
    {
        //Для Ser2Net
        private Dictionary<int, string> parityList = new Dictionary<int, string>
        {
            {0, "NONE"},
            {1, "ODD"},
            {2, "EVEN"}
        };
        //Для консольной команды
        private Dictionary<STOPBITS_TYPE, string> stopBitList = new Dictionary<STOPBITS_TYPE, string>
        {
            {STOPBITS_TYPE.STOPBITS_ONE, "-cstopb"},
            {STOPBITS_TYPE.STOPBITS_TWO, "cstopb"}
        };
        //None = -parenb
        //Odd = parenb parodd
        //Even = parenb -parodd
        private Dictionary<int, string> parityListForCMD = new Dictionary<int, string>
        {
            {0, "-parenb"}, 
            {1, "parenb parodd"},
            {2, "parenb -parodd"}
        };
        //Описания физических интерфейсов контроллера, для того что бы можно было сличать с ними значения
        private ObservableCollection<CComPort> portsList = new ObservableCollection<CComPort>();
        //Текущее соединение с базой данных
        private NpgsqlConnection connection;
        //2001:raw:0:\/dev\/ttyO1:9600 NONE 8DATABITS 1STOPBIT -XONXOFF -RTSCTS LOCAL  
        public ObservableCollection<CComPort> PortsList { get => portsList; }
        //Экземпляр для прописывание портов в nftables
        private FirewallWindow firewallWindow = null;
        private bool isAssembly510 = false;
        private const string ser2net = "/etc/ser2net.conf";
        public COMPortsPLC3Window(NpgsqlConnection connection, CSSHClient sshClient)
        {
            InitializeComponent();
            this.connection = connection;
            firewallWindow = new FirewallWindow(sshClient, false);
        }

        /// <summary>
        /// Функция загружает из базы список последовательных интерфейсов контроллера
        /// </summary>
        private void loadPortsList()
        {
            this.portsList.Clear();

            String sql = "select * from tty_settings order by num";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (!reader.HasRows)
                        return;

                    while (reader.Read())
                    {
                        CComPort port = new CComPort();
                        port.SetValuesFromSQL(reader);

                        this.portsList.Add(port);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            catch
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int numb = CGlobal.CurrState.AssemblyHi * 1000
               + CGlobal.CurrState.AssemblyMid * 100 + CGlobal.CurrState.AssemblyLo;
            //ASSEMBLY 5.1.0 => 5000 + 100 + 0 = 5100
            isAssembly510 = numb >= 5100;
            if (!isAssembly510)
                RedirectColumn.Width = IpColumn.Width = 0;
            this.loadPortsList();
        }

        private void RefreshButton_Handler(object sender, RoutedEventArgs e)
        {
            this.loadPortsList();
        }

        /// <summary>
        /// Вызов окна для изменения настроек последовательного интерфейса
        /// </summary>
        private void editCOMPortSettings()
        {
            CGlobal.Handler.UserLog(2, string.Format("Change COM settings"));

            CComPort comPort = this.comPortsListView.SelectedItem as CComPort;
            if (comPort == null)
                return;

            int savedRedirectPort = comPort.RedirectPort;
            bool savedRedirectState = comPort.IsRedirect;

            EditCOMPortWindow editCOMPort = new EditCOMPortWindow(comPort, firewallWindow.Rules, isAssembly510);
            editCOMPort.Owner = this;
            if (editCOMPort.ShowDialog() != true)
                return;

            bool isTheSame = savedRedirectPort == comPort.RedirectPort && savedRedirectState == comPort.IsRedirect;
            if (!isTheSame)
            {
                string msg = "Все COM-порты, находящиеся в режиме \"редирект\", будут перезагружены. " +
                  "Вы уверены, что хотите продолжить?";
                string title = "Внимание!";
                if (MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            comPort.Assign(editCOMPort.Port);
            UpdateDB(editCOMPort.Port);
            UpdateLinux(comPort);
            if (!isAssembly510 && isTheSame)
                return;

            UpdateSer2Net();
            UpdateFireWall(comPort);
        }
        //Запись настроект в линукс, чтобы Codesys сразу увидел
        private void UpdateLinux(CComPort comPort)
        {
            //https://unix.stackexchange.com/questions/242778/what-is-the-easiest-way-to-configure-serial-port-on-linux
            String cmd = $"sudo stty -F /dev/COM{comPort.ID} {comPort.BaudRate} {stopBitList[comPort.StopBits]}" +
                $" {parityListForCMD[(int)comPort.ParityBit]}";
            CAuxil.ExecuteSingleSSHCommand(cmd);
        }
        //Запись настроек порта в базу
        private void UpdateDB(CComPort comPort)
        {
            String sql = $"update tty_settings set baud = {comPort.BaudRate}, parity = {(int)comPort.ParityBit}," +
                         $" stopbits = {(int)comPort.StopBits}, interval = {comPort.Interval}, slave = {comPort.SlaveMode}," +
                         $" maddr = {comPort.ModbusAddr}";

            if (isAssembly510)
                sql += $", redirect = {comPort.IsRedirect}, redirect_port = {comPort.RedirectPort}";

            sql += $" where id ={comPort.ID}";

            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
            cmd.ExecuteNonQuery();
        }
        private void UpdateSer2Net()
        {
            //Обновление файла с настройками ser2net
            string rules = "";
            foreach (var port in portsList.Where(x => x.IsRedirect && x.Active))
            {
                rules += $"{port.RedirectPort}:raw:0:{port.FileName}:{port.BaudRate} {parityList[(int)port.ParityBit]}" +
                      $" 8DATABITS {(int)port.StopBits + 1}STOPBIT -XONXOFF -RTSCTS LOCAL\n";
            }
            CGlobal.Session.SSHClient.WriteFile(ser2net, CAuxil.StringToStream(rules));
            CGlobal.Session.SSHClient.ExecuteCommand("systemctl restart ser2net");
        }
        private void UpdateFireWall(CComPort comPort)
        {
            FireWallRule oldRule = firewallWindow.Rules.FirstOrDefault(port => port.Description == $"Редирект для {comPort.Name}");
            if (oldRule != null)
                firewallWindow.Rules.Remove(oldRule);

            if (comPort.IsRedirect)
            {
                FireWallRule newRule = new FireWallRule();
                newRule.Tcp = true;
                newRule.Port = comPort.RedirectPort.ToString();
                newRule.Description = $"Редирект для {comPort.Name}";
                firewallWindow.Rules.Add(newRule);
            }

            if (firewallWindow.FireWallState)
                firewallWindow.saveSettingsAsXml();
            else
                firewallWindow.SaveDataToPLC();
        }
        private void EditPortSettings_Click(object sender, RoutedEventArgs e)
        {
            this.editCOMPortSettings();
        }

        private void editButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.editCOMPortSettings();
        }

        private void deletePortsButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            //Удаление неиспользуемых системой портов
            String message = CGlobal.GetResourceValue("l_comDeletionConfirmation");
            if (MessageBox.Show(message, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                String sql = "select delete_unused_tty()";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
                cmd.ExecuteNonQuery();

                this.loadPortsList();
            }
            catch (Exception ex)
            {
                message = String.Format("{0} {1}", CGlobal.GetResourceValue("l_comDeleteError"), ex.Message);
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Сброс настроек всех портов к заводским
        private void resetPortsButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            String message = CGlobal.GetResourceValue("l_comResetPortSettingConfirm");
            if (MessageBox.Show(message, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                String sql = "select reset_tty_settings()";
                NpgsqlCommand sqlCMD = new NpgsqlCommand(sql, this.connection);
                sqlCMD.ExecuteNonQuery();
                //Очистка файла ser2net
                MemoryStream mem = new MemoryStream();
                CGlobal.Session.SSHClient.WriteFile(ser2net, mem);
                foreach (var port in portsList)
                {
                    FireWallRule rule = firewallWindow.Rules.FirstOrDefault(checkRule => checkRule.Description == $"Редирект для {port.Name}");
                    if (rule != null)
                        firewallWindow.Rules.Remove(rule);
                }
                firewallWindow.SaveDataToPLC();
                this.loadPortsList();
            }
            catch (Exception ex)
            {
                message = String.Format("{0} {1}", CGlobal.GetResourceValue("l_comResetPortSettingError"), ex.Message);
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void comPortsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.editCOMPortSettings();
        }
    }
}
