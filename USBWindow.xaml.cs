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
using System.ComponentModel;
using Npgsql;
using System.Collections.ObjectModel;
using System.Threading;

namespace AbakConfigurator
{
    //ВНИМАНИЕ!!!! Ниже описывается случай ТОЛЬКО для ПЛК 3
    //В линуксе порты называются иначе. Надо это учитывать 
    //Слева конфигуратор, справа линукс
    //      USB 1            USB 4   
    //      USB 2            USB 5   
    //      USB 3            USB 2   
    //      USB 4            USB 3
    //Актуально только для ЦПУ
    //Чтобы посмотреть состояние USB портов нужно ввести команду uhubctl


    /// <summary>
    /// Логика взаимодействия для USBWindow.xaml
    /// </summary>
    public partial class USBWindow : Window
    {
        //public UInt32 number = 3;

        private NpgsqlConnection connection;

        private MainViewModel MainViewMod = new MainViewModel();

        //Функция для работы с цветом и доступом к полям модулей IM
        public void Field_Style()
        {
            int row_num = 3;
            int index = 2;
            if (CGlobal.CurrState.PLCVersionInfo == 2)
                index = 0;
            if (this.MainViewMod.CPUCheckBoxes[index].Block)
            {
                IM1_FIELD.Visibility = Visibility.Hidden;
                row_num = 2;
            }
            else
            {
                row_num = 3;
                IM1_FIELD.Visibility = Visibility.Visible;
            }
            index = 3;
            if (CGlobal.CurrState.PLCVersionInfo == 2)
                index = 1;
            if (this.MainViewMod.CPUCheckBoxes[index].Block)
                IM2_FIELD.Visibility = Visibility.Hidden;
            else
                IM2_FIELD.Visibility = Visibility.Visible;

            IM1_FIELD.SetValue(Grid.RowProperty, 2);
            IM1_FIELD.DataContext = MainViewMod;

            IM2_FIELD.SetValue(Grid.RowProperty, row_num);
            IM2_FIELD.DataContext = MainViewMod;
        }

        public USBWindow(CSSHClient sshClient, NpgsqlConnection connection)
        {   
            this.connection = connection;
            this.MainViewMod.LoadUSB(this.connection);
            InitializeComponent();
            this.Field_Style();
            DataContext = MainViewMod;
        }

        private void RefreshButton_Handler(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.Sleep(5000);
            this.MainViewMod.LoadUSB(this.connection);
            InitializeComponent();
            this.Field_Style();
            DataContext = MainViewMod;
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            CGlobal.Handler.UserLog(2, string.Format("Change USB settings"));

            bool plcAbove550 = MainViewMod.PlcVersionAbove550();

            if(!plcAbove550)
            {
                for (int i = 0; i < 2; i++)
                {
                    int index = 4;
                    if (CGlobal.CurrState.PLCVersionInfo == 2)
                        index = 2;
                    //Отключаем USB 1 (В линуксе он USB 4) и Отключаем USB 2 (В линуксе он USB 5) - ПЛК 3
                    //Отключаем USB 1 (В линуксе он USB 2) и Отключаем USB 2 (В линуксе он USB 3) - ПЛК 2
                    if (this.MainViewMod.CPUCheckBoxes[i].IsChanged)
                        if (this.MainViewMod.CPUCheckBoxes[i].Work)
                        {
                            CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1 -p " + (i + index).ToString() + " -a on");
                            NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=true where num=" + (i + 1).ToString(), this.connection);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            CGlobal.Session.SSHClient.ExecuteCommand($"echo '1-1.{i + index}' |sudo tee /sys/bus/usb/drivers/usb/unbind");
                            Thread.Sleep(100);
                            CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1 -p " + (i + index).ToString() + " -a off");
                            NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=false where num=" + (i + 1).ToString(), this.connection);
                            cmd.ExecuteNonQuery();
                        }
                }

                if (CGlobal.CurrState.PLCVersionInfo == 3)
                {
                    for (int i = 2; i < 4; i++)
                    {  //Отключаем USB 3 (В линуксе он USB 2) и Отключаем USB 4 (В линуксе он USB 3)

                        if (this.MainViewMod.CPUCheckBoxes[i].IsChanged)
                            if (this.MainViewMod.CPUCheckBoxes[i].Work)
                            {
                                CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1 -p " + (i).ToString() + " -a on");
                                NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=true where num=" + (i + 1).ToString(), this.connection);
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1 -p " + (i).ToString() + " -a off");
                                Thread.Sleep(100);
                                CGlobal.Session.SSHClient.ExecuteCommand($"echo '1-1.{i}' |sudo tee /sys/bus/usb/drivers/usb/unbind");
                                NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=false where num=" + (i + 1).ToString(), this.connection);
                                cmd.ExecuteNonQuery();
                            }
                    }
                }
            }
            else
            {
                if (MainViewMod.CPUCheckBoxes.Count > 0)
                {
                    string sqlUpdate = string.Empty;
                    foreach (CheckBoxModel boxVm in MainViewMod.CPUCheckBoxes)
                    {
                        var param = CGlobal.Config.ControllerParams.AllParams.FirstOrDefault(p =>
                            p.Tagname == boxVm.TagName);
                        if (param != null)
                        {
                            param.WriteValue = boxVm.Work.ToString();
                            param.Value = param.WriteValue;
                            param.ManualChanged = true;

                            sqlUpdate +=
                                $" update fast_table set value='{boxVm.Work}', changed=true where tag='{param.Tagname}';";
                        }
                    }

                    if (!string.IsNullOrEmpty(sqlUpdate))
                    {
                        NpgsqlCommand cmdUpdate = new NpgsqlCommand(sqlUpdate, connection);
                        cmdUpdate.ExecuteNonQuery();
                    }
                }
            }

            //Тут отключаем для IM, подключенный к USB 3
            for (int i = 0; i < 2; i++)
            {

                if (this.MainViewMod.IM_1CheckBoxes[i].IsChanged)
                    if (this.MainViewMod.IM_1CheckBoxes[i].Work)
                    {
                        CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1.2 -p " + (i + 2).ToString() + " -a on");
                        NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=true where num=" + (i + 5).ToString(), this.connection);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1.2 -p " + (i + 2).ToString() + " -a off");
                        NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=false where num=" + (i + 5).ToString(), this.connection);
                        cmd.ExecuteNonQuery();
                    }
            }

            //Тут отключаем для IM, подключенный к USB 4
            for (int i = 0; i < 2; i++)
            {

                if (this.MainViewMod.IM_2CheckBoxes[i].IsChanged)
                    if (this.MainViewMod.IM_2CheckBoxes[i].Work)
                    {
                        CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1.3 -p " + (i + 2).ToString() + " -a on");
                        NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=true where num=" + (i + 7).ToString(), this.connection);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        CGlobal.Session.SSHClient.ExecuteCommand("uhubctl -l 1-1.3 -p " + (i + 2).ToString() + " -a off");
                        NpgsqlCommand cmd = new NpgsqlCommand("update usb_settings set work=false where num=" + (i + 7).ToString(), this.connection);
                        cmd.ExecuteNonQuery();
                    }
            }

            this.DialogResult = true;
        }
    }

    public class Changed : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CheckBoxModel : Changed
    {
        public void GetValuesFromSQL(NpgsqlDataReader reader)
        {
            this.Num = Convert.ToUInt32(reader["num"]);
            this.Work = Convert.ToBoolean(reader["work"]);
            this.Block = !Convert.ToBoolean(reader["block"]);
            this.IsChanged = false;
        }

        public void GetValuesFromBaseParam(CBaseParam parameter)
        {
            Work = Convert.ToBoolean(parameter.Value);
            TagName = parameter.Tagname;
            Block = true;
            IsChanged = false;
        }

        private Boolean ischanged;
        public Boolean IsChanged
        {
            get { return this.ischanged; }

            set
            {
                if (this.ischanged != value)
                {
                    this.ischanged = value;
                    OnPropertyChanged("IsChanged");
                }
            }
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {

                    this.name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string TagName { get; set; }

        private UInt32 num;
        public UInt32 Num
        {
            get { return this.num; }

            set
            {
                if (this.num != value)
                {

                    this.num = value;
                    OnPropertyChanged("Num");
                }
            }
        }

        private Boolean work;
        public Boolean Work
        {
            get { return this.work; }

            set
            {
                if (this.work != value)
                {
                    this.IsChanged = true;
                    this.work = value;
                    OnPropertyChanged("Work");
                }
            }
        }

        private Boolean block;
        public Boolean Block
        {
            get { return this.block; }

            set
            {
                if (this.block != value)
                {
                    this.block = value;
                    OnPropertyChanged("Block");
                }
            }
        }
    }
    

    public class MainViewModel
    {
        private NpgsqlConnection connection; //Для SQL

        //Тут хранятся чекбоксы
        public ObservableCollection<CheckBoxModel> CPUCheckBoxes { get; set; } = new ObservableCollection<CheckBoxModel>();

        public ObservableCollection<CheckBoxModel> IM_1CheckBoxes { get; set; } = new ObservableCollection<CheckBoxModel>();

        public ObservableCollection<CheckBoxModel> IM_2CheckBoxes { get; set; } = new ObservableCollection<CheckBoxModel>();

        //Для проверки наличия IM модулей
        private ObservableCollection<CComPort> portsList = new ObservableCollection<CComPort>();

        //Функция для создания чекбоксов 
        public void LoadUSB(NpgsqlConnection connection)
        {
            this.CPUCheckBoxes.Clear();
            this.IM_1CheckBoxes.Clear();
            this.IM_2CheckBoxes.Clear();

            bool plcAbove550 = PlcVersionAbove550();

            this.connection = connection;

            this.loadPortsList();

            String sql = "select * from usb_settings order by num";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (!reader.HasRows)
                        return;

                    int i = 1;
                    while (reader.Read())
                    {
                        if (i < 5 && !plcAbove550)
                        {
                            CheckBoxModel box = new CheckBoxModel();
                            CComPort IM_Modules = new CComPort();
                            //Читаем данные из SQL
                            box.GetValuesFromSQL(reader);
                            if (CGlobal.CurrState.PLCVersionInfo == 2 && i > 2 && i < 5)
                            {
                                i++;
                                continue;
                            }
                            box.Name = "USB " + i.ToString();
                            //Добавляем в ObservableCollection
                            CPUCheckBoxes.Add(box);
                            //Завершаем, тут создаются боксы только для CPU 
                        }

                        if (i >= 5 && i < 7)
                        {
                            CheckBoxModel box = new CheckBoxModel();
                            //Читаем данные из SQL
                            box.GetValuesFromSQL(reader);
                            box.Name = "USB " + (i - 4).ToString();
                            //Добавляем в ObservableCollection
                            IM_1CheckBoxes.Add(box);
                            //Завершаем, тут создаются боксы только для IM 1 

                        }

                        if (i >= 7 && i < 9)
                        {
                            CheckBoxModel box = new CheckBoxModel();
                            //Читаем данные из SQL
                            box.GetValuesFromSQL(reader);
                            box.Name = "USB " + (i - 6).ToString();
                            //Добавляем в ObservableCollection
                            IM_2CheckBoxes.Add(box);
                            //Завершаем, тут создаются боксы только для IM 2 

                        }
                        i++;
                    }
                }

                finally
                {
                    reader.Close();
                }

                if (plcAbove550)
                {
                    // Чтение по тегам
                    var searchList = CGlobal.Config.ControllerParams.AllParams
                        .Where(p => p.Tagname.Contains("USB_PORT_") && p.Tagname.Contains("_STATE"))
                        .OrderBy(p => p.Tagname).ToList();

                    if (searchList.Count > 0)
                    {
                        int i = 1;
                        foreach (CBaseParam baseParam in searchList)
                        {
                            CheckBoxModel box = new CheckBoxModel();
                            box.GetValuesFromBaseParam(baseParam);

                            if (int.TryParse(string.Join("", baseParam.Tagname.Where(char.IsDigit)), out int value))
                            {
                                box.Name = $"USB {value}";
                                box.Num = Convert.ToUInt32(value);
                            }
                            else
                            {
                                box.Name = $"USB {i}";
                                i++;
                            }

                            CPUCheckBoxes.Add(box);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                String message = String.Format("{0} {1}", CGlobal.GetResourceValue("l_comErrorLoadPorts"), ex.Message);

                //MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

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
            catch (Exception ex)
            {
                String message = String.Format("{0} {1}", CGlobal.GetResourceValue("l_comErrorLoadPorts"), ex.Message);

            }
            
            if (portsList[5].Active)
            {
                String sql_update = "update usb_settings set block=true where num=3";
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql_update, this.connection);
                cmd_update.ExecuteNonQuery();
            }
            else
            {
                String sql_update = "update usb_settings set block=false where num=3";
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql_update, this.connection);
                cmd_update.ExecuteNonQuery();
            }
            if (portsList[9].Active)
            {
                String sql_update = "update usb_settings set block=true where num=4";
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql_update, this.connection);
                cmd_update.ExecuteNonQuery();
            }
            else
            {
                String sql_update = "update usb_settings set block=false where num=4";
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql_update, this.connection);
                cmd_update.ExecuteNonQuery();
            }

        }

        public MainViewModel()
        {
        }

        public bool PlcVersionAbove550()
        {
            int numb = CGlobal.CurrState.AssemblyHi * 1000
                       + CGlobal.CurrState.AssemblyMid * 100 + CGlobal.CurrState.AssemblyLo;

            return numb >= 5500;
        }

    }
}
