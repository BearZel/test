using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for ControllersWindow.xaml
    /// </summary>
    public partial class ControllersWindow : Window
    {
        private BackgroundWorker findAbakWorker;
        private const int DEVICE_PORT = 9310;
        private int APP_PORT = DEVICE_PORT + 1;
        private byte[] MESSAGE = { 0x00, 0x00, 0x86, 0x83, 0x74, 0x8F, 0x91, 0xDE, 0x4C, 0xE8, 0x80, 0x14, 0x41, 0x0A, 0x26, 0x7C, 0x3E, 0xB9 };
        //UDP клиент для отправки широковещательных сообщений
        private UdpClient udpClient = null;
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private bool _isInit = true;

        public CSettings Settings => CSettings.GetSettings();

        public ControllersWindow()
        {
            InitializeComponent();
            findAbakWorker = (BackgroundWorker)FindResource("findAbaksWorker");
        }
        private void BlinkButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.MESSAGE[0] = (byte)this.APP_PORT;
            this.MESSAGE[1] = (byte)(this.APP_PORT >> 8);
            CAbakInfo beckInfo = (CAbakInfo)ControllersListView.SelectedItem;
            if ((CAbakInfo)ControllersListView.SelectedItem == null)
                MessageBox.Show("Выберите контроллер", "Контроллер не выбран", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                this.udpClient = new UdpClient();
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(beckInfo.IP), DEVICE_PORT);
                while (s.Elapsed < TimeSpan.FromMilliseconds(1000))
                {
                    this.udpClient.Send(this.MESSAGE, this.MESSAGE.Length, endpoint);
                    Thread.Sleep(50);
                }
                s.Stop();
            }
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
        
        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView =
              CollectionViewSource.GetDefaultView(ControllersListView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void findAbakWorker_Handler(object sender, DoWorkEventArgs e)
        {
            while (!findAbakWorker.CancellationPending)
            {
                CGlobal.BeckFinder.FindControllers();
                Thread.Sleep(1000);
                findAbakWorker.ReportProgress(1);
            }
        }
        private void findAbakProgressChanged_Handler(object sender, ProgressChangedEventArgs e)
        {
            if(_isInit)
                LoadControllerLest();
            _isInit = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CGlobal.BeckFinder.Start();
            findAbakWorker.RunWorkerAsync();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            LoadControllerLest();
        }

        public void LoadControllerLest()
        {
            Settings.ControllersList.Clear();
            var collection = CGlobal.BeckFinder.ControllersList;
            if (collection != null && collection.Count > 0)
            {
                collection = collection.OrderBy(c => c.Serial).ToList();
                Settings.ControllersList = new ObservableCollection<CAbakInfo>(collection);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CGlobal.BeckFinder.Stop();
            //Останов потока поиска абаков
            this.findAbakWorker.CancelAsync();
        }
    }
}