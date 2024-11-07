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
using System.IO.Ports;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Collections.ObjectModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for ChannelsWindow.xaml
    /// </summary>
    public partial class ChannelsWindow : Window, INotifyPropertyChanged
    {
        private CSettings settings = new CSettings();
        private ObservableCollection<String> ipList = new ObservableCollection<string>();
        public bool IsChoosen = false;
        public string IP = "";
        public ChannelsWindow(CSettings settings, bool isUsb = false)
        {
            //Чтение из файла сохраненных IP адресов контроллеров
            InitializeComponent();
            this.IPComboBox.Text = "192.168.3.100";
            this.settings.Assign(settings);
            foreach (String ip in this.settings.IPList)
                this.ipList.Add(ip);

            this.settings.USB = isUsb;
            SaveIP.SetUSB(isUsb);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public CSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        public ObservableCollection<String> IPList { get => this.ipList; }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            
            //Добавление нового IP адреса
            if ((this.IPComboBox.Text != "") && this.settings.NotUSB)
            {
                if (this.settings.IPList.IndexOf(this.IPComboBox.Text) == -1)
                    this.settings.IPList.Add(this.IPComboBox.Text);
            }
            IP = IPComboBox.Text;
            SaveIP.SetIP(this.IPComboBox.Text);
            string message = CGlobal.Session.CheckPing();
            if (message != "")
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsChoosen = true;
            this.DialogResult = true;
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

        private void DataNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "." };
            e.Handled = !range.Contains(e.Text);
        }

        private void SelectIPAddressClick_Handler(object sender, RoutedEventArgs e)
        {
            //Выбор IP адреса
            try
            {
                ControllersWindow cWindow = new ControllersWindow();
                cWindow.Owner = this;
                if (cWindow.ShowDialog() != true)
                    return;

                CAbakInfo beckInfo = (CAbakInfo)cWindow.ControllersListView.SelectedItem;
                if (beckInfo == null)
                    return;
                SaveIP.SetIP(beckInfo.IP);
                this.settings.IP = beckInfo.IP;
                this.IPComboBox.Text = beckInfo.IP;
                SaveIP.SetIP(this.IPComboBox.Text);
            }
            catch
            {

            }
        }

        private void EraseIPAddressClick_Handler(object sender, RoutedEventArgs e)
        {
            this.ipList.Clear();
            this.settings.IPList.Clear();
        }
    }
}
