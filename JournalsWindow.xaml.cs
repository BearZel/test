using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
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


namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для JournalsWindow.xaml
    /// </summary>
    public partial class JournalsWindow : Window, INotifyPropertyChanged
    {
        //Флаг запуска
        private Boolean starting = true;
        //База с кторой работаем
        NpgsqlConnection connection;
        //Время начала запроса
        private DateTime protStartTime;
        //Время конца запроса
        private DateTime protEndTime;
        //Время начала запроса
        private DateTime journalStartTime;
        //Время конца запроса
        private DateTime journalEndTime;
        //Фильтр по источнику
        //private String journalSourceFilter;
        //Таблица с данными протокола событий
        private DataTable protocolTable;
        //Количество строк запроса 
        private String countRecInDB = null;
        //строка поиска по событию
        private String eventSelInDB = null;

        //Список типов событий
        private ObservableCollection<String> typeList = new ObservableCollection<String>();
        //Список источников событий
        private ObservableCollection<String> sourceList = new ObservableCollection<String>();
        private ObservableCollection<ProtocolType> protocolTypeNameList = new ObservableCollection<ProtocolType>();

        public JournalsWindow(NpgsqlConnection connection)
        {
            this.protStartTime = DateTime.Now.Date;
            this.protEndTime = this.protStartTime;

            InitializeComponent();

            this.connection = connection;
            this.updateTypesList();
            this.updateSourcesList();
            this.starting = false;
            this.updateProtocol();
            this.updateJournals();
        }

        /// <summary>
        /// Обновление списка типов событий
        /// </summary>
        private void updateTypesList()
        {
            this.ProtocolTypeNameList.Clear();
             NpgsqlCommand cmd = new NpgsqlCommand("select * from events_types", this.connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        //Тип события
                        String sType = reader["message_type"].ToString();
                        this.ProtocolTypeNameList.Add(new ProtocolType(false, sType));
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            catch
            {
                this.ProtocolTypeNameList.Clear();
            }
            this.OnPropertyChanged("ProtocolTypeNameList");
        }

        /// <summary>
        /// Обновление списка источников событий
        /// </summary>
        private void updateSourcesList()
        {
            this.sourceList.Clear();
            this.sourceList.Add("*");
            NpgsqlCommand cmd = new NpgsqlCommand("select * from events_source", this.connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        //Тип события
                        String sType = reader["source"].ToString();
                        this.sourceList.Add(sType);
                    }
                    this.ProtocolSourceComboBox.SelectedItem = "*";
                    this.JournalSourceComboBox.SelectedItem = "*";
                }
                finally
                {
                    reader.Close();
                }
            }
            catch
            {
                this.sourceList.Clear();
            }
        }

        /// <summary>
        /// Функция "собирает" условия для запроса по типу сообщений
        /// </summary>
        /// <returns></returns>
        private static string MakeTypeRequest(string str)
        {
            string res="";
            int count = 0;
            if (str.Length > 0)
            { 
                string[] words = str.Split(',');
                foreach (string s in words)
                { 
                    if(count > 0)
                       res += String.Format(" or message_type = '{0}'", s);
                    else
                       res += String.Format(" and message_type = '{0}'", s);
                    count++;
                }
            }
            return res;
        }
        /// <summary>
        /// Подгружает с контроллера протокол событий
        /// </summary>
        private void updateProtocol()
        {
            string res = "";
            if (this.starting)
                return;

    //SELECT j.dt,
    //j.message,
    //t.message_type,
    //s.source
    //FROM events_journal j
    //JOIN events_types t ON t.id = j.event_type
    //JOIN events_source s ON s.id = j.event_source;


            //Подготовка SQL запроса
            String sql = "select j.id, j.dt, j.message, t.message_type, s.source from events_journal j join events_types t on t.id = j.event_type join events_source s on s.id = j.event_source";
            Boolean desc = false;
            if (this.protStartTime > this.protEndTime)
                desc = true;
            if (desc)
                sql = String.Format("{0} where(j.dt<='{1}' and j.dt>'{2}'", sql, this.protStartTime.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"), this.protEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            else
                sql = String.Format("{0} where(j.dt>='{1}' and j.dt<'{2}'", sql, this.protStartTime.ToString("yyyy-MM-dd HH:mm:ss"), this.protEndTime.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));
            //Фильтр по совпадению
            if (EventSelDB != null & EventSelDB != String.Empty)
            {
                sql += String.Format(" and message ilike '%{0}%'", this.EventSelDB);
            }

            res = MakeTypeRequest(this.PtObject.Text);
            if (res !="")
                sql = String.Format("{0} {1}", sql, res);

            if (this.ProtocolSourceComboBox.SelectedValue.ToString() != "*")
                sql = String.Format("{0} and source='{1}'", sql, this.ProtocolSourceComboBox.SelectedValue.ToString());
            sql += ")";     
            if (this.protStartTime > this.protEndTime)
                sql += " order by dt desc";
            else
                sql += " order by dt asc";
            //Количество записей
            if (countRecInDB != null & CountRecDB != String.Empty)
            {
                sql += String.Format(" limit {0}", this.countRecInDB);
            }
            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(sql, this.connection);
            this.protocolTable = new DataTable();
            adapter.Fill(this.protocolTable);

            this.protocolDataGrid.ItemsSource = this.protocolTable.DefaultView;
        }

        private void updateJournals()
        {
            if (this.starting)
                return;

            //Подготовка SQL запроса
            String sql = "select * from changes_journal_view";
            Boolean desc = false;
            if (this.journalStartTime > this.journalEndTime)
                desc = true;
            if (desc)
                sql = String.Format("{0} where(dt<='{1}' and dt>'{2}'", sql, this.journalStartTime.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"), this.journalEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
            else
                sql = String.Format("{0} where(dt>='{1}' and dt<'{2}'", sql, this.journalStartTime.ToString("yyyy-MM-dd HH:mm:ss"), this.journalEndTime.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));

            if (this.JournalSourceComboBox.SelectedValue.ToString() != "*")
                sql = String.Format("{0} and source='{1}'", sql, this.JournalSourceComboBox.SelectedValue.ToString());
            sql += ")";

            if (this.journalStartTime > this.journalEndTime)
                sql += " order by dt desc";
            else
                sql += " order by dt asc";

            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(sql, this.connection);
            DataTable dt = new DataTable();
            adapter.Fill(dt);

            this.journalDataGrid.ItemsSource = dt.DefaultView;
        }

        public DateTime ProtStartTime
        {
            get
            {
                return this.protStartTime;
            }
            set
            {
                this.protStartTime = value;
                this.OnPropertyChanged("ProtStartTime");
             //   this.updateProtocol();
            }
        }

        public String CountRecDB
        {
            get
            {
                return this.countRecInDB;
            }
            set
            {
                this.countRecInDB = value;
                this.OnPropertyChanged("CountRecDB");
             //   this.updateProtocol();
            }
        }

        public String EventSelDB
        {
            get
            {
                return this.eventSelInDB;
            }
            set
            {
                this.eventSelInDB = value;
                this.OnPropertyChanged("EventSelDB");
            //    this.updateProtocol();
            }
        }
        public DateTime ProtEndTime
        {
            get
            {
                return this.protEndTime;
            }
            set
            {
                this.protEndTime = value;
                this.OnPropertyChanged("ProtEndTime");
            //    this.updateProtocol();
            }
        }

        //Время начала запроса
        public DateTime JournalStartTime
        {
            get
            {
                return this.journalStartTime;
            }
            set
            {
                this.journalStartTime = value;
                this.OnPropertyChanged("JournalStartTime");
                this.updateJournals();
            }
        }

        //Время конца запроса
        private DateTime JournalEndTime
        {
            get
            {
                return this.journalEndTime;
            }
            set
            {
                this.journalEndTime = value;
                this.OnPropertyChanged("JournalEndTime");
                this.updateJournals();
            }
        }

        public ObservableCollection<String> TypesList
        {
            get
            {
                return this.typeList;
            }
        }

        public ObservableCollection<String> SourceList
        {
            get
            {
                return this.sourceList;
            }
        }

        public ObservableCollection<ProtocolType> ProtocolTypeNameList
        {
            get
            {
                return this.protocolTypeNameList;
            }
        }



        private void ProtocolTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProtocolTypeComboBox.SelectedIndex = -1;
        }

        private void ProtocolSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
         //   this.updateProtocol();
        }

        private void JournalSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.updateJournals();
        }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void CSVExportMenuItemClick_Handler(object sender, RoutedEventArgs e)
        {
            //Экспорт протокола событий в CSV файл
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "csv";
            saveDialog.Filter = "csv|*.csv";
            saveDialog.OverwritePrompt = true;

            if (saveDialog.ShowDialog(this) != true)
                return;

            //Заполнение списка для сохранения в файл
            List<String> csv = new List<String>();
            //Заголовок csv файла
            csv.Add("date;type;source;event");
            //Данные из сформированной таблицы
            foreach (DataRow row in this.protocolTable.Rows)
            {
                //select j.id, j.dt, j.message, t.message_type, s.source from events_journal
                String s = String.Format("{0};{1};{2};{3}", DateTimeConverter.DateTimeString((DateTime)row["dt"]), row["message_type"], row["source"], row["message"]);
                csv.Add(s);
            }

            File.WriteAllLines(saveDialog.FileName, csv.ToArray(), Encoding.Default);
        }

        private void UpdateProtocolMenuItemClick_Handler(object sender, RoutedEventArgs e)
        {
            //Обновление таблицы с протоколом событий
            this.updateProtocol();
        }

        //Проверка вводимых данных
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Если не цифра - запрещаем.
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

        private void ProtocolTypeCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ProtocolType pt in ProtocolTypeComboBox.Items)
            {
                if (pt.IsCheckType)
                    sb.AppendFormat("{0},",pt.ProtocolTypeName);
            }

            PtObject.Text = sb.ToString().Trim().TrimEnd(',');
           
        }


    }
}
