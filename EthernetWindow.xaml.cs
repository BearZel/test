using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;
using Npgsql;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для EthernetWindow.xaml
    /// </summary>
    public partial class EthernetWindow : Window, INotifyPropertyChanged
    {
        //класс управляющий списком Ethernet интерфейсов
        private CEthernetIterfacesList interfaces;
        //Копия описания физических интерфейсов контроллера, для того что бы с ними работать
        private ObservableCollection<CEthernetInterface> copyInterfacesList = new ObservableCollection<CEthernetInterface>();
        //Описания физических интерфейсов контроллера, для того что бы можно было сличать с ними значения
        private ObservableCollection<CEthernetInterface> interfacesList = new ObservableCollection<CEthernetInterface>();
        //Фокус
        Dictionary<string, Delegate> FocusState = new Dictionary<string, Delegate>();
        private Dictionary<string, Delegate> textBoxes = new Dictionary<string, Delegate>();
        //Картинки
        Dictionary<string, string> Images = new Dictionary<string, string>();
        Dictionary<int, string> Sources = new Dictionary<int, string>();
        //Последний интерфейс с которым работал пользователе
        private String lastNetClass = "";
        //Ссылка на экземпляр класса работающего с SSH клиентом
        private CSSHClient sshClient = null;
        private CEthernetInterface selectedInterface = null;
        public bool isTheSame = false;
        private List<string> accessibleOctets = new List<string>()
        {
            {"128"}, {"192"}, {"224"}, {"240"}, {"248"}, {"252"}, {"254"}, {"255"}, {"0"}
        };
        public EthernetWindow(CSSHClient sshClient)
        {
            InitializeComponent();
            this.sshClient = sshClient;
            this.interfaces = new CEthernetIterfacesList(this.sshClient);
            FillDictionary();
        }

        private void FillDictionary()
        {
            Images.Add("IPBox", "IPImage");
            Images.Add("MaskBox", "MaskImage");
            Images.Add("GlobalIPBox", "GlobalIPImage");
            Images.Add("GateBox", "GatewayImage");

            Sources.Add(1, "pack://application:,,,/AbakConfigurator;component/icons/CheckMark.png");
            Sources.Add(0, "pack://application:,,,/AbakConfigurator;component/icons/CrossMark.png");
            Sources.Add(2, "pack://application:,,,/AbakConfigurator;component/icons/empty480.png");
        }

        private void updateInterfacesList()
        {
            //Подгрузка списка сетевых интерфейсов
            this.interfaces.LoadInterfaces();
            //Формирование копии списка для отслеживания изменений
            this.interfacesList.Clear();
            this.copyInterfacesList.Clear();
            foreach (CEthernetInterface eth in this.interfaces.InterfacesList)
            {
                this.interfacesList.Add(eth);
                CEthernetInterface ethCopy = new CEthernetInterface();
                ethCopy.Assign(eth);
                this.copyInterfacesList.Add(ethCopy);
            }

            //Выбор элемента по умолчанию
            this.selectedInterface = null;
            this.listBox.SelectedItem = null;
            this.OnPropertyChanged("SelectedInterface");
            if (this.copyInterfacesList.Count > 0)
            {
                if (this.lastNetClass != "")
                {
                    //До этого работали с одним из интерфейсов, надо его показать
                    this.selectedInterface = this.copyInterfacesList.Single(eth => eth.NetClass.Contains(this.lastNetClass));
                }
                //Не удалоьс найти того с кем работали, значит берётся первый по списку
                if (this.selectedInterface == null)
                    this.selectedInterface = this.interfacesList[0];

                this.listBox.SelectedItem = this.selectedInterface;
                this.OnPropertyChanged("SelectedInterface");
            }
            this.OnPropertyChanged("IsInterfaceSelected");
        }

        private void EthernetWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.lastNetClass = "";
                this.updateInterfacesList();
                if (interfaces.PingSettings == null)
                {
                    PingIpGroupBox.Visibility = PingIpCheckbox.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        public ObservableCollection<CEthernetInterface> InterfacesList
        {
            get
            {
                return this.copyInterfacesList;
            }
        }

        private void RefreshButton_Handler(object sender, RoutedEventArgs e)
        {
            try
            {
                this.lastNetClass = "";
                this.updateInterfacesList();
            }
            catch
            {
                Close();
            }
        }

        private void ListBoxItemMouseUp_Handler(object sender, RoutedEventArgs e)
        {
            this.selectedInterface = this.listBox.SelectedItem as CEthernetInterface;
            UpdateWindow();
            CheckDHCP();
        }

        private void UpdateWindow()
        {
            this.OnPropertyChanged("SelectedInterface");
            this.OnPropertyChanged("IsInterfaceSelected");
            DnsGrid.ItemsSource = this.selectedInterface.dnsServers;
            DnsGrid.CellEditEnding += CellChanged_EventHandler;
            StaticRouteGrid.ItemsSource = this.selectedInterface.staticRoutes;
            CEthernetInterface eth = interfacesList.FirstOrDefault(x => x.NetClass == selectedInterface.NetClass);
            StaticIPBox.IsChecked = !eth.DHCP;
            DynamicIPBox.IsChecked = eth.DHCP;
            if (selectedInterface.DHCP)
                GlobalIPGroupBox.IsEnabled = GlobalIpCheckbox.IsEnabled = false;
        }

        public CEthernetInterface SelectedInterface
        {
            get
            {
                return this.selectedInterface;
            }
        }

        public Boolean IsInterfaceSelected
        {
            get
            {
                return this.selectedInterface != null;
            }
        }

        /// <summary>
        /// Сканирование доступных WiFi сетей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanWifi_Handler(object sender, RoutedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                return;

            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            List<CWifiInfo> list = interfaces.ScanWifi(eth);
            WiFiListWindow wifiWindow = new WiFiListWindow();
            wifiWindow.Owner = this;
            wifiWindow.WifiList.Clear();
            foreach (CWifiInfo info in list)
                wifiWindow.WifiList.Add(info);
            if (wifiWindow.ShowDialog() == true)
            {
                if (wifiWindow.SelectedWiFi == null)
                    return;

                eth.AccessPoint = wifiWindow.SelectedWiFi.Name;
                eth.SSID = wifiWindow.SelectedWiFi.SSID;
                eth.Opened = wifiWindow.SelectedWiFi.Opened;
            }
        }

        /// <summary>
        /// Сброс настроек WiFi сети
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetWifi_Handler(object sender, RoutedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                return;

            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            String mess = String.Format("{0} \"{1}\"?", CGlobal.GetResourceValue("l_resetWifiQuestion"), eth.Description);
            if (MessageBox.Show(mess, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            this.interfaces.ResetInterfaceSettings(eth);
            this.lastNetClass = eth.NetClass;
            this.updateInterfacesList();
        }

        /// <summary>
        /// Установка соединения с wifi сеткой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectWifi_Handler(object sender, RoutedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                return;

            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            interfaces.ConnectWifi(eth);
            this.lastNetClass = eth.NetClass;
            this.updateInterfacesList();
        }

        /// <summary>
        /// Отключение Wifi сети
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectWifi_Handler(object sender, RoutedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                return;

            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            interfaces.DisconnectWifi(this.listBox.SelectedItem as CEthernetInterface);
            this.lastNetClass = eth.NetClass;
            this.updateInterfacesList();
        }

        private void InterfaceSettingsGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                this.InterfaceSettingsGrid.Visibility = System.Windows.Visibility.Hidden;
            else
                this.InterfaceSettingsGrid.Visibility = System.Windows.Visibility.Visible;
        }
        private void applyUpdates_Handler(object sender, RoutedEventArgs e)
        {
            //Kill logical focus
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(DnsGrid), null);
            //Kill keyboard focus
            Keyboard.ClearFocus();
            foreach (DnsServers dnsServer in DnsGrid.Items)
            {
                if (!CheckDns(dnsServer.IP))
                    return;
            }
            //Применение настроек
            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            if (eth == null)
                return;
            CEthernetInterface eth1 = interfacesList.FirstOrDefault(x => x.NetClass == eth.NetClass);
            String mess = String.Format("{0} \"{1}\"?", CGlobal.GetResourceValue("l_applySettingsQuestion"), eth.Description);
            if (MessageBox.Show(mess, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            WriteJSON();
            interfaces.SaveToJson(eth);
            if (!IsChanged())
            {
                return;
            }
            string cable = String.Format("ethernet_{0}_cable", eth.MacAddress.Replace(":", ""));
            string cmd = $"mkdir -p /var/lib/connman/{cable}";
            sshClient.ExecuteCommand(cmd);
            int state = this.interfaces.ChangeEthernetSettings(eth);
            interfaces.ChangeResolveFile(eth);
            interfaces.UpdateBackupSettings(eth);
            if (state == 1)
            {
                isTheSame = true;
                MessageBox.Show("Для продолжения работы необходимо переподключиться к ПЛК", "Связь с ПЛК отключена!", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
                return;
            }
            Thread.Sleep(3000);
            MessageBox.Show($"Настройки для '{eth.NetClass}' применены!", $"Настройка '{eth.NetClass}'", MessageBoxButton.OK, MessageBoxImage.Information);
            this.lastNetClass = eth.NetClass;
            this.updateInterfacesList();
            UpdateWindow();
            CGlobal.Handler.UserLog(2, string.Format($"Change {eth.NetClass} settings"));
        }
        private void WriteJSON()
        {
            if (interfaces.PingSettings == null)
            {
                return;
            }
            bool isError = false;
            //Береём то кол-во портов, что в ЦПУ.
            //Нужно если модули IM сняли, но потом могут вернуть + для совместимости с K4
            foreach (JObject jToken in interfaces.PingSettings.First.First.Children())
            {
                string netClass = jToken["interface"].ToString();
                CEthernetInterface eth = copyInterfacesList.FirstOrDefault(x => x.NetClass == netClass);
                if (eth == null)
                    continue;

                if (!CheckPingSettings(eth, out string error))
                {
                    return;
                }

                jToken["enable"] = eth.IsPingOn;
                jToken["ip"] = eth.PingIP;
                jToken["errors"] = Convert.ToInt32(eth.MinErrors);
                int timeout = Convert.ToInt32(eth.TimeOut);
                if (timeout < 500)
                {
                    isError = true;
                    eth.TimeOut = "500";
                }
                jToken["timeout"] = Convert.ToInt32(eth.TimeOut);
            }
            if(isError)
            {
                MessageBox.Show("Таймаут не может быть меньше 500 мс", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            string json = $"{interfaces.PingSettings}";
            Stream stream = CAuxil.StringToStream(json);
            CGlobal.Session.SSHClient.WriteFile("/opt/abak/A:/assembly/configs/ping_service.config", stream);

            string sql = "NOTIFY ping_service_changed";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();
        }
        private bool CheckPingSettings(CEthernetInterface eth, out string error)
        {
            error = "";
            if (!eth.IsPingOn)
            {
                return true;
            }

            if (CheckBaseData(eth.PingIP) != 1)
            {
                SetFocus(eth);
                MessageBox.Show("Неверный формат IP адреса", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            int timeOut = Convert.ToInt32(eth.TimeOut);
            if (timeOut < 500)
            {
                SetFocus(eth);
                MessageBox.Show("Нельзя указывать таймаут меньше 500 мс", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
        private void SetFocus(CEthernetInterface eth)
        {
            listBox.SelectedItem = eth;
            ListBoxItem listBoxItem = (ListBoxItem)listBox.ItemContainerGenerator
                .ContainerFromItem(listBox.SelectedItem);
            if (listBoxItem == null)
                return;

            listBoxItem.Focus();
            Keyboard.Focus(listBoxItem);
            selectedInterface = (CEthernetInterface)listBox.SelectedItem;
            this.OnPropertyChanged("SelectedInterface");
            this.OnPropertyChanged("IsInterfaceSelected");
        }
        private bool IsChanged()
        {
            CEthernetInterface ethNew = this.listBox.SelectedItem as CEthernetInterface;
            if (ethNew == null)
            {
                return false;
            }
            CEthernetInterface ethOld = interfacesList.FirstOrDefault(x => ethNew.NetClass == x.NetClass);

            if (ethOld == null)
            {
                return false;
            }
            
            bool isChanged = ethOld.IP != ethNew.IP
                || ethOld.NetMask != ethNew.NetMask
                || ethOld.Gateway != ethNew.Gateway
                || ethOld.Speed != ethNew.Speed
                || ethOld.Duplex != ethNew.Duplex
                || ethOld.GLobalIP != ethNew.GLobalIP
                || ethOld.IsGlobalIpOn != ethNew.IsGlobalIpOn
                || ethNew.dnsServers.Any(x => x.IsChanged)
                || ethOld.DHCP != ethNew.DHCP 
                || ethOld.StatIP != ethNew.StatIP
                || !ethNew.staticRoutes.SequenceEqual(ethOld.staticRoutes);

            return isChanged;
        }
        private void discardUpdates_Handler(object sender, RoutedEventArgs e)
        {
            //Сброс сделанных настроек
            CEthernetInterface eth = this.listBox.SelectedItem as CEthernetInterface;
            if (eth == null)
                return;

            int index = this.copyInterfacesList.IndexOf(eth);
            eth.Assign(this.interfacesList[index]);
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ShowInterfaceInfoButton_Handler(object sender, RoutedEventArgs e)
        {
            //Показывает окно со статистикой сетевого интерфейса
            EthernetStatisticWindow statisticWindow = new EthernetStatisticWindow(this.sshClient, this.selectedInterface.NetClass);
            statisticWindow.Owner = this;
            statisticWindow.Show();
        }

        //Контроль ввода данных
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox numb = sender as TextBox;

                List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "." };
                string[] str = numb.Text.Split('.');
                if (str.Count() == 4)
                    range.Remove(".");

                e.Handled = !range.Contains(e.Text);
            }
            catch
            {

            }
        }
        private void PingValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            try
            {
                TextBox numb = sender as TextBox;

                List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                string[] str = numb.Text.Split('.');
                if (str.Count() == 4)
                    range.Remove(".");

                e.Handled = !range.Contains(e.Text);
            }
            catch
            {

            }
        }
        private void DNSNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "." };
            e.Handled = !range.Contains(e.Text);
        }

        private void FirstNumb_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox get_numb = sender as TextBox;
            bool setfocus = FocusState.ContainsKey(get_numb.Name) && get_numb.Text.Count() >= 3;
            if (setfocus)
                FocusState[get_numb.Name].DynamicInvoke();
        }

        private bool CheckNetBox()
        {
            string[] str = selectedInterface.NetMask.Split('.');
            IEnumerable<string> numbers = str.Where(item => item != "");
            bool isZero = false;
            foreach (string s in numbers)
            {
                if (!accessibleOctets.Contains(s))
                    return false;

                //Так нельзя, нули могут быть только в конце
                bool isCorrect = isZero && s != "0";
                if (isCorrect)
                    return false;

                isZero = s == "0";
            }

            return true;
        }

        private bool CheckIP()
        {
            string[] globalIP = selectedInterface.GLobalIP.Split('.');
            string[] ethIP = selectedInterface.IP.Split('.');
            if (globalIP.Length != 4 || ethIP.Length != 4)
                return false;

            bool isTheSame = globalIP[2] == ethIP[2];

            return isTheSame;
        }

        private int CheckBaseData(string content)
        {
            try
            {
                if (content == "" || content == "default")
                    return 2;

                string[] str = content.Split('.');
                IEnumerable<string> numbers = str.Where(item => item != "");
                bool isOk = !numbers.Any(numb => Convert.ToInt64(numb) > 255)
                            && numbers.Count() == 4
                            && !numbers.Any(numb => numb.Length > 3);

                return Convert.ToInt32(isOk);
            }
            catch
            {
                return 2;
            }
        }
        private void CheckAllIP()
        {
            try
            {
                int IpState = CheckBaseData(selectedInterface.IP);
                IPImage.Source = new BitmapImage(new Uri(Sources[IpState], UriKind.RelativeOrAbsolute));
            }
            catch
            {

            }
        }
        private void OrdinaryIP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(selectedInterface == null)
            {
                return;
            }
            CheckIpAddres();
            if (selectedInterface.IsGlobalIpOn)
            {
                WriteGlobalIP();
            }
        }
        private void WriteGlobalIP()
        {
            string[] str = selectedInterface.IP.Split('.');
            if (str.Length != 4)
            {
                if (str.Length == 1)
                {
                    if (selectedInterface.IP == "")
                    {
                        selectedInterface.PrefixGLobalIP = "0.0.0.";
                        selectedInterface.PrefixLocalGLobalIP  = "0.0.0.";
                    }
                    else
                    {
                        selectedInterface.PrefixGLobalIP = selectedInterface.IP;
                        selectedInterface.PrefixLocalGLobalIP = selectedInterface.IP;
                    }
                }
                else
                {
                    selectedInterface.PrefixGLobalIP = selectedInterface.IP;
                    selectedInterface.PrefixLocalGLobalIP = selectedInterface.IP;
                }
            }
            else
            {
                selectedInterface.PrefixGLobalIP = $"{str[0]}.{str[1]}.{str[2]}.";
                //selectedInterface.PrefixLocalGLobalIP = $"{str[0]}.{str[1]}.{str[2]}.";
            }
        }
        private void IP_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckIpAddres();
        }
        private bool CheckIpAddres()
        {
            try
            {
                if (selectedInterface == null)
                    return false;

                if (interfaces.GlobalEthsSettings.ContainsKey(selectedInterface.NetClass))
                {
                    CheckAllIP();
                    return false;
                }

                int ipStatus = CheckBaseData(selectedInterface.IP);
                IPImage.Source = new BitmapImage(new Uri(Sources[ipStatus], UriKind.RelativeOrAbsolute));
            }
            catch
            {
                return false;
            }
            return true;
        }
        private void Mask_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (selectedInterface == null)
                    return;

                if (selectedInterface.NetMask == "default")
                {
                    selectedInterface.NetMask = "";
                    MaskImage.Source = new BitmapImage(new Uri(Sources[2], UriKind.RelativeOrAbsolute));
                    return;
                }
                int isCorrect = CheckBaseData(selectedInterface.NetMask);
                if (isCorrect != 1)
                {
                    MaskImage.Source = new BitmapImage(new Uri(Sources[isCorrect], UriKind.RelativeOrAbsolute));
                    return;
                }

                isCorrect = Convert.ToInt32(CheckNetBox());
                MaskImage.Source = new BitmapImage(new Uri(Sources[isCorrect], UriKind.RelativeOrAbsolute));
            }
            catch
            {

            }
        }

        private void Element_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedInterface == null)
                return;

            int isCorrect = CheckBaseData(selectedInterface.Gateway);
            GatewayImage.Source = new BitmapImage(new Uri(Sources[Convert.ToInt32(isCorrect)], UriKind.RelativeOrAbsolute));
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
                e.Handled = true;
        }

        private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.selectedInterface = null;
            this.OnPropertyChanged("SelectedInterface");
            this.selectedInterface = this.listBox.SelectedItem as CEthernetInterface;
            this.OnPropertyChanged("SelectedInterface");
            this.OnPropertyChanged("IsInterfaceSelected");
        }

        private void GlobalIpCheckbox_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInterface == null)
                return;

            WriteGlobalIP();
        }

        private Visibility SetVisibility(bool state)
        {
            if (!state)
                return Visibility.Visible;

            return Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (!isTheSame)
                return;
        }

        public void UpdateEth()
        {
            try
            {
                this.interfaces.UpdateSettingsAfterClosing();
            }
            catch { }
        }

        private void AddStaticRouteColumn_Click(object sender, RoutedEventArgs e)
        {
            int counter = selectedInterface.staticRoutes.Count;
            if (counter == 5)
            {
                MessageBox.Show("Не допускается использование более пяти маршрутов",
                    "Максимальное значение маршрутов",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StaticRoute newRoute = new StaticRoute(counter + 1, "0.0.0.0", "0.0.0.0");
            selectedInterface.staticRoutes.Add(newRoute);
            selectedInterface.Changed = true;
        }

        private void DeleteStaticRouteColumn_Click(object sender, RoutedEventArgs e)
        {
            int count = selectedInterface.staticRoutes.Count();
            if (count <= 0)
                return;

            this.selectedInterface.staticRoutes.RemoveAt(count - 1);
            selectedInterface.Changed = true;
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            int counter = this.selectedInterface.dnsServers.Count();
            if (counter == 5)
            {
                MessageBox.Show("Не допускается использование более пяти DNS-серверов",
                    "Максимальное значение DNS-серверов",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DnsServers dnsServers = new DnsServers();
            dnsServers.Num = counter + 1;
            selectedInterface.dnsServers.Add(dnsServers);
            selectedInterface.Changed = true;
        }

        private void DeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            int count = selectedInterface.dnsServers.Count();
            if (count <= 0)
                return;

            DnsServers dnsServers = this.selectedInterface.dnsServers[count - 1];
            this.selectedInterface.dnsServers.Remove(dnsServers);
            selectedInterface.Changed = true;
        }
        //Проверка DNS
        void CellChanged_EventHandler(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            var column = e.Column as DataGridBoundColumn;
            if (column == null)
                return;

            TextBox textBox = e.EditingElement as TextBox;
            if (textBox.Text == "")
                return;

            CheckDns(textBox.Text);
            CEthernetInterface eth = interfacesList.FirstOrDefault(x => x.NetClass == selectedInterface.NetClass);
        }
        private bool CheckDns(string text)
        {
            if (text == "")
                return true;

            string error = "";
            try
            {
                string[] str = text.Split('.');

                IEnumerable<string> numbers = str.Where(item => item != "");
                bool isOk = str.Length == 4 && numbers.Count() == 4;
                if (!isOk)
                    error = "Неверный формат!";

                isOk = !numbers.Any(numb => Convert.ToInt64(numb) > 255);
                if (!isOk)
                    error = "Превышение границ!";

                if (error != "")
                    MessageBox.Show(error, "Ошибка ввода DNS!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                error = "Ошибка ввода";
                MessageBox.Show(error, "Ошибка ввода DNS!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            selectedInterface.Changed = error == "";
            return error == "";
        }
        private void CheckDHCP()
        {
            if (selectedInterface.DHCP)
                GlobalIPGroupBox.IsEnabled = selectedInterface.IsGlobalIpOn = GlobalIpCheckbox.IsEnabled = false;
            else
            {
                CEthernetInterface eth = interfacesList.FirstOrDefault(x => x.NetClass == selectedInterface.NetClass);
                GlobalIPGroupBox.IsEnabled = GlobalIpCheckbox.IsEnabled = true;
                if (eth.IsGlobalIpOn)
                    selectedInterface.IsGlobalIpOn = true;
            }
            if (interfaces.GlobalEthsSettings.ContainsKey(selectedInterface.NetClass))
                CheckAllIP();
        }
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            CheckDHCP();
        }
        private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = e.Command == ApplicationCommands.Paste;
        }

        private void GlobalIpP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            PrefixTextBox prefixTextBox = sender as PrefixTextBox;
            if(prefixTextBox.Name == "GlobalIpPrefix")
            {
                if(!CAuxil.CheckStringForInt(selectedInterface.GLobalIP + e.Text, false))
                {
                    e.Handled = true;
                    return;
                }
                int ip = Convert.ToInt32(selectedInterface.GLobalIP + e.Text);
                if (ip > 255)
                {
                    e.Handled = true;
                    return;
                }
                selectedInterface.GLobalIP = ip.ToString();
            }
            if (prefixTextBox.Name == "LocalGlobalIpPrefix")
            {
                int ip = Convert.ToInt32(selectedInterface.LocalGlobalIP + e.Text);
                if (ip > 255)
                {
                    e.Handled = true;
                    return;
                }
                //selectedInterface.LocalGlobalIP = ip.ToString();
            }
        }
        private void GlobalIpPrefix_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            PrefixTextBox prefixTextBox = sender as PrefixTextBox;

            if (e.Key == Key.Delete || e.Key == Key.Back) 
            {
                if (prefixTextBox.Name == "GlobalIpPrefix" && selectedInterface.GLobalIP.Length != 0)
                {
                    selectedInterface.GLobalIP.Remove(selectedInterface.GLobalIP.Length - 1);
                }
                if(prefixTextBox.Name == "LocalGlobalIpPrefix" && selectedInterface.LocalGlobalIP.Length != 0)
                {
                    selectedInterface.LocalGlobalIP.Remove(selectedInterface.LocalGlobalIP.Length - 1);
                }
            }
        }
    }
}