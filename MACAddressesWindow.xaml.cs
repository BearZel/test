using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Npgsql;
using System.Windows.Threading;
using System.ComponentModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for MACAddressesWindow.xaml
    /// </summary>
    public partial class MACAddressesWindow : Window, INotifyPropertyChanged
    {
        private string Serial = "";
        Dictionary<int, Delegate> FillWindow = new Dictionary<int, Delegate>();
        Config _config = new Config("options");

        //Таймер обновления данных на экране
        public MACAddressesWindow()
        {
            InitializeComponent();
            TransformToCharArray();
            SetMac();
        }

        private void TransformToCharArray()
        {
            string[] str = CGlobal.CurrState.Serial.Split(':');
            str = str[1].Split(' ');
            SetSerial.Text = Serial = str[1];
        }

        private string GetMacFromFile(int mac)
        {
            Stream streamReader = CGlobal.Session.SSHClient.ReadFile($"/etc/cpsw_{mac}_mac");
            if (streamReader == null)
                return "";
            var reader = new StreamReader(streamReader);
            return reader.ReadLine();
        }

        private string DriftMAC(int macNumb)
        {
            string mac = "";
            string[] macFormFile = GetMacFromFile(macNumb).Split(new char[] { ':' });
            if (macFormFile[0].Length == 0)
            {
                return "";
            }
            macFormFile[0] = (int.Parse(macFormFile[0], System.Globalization.NumberStyles.HexNumber) + 2).ToString("x");
            for (int index = 0; index < macFormFile.Count() - 1; index++)
                mac = mac + macFormFile[index] + ":";


            mac = mac + macFormFile[macFormFile.Count() - 1];
            return mac;
        }

        private void SetMac()
        {
            CPUMAC.Text = _config.Root().Get("mac_address_iface_0");
            IM1MAC.Text = DriftMAC(4);
            IM2MAC.Text = DriftMAC(5);
        }

        private void WriteNewSerial(object sender, RoutedEventArgs e)
        {
            List<char> range = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' };
            char symbol = SetSerial.Text.FirstOrDefault(x => !range.Contains(x));
            if (symbol != '\0')
            {
                MessageBox.Show($"Обнаружен непостумиый символ \"{symbol}\"", "Недопустимый символ!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                string cmd = $"echo \"{SetSerial.Text}\" > /opt/abak/A:/assembly/ser.txt";
                CAuxil.ExecuteSingleSSHCommand(cmd);
                CAuxil.ExecuteSingleSSHCommand("chmod -R 755 /opt/abak/A:/assembly/ser.txt");
                LoadToBackup("2");
                LoadToBackup("3");
            }
            catch (Exception ex)
            {
                String message = String.Format("Ошибка при изменении серийного номера: {0}", ex.Message);
                MessageBox.Show(message, "Смена серийного номера", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (CGlobal.CurrState.IsRockChip)
            {
                NotifyMacAddressChanged();
            }

            MessageBox.Show("Необходимо перезагрузить контроллер", "Изменения внесены!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadToBackup(string plc)
        {
            string codesysControlCFG = $"/opt/backup/IMAGES/PLC{plc}/etc/CODESYSControl.cfg";
            string serialPlcName = "nodename=ABAKPLC" + SetSerial.Text;
            string plcName = "";
            string nameForDelete = "";
            string cmd = "";
            //Если только unicode, что странно, то надо обязательно его удалить и заменить
            if (CAuxil.LoadCFG(out string emptyName, out string unicodeName, plc) == 1)
            {
                string[] str = unicodeName.Split('\\');
                plcName = "nodenameunicode=A";
                for (int i = 1; i < str.Count(); i++)
                    plcName += $"\\\\" + str[i];
            }
            //Это действие стандартное
            if (CAuxil.LoadCFG(out string nodeName, out string lostName, plc) == 2)
                plcName = "nodename=" + nodeName;

            //Если вдруг всё сразу, тоже странно
            if (CAuxil.LoadCFG(out string correctName, out string inCorrectName, plc) == 3)
            {
                string[] str = inCorrectName.Split('\\');
                nameForDelete = "nodenameunicode=A";
                for (int i = 1; i < str.Count(); i++)
                    nameForDelete += $"\\\\" + str[i];
                cmd = $"sed -i 's/{nameForDelete}/" + "" + $"/g' {codesysControlCFG}";
                CAuxil.ExecuteSingleSSHCommand(cmd);
            }

            cmd = $"sed -i 's/{plcName}/" + serialPlcName + $"/g' {codesysControlCFG}";
            CAuxil.ExecuteSingleSSHCommand(cmd);
        }

        void NotifyMacAddressChanged()
        {
            string sql = $"NOTIFY mac_address_changed, '{CPUMAC.Text}'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
