using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Interop;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для PrepareSDWindow.xaml
    /// </summary>
    public partial class PrepareSDWindow : Window
    {
        private const int WM_DEVICECHANGE = 0x0219;

        private const int DBT_DEVNODES_CHANGED = 0x0007;

        private const int DBT_DEVICEARRIVAL = 0x8000;  // system detected a new device
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;  // wants to remove, may fail
        private const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;  // removal aborted
        private const int DBT_DEVICEREMOVEPENDING = 0x8003;  // about to remove, still avail.
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;  // device is gone
        private const int DBT_DEVICETYPESPECIFIC = 0x8005;  // type specific event

        private const uint DBT_DEVTYP_OEM = 0x00000000;  // oem-defined device type
        private const uint DBT_DEVTYP_DEVNODE = 0x00000001;  // devnode number
        private const uint DBT_DEVTYP_VOLUME = 0x00000002;  // logical volume
        private const uint DBT_DEVTYP_PORT = 0x00000003;  // serial, parallel
        private const uint DBT_DEVTYP_NET = 0x00000004;  // network resource

        private const ushort DBTF_MEDIA = 0x0001;          // media comings and goings


        /// <summary>
        /// Класс описывающий диск ЭВМ
        /// </summary>
        public class CDiskInfo
        {
            private String name;
            private String label;

            public String Name { get => name; set => name = value; }
            public String Label { get => label; set => label = value; }

            public String FullName
            {
                get
                {
                    return String.Format("{0} ({1})", this.label, this.name);
                }
            }
        }

        //Список дисков
        private ObservableCollection<CDiskInfo> disksList = new ObservableCollection<CDiskInfo>();

        public ObservableCollection<CDiskInfo> DisksList { get => disksList;}

        public PrepareSDWindow()
        {
            InitializeComponent();
        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        /// <summary>
        /// Обновление списка дисков
        /// silentOnError - если выставлен в true сообщение о ошибке не показывается
        /// </summary>
        private void updateDisksList(Boolean silentOnError)
        {
            this.disksList.Clear();
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (!(drive.IsReady && (drive.DriveType == DriveType.Removable)))
                        continue;

                    CDiskInfo diskInfo = new CDiskInfo();
                    diskInfo.Name = drive.Name;
                    diskInfo.Label = drive.VolumeLabel;
                    this.disksList.Add(diskInfo);
                }
            }
            catch (Exception ex)
            {
                if (silentOnError)
                    return;
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_prepareSDDrivesError"), ex.Message);
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.updateDisksList(false);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch(wParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:
                        //Добавлено новое устройство
                        this.updateDisksList(true);
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        //Удаление устройства
                        this.updateDisksList(true);
                        break;
                }
            }

            return IntPtr.Zero;
        }

        public String GetDiskName
        {
            get
            {
                if (this.DisksComboBox.SelectedIndex == -1)
                    return "";

                CDiskInfo diskInfo = this.DisksComboBox.SelectedItem as CDiskInfo;
                return diskInfo.Name;
            }
        }
    }
}
