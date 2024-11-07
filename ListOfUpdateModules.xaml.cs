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
using System.ComponentModel;
using Npgsql;
using System.Collections.ObjectModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ListOfUpdateModules : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<ModuleData> ModulesData { get; set; } = new ObservableCollection<ModuleData>();

        public ListOfUpdateModules(NpgsqlConnection connection)
        {
            GetValuesFromSQL(connection);
            InitializeComponent();
            dataGrid.Columns.Add(new DataGridTextColumn());
        }

        public void GetValuesFromSQL(NpgsqlConnection connection)
        {
            string sql = "select * from update_info order by id";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            while (reader.Read())
            {
                ModuleData moduleData = new ModuleData();
                moduleData.Node_Id = Convert.ToUInt32(reader["id"]);
                moduleData.SetColor = Convert.ToUInt32(reader["state"]);
                moduleData.Update_Status = this.IsFinished(moduleData.SetColor);
                moduleData.Com_Status = this.GetEvent(Convert.ToString(reader["com_part"]), null);
                moduleData.Type = reader["type"].ToString();

                string[] plcversion = moduleData.Type.Split('.');

                moduleData.Chan_Status = this.GetEvent(Convert.ToString(reader["channel_part"]), plcversion.Contains("k2") || plcversion.Contains("K2"));
                moduleData.OldSoftVer = Convert.ToString(reader["old_softver"]);
                moduleData.NewSoftVer = Convert.ToString(reader["new_softver"]);
                moduleData.LastUpdate = Convert.ToString(reader["date_time"]);
                ModulesData.Add(moduleData);
            }
            reader.Close();
        }

        private string IsFinished (UInt32 state)
        {
            Dictionary<UInt32, string> dictionary = new Dictionary<UInt32, string>
            {      
                { 3, "Модуль уже обновлён до последней версии" },
                { 2, "Успешно" },
                { 0, "Неудачно" },
                { 1, "Отменено пользователем" }
            };

            if (!dictionary.ContainsKey(state))
                return state.ToString();

            return dictionary[state];           
        }

        private string GetEvent(string event_, bool? plc2version)
        {
            if (plc2version == true)
                return "Отсутствует для модулей ПЛК 2";

            Dictionary<string, string> EventDict = new Dictionary<string, string>
            {
                                    { "Module rejected firmware for CRC", "Модуль отклонил прошивку для CRC"},
                                    { "Waiting", "В очереди" },
                                    { "Running", "В процессе" },
                                    { "Finished", "Обновление выполнено" },
                                    { "Starting", "Запуск" },
                                    { "Module failed finish command", "Не удалось выполнить обновление" },
                                    { "Download image completed", "Загрузка образа завершена" },
                                    { "Canceled by user", "Отменено пользователем" },
                                    { "Incompatible hardware.", "Несовместимые версии железа и программы" },
            };


            //Dictionary<string, string> EventDict = new Dictionary<string, string>();
            
            if (!EventDict.ContainsKey(event_))
                return event_;             

            return EventDict[event_];
        }
    }

    //Вся инфа по модулю для пользователя
    public class ModuleData : Changed
    {
        //Адрес модуля
        public UInt32 Node_Id { get; set; } = 0;
        //Статус обновления модуля
        public string Update_Status { get; set; } = "";
        //Тип модуля
        public string Type { get; set; } = "";
        //Событие обновления COm
        public string Com_Status { get; set; } = "";
        //Событие обновления Chanel
        public string Chan_Status { get; set; } = "";
        //Старая версия ПО
        public string OldSoftVer { get; set; } = "";
        //Новая версия ПО
        public string NewSoftVer { get; set; } = "";
        //Последнее обновление
        public string LastUpdate { get; set; } = "";
        //Тут цвет
        public UInt32 SetColor { get; set; } = 3;
    }
}
