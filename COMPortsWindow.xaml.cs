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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для COMPortsWindow.xaml
    /// </summary>
    public partial class COMPortsWindow : Window, INotifyPropertyChanged
    {
        //Список COM портов
        private CComPortsList ports;
        //Выбранный COM порт
        private CComPort selectedPort = null;
        //Экземпляр SSH клиента
        private CSSHClient sshClient = null;

        //Копия описания физических интерфейсов контроллера, для того что бы с ними работать
        private ObservableCollection<CComPort> copyPortsList = new ObservableCollection<CComPort>();
        //Описания физических интерфейсов контроллера, для того что бы можно было сличать с ними значения
        private ObservableCollection<CComPort> portsList = new ObservableCollection<CComPort>();

        private String lastPortName = "";

        public COMPortsWindow(CSSHClient sshClient)
        {
            InitializeComponent();
            this.sshClient = sshClient;
            this.ports = new CComPortsList(this.sshClient);
        }

        private void updatePortsList()
        {
            //Подгрузка списка COM портов
            this.ports.LoadSettings();
            //Формирование копии списка для отслеживания изменений
            this.portsList.Clear();
            this.copyPortsList.Clear();
            foreach (CComPort comPort in this.ports.PortsList)
            {
                this.portsList.Add(comPort);
                CComPort portCopy = comPort.Clone() as CComPort;
                this.copyPortsList.Add(portCopy);
            }

            //Выбор элемента по умолчанию
            this.selectedPort = null;
            this.listBox.SelectedItem = null;
            this.OnPropertyChanged("SelectedPort");
            if (this.portsList.Count > 0)
            {
                if (this.lastPortName != "")
                    this.selectedPort = this.portsList.Single(eth => eth.Name.Contains(this.lastPortName));
                if (this.selectedPort == null)
                    this.selectedPort = this.portsList[0];
                this.listBox.SelectedItem = this.selectedPort;
                this.OnPropertyChanged("SelectedPort");
            }
        }

        private void COMWindowLoaded(object sender, RoutedEventArgs e)
        {
            //Обновление списка портов
            this.updatePortsList();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void RefreshButton_Handler(object sender, RoutedEventArgs e)
        {
            this.lastPortName = "";
            this.updatePortsList();
        }

        private void InterfaceSettingsGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.listBox.SelectedItem == null)
                this.InterfaceSettingsGrid.Visibility = System.Windows.Visibility.Hidden;
            else
                this.InterfaceSettingsGrid.Visibility = System.Windows.Visibility.Visible;
        }

        private void discardUpdates_Handler(object sender, RoutedEventArgs e)
        {
            //Сброс сделанных настроек
            CComPort commPort = this.listBox.SelectedItem as CComPort;
            if (commPort == null)
                return;

            int index = this.portsList.IndexOf(commPort);
            commPort.Assign(this.copyPortsList[index]);
        }

        private void applyUpdates_Handler(object sender, RoutedEventArgs e)
        {
            CGlobal.Handler.UserLog(2, string.Format("Change COM settings"));

            //Принятие настроек
            CComPort commPort = this.listBox.SelectedItem as CComPort;
            if (commPort == null)
                return;


            String mess = String.Format("{0} \"{1}\"?", CGlobal.GetResourceValue("l_applySettingsQuestion"), commPort.Name);
            if (MessageBox.Show(mess, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            this.ports.ChangeCommPortSettings(commPort);
            this.lastPortName = commPort.Name;
            this.updatePortsList();
        }

        private void ListBoxItemMouseUp_Handler(object sender, RoutedEventArgs e)
        {
            this.selectedPort = null;
            this.OnPropertyChanged("SelectedPort");

            this.selectedPort = this.listBox.SelectedItem as CComPort;
            this.OnPropertyChanged("SelectedPort");
        }

        public CComPort SelectedPort
        {
            get
            {
                return this.selectedPort;
            }
        }

        public ObservableCollection<CComPort> PortsList
        {
            get
            {
                return this.portsList;
            }
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
