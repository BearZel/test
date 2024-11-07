using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for CanManagerSettingsWindow.xaml
    /// </summary>
    public partial class CanManagerSettingsWindow : Window, INotifyPropertyChanged
    {
        private string abakStart = "";
        public event PropertyChangedEventHandler PropertyChanged;
        public CCanBusData SelectedItem { get; private set; } = new CCanBusData();
        private readonly ObservableCollection<CCanBusData> backup = new ObservableCollection<CCanBusData>();
        public ObservableCollection<CCanBusData> CanBusDataCollection 
        { get; private set;} = new ObservableCollection<CCanBusData>();
        public CanManagerSettingsWindow()
        {
            GetJsonFromPLC();
            if (CanBusDataCollection.Count == 0)
            {
                AddCanBus();
            }
            DataContext = this;
            SelectedItem = CanBusDataCollection[0];
            InitializeComponent();
            CanListBox.SelectedItem = SelectedItem;

            Stream streamReader = CGlobal.Session.SSHClient.ReadFile("/opt/abak/A:/assembly/abak_start");
            if (streamReader == null)
                return;

            StreamReader reader = new StreamReader(streamReader);
            abakStart = reader.ReadToEnd();
        }
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private void GetJsonFromPLC()
        {
            JObject json = CAuxil.ParseJson("/opt/abak/A:/assembly/configs/can_manager.cfg");
            if (json == null)
                return;

            foreach (JToken jToken in json.First.First.Children())
            {
                CCanBusData canBusData = new CCanBusData();
                canBusData.Type = jToken["type"].ToString();
                canBusData.VCanName = jToken["vcan_name"].ToString();
                int listNumb = Convert.ToInt16(canBusData.VCanName.Replace("vxcan10", string.Empty)) - 1;
                canBusData.ListName = $"Корзина №{listNumb}";
                string[] ar = jToken["ip"].ToString().Split('.');
                if (ar.Count() == 4)
                {
                    canBusData.IP = ar;
                }
                canBusData.Port = jToken["port"].ToString();
                canBusData.SelectedBaudRate = jToken["baud_rate"].ToString();
                CanBusDataCollection.Add(canBusData);
                backup.Add(canBusData);
            }
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            SetFocus();
            CCanBusData canBus = CanBusDataCollection.FirstOrDefault(can => can.IP.Any(ip => ip == ""));
            if (canBus != null)
            {
                CanListBox.SelectedItem = canBus;
                SetFocus();
                MessageBox.Show("Введите IP адрес", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string shFile = "";
            string json = "{\"remote_baskets\": [";
            int counter = 2;
            foreach (CCanBusData data in CanBusDataCollection)
            {
                shFile += $"echo $(timestamp)\"vxcan{counter} restart(\"$SECONDS\")\" >> $LOG_FILE\n";
                shFile += $"ip link add dev vxcan10{counter}  type vxcan\n";
                shFile += $"ip link set up vxcan{counter}\n";
                shFile += $"ip link set up vxcan10{counter}\n\n";
                json += $"\n{data.GetJson()},";
            }
            json = json.Remove(json.Length - 1);
            json += "\n]}";
            Stream stream = CAuxil.StringToStream(json);
            CGlobal.Session.SSHClient.WriteFile("/opt/abak/A:/assembly/configs/can_manager.cfg", stream); 
            UpdateFileSH(shFile);
        }
        private void UpdateFileSH(string shFile)
        {
            //can_bus_start.sh
            Stream stream = CAuxil.StringToStream(shFile);
            string canBusSH = "/opt/abak/A:/assembly/can_bus_start.sh";
            CGlobal.Session.SSHClient.WriteFile(canBusSH, stream);
            //abak_start
            if (abakStart.Contains(canBusSH))
                return;

            abakStart += $"\n{canBusSH}";
            stream = CAuxil.StringToStream(abakStart);
            string abakStartPath = "/opt/abak/A:/assembly/abak_start";
            CGlobal.Session.SSHClient.WriteFile(abakStartPath, stream);
            CGlobal.Session.SSHClient.ExecuteCommand($"chmod -R 755 {canBusSH}");
        }
        private void ListBoxItemMouseUp_Handler(object sender, RoutedEventArgs e)
        {
            ListBoxItem listBox = sender as ListBoxItem;
            if (listBox == null)
                return;

            SelectedItem = listBox.Content as CCanBusData;
            if(SelectedItem == null)
                return;

            OnPropertyChanged("SelectedItem");
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox))
                return;

            bool isError = e.Text == "" || !CAuxil.CheckStringForInt(e.Text, true);
            if (textBox.SelectionLength > 0)
            {
                e.Handled = isError;
            }
            else
            {
                e.Handled = textBox.Text.Length == 3 || isError;
            }    
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
                e.Handled = true;
        }
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            CanBusDataCollection.Clear();
            Thread.Sleep(100);
            CanBusDataCollection = new ObservableCollection<CCanBusData>(backup);
            SelectedItem = CanBusDataCollection[0];
            OnPropertyChanged("CanBusDataCollection");
            OnPropertyChanged("SelectedItem");
            Thread.Sleep(100);
            ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateLayout();
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox))
                return;

            if (textBox.Text == "")
                return;

            if (Convert.ToUInt16(textBox.Text) > 255)
                textBox.Text = "255";
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddCanBus();
        }
        private void AddCanBus()
        {
            if (CanBusDataCollection.Count == 5)
            {
                MessageBox.Show("Нельзя использовать больше 5 корзин!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CCanBusData busData = new CCanBusData();
            int cunNumb = CanBusDataCollection.Count + 1;
            busData.VCanName = $"vxcan10{CanBusDataCollection.Count + 2}";
            busData.ListName = $"Корзина №{cunNumb}";
            CanBusDataCollection.Add(busData);
        }
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            CanBusDataCollection.Remove(SelectedItem);
            int cunNumb = 1;
            foreach(CCanBusData busData in CanBusDataCollection)
            {
                busData.ListName = $"Корзина №{cunNumb}";
                busData.VCanName = $"vxcan10{cunNumb + 1}";
                cunNumb++;
            }

            if(CanBusDataCollection.Count == 0)
            {
                AddCanBus();
            }

            CanListBox.SelectedItem = CanBusDataCollection[CanBusDataCollection.Count - 1];
            OnPropertyChanged("CanBusDataCollection");
            SetFocus();
        }
        private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = e.Command == ApplicationCommands.Paste;
        }
        private void SetFocus()
        {
            ListBoxItem listBoxItem = (ListBoxItem)CanListBox.ItemContainerGenerator
                .ContainerFromItem(CanListBox.SelectedItem);
            if (listBoxItem == null)
                return;

            listBoxItem.Focus();
            Keyboard.Focus(listBoxItem);
            SelectedItem = (CCanBusData)CanListBox.SelectedItem;
            OnPropertyChanged("SelectedItem");

        }
        public class CCanBusData : INotifyPropertyChanged
        {
            private string type = "i7540D";
            private string vCanName = "";
            private string listName = "";
            private string[] ip = new string[4] { "", "", "", "" };
            private string port = "10003";
            private string selectedBaudRate = "800";
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string prop)
            {
                if (this.PropertyChanged != null)
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
            }
            public string Type
            {
                get => type;
                set
                {
                    type = value;
                    OnPropertyChanged("VCanName");
                }
            }
            public string VCanName
            {
                get => vCanName;
                set
                {
                    vCanName = value;
                    OnPropertyChanged("VCanName");
                }
            }
            public string[] IP
            {
                get => ip;
                set
                {
                    ip = value;
                    OnPropertyChanged("IP");
                }
            }
            public string Port
            {
                get => port;
                set
                {
                    port = value;
                    OnPropertyChanged("Port");
                }
            }
            public string ListName
            {
                get => listName;
                set
                {
                    listName = value;
                    OnPropertyChanged("ListName");
                }
            }
            public string SelectedBaudRate
            {
                get => selectedBaudRate;
                set
                {
                    selectedBaudRate = value;
                    OnPropertyChanged("SelectedBaudRate");
                }
            }
            public ObservableCollection<string> BaudRates { get; } = new ObservableCollection<string>
            {
                "10", "20", "50", "100", "125", "250", "500", "800", "1000"
            };
            public JObject GetJson()
            {
                JObject settings = new JObject(
                   new JProperty("type", type),
                   new JProperty("vcan_name", vCanName),
                   new JProperty("ip", $"{IP[0]}.{IP[1]}.{IP[2]}.{IP[3]}"),
                   new JProperty("port", port),
                   new JProperty("baud_rate", selectedBaudRate)
               );
                return settings;
            }
        }
    }
}
