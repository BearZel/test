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
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for CodesyscontrolWindow.xaml
    /// </summary>
    public partial class CodesyscontrolWindow : Window
    {
        //Частота вызова планировщика улучшает точность вызова задач, но повышает нагрузку на процессор
        private Dictionary<string, int[]> limits = new Dictionary<string, int[]>
        {
            {"SchedulerInterval", new int [2] { 1000, 60000} },
            {"MaxEntries", new int [2] { 100, 100000} }
        };
        private Dictionary<string, string> paths = new Dictionary<string, string>
        {
            {"sd", "/media/sd/" }, {"usb1", "/media/usb1/" }, {"usb2", "/media/usb2/" },
            { "PLC" , "/var/log/"}
        };
        private BitArray filterArray;
        //Путь к CFG файлу
        private const String codesysControlCFG = "/etc/CODESYSControl.cfg";
        //Ссылка на экземпляр класса работающего с SSH клиентом
        private CSSHClient sshClient = null;
        //класс управляющий списком Ethernet интерфейсов
        private CEthernetIterfacesList interfaces;
        private List<string> ethList = new List<string>();
        //CFG файл
        CIniFile iniCFG = null;
        //Номер секции с профинетом
        int profinetSection = 0;
        public CodesyscontrolWindow(CSSHClient sshClient)
        {
            this.sshClient = sshClient;
            interfaces = new CEthernetIterfacesList(sshClient);
            //Подгрузка списка сетевых интерфейсов
            this.interfaces.LoadInterfaces();
            foreach (CEthernetInterface eth in this.interfaces.InterfacesList)
                ethList.Add(eth.NetClass);
            InitializeComponent();
            EthBox.ItemsSource = ethList;
            ReadAndSetData();
        }

        private void ReadAndSetData()
        {
            Stream stream = sshClient.ReadFile(codesysControlCFG);
            if (stream == null)
                stream = new MemoryStream();
            iniCFG = new CIniFile(stream);
            iniCFG.NewLine = "\n";
            //Вкл/выкл логов
            string value = iniCFG.ReadValue("cmplog", "logger.0.enable");
            EnableLogs.IsChecked = value == "1";
            //Путь к логам
            this.Path.ItemsSource = paths.Keys;
            this.Path.SelectedItem = GetLogPathAndName(iniCFG.ReadValue("cmplog", "logger.0.name"), out string name);
            //Имя файла
            FileName.Text = name;
            //Строки в IDE
            MaxEntries.Text = iniCFG.ReadValue("cmplog", "logger.0.maxentries");
            //Размер файлов
            MaxFileSize.Text = iniCFG.ReadValue("cmplog", "logger.0.maxfilesize");
            //Количество файлов 
            MaxFiles.Text = iniCFG.ReadValue("cmplog", "logger.0.maxfiles");
            //Метка времени
            string type = iniCFG.ReadValue("cmplog", "logger.0.type");
            Millisec.IsChecked = type == "0x2304";
            Sec.IsChecked = type == "0x314";
            //Фильтр
            GetLogsFilter(iniCFG.ReadValue("cmplog", "logger.0.filter"));
            //Логи планировщика
            value = iniCFG.ReadValue("cmpschedule", "enablelogger");
            EnableLogger.IsChecked = value == "1";
           //Интервал вызова планировщика
            SchedulerInterval.Text = iniCFG.ReadValue("cmpschedule", "schedulerinterval", limits["SchedulerInterval"][0].ToString());
            //Profinet
            value = iniCFG.ReadValue("cmpblkdrvudp", "itf.0.AdapterName");
            string eth = "";
            int index = ethList.IndexOf(value);
            if(index != -1)
                eth = ethList[index];
            else
                eth = "eth0";
            EthBox.SelectedItem = eth;
            GetProfinetSettings();
        }
        private void GetLogsFilter(string filter)
        {
            try
            {
                int value = 0;
                if (filter == "0xFFFFFFFF")
                    value = 31;
                else
                    value = Convert.ToInt32(filter, 16);

                filterArray = new BitArray(new int[] { value });
                Info.IsChecked = filterArray[0];
                Warning.IsChecked = filterArray[1];
                Error.IsChecked = filterArray[2];
                Exception.IsChecked = filterArray[3];
                Debug.IsChecked = filterArray[4];
            }
            catch
            {

            }
        }
        private string SetLogsFilter()
        {
            string filter;
            filterArray[0] = Info.IsChecked == true;
            filterArray[1] = Warning.IsChecked == true;
            filterArray[2] = Error.IsChecked == true;
            filterArray[3] = Exception.IsChecked == true;
            filterArray[4] = Debug.IsChecked == true;
            int[] array = new int[1];
            filterArray.CopyTo(array, 0);
            if (array[0] == 31)
                filter = "0xFFFFFFFF";
            else
                filter = "0x" + array[0].ToString("X");
            iniCFG.WriteValue("cmplog", "logger.0.filter", filter);
            return filter;
        }
        private void WriteSettings()
        {
            if (IsErrorsInParams())
                return;
            //Логи
            iniCFG.WriteValue("cmplog", "logger.0.enable", Convert.ToInt32(EnableLogs.IsChecked).ToString());
            iniCFG.WriteValue("cmplog", "logger.0.name", paths[Path.SelectedItem.ToString()] + FileName.Text + ".log");
            iniCFG.WriteValue("cmplog", "logger.0.maxentries", MaxEntries.Text);
            iniCFG.WriteValue("cmplog", "logger.0.maxfilesize", MaxFileSize.Text);
            iniCFG.WriteValue("cmplog", "logger.0.maxfiles", MaxFiles.Text);
            iniCFG.WriteValue("cmplog", "logger.0.filter", SetLogsFilter());
            string value = "0x2304";
            if(Sec.IsChecked == true)
                value = "0x314";
            iniCFG.WriteValue("cmplog", "logger.0.type", value);
            //Задачи
            iniCFG.WriteValue("cmpschedule", "enablelogger", Convert.ToInt32(EnableLogger.IsChecked).ToString());
            iniCFG.WriteValue("cmpschedule", "schedulerinterval", SchedulerInterval.Text);

            if (ProfinetEnable.IsChecked == false)
                DeleteProfinetSettings();
            else
                WriteProfinetSettings();
            
            iniCFG.ExtraSpace = false;
            bool isRunSwitchOn  = CAuxil.IsRunSwitchOn();
            string msg = "Службы Codesys будут перезапущены. Вы уверены, что хотитие сохранить настройки?";
            sshClient.WriteFile("/etc/CODESYSControl.cfg", iniCFG.GetStream());
            if (!isRunSwitchOn)
            {
                if (MessageBox.Show(msg, Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                Thread.Sleep(1000);
                CAuxil.SetTagValue("true", "CDS_RESTART");
                Thread.Sleep(1000);
            }
            MessageBox.Show("Настройки применены!", "Настройки резервирования", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WriteProfinetSettings()
        {
            if (profinetSection == 0)
            {
                CSection componentmanager = iniCFG.GetSection("componentmanager");
                if (componentmanager == null)
                    return;

                profinetSection = componentmanager.Values.Count();
                iniCFG.RemoveKey("componentmanager", "overloadablefunctions");
                iniCFG.WriteValue("componentmanager", $"component.{profinetSection}", "CmpSysEthernet");
                iniCFG.WriteValue("componentmanager", "overloadablefunctions", "1");
            }
            iniCFG.WriteValue("cmpblkdrvudp", "itf.0.AdapterName", EthBox.Text);
            iniCFG.WriteValue("cmpblkdrvudp", "itf.0.DoNotUse", "1");

            iniCFG.WriteValue("sysethernet", "Linux.PACKET_QDISC_BYPASS", "1");
            iniCFG.WriteValue("sysethernet", "Linux.ProtocolFilter", "3");
        }
        private void DeleteProfinetSettings()
        {
            if (profinetSection == 0)
                return;

            iniCFG.RemoveKey("componentmanager", $"component.{profinetSection}");
            iniCFG.RemoveSection("sysethernet");
            iniCFG.RemoveSection("cmpblkdrvudp");
        }
        private void GetProfinetSettings()
        {
            CSection componentmanager = iniCFG.GetSection("componentmanager");
            if (componentmanager == null)
                return;

            string component = "component.";
            for (int i = 1; i < componentmanager.Values.Count(); i++)
            {
                if (!componentmanager.Values.ContainsKey(component + i.ToString()))
                    continue;

                if (componentmanager.Values[component + i.ToString()] != "CmpSysEthernet")
                    continue;

                ProfinetEnable.IsChecked = true;
                profinetSection = i;
                return;
            }
        }
        private bool IsErrorsInLogs()
        {
            if (Path.Text != "PLC")
                return false;
            try
            {
                int maxFiles = Convert.ToInt32(MaxFiles.Text);
                int maxFileSize = Convert.ToInt32(MaxFileSize.Text);
                //Не более 10 мб
                if (maxFiles * maxFileSize <= 100000000)
                {
                    MaxFileSize.ClearValue(Border.BorderBrushProperty);
                    MaxFiles.ClearValue(Border.BorderBrushProperty);
                    return false;
                }
                MaxFileSize.BorderBrush = Brushes.Red;
                MaxFiles.BorderBrush = Brushes.Red;
                MessageBox.Show("Суммарный размер файлов с логами на ПЛК не должен превышать 100 мегабайт",
                     "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка проверки данных " + ex.Message, "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
        }
        private void DataValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            string pattern = @"^[a-zA-Z0-9]+\z";
            Regex regex = new Regex(pattern);
            e.Handled = !regex.IsMatch(e.Text);
        }
        private void NumbersBoxValidation(object sender, TextCompositionEventArgs e)
        {
            List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
            e.Handled = !range.Contains(e.Text);
        }
        private bool IsErrorsInParams()
        {
            if (IsErrorsInLogs())
                return true;

            if (IsErrorInBoxes(LogsGrid))
                return true;

            if (IsErrorInBoxes(TasksGrid))
                return true;

            return false;
        }
        private bool IsErrorInBoxes(Grid grid)
        {
            bool error = false;
            foreach (var child in grid.Children)
            {

                bool state;
                TextBox textBox = child as TextBox;
                if (textBox == null)
                    continue;

                if (textBox.Text == "")
                {
                    error = true;
                    textBox.BorderBrush = Brushes.Red;
                    continue;
                }

                if (!limits.ContainsKey(textBox.Name))
                    continue;

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

            if (error)
                MessageBox.Show("Выход за границы допустимых значений", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);

            return error;
        }
        private string GetLogPathAndName(string file, out string name)
        {
            name = "";
            try
            {
                string[] str = file.Split('/');
                if (!str[str.Count() - 1].Contains('.'))
                    return "";

                name = str[str.Count() - 1];
                int index = name.IndexOf('.');
                name = name.Remove(index);

                foreach(string key in paths.Keys)
                    if (str.Contains(key))
                        return paths[key];
               
                return "PLC";
            }
            catch
            {
                return "";
            }

        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            WriteSettings();
        }
    }

}
