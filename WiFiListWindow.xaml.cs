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
using System.Collections.ObjectModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для WiFiListWindow.xaml
    /// </summary>
    public partial class WiFiListWindow : Window
    {
        //Описания физических интерфейсов контроллера, для того что бы можно было сличать с ними значения
        private ObservableCollection<CWifiInfo> wifiList = new ObservableCollection<CWifiInfo>();


        public WiFiListWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Список wifi сетей для отображения
        /// </summary>
        public ObservableCollection<CWifiInfo> WifiList
        {
            get
            {
                return this.wifiList;
            }
        }

        /// <summary>
        /// Возвращает выбранную wifi сеть
        /// </summary>
        public CWifiInfo SelectedWiFi
        {
            get
            {
                return this.wifiListView.SelectedItem as CWifiInfo;
            }
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
