using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Xml.XPath;
using AbakConfigurator.Secure;
using NpgsqlTypes;
using System.Windows.Interop;


namespace AbakConfigurator
{
    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        /// <summary>
        /// Publically expose BringIndexIntoView.
        /// </summary>
        public void BringIntoView(int index)
        {

            this.BringIndexIntoView(index);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        //Путь к CFG файлу
        private const string codesysControlCFG = "/etc/CODESYSControl.cfg";
        //Таймер обновления данных на экране
        private DispatcherTimer timer = new DispatcherTimer();
        //Таймер периодического чтения данных с модуля
        private DispatcherTimer moduleUpdateTimer = new DispatcherTimer();
        //Таймер периодического чтения данных с цпу
        private DispatcherTimer cpuUpdateTimer = new DispatcherTimer();
        //Период обновления данных с модуля в сек.
        private int moduleUpdatePeriod = 1;
        //список полей
        private List<GridViewColumn> columns = new List<GridViewColumn>();
        //Название произошедшего события в базе
        private String sqlNotification = "";
        //Параметр переданный при возникноаении события
        private String sqlNotificationParam = "";
        //Список сообщений с модулей
        private List<NpgsqlNotificationEventArgs> sqlMessages = new List<NpgsqlNotificationEventArgs>();
        //Выбранный модуль
        private CModule selectedModule = null;
        //Выбранная группа
        private CModuleGroup selectedGroup = new CModuleGroup();
        //Параметр, на который навели курсор
        //private CParam pointetParam = null;
        //Список модулей контроллера
        private CModulesList modulesList = null;
        //Контекстное меню управления модулем
        private ContextMenu moduleContextMenu;
        //Контекстное меню управления модулями
        private ContextMenu modulesContextMenu;
        //Путь к папке для сохранения архива с логами
        private string userDir = "";
        //+1 если модуль обновлён/не смог обновится (по ней отслеживается окончание всего обновления)
        private static int update_stopped = 0;
        //Количество модулей, которые должны быть обновлены
        private static int amountofrows = 0;
        //Количество модулей на шине до обновления(для проверки, что модуль не вытащили)
        private static int amountBeforeUpdate = 0;
        //Количество модулей на шине во время обновления(для проверки, что модуль не вытащили)
        private static int amountRigthNow = 0;
        //Тут хранится число ТОЛЬКО обновлённых модулей (для пользователя внизу)
        private static int modulesupdated = 0;
        //Этап прошивки
        private static int update_state = 0;
        //Список кодов АЦП (Актуально только для старых прошивок)
        CImagesList imagesList = new CImagesList();
        //Список модулей, которым отменили обновление
        List<int> CanceledModules = new List<int>();
        //Список модулей, которые будут обновляться
        ObservableCollection<CModule> ChoosedModules = new ObservableCollection<CModule>();
        //Модуль, который быль "отменен" во время обновления
        private static int private_module = 0;
        //Модули, которые не помечаны как "Неизвестное устройство"
        ObservableCollection<CModule> KnownModules = new ObservableCollection<CModule>();
        private static int save_update_state = 0;
        //Блокируем открытие данных об обновлённых модулях
        private static bool BlockList = false;
        //
        private string newIP = "";
        //
        private string oldIP = "";
        //Перевод текста из базы данных
        Dictionary<string, string> Translation = new Dictionary<string, string>
                                {
                                    { "Module rejected firmware for CRC", "Модуль отклонил прошивку для CRC"},
                                    { "Waiting", "В очереди" },
                                    { "Running", "В процессе" },
                                    { "Finished", "Завершено" },
                                    { "Starting", "Запуск" },
                                    { "Module failed finish command", "Не удалось выполнить обновление" },
                                    { "Download image completed", "Загрузка образа завершена" },
                                    { "Canceled by user", "Отменено пользователем" },
                                    { "Incompatible hardware.", "Несовместимые версии железа и программы" },
                                    { "Removed", "Модуль был удалён с шины" },
                                };
        //Словарь, в котором содержатся функции для состояний обновления модуля (завершено, отменено, ошибка)
        Dictionary<string, Delegate> UpdateActions = new Dictionary<string, Delegate>();
        //Словарь, в котором содержатся функции для состояний обновления все системы (запуск первого или второго этапа)
        Dictionary<int, Delegate> UpdateStateActions = new Dictionary<int, Delegate>();
        //Словарь, в котором содержатся прошивки канальных плат
        Dictionary<int, int> ChannelImagesList = new Dictionary<int, int>();
        //Словарь, в котором содержатся прошивки ком плат
        Dictionary<int, int> CommImagesList = new Dictionary<int, int>();
        private bool cdsForceFlag = false;
        private WaitingWindow WaitingWindow;
        private static bool sleepMode = false;
        //Словарь, в котором содержатся варианты сообщений для пользователя, если он решит отменить обновление модуля
        Dictionary<int, string> CanceledVariants = new Dictionary<int, string>()
        {
            {1,  "Обновление дополнительной части модуля было отменено"},
            {2,  "Остановка обновления невозможна!"},

        };
        //Таймер времени обновления параметров модуля при скролле
        Stopwatch screenParamsUpdateStopwatch = new Stopwatch();
        //(key - tag, value - key in StatusBar)
        private Dictionary<string, string> statusBarData = new Dictionary<string, string>()
        {
            { "INFO_STRING", "Description" },
            { "MODULES_COLLECTION_VERSION", "ModulesCollectionVersion" }
        };
        //Видимые параметры типа ListItemParamControl
        private Collection<ListItemParamControl> visibleValues = null;
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            DebugButton.Visibility = Visibility.Visible;
#endif
            CGlobal.CurrState = (CCurrentState)this.FindResource("currentState");

            this.moduleContextMenu = (ContextMenu)this.FindResource("moduleContextMenu");
            this.modulesContextMenu = (ContextMenu)this.FindResource("modulesContextMenu");

            //Инициализация таймера обновляющего экран
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            this.timer.Tick += new EventHandler(this.timer_Tick);
            //Инициализация таймера обновления модулей
            this.moduleUpdateTimer.IsEnabled = false;
            this.moduleUpdateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            this.moduleUpdateTimer.Tick += new EventHandler(this.moduleUpdateTimer_Tick);
            
            GridView grid = this.tagsListView.View as GridView;
            foreach (GridViewColumn col in grid.Columns)
            {
                this.columns.Add(col);
            }
            this.showTagsListColumns();
            SaveIP.SetUSB(CSettings.GetSettings().USB);
            SaveIP.SetIP(CSettings.GetSettings().IP);
            CGlobal.CurrState.Update_On = false;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            sleepMode = e.Mode == PowerModes.Suspend;
        }
        /// <summary>
        /// Запись в базу настроек синхроизации модулей
        /// </summary>
        private void writeSynchronizationSettings()
        {
            JObject settings = this.modulesList.GenerateJson();
            String sql = String.Format("select save_settings('modules_synch', '{0}')", settings.ToString());
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_writigSynchSettings"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static int CounterForUpdate = 0;
        private void BeforeUpdate()
        {
            CGlobal.CurrState.Update_On = true;
            if (WaitingWindow.EnableClosing)
                return;

            CounterForUpdate++;

            if (CounterForUpdate < 150 && amountRigthNow != amountBeforeUpdate)
                return;

            AfterWaitingWindow();
            WaitingWindow.HardClose();
            CounterForUpdate = 0;
            amountRigthNow = 0;
            WaitingWindow = null;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (sleepMode)
            {
                stopConnection();
                return;
            }

            if (CGlobal.CurrState.Update_On && WaitingWindow != null)
                BeforeUpdate();

            //Тут теги модуля, но не ЦПУ!
            List<CModuleParam> writeList = new List<CModuleParam>();
            foreach (CBaseParam param in CGlobal.Config.ModulesParamsList)
            {
                if (param.ManualChanged)
                {
                    writeList.Add(param as CModuleParam);
                    param.ManualChanged = false;
                }
            }

            if (writeList.Count > 0)
            {
                bool isNotify = false;

                try
                {
                    String sql = "";
                    NpgsqlCommand cmd;
                    //Запись значений отдельных параметров
                    foreach (CModuleParam param in writeList)
                    {
                        if (param.DescriptionChanged)
                        {
                            param.DescriptionChanged = false;
                            cmd = GetCmdForTagsDescription(param.Tagname, param.NameKey, param.Description);
                        }
                        else
                        {
                            sql = String.Format("execute write_cmd('{0}', '{1}', {2})",
                                param.Tagname, param.SQLValue, param.Module.NodeID);
                            isNotify = true;
                            cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                        }             
                        cmd.ExecuteNonQuery();
                    }
                    //Оповещение о том что мгновенные значения у модулей поменялись
                    if (isNotify)
                    {
                        sql = "notify module_write_values";
                        cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                        cmd.ExecuteNonQuery();
                        isNotify = false;
                    }
                }
                catch
                {

                }
            }

            if (this.sqlMessages.Count > 0)
            {

                //Проход по сообщениям
                while (this.sqlMessages.Count > 0)
                {
                    NpgsqlNotificationEventArgs notify = this.sqlMessages[0];
                    switch (notify.Channel)
                    {
                        //Добавление модуля на шину
                        case "add_module":
                            {
                                try
                                {
                                    UInt16 node_id = Convert.ToUInt16(notify.Payload);
                                    if (this.selectedModule?.NodeID == node_id)
                                    {
                                        //Убираются все отображаемые элементы
                                        //только если пропал выбранный модуль
                                        CGlobal.Config.ModulesParamsList.Clear();
                                    }

                                    CGlobal.Config.Modules.Load(CGlobal.Session.Connection, node_id);
                                    CModule module = CGlobal.Config.Modules.FindModule(node_id);
                                    if (module == null)
                                        break;

                                    module.FillParamsList(CConfig.ParamsList);
                                    module.ReadCurrentValues(CGlobal.Session.Connection);
                                    CGlobal.Config.SetModulesImagesData(module);
                                    if (ChoosedModules.Any(is_the_same => is_the_same.NodeID == module.NodeID))
                                        module.IsChecked = true;
                                    else
                                        module.IsChecked = false;
                                    CounterForUpdate = 0;
                                    if (CGlobal.CurrState.Update_On)
                                        amountRigthNow++;
                                }

                                catch
                                {

                                }
                            }
                            break;
                        //Удаление модуля с шины
                        case "remove_module":
                        //Изменение настроек модуля
                        case "update_module_persistent":
                            {
                                UInt16 node_id = Convert.ToUInt16(notify.Payload);
                                if (this.selectedModule?.NodeID == node_id)
                                {
                                    //Убираются все отображаемые элементы
                                    //только если пропал выбранный модуль
                                    CGlobal.Config.ModulesParamsList.Clear();
                                }
                                //Удаление модуля из списка
                                CGlobal.Config.Modules.RemoveModule(node_id);
                                //Теперь подгрузка, вдруг в базе есть настройки модуля
                                CGlobal.Config.Modules.Load(CGlobal.Session.Connection, node_id);
                            }
                            break;

                        //Изменение состояние модуля
                        case "update_module_state":
                            {
                                UInt16 node_id = Convert.ToUInt16(notify.Payload);
                                CModule module = CGlobal.Config.Modules.FindModule(node_id);
                                if (module == null)
                                    break;

                                module.ReadState(CGlobal.Session.Connection);
                            }
                            break;

                        //Изменение значений параметров в модуле
                        case "module_values_change":
                            {
                                try
                                {
                                    UInt16 node_id = Convert.ToUInt16(notify.Payload);
                                    CModule module = CGlobal.Config.Modules.FindModule(node_id);

                                    if (module == null)
                                    {
#if DEBUG
                                        MessageBox.Show("module == null");
#endif
                                        break;
                                    }
                                    module.ReadCurrentValues(CGlobal.Session.Connection);
                                    if (!emptyModules.Contains(module.NodeID))
                                        break;

                                    saveModuleSettings(module);
                                    emptyModules.Remove(module.NodeID);
                                }
                                catch (Exception ex)
                                {
#if DEBUG
                                    MessageBox.Show(ex.Message);
#endif
                                }
                            }
                            break;

                        //Обновление состояния процедцры обновления модуля
                        case "module_update_image_state":
                            {
                                JObject json = JObject.Parse(notify.Payload);
                                UInt16 node_id = Convert.ToUInt16(json["node"]);
                                CModule module = CGlobal.Config.Modules.FindModule(node_id);
                                if (module == null)
                                    break;

                                string[] isk2 = module.Module_Name.Split('.');

                                if (isk2.Contains("K2") && update_state == 2)
                                    break;

                                module.UpdateState = Convert.ToUInt16(json["state"]);
                                module.Progress = Convert.ToUInt16(json["progress"]);
                                module.GreenColor = 0;
                                module.RedColor = 10;
                                string get_text = Convert.ToString(json["state_text"]);

                                if (!Translation.ContainsKey(get_text))
                                    module.UpdateText = get_text;
                                else
                                    module.UpdateText = Translation[Convert.ToString(json["state_text"])];

                                if (UpdateActions.ContainsKey(module.UpdateText))
                                    UpdateActions[module.UpdateText].DynamicInvoke(module);

                                if (UpdateActions.ContainsKey(module.UpdateStateString))
                                    UpdateActions[module.UpdateStateString].DynamicInvoke(module);
                            }
                            break;
                    }

                    this.sqlMessages.RemoveAt(0);

                    UpdateScreen(amountofrows != 0);

                    if (amountofrows == update_stopped && update_stopped != 0)
                        if (amountofrows == update_stopped)
                        {
                            try
                            { UpdateStateActions[update_state].DynamicInvoke(); }
                            catch { };
                        }
                }
            }
            try
            {
                if(CGlobal.Session.Connection != null)
                    CGlobal.Session.Connection.Wait(1);
            }
            catch
            {
                //Если произошло отключение прибора, сессия закрывается
                CGlobal.Session.CloseSession();
            }
            UpdateScreenParamsList();
        }
        private NpgsqlCommand GetCmdForTagsDescription(string tag, string localization, string description)
        {
            //param.Name
            if (CGlobal.Config.TagsDescriptions.ContainsKey(tag))
            {
                CGlobal.Config.TagsDescriptions[tag] = new string[2] { localization, description };
            }
            else
            {
                CGlobal.Config.TagsDescriptions.Add(tag, new string[2] { localization, description });
            }
            const string query = "select update_values_descriptions_table(@in_tag, @in_localization, @in_description)";
            NpgsqlCommand command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);
            command.Parameters.AddWithValue("in_tag", NpgsqlDbType.Text, tag);
            command.Parameters.AddWithValue("in_localization", NpgsqlDbType.Text, localization);
            command.Parameters.AddWithValue("in_description", NpgsqlDbType.Text, description);
            return command;
        }
        private void UpdateFinished(CModule module)
        {
            if (private_module == module.NodeID)
                BlockList = false;

            int key;
            if (update_state != 0)
                key = update_state;
            else
            {
                key = save_update_state + 2;
                module.IsChecked = false;
            }

            module.SoftVersion = GetNewSoft(module.NodeID);
            update_stopped += 1;
            modulesupdated += 1;
            Dictionary<int, string> sql_update = new Dictionary<int, string>();
            try
            {
                string datetime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                sql_update.Add(1, "update update_info set state = 2, new_softver ='{0}', com_part ='Finished', date_time = '{2}'  where id ={1}");
                sql_update.Add(2, "update update_info set state = 2, new_softver ='{0}', channel_part ='Finished', date_time = '{2}' where id ={1}");
                sql_update.Add(3, "update update_info set state = 1, new_softver ='{0}', com_part ='Finished', channel_part = 'Canceled by user', date_time = '{2}'  where id ={1}");
                sql_update.Add(4, "update update_info set state = 1, new_softver ='{0}', channel_part ='Finished', date_time = '{2}' where id ={1}");
                string sql = String.Format(sql_update[key], module.SoftVersion, module.NodeID, datetime);
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_update.ExecuteNonQuery();
            }
            catch
            {

            }
        }
        private void UpdateError(CModule module)
        {
            string[] str = module.Module_Name.Split('.');

            int key = 0;
            //MessageBox.Show(module.Module_Name);
            if (update_state != 0)
                key = update_state;
            else
            {
                key = save_update_state;
                module.IsChecked = false;
            }
            update_stopped += 1;
            //Цвет ProgressBar
            module.GreenColor = 10;
            module.RedColor = 0;
            ChannelImagesList.Remove(module.NodeID);
            Dictionary<int, string> sql_update = new Dictionary<int, string>();
            try
            {
                string datetime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                sql_update.Add(1, "update update_info set state = 0, new_softver ='{0}', com_part ='Произошла ошибка', channel_part ='Отменено контроллером', date_time = '{2}'  where id ='{1}'");
                sql_update.Add(2, "update update_info set state = 0, new_softver ='{0}', channel_part ='Произошла ошибка', date_time = '{2}' where id ='{1}'");
                string sql = String.Format(sql_update[key], "", module.NodeID, datetime);
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_update.ExecuteNonQuery();
            }
            catch
            {

            }
        }
        private void UpdateCanceled(CModule module)
        {
            if (private_module == module.NodeID)
                return;

            int key = 0;
            if (update_state != 0)
                key = update_state;
            else
            {
                key = save_update_state;
                module.IsChecked = false;
            }
            amountofrows -= 1;
            Dictionary<int, string> sql_update = new Dictionary<int, string>();
            try
            {
                string datetime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                sql_update.Add(1, "update update_info set state = 1, new_softver ='{0}', com_part ='Canceled by user', channel_part ='Canceled by user', date_time = '{2}' where id ={1}");
                sql_update.Add(2, "update update_info set state = 1, new_softver ='{0}', channel_part ='Canceled by user', date_time = '{2}' where id ={1}");
                string sql = String.Format(sql_update[key], "", module.NodeID, datetime);
                NpgsqlCommand cmd_update = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd_update.ExecuteNonQuery();
            }
            catch
            {

            }
        }

        //Функция для проверки, что модуль обновляется и его нельзя трогать
        private void SQLProgress(int node_id)
        {
            if (!CanceledModules.Contains(node_id) || private_module == node_id)
                return;

            try
            {
                string sql = $"select update_progress from fast_modules where node_id = {node_id}";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                NpgsqlDataReader reader = cmd.ExecuteReader();

                reader.Read();
                int progress = Convert.ToInt32(reader.GetValue(0));
                reader.Close();

                if (progress == 0)
                    return;

                private_module = node_id;
                BlockList = true;
                MessageBox.Show(CanceledVariants[update_state], "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch
            {

            }
        }
        private string GetNewSoft(int node_id)
        {
            String sql = $"select swver from fast_modules where node_id = {node_id}";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            string softver = "";
            try
            {
                if (!reader.HasRows)
                    return "";

                Thread.Sleep(500);
                reader.Read();
                softver = reader.GetString(0);
                reader.Close();
            }
            catch
            {

            }
            return softver;
        }
        private void UpdateScreen(bool state)
        {
            if (!state)
                return;

            Dictionary<int, string> dictionary = new Dictionary<int, string>
            {
                {1,  "Обновлено модулей (Основная часть): "},
                {2,  "Обновлено модулей (Дополнительная часть): "},
            };

            try
            {
                CGlobal.CurrState.UpdatedModules = dictionary[update_state] + modulesupdated.ToString() + "/" + amountofrows.ToString();
            }
            catch
            {
                //CGlobal.CurrState.UpdatedModules = 
            }
        }
        //Старт обновления канальный плат
        private void StartChannelUpdate()
        {
            //Модули иногда отваливаются, после обновления. Это критично для последнего из них, потому что он просто не запуститься
            Thread.Sleep(5000);
            UpdateStart(ChannelImagesList);
        }
        private void UpdatesFinished()
        {
            foreach (CModule module in ChoosedModules)
                module.IsChecked = false;

            ChoosedModules.Clear();

            if (CGlobal.CurrState.Update_On)
            {
                ListOfUpdateModules UpdatedWindow = new ListOfUpdateModules(CGlobal.Session.Connection);
                UpdatedWindow.Owner = this;
                UpdatedWindow.ShowDialog();
            }

            try
            {
                String sql = $"select * from module_update_queue";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                    return;

                reader.Close();
                MainTick.IsChecked = false;
                ChooseAllModules();
                CGlobal.CurrState.Update_On = false;
                amountofrows = 0;
                update_stopped = 0;
                modulesupdated = 0;
                update_state = 0;

            }

            catch (Exception ex)
            {
                CGlobal.CurrState.Update_On = false;
                MessageBox.Show("Во время завершения обновления произошла ошибка:" + "" + ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (cdsForceFlag)
            {
                cdsForceFlag = false;
                CAuxil.SetTagValue(false.ToString(), "CDS_FORCED");
            }
            return;
        }

        private void FillTheQueue(int node_id, int id)
        {
            String sql = $"insert into module_update_queue(node, update) values ({node_id},{id})";
            try
            {
                CPostgresAuxil.ExecuteNonQuery(CGlobal.Session.Connection, sql);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_moduleUpdateInitError"), ex.Message); //Ошибка инициализации процедуры обновления
                MessageBox.Show(message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        //Период обновления данных с модуля в сек
        public int ModuleUpdatePeriod
        {
            get
            {
                return this.moduleUpdatePeriod;
            }
            set
            {
                if (this.moduleUpdatePeriod == value)
                    return;

                this.moduleUpdatePeriod = value;

                this.moduleUpdateTimer.Interval = new TimeSpan(0, 0, 0, this.moduleUpdatePeriod, 0);
                this.OnPropertyChanged("ModuleUpdatePeriod");
            }
        }
        public Boolean EnableModuleUpdateTimer
        {
            get
            {
                return this.moduleUpdateTimer.IsEnabled;
            }
            set
            {
                this.moduleUpdateTimer.IsEnabled = value;
                this.OnPropertyChanged("EnableModuleUpdateTimer");
            }
        }
        private void moduleUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!CGlobal.CurrState.IsRunning)
                return;

            //Опрос запущен, значит можно и состояние модуля обновить

            if (!this.updateDataFromModule())
                updateDataFromModule(null, false);
        }
        private bool WriteTagsValues()
        {
            if (CGlobal.Session == null)
                return true;

            if (CGlobal.Session.Connection == null)
                return true;
            string sql = "";
            //Список для записи
            List<CParam> writeList = new List<CParam>();
            //Отработка по активным тегам
            foreach (CParam param in CGlobal.Config.GetActiveTags())
            {
                if (param.ManualChanged)
                {
                    writeList.Add(param);
                    param.ManualChanged = false;
                }
            }
            //Формирование SQL запроса на запись
            if (writeList.Count > 0)
            {
                foreach (CParam param in writeList)
                {
                    sql = String.Format("execute update_fast('{0}', {1})", param.WriteValue, param.ID);
                    NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                    cmd.CommandTimeout = 5;
                    cmd.ExecuteNonQuery();
                }
            }
            return false;
        }
        private void UpdateStatusBar(string tag, string value)
        {
            switch (tag)
            {
                case "INFO_STRING":
                    CGlobal.CurrState.Caption = value;
                    break;

                case "MODULES_COLLECTION_VERSION":
                    CGlobal.CurrState.ModulesCollectionVersion = value;
                    break;
            }
        }
        private bool ReadTagsValues(string sql)
        {
            if (CGlobal.Session == null)
                return true;

            if (CGlobal.Session.Connection == null)
                return true;

            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.CommandTimeout = 2;
            NpgsqlDataReader reader = cmd.ExecuteReader();

            //Если не получилось группы прочитать, то и делать тут нечего
            if (!reader.HasRows)
                return true;

            //Чтение полученных значений
            while (reader.Read())
            {
                //Здесь идет обновление по полному списку, т.к. есть параметры которые должны опрашиваться всегда
                //Тег параметра в базе данных
                if (reader.IsDBNull(0))
                    continue;

                string[] select = sql.Split(',');
                CParam param = null;
                String data = reader.GetValue(0).ToString();
                switch (select[0])
                {
                    case "select tag":
                        param = CConfig.ParamByTag(data) as CParam;
                        break;
                    case "select id":
                        KeyValuePair<int, string> pair = CConfig.PairParamsList.FirstOrDefault(x => x.Key == Convert.ToInt32(data));
                        if (pair.Value != null)
                        {
                            param = CConfig.ParamByTag(pair.Value) as CParam;
                        }
                        break;
                }

                if (param == null)
                    continue;

                //Текущее значение параметра
                if (reader.IsDBNull(1))
                {
                    param.Value = "";
                    continue;
                }
                param.Value = reader.GetString(1);
                UpdateStatusBar(param.Tagname, param.Value.ToString());
                param.CpuTag = true;
            }
            reader.Close();
            return false;
        }
        //Счётчик ошибок от БД
        private int dbErrors = 0;
        //соединение которое опрашивает активные теги
        Stopwatch sw = new Stopwatch();
        private void CpuUpdateTimer_Tick(object sender, EventArgs e)
        {
            if(!CGlobal.CurrState.IsConnected && !CGlobal.CurrState.IsRunning)
            {
                cpuUpdateTimer.Stop();
                return;
            }

            //Соединение есть, опрос активных тегов
            try
            {
                bool writeError = WriteTagsValues();
                bool readError = ReadTagsValues("select tag, value from fast_table");
               
                //ЦПУ их обновляет раз в 5 секунд
                if (sw.ElapsedMilliseconds == 0 || sw.ElapsedMilliseconds > 5000)
                {
                    sw.Restart();
                    ReadTagsValues("select id, value from fast_redundancy_partner");
                }

                if (writeError || readError)
                    dbErrors++;
            }
            catch
            {
                dbErrors++;
            }
          
            if (dbErrors >= 2)
            {
                stopConnection();
                CGlobal.Handler.UserLogout();
                dbErrors = 0;
                MessageBox.Show("Связь с контроллером потеряна!", "Ошибка связи!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private ScrollViewer getScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer)
                return d as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                ScrollViewer scr = this.getScrollViewer(VisualTreeHelper.GetChild(d, i));
                if (scr != null)
                    return scr;
            }
            return null;
        }

        /// <summary>
        /// Функция перемещает список ч тегами что бы выбранная группа или подгруппа была на видном месте
        /// </summary>
        /// <param name="param"></param>
        private void scrollTagsListView(CParam param)
        {
            ScrollViewer scr = this.getScrollViewer(this.tagsListView);
            if (scr == null)
                return;

            ItemsPresenter ip = scr.Content as ItemsPresenter;

            int index = this.tagsListView.Items.IndexOf(param);
            FrameworkElement element = this.tagsListView.ItemContainerGenerator.ContainerFromItem(param) as FrameworkElement;

            Point point = element.TranslatePoint(new Point(), ip);
            scr.ScrollToVerticalOffset(point.Y - element.ActualHeight);
        }

        private void Refreshtem_Click(object sender, RoutedEventArgs e)
        {
            foreach (CModule module in ModulesList.GroupsList)
                updateDataFromModule(module.NodeID);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Показываем окно настроек связи с контроллером
            ChannelsWindow chWindow = new ChannelsWindow(CGlobal.Settings);
            chWindow.Owner = this;
            if (chWindow.ShowDialog() == true)
            {
                CGlobal.Settings.Assign(chWindow.Settings);
                CGlobal.Settings.Save();
            }
        }

        private bool loadControllerConfiguration(NpgsqlConnection connection)
        {
            if (!CGlobal.Config.Load(connection))
            {
                CGlobal.Handler.UserLogout();
                return false;
            }

            this.ModulesList = CGlobal.Config.Modules;
            //Подгрузка непосредственно значений параметров
            String sql = "select tag, value from fast_table";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (!reader.HasRows)
                    return false;

                while (reader.Read())
                {
                    try
                    {
                        //id параметра в базе данных
                        String tag = reader.GetString(0);
                        if (!CConfig.ParamsList.TryGetValue(tag, out CBaseParam param))
                            continue;

                        param = param as CParam;
                        //Текущее значение параметра
                        if (!reader.IsDBNull(1))
                        {
                            param.WriteValue = reader.GetString(1);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            finally
            {
                reader.Close();
            }
            return true;
        }
        //Сбрасываем флаги авторизации с окна сессий
        private void FlagsReset()
        {
            CGlobal.Settings.flagSessionWindow = false;
            CGlobal.CurrState.flagSessionWindows = false;
            CGlobal.Session.flagStartSession = false;
        }
        private void SetVisiblity()
        {
            if(CGlobal.Handler.Auth.Account != "root")
            {
                StopAbak.Visibility = Visibility.Collapsed;
            }
            if(CGlobal.CurrState.PLCVersionInfo == 2)
            {
                WDTstatus.Visibility = Visibility.Collapsed;
            }
            ModulesUpdateGrid.Visibility = Visibility.Visible;
            DownloadToolButton.IsEnabled = CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType < GroupTypeEnum.Spectator;
            UploadToolButton.IsEnabled = CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType < GroupTypeEnum.Spectator;
            PrepareMicroSD.Visibility = (CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType == GroupTypeEnum.Developer) ? Visibility.Visible : Visibility.Collapsed;

            if (CGlobal.NaladchikStyle)
            {
                Naladchik.Visibility = Visibility.Visible;
                NaladchikButton.Visibility = Visibility.Visible;
            }
            else
            {
                Naladchik.Visibility = Visibility.Collapsed;
                NaladchikButton.Visibility = Visibility.Collapsed;
            }
        }

        bool isStarted = false;
        private bool StartConnections(bool isTheSame)
        {
            sqlMessages.Clear();
            CGlobal.Session.flagStartSession = true;
            CGlobal.Config.Clear();

            if (!CGlobal.Session.OpenSession(true))
            {
                return false;
            }

            if (!isTheSame || !isStarted || CGlobal.Handler == null || !CGlobal.Handler.Auth.Authorized)
            {
                CGlobal.Handler = new SecureHandler(this);

                CGlobal.Handler.Repo.Init();
                CGlobal.Handler.Repo["Rule"].Load();

                if (!CGlobal.Handler.Auth.Authorized && !CGlobal.Handler.UserLogin())
                {
                    return false;
                }

                CGlobal.CurrState.Menu.Refresh();

                oldIP = newIP;
            }

            //Удалось открыть сессию
            //Пытаемся получить данны о сборке
            if (!CGlobal.CurrState.PlcTypeReader())
            {
                MessageBox.Show("Не получить данные о сборке", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            //Подгрузка конфигурации контроллера
            if (!this.loadControllerConfiguration(CGlobal.Session.Connection))
                return false;
            CGlobal.CurrState.IsRunning = true;
            SetVisiblity();
            CGlobal.CurrState.IP = CSettings.GetSettings().IP;
            //Сбрасываем флаги авторизации с окна сессий
            FlagsReset();
            //Отработка по фиксированным параметрам
            this.processFixedParams();
            IsDictionaryEmpty();
            CGlobal.Session.Connection.Notification += this.NotificationEventHandler;
            this.timer.IsEnabled = true;
            cpuUpdateTimer.IsEnabled = true;
            cpuUpdateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            cpuUpdateTimer.Tick += new EventHandler(CpuUpdateTimer_Tick);
            return true;
        }

        private void ConnectViaUsb_Click(object sender, RoutedEventArgs e)
        {
            SaveIP.SetIP("192.168.7.2");
            string message = CGlobal.Session.CheckPing();
            if (message != "")
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TryOpenChannelsWindow(true);
        }

        private void ConnectViaEthernet_Click(object sender, RoutedEventArgs e)
        {
            TryOpenChannelsWindow(false);
        }

        private void TryOpenChannelsWindow(bool isUsb)
        {
            if (!OpenChannelsWindow(isUsb))
                return;

            isStarted = CGlobal.CurrState.IsRunning = CGlobal.CurrState.IsConnected = StartConnections(newIP == oldIP);
            if (!isStarted)
            {
                CGlobal.Handler.UserLogout();
            }
        }

        private bool OpenChannelsWindow(bool isUsb)
        {
            //Показываем окно настроек связи с контроллером
            ChannelsWindow chWindow = new ChannelsWindow(CGlobal.Settings, isUsb);
            chWindow.Owner = this;
            if (!isUsb)
            {
                chWindow.ShowDialog();
                if (!chWindow.IsChoosen)
                    return false;

                newIP = chWindow.IP;
            }
            else
                newIP = "192.168.7.2";

            CGlobal.Settings.Assign(chWindow.Settings);
            CGlobal.Settings.Save();
            return true;
        }

        /// <summary>
        /// Функция останавливает работу с контроллером
        /// </summary>
        private void stopConnection()
        {
            CGlobal.NaladchikStyle = false;
            this.timer.Stop();
            cpuUpdateTimer.Stop();
            try
            {
                if (CGlobal.Session != null)
                    CGlobal.Session.flagStartSession = false;

                CGlobal.Session.CloseSession();
            
            }
            catch
            {

            }
            finally
            {
                CGlobal.CurrState.IsConnected = false;
                CGlobal.CurrState.IsRunning = false;
                this.EnableModuleUpdateTimer = false;
                cpuUpdateTimer.Stop();
            }
        }

        //Функция для проверки версии модуля, обновится ли скорость CAN-шины автоматически или нет
        private bool CheckModuleVersion(string version, UInt32 moduletype)
        {
            string[] isplc3 = version.Split(new char[] { '.' });

            //Если версия ПО содержит четыре цифры, то это модуль ПЛК 3 и он с новой сборкой, которая автоматически обновит скорость
            if (isplc3.Count() >= 4)
                return true;

            //Если пришли сюда, то ПЛК 3 точно не обновится и надо проверять, а не ли ПЛК 2 ли это
            //Тип модуля должен быть меньше 10 и не равен 999 (это всё типы ПЛК 2)
            if ((Convert.ToInt32(isplc3[0]) >= 2) && moduletype < 10)
                return true;

            return false;
        }
        public void SetCanBuses_Click(object sender, RoutedEventArgs e)
        {
            CanManagerSettingsWindow canManager = new CanManagerSettingsWindow();
            canManager.Owner = this;
            canManager.ShowDialog();
        }
        public void ChangeCANSpeed_Click(object sender, RoutedEventArgs e)
        {
            Stream stream = CGlobal.Session.SSHClient.ReadFile("/opt/abak/A:/assembly/configs/can_speed.json");
            if (stream == null)
            {
                MessageBox.Show("Не удалось прочитать настройки CAN-шины", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CANspeedWindow canSpeed;
            try
            {
                canSpeed = new CANspeedWindow(stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Проблема инициализации окна настроек CAN-шины. Сообщение: {ex.Message}", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            canSpeed.Owner = this;
            if (canSpeed.ShowDialog() != true)
                return;
#if (!DEBUG)
            try
            {
                string cmd = "cansend can0 000#81FF;cansend can1 000#81FF;ifconfig can0 down;" +
                             "ifconfig can1 down;reboot;";
                CGlobal.Session.SSHClient.ExecuteCommand(cmd);
            }
            catch
            {
                CGlobal.Handler.UserLogout();
                stopConnection();
                MessageBox.Show("Пожалуйста, дождитесь перезапуска контроллера.", "Перезагрузка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
#endif
        }

        private void Naladchik_Click(object sender, RoutedEventArgs e)
        {

            MACAddressesWindow mACAddresses = new MACAddressesWindow();
            mACAddresses.Owner = this;
            mACAddresses.ShowDialog();
        }

        private bool FindWord(string fullString, string subString, bool registerCheck)
        {
            if (!registerCheck)
            {
                fullString = fullString.ToLower();
                subString = subString.ToLower();
            }
            //Сколько символов в нужном нам слове
            int count = subString.Count();

            bool[] chrFlags = new bool[count];
            //Массив chr для того слова, что надо найти
            char[] chrSubstring = subString.ToCharArray();
            //Массив chr для всей строки, чтобы искать совпадения по символам
            char[] chrFullString = fullString.ToCharArray();

            for (int chrNumb = 0; chrNumb < chrFullString.Count(); chrNumb++)
            {
                if (chrFullString[chrNumb] != chrSubstring[0])
                    continue;

                //Если нашли, то ставим флаг этому символу
                for (int counter = 0; counter < count; counter++)
                {
                    chrFlags[counter] = chrFullString[chrNumb + counter] == chrFullString[counter];

                    if (chrFlags[counter])
                        continue;

                    break;
                }
            }
            //Если первый и последний член массива имеет флаг, то точно нашли слово
            return chrFlags[0] && chrFlags[count - 1];
        }
        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
           
            if (dialog.ShowDialog() != true)
                return;

            using (StreamReader r = new StreamReader(dialog.FileName))
            {
                try
                { string json = r.ReadToEnd();
                    JObject info_obj = JObject.Parse(json);
                    var children = info_obj.Children().Last().First();
                    var count = children.Count();
                    foreach (var child in children)
                    {
                        var t = child["eth"].ToString();
                    }

                }
                catch
                {

                }
            }
        }
        private JObject ParseJson(Stream stream)
        {
            StreamReader readerJson = new StreamReader(stream);
            string jsonText = "";
            while (readerJson.Peek() >= 0)
            {
                string line = readerJson.ReadLine();
                if (line == "")
                    continue;

                jsonText += $"{line}";
            }
            return JObject.Parse(jsonText);
        }
        private void StopMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Останов процедуры опроса контроллера
            CGlobal.Handler.UserLogout();
            this.stopConnection();
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            //Выход из приложения
            this.Close();
        }

        /// <summary>
        /// Функция отрабатывает по значениям фиксированных параметров
        /// </summary>
        private void processFixedParams()
        {
            try
            {
                //Серийный номер
                CParam param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "SERNUM") as CParam;
                if (param != null)
                    CGlobal.CurrState.Serial = param.Value.ToString();
                //Описание контроллера
                param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "INFO_STRING") as CParam;
                if (param != null)
                    CGlobal.CurrState.Caption = param.Value.ToString();
                //Версия сборки

                param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "ASSEMBLY") as CParam;
                if (param != null)
                    CGlobal.CurrState.ProcessAssemblyString(param.Value.ToString());

                //Флаг работы сторожевого таймера
                param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "WDT_STATE") as CParam;
                if (param != null)
                    CGlobal.CurrState.WDT_On = (Boolean)param.Value; //Параметр есть такой в базе, надо отработать по значению
                else
                    CGlobal.CurrState.WDT_On = null; //Нет такого параметра
            }
            catch
            {

            }
        }
        private void workerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //this.tagsListView.BeginInit();
            //CXparams.UpdateParamsList();
            //this.tagsListView.EndInit();
        }

        /// <summary>
        /// Функция сохраняет конфигурацию в файл
        /// </summary>
        /// <param name="path"></param>
        private void saveConfig()
        {
            if (CGlobal.Session.SSHClient == null)
                return;

            const string path = "/tmp/backup/tags.xml";
            //Создание документа с проектом
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(decl, root);
            XmlNode rootNode = doc.CreateNode("element", "root", "");
            XmlAttribute attr = doc.CreateAttribute("hardware");
            if (CGlobal.IsIVKAbak)
                attr.Value = "ivk";
            else
                attr.Value = "plc";
            rootNode.Attributes.Append(attr);
            doc.AppendChild(rootNode);

            //Узел с настройками контроллера
            XmlNode controllerNode = doc.CreateElement("element", "controller", "");
            rootNode.AppendChild(controllerNode);
            CGlobal.Config.Save(controllerNode);
            MemoryStream mem = new MemoryStream();
            doc.Save(mem);
            mem.Position = 0;
            CGlobal.Session.SSHClient.ExecuteCommand($"touch {path}");
            CGlobal.Session.SSHClient.WriteFile(path, mem);
        }
        private void SaveConfigHandler(object sender, RoutedEventArgs e)
        {
            //Сохранение конфигурации
            String config = "";
            if (CGlobal.CurrState.ConfigPath == "")
                config = CAuxil.SelectFileToSaveOrOpen(true, true);
            else
                config = CGlobal.CurrState.ConfigPath;

            this.saveConfig();
        }

        private void SaveConfigAsHandler(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Session.SSHClient == null)
                return;

            this.saveConfig();
            string sql = "select copy_data_from_tables_to_files()";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();
            CGlobal.Session.SSHClient.ExecuteCommand("/opt/abak/A:/assembly/createBackup.sh &");
        }

        /// <summary>
        /// Флаг загружает проект из указанного файла
        /// </summary>
        /// <param name="config">Путь к файлу с проектом</param>
        /// <param name="userParamsOnly">Если true то надо загрузить только пользовательские параметры и конфигурацию, а xparams не читать</param>
        private void OpenConfig(String config, Boolean userParamsOnly)
        {
            //Открывается конфигурация
            if (config == "")
                return;

            XPathDocument doc = new XPathDocument(config);
            XPathNavigator nav = doc.CreateNavigator();
            //Вначале получаем информацию с каким устройством идет работа
            String type = CXML.getAttributeValue(nav.SelectSingleNode("root"), "hardware", "ivk");
            CGlobal.IsIVKAbak = type == "ivk";
            XPathNavigator xNav = nav.SelectSingleNode("//controller");
            CGlobal.Config.Load(xNav);
            this.ModulesList = CGlobal.Config.Modules;
        }
        private void OpenConfigHandler(object sender, RoutedEventArgs e)
        {
            //Открывается конфигурация
            String config = CAuxil.SelectFileToSaveOrOpen(false, true);
            this.OpenConfig(config, false);
            CGlobal.CurrState.ConfigPath = config;
        }
        private void ReadConfigFromController_Handler(object sender, RoutedEventArgs e)
        {
            BackupWindow backupWindow = new BackupWindow() { Owner = this };
            backupWindow.ShowDialog();

            CGlobal.Handler.UserLog(2, string.Format("Backup configuration"));
        }
        private void ListViewDoubleClick_Handler(object sender, MouseButtonEventArgs e)
        {
            //ListViewItem item = sender as ListViewItem;
            //CXParam param = item.DataContext as CXParam;
        }
        private void WriteConfigToController(string path)
        {
            const string msg = "Ошибка при загрузке файла в контроллер.";
            CAuxil.DecryptFile(path, out string error);
            if (error != "")
            {
                MessageBox.Show($"{msg}. {error}",
                    "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                CConfigLogic cConfigLogic = new CConfigLogic();
                if (!cConfigLogic.Start())
                {
                    MessageBox.Show($"{msg}",
                    "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ChangeWdtState(2);
                CAuxil.ExecuteSingleSSHCommand("systemctl stop abak_power");
                CGlobal.Session.SSHClient.ExecuteCommand("/tmp/backup/writeCmd.sh >> /var/log/abak/config.log");
                CGlobal.Handler.UserLogout();
                RestartOrRebootAbak("reboot");
            }
            catch
            {

            }
        }
        private async void WriteConfigToController_Handler(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Службы контроллера будут перезапущены. Вы уверены, что хотите продолжить?",
                "Запись настроек", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            openDialog.DefaultExt = "abak_cfg";
            openDialog.Filter = "Файлы конфигурации(*.abak_cfg)|*.abak_cfg";
            if (openDialog.ShowDialog() != true)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Write configuration to controller"));

            WaitingWindow = new WaitingWindow();
            WaitingWindow.Owner = this;
            WaitingWindow.Show();
            await Task.Run(() => WriteConfigToController(openDialog.FileName));
            Thread.Sleep(100);
            WaitingWindow.Close();

            CGlobal.Handler.UserLog(2, string.Format("Write configuration"));

            stopConnection();
            MessageBox.Show(CGlobal.GetResourceValue("l_configSuccessWrite") +
                ". Пожалуйста, дождитесь перезапуска служб контроллера.",
                this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void NewConfigClick_Handler(object sender, RoutedEventArgs e)
        {
            //Создание новой конфигурации
            CGlobal.CurrState.ConfigPath = "";
            CGlobal.Config.Clear();
        }

        private void MainFormLoaded(object sender, RoutedEventArgs e)
        {
            //Маркер, нужен для отслеживания запуска конфигуратора при инсталяции
            Mutex mut = new Mutex(false, "Abak_mutex");
            //Разворачиваются два корневых элемента в дереве проекта, что бы поиск по содержимому заработал
            foreach (object i in this.groupsTreeView.Items)
            {
                //Get the TreeViewItem associated with the iterated object model
                TreeViewItem tvi = this.groupsTreeView.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                if (tvi != null)
                    tvi.IsExpanded = true;
            }
        }

        private void AddSubgroupClick(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteSubGroupsClick(object sender, RoutedEventArgs e)
        {
        }

        private void DeleteSubGroup_Handler(object sender, RoutedEventArgs e)
        {
        }

        private void AddParamHandler(object sender, RoutedEventArgs e)
        {
        }

        private void ListViewMouseDown_Handler(object sender, MouseButtonEventArgs e)
        {

            this.ChangeParamMenuItem.IsEnabled = false;
            this.DeleteParamMenuItem.IsEnabled = false;
        }

        //private void ListViewItemMouseLeave_Handler(object sender, MouseEventArgs e)
        //{
        //    if (!topltipState)
        //        return;

        //    topltipState = false;
        //    toolTipTimer.Stop();
        //    toolTip.IsOpen = false;
        //}

        //private void ListViewItemMouseEnter_Handler(object sender, MouseEventArgs e)
        //{
        //    //if (Propmts.IsChecked != true)
        //    //return;

        //    topltipState = true;
        //    ListViewItem listViewItem = sender as ListViewItem;
        //    pointetParam = listViewItem.Content as CParam;
        //    toolTipTimer.Start();
        //}

        private void ListViewItemMouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            //Нажали на мышку
            this.ChangeParamMenuItem.IsEnabled = false;
            this.DeleteParamMenuItem.IsEnabled = false;

            if ((this.tagsListView.SelectedItem == null) || (!(this.tagsListView.SelectedItem is CParam)))
                return;

            //CParam param = (CParam)this.tagsListView.SelectedItem;
            //if (param.Editable)
            //{
            //    this.ChangeParamMenuItem.IsEnabled = true;
            //    this.DeleteParamMenuItem.IsEnabled = true;
            //}
        }

        private void ModulesTagsViewItemMouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            //Нажали на мышку
        }

        private void MainFormClosed(object sender, EventArgs e)
        {
            if (CGlobal.Handler != null && CGlobal.Handler.Auth.Authorized)
            {
                CGlobal.Handler.UserLogout();
            }
            this.timer.Stop();
            Process.GetCurrentProcess().Kill();
        }
        
        private void ShowAboutWindow_Handler(object sender, RoutedEventArgs e)
        {
            AboutWindow.ShowAboutWindow(this);
        }

        private void listViewItemKeyUp_Handler(object sender, KeyEventArgs e)
        {
            ListView listView = CAuxil.GetDependencyObjectFromVisualTree(sender as DependencyObject, typeof(ListView)) as ListView;
            if ((listView.SelectedItem == null) || (!(listView.SelectedItem is CBaseParam cBaseParam)))
                return;
            //moduleValueCol
            //visibleValues;
            if (e.Key == Key.Enter)
            {
                ListViewItem item = (ListViewItem)sender;
                EditParamControl editControl = (EditParamControl)item.Tag;
                ListItemParamControl param = visibleValues.FirstOrDefault(x => x.param.Tagname == cBaseParam.Tagname);
                param.showEdit(true);
            }
        }

        private void WritePCTimeToController_Handler(object sender, RoutedEventArgs e)
        {
            //Запись в контроллер текущего времени
            CorrectTimeWindow timeWindow = new CorrectTimeWindow(CGlobal.Config);
            timeWindow.Owner = this;
            timeWindow.IsRunning = CGlobal.CurrState.IsRunning;
            timeWindow.ShowDialog();
        }

        private void CopyXparamTagName_Handler(object sender, RoutedEventArgs e)
        {
            //Копирование имени тега в буфер обмена
            if (this.tagsListView.SelectedItem == null)
                return;

            //Редактирование выбранного параметра
            CParam param = (CParam)this.tagsListView.SelectedItem;
            try
            {
                Clipboard.Clear();
                Clipboard.SetDataObject(param.Tagname);
            }
            catch (Exception ex)
            {
                String message = String.Format("Ошибка копирования в буфер: '{0}'", ex.Message);
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RussianLanguageSelected(object sender, RoutedEventArgs e)
        {
            App.Language = App.RussianLang;
            CGlobal.Config.ChangeLanguage();
        }

        private void EnglishLanguageSelected(object sender, RoutedEventArgs e)
        {
            App.Language = App.EnglishLang;
            CGlobal.Config.ChangeLanguage();
        }

        private void ShowAppSettings_Handler(object sender, RoutedEventArgs e)
        {

            //Показываем окно настроек связи с контроллером
            AppSettingsWindow appWindow = new AppSettingsWindow();
            appWindow.Owner = this;
            appWindow.Settings.Assign(CGlobal.Settings);

            if (appWindow.ShowDialog() == true)
            {
                CGlobal.Settings.Assign(appWindow.Settings);
                CGlobal.Settings.Save();

                this.showTagsListColumns();
            }
        }

        /// <summary>
        /// Функция показывает или скрывает колонки у списка тегов
        /// </summary>
        private void showTagsListColumns()
        {
            GridView grid = this.tagsListView.View as GridView;
            grid.Columns.Clear();

            grid.Columns.Add(this.columns[0]);
            if (CGlobal.Settings.NameColumnVisible)
                grid.Columns.Add(this.columns[1]);
            if (CGlobal.Settings.ValueColumnVisible)
                grid.Columns.Add(this.columns[2]);
            if (CGlobal.Settings.TypeColumnVisible)
                grid.Columns.Add(this.columns[3]);
            if (CGlobal.Settings.TagColumnVisible)
                grid.Columns.Add(this.columns[4]);
        }

        private void groupsTreeViewMouseUp_Handler(object sender, MouseButtonEventArgs e)
        {
            if (this.groupsTreeView.SelectedItem == null)
            {
                return;
            }

            CParamsGroup group = this.groupsTreeView.SelectedItem as CParamsGroup;
            foreach (CParam param in group.ParamsList)
            {
                this.scrollTagsListView(param);
                break;
            }
        }

        private void ResetSession_Handler(object sender, RoutedEventArgs e)
        {
            //Сброс сессии
            CGlobal.Session.ResetSession();
        }

        private void DeleteSession_Handler(object sender, RoutedEventArgs e)
        {
            CGlobal.Session.DeleteCurentSession();
        }

        private void StartAbak_Handler(object sender, RoutedEventArgs e)
        {

            if (MessageBox.Show(CGlobal.GetResourceValue("l_startAbakMenuItem"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Start controller services"));
            CAuxil.ExecuteSingleSSHCommand("systemctl start abak_power"); 
        }

        private void StopAbak_Handler(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(CGlobal.GetResourceValue("l_stopAbakMenuItem"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Stop controller services"));
            CAuxil.ExecuteSingleSSHCommand("systemctl stop abak_power");
        }

        private void InterfacesList_Handler(object sender, RoutedEventArgs e)
        {
            //Показывает окно с настройками Ethernet интерфейсов
            CSSHClient sshClient = new CSSHClient(CSettings.GetSettings().ConnectIP, CSettings.GetSettings().ConnectSSHport, CGlobal.Session.SshUser, CGlobal.Session.SshPassword);
            EthernetWindow ethWindow = new EthernetWindow(sshClient);
            ethWindow.Owner = this;
            ethWindow.ShowDialog();
            CGlobal.Handler.UserLog(2, string.Format("Update Ethernet"));
            if (!ethWindow.isTheSame)
            {
                return;
            }
            try
            {
                Thread myThread = new Thread(ethWindow.UpdateEth);
                myThread.Start();
                Thread.Sleep(2000);
            }
            catch
            {
                this.stopConnection();
            }
            finally
            {
                CGlobal.Handler.UserLogout();
                this.stopConnection();
                MessageBox.Show("Настройки успешно изменены!", "Настройки сетевых интерфейсов",
                     MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        //Сохранение проекта Codesys
        private void DownloadSourceCode_Handler(object sender, RoutedEventArgs e)
        {
            Stream fileStream = CGlobal.Session.SSHClient.ReadFile("/var/opt/codesys/PlcLogic/Archive.prj");
            if (fileStream == null)
            {
                MessageBox.Show("В контроллере отсутствует исходный код проекта Codesys", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = "Archive.prj";
            saveDialog.ShowDialog();
            using (Stream file = File.Create(saveDialog.FileName))
            {
                fileStream.CopyTo(file);
            }

            CGlobal.Handler.UserLog(2, string.Format("Download Codesys project"));
        }

        private void USBsettings_Handler(object sender, RoutedEventArgs e)
        {
            //Показывает окно с настройками USB интерфейсов
            USBWindow usbWindow = new USBWindow(CGlobal.Session.SSHClient, CGlobal.Session.Connection);
            usbWindow.Owner = this;
            usbWindow.ShowDialog();
        }

        private void COMList_Handler(object sender, RoutedEventArgs e)
        {
            //Показывает окно с настройками COM портов
            try
            {
                //Либо для ПЛК 3, либо для ПЛК 2, но уже с прошивкой пятой версии
                if (CGlobal.CurrState.PLCVersionInfo == 3 || CGlobal.CurrState.AssemblyHi == 5)
                {
                    COMPortsPLC3Window portsWindow = new COMPortsPLC3Window(CGlobal.Session.Connection, CGlobal.Session.SSHClient);
                    portsWindow.Owner = this;
                    portsWindow.ShowDialog();
                }
                else
                {
                    COMPortsWindow portsWindow = new COMPortsWindow(CGlobal.Session.SSHClient);
                    portsWindow.Owner = this;
                    portsWindow.ShowDialog();
                }
            }
            catch
            {
            }
        }

        public CConfig Config
        {
            get
            {
                return CGlobal.Config;
            }
        }

        private void FirewallList_Handler(object sender, RoutedEventArgs e)
        {
            //Вызов окна настройки Firewall
            FirewallWindow fireWindow = new FirewallWindow(CGlobal.Session.SSHClient, true);
            fireWindow.Owner = this;
            fireWindow.ShowDialog();
        }

        private void TreeViewMouseLeftButtonUp_Handler(object sender, MouseButtonEventArgs e)
        {
            //Все панели сначала прячутся
            this.tagsListView.Visibility = Visibility.Hidden;
            this.moduleGrid.Visibility = Visibility.Hidden;
            this.modulesInfoTabControl.Visibility = Visibility.Hidden;

            this.groupsTreeView.ContextMenu = null;

            lock (CGlobal.Config.VisibleParamsList)
                CGlobal.Config.VisibleParamsList.Clear();
            lock (CGlobal.Config.ModulesParamsList)
                CGlobal.Config.ModulesParamsList.Clear();

            CBaseGroup group = this.groupsTreeView.SelectedItem as CBaseGroup;

            //Группа параметров контроллера
            if (group is CParamsGroup)
            {
                this.SelectedModule = null;
                this.tagsListView.Visibility = Visibility.Visible;
                //Выводится информация о параметрах контроллера
                lock (CGlobal.Config.VisibleParamsList)
                    CGlobal.Config.UpdateSelectedParamsList(group);
                return;
            }

            //Список модулей
            if (group is CModulesList)
            {
                this.SelectedModule = null;
                if (CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType < GroupTypeEnum.Spectator)
                {
                    this.modulesInfoTabControl.Visibility = Visibility.Visible;
                    this.groupsTreeView.ContextMenu = this.modulesContextMenu;
                    selectedGroup = null;
                }
                return;
            }

            //Модуль
            if (group is CModuleGroup)
            {
                UpdateModuleGroup((CModuleGroup)group);
                return;
            }
        }
        private void UpdateModuleGroup(CModuleGroup group)
        {
            //Чтобы не обновлять каждый раз при нажатии
            if (selectedGroup != null &&  selectedGroup.Name == group.Name && selectedGroup.Module.NameKey == group.Module.NameKey)
                return;

            selectedGroup = group;
            this.SelectedModule = group.Module;
            this.moduleGrid.Visibility = Visibility.Visible;
            if (group is CModule)
            {
                if (CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType < GroupTypeEnum.Spectator)
                {
                    this.groupsTreeView.ContextMenu = this.moduleContextMenu;
                }
                CModule module = group as CModule;
                if (module.Active)
                {
                    this.updateDataFromModule(module.NodeID);
                }
            }
            else
            {
                int? nodeId = null;
                if (group.AllParams.Count() == 0)
                {
                    nodeId = group.Module.NodeID;
                }
                else
                {
                    UpdateScreenParamsList(group.AllParams);
                }
                //Не выбрали целый модуль, а только подгруппу
                this.updateDataFromModule(nodeId);
            }

            //Выводится информация о параметрах контроллера
            lock (CGlobal.Config.ModulesParamsList)
                CGlobal.Config.UpdateModulesParamsList(group);
            return;
        }
        private void NotificationEventHandler(object sender, NpgsqlNotificationEventArgs e)
        {
            this.sqlNotification = e.Channel;
            this.sqlNotificationParam = e.Payload;

            this.sqlMessages.Add(e);
        }

        public CModule SelectedModule
        {
            get
            {
                return this.selectedModule;
            }
            set
            {
                this.selectedModule = value;
                this.OnPropertyChanged("SelectedModule");
            }
        }

        public CModulesList ModulesList
        {
            get
            {
                return this.modulesList;
            }
            set
            {
                this.modulesList = CGlobal.Config.Modules;
                this.OnPropertyChanged("ModulesList");
            }
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void saveModuleSettings(CModule module)
        {
            CSession session = new CSession();
            NpgsqlConnection connection = session.CreateSQLConnection(CGlobal.DBUser,
                CGlobal.DBPassword, false, true);
            try
            {
                //Сохранение информации о модуле в энергонезависимой памяти
                String sql = String.Format("select save_module_settings({0},{1},{2},'{3}','{4}')", module.NodeID, module.Type, module.Revision, module.SoftVersion,module.GenerateConfig().ToLower());
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();

                module.Persistent = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_saveModuleSettings"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохранение конфигурации модуля в базе контроллера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveModuleSettingsMenuItem_Handler(object sender, RoutedEventArgs e)
        {
            //Сохранение настроек модуля в базе
            if (this.selectedModule == null)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Save module '{0}' settings", selectedModule));

            this.saveModuleSettings(this.selectedModule);
        }

        private void RemoveModuleSettingsMenuItem_Handler(object sender, RoutedEventArgs e)
        {
            //Удаление настроек модуля из базы
            if (this.selectedModule == null)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Remove module '{0}' settings", selectedModule));

            try
            {
                string sql = $"select remove_module_settings({selectedModule.NodeID});" +
                        $"notify remove_module_settings_peer, '{selectedModule.NodeID}'";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd.ExecuteNonQuery();

                this.selectedModule.Persistent = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_deleteModuleSettings"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void saveSynchronizationSettings_Handler(object sender, RoutedEventArgs e)
        {
            this.writeSynchronizationSettings();
        }

        private void EditControllersTemplatesClick_Handler(object sender, RoutedEventArgs e)
        {
            ModulesTemplates templatesWindow = new ModulesTemplates();
            //Показ окна управления шаблонами модулей
            try
            {

                templatesWindow.Owner = this;
                templatesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                templatesWindow.Close();
                MessageBox.Show($"Ошибка вызова функции: {ex.Message}", 
                    "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        /// <summary>
        /// Формируется команда на обновление состояния модуля
        /// </summary>
        public bool updateDataFromModule(int? node_id = null, bool firstLoop = true)
        {
            try
            {
                String sql = "";
                if (node_id != null)
                {
                    sql = String.Format("notify module_update_values, '{0}'", node_id);
                }
                else
                {
                    if (this.SelectedModule == null)
                        return false;

                    lock (Config.ScreenParamsList)
                    {
                        if (Config.ScreenParamsList.Count > 0)
                        {
                            String screen_params = "";

                            for (int i = 0; i < Config.ScreenParamsList.Count; ++i)
                            {
                                screen_params += Config.ScreenParamsList[i];
                                if (i != Config.ScreenParamsList.Count - 1)
                                {
                                    screen_params += "|";
                                }
                            }

                            sql = String.Format("notify module_update_values, '{0} [{1}]'", this.SelectedModule.NodeID, screen_params);
                        }
                        else
                        {
                            sql = String.Format("notify module_update_values, '{0}'", this.SelectedModule.NodeID);
                        }
                    }
                }
                if (firstLoop)
                {
                    NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                    cmd.CommandTimeout = 1;
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                    cmd.CommandTimeout = 1;
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void updateModuleVlues_Handler(object sender, RoutedEventArgs e)
        {
            //Обновление текущих значений модуля
            if (!this.updateDataFromModule())
                updateDataFromModule(null, false);
        }
        /// <summary>
        /// Сохранение настроек всех модулей в базе контроллера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private List<int> emptyModules = new List<int>(); 
        private void SaveModules()
        {
            try
            {
                emptyModules.Clear();
                CSession session = new CSession();
                NpgsqlConnection connection = session.CreateSQLConnection(CGlobal.DBUser,
                    CGlobal.DBPassword, false, true);

                foreach (CModule module in this.ModulesList.GroupsList)
                {
                    //В таблице fast_can_params нет параметров
                    if (module.GenerateConfig().ToLower() == "[]")
                    {
                        emptyModules.Add(module.NodeID);
                        updateDataFromModule(module.NodeID);
                    }
                    else
                    {
                        saveModuleSettings(module);
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить настройки модулей." + ex.Message, 
                    CGlobal.GetResourceValue("l_saveModuleSettings"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveModulesSettingsMenuItem_Handler(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() => SaveModules());
            thread.Start();
        }

        /// <summary>
        /// Удаление всех модулей из базы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveModulesSettingsMenuItem_Handler(object sender, RoutedEventArgs e)
        {
            CSession session = new CSession();
            NpgsqlConnection connection = session.CreateSQLConnection(CGlobal.DBUser,
                CGlobal.DBPassword, false, true);

            CGlobal.Handler.UserLog(2, string.Format("Remove all modules"));

            if (connection == null)
                return;

            try
            {
                foreach (CModule module in this.ModulesList.GroupsList)
                {
                    if (!module.Persistent)
                        continue;

                    string sql = $"select remove_module_settings({module.NodeID});" +
                        $"notify remove_module_settings_peer, '{module.NodeID}'";
                    NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                    cmd.ExecuteNonQuery();
                    module.Persistent = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_deleteModuleSettings"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void moduleUpdatePeriodTextBox_PreviewInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !CAuxil.CheckStringForInt(e.Text, true);
        }

        private void SecurityClick_Handler(object sender, RoutedEventArgs e)
        {
            try
            {
                UsersWindow usersWindow = new UsersWindow(CGlobal.Session.Connection);
                usersWindow.Owner = this;
                usersWindow.ShowDialog();
            }
            catch
            {

            }
        }
        private void RebootAbak_Handler(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(CGlobal.GetResourceValue("l_rebootAbakQuestion"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Reboot controller"));

            CGlobal.Handler.UserLogout();
            RestartOrRebootAbak("reboot");
        }
        private void SetCMD(string cmd)
        {
            try
            {
                string[] can1 = { "", "" }; ;
                if (CGlobal.CurrState.PLCVersionInfo == 3)
                {
                    can1[0] += "ifconfig can1 down;";
                    can1[1] += "ifconfig can1 up;";
                }
                string command = $"systemctl stop codesyscontrol;ifconfig eth0 down;ifconfig can0 down;{can1[0]}{cmd};";
                command += $"sleep 5;ifconfig eth0 up;ifconfig can0 up;{can1[1]}";
                Stream stream = CAuxil.StringToStream(command);
                CAuxil.ExecuteSingleSSHCommand("install -m 755 /dev/null /tmp/restart.sh");
                CGlobal.Session.SSHClient.WriteFile("/tmp/restart.sh", stream);
                CAuxil.ExecuteSingleSSHCommand("/tmp/restart.sh &");
            }
            catch
            {

            }
        }

        private void RestartOrRebootAbak(string cmd)
        {
            Thread thread = new Thread(() => SetCMD(cmd));
            thread.Start();
            Thread.Sleep(3000);
            this.stopConnection();
        }

        private void RestartAbak_Handler(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(CGlobal.GetResourceValue("l_restartAbakMenuItem"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Restart controller services"));

            CAuxil.ExecuteSingleSSHCommand("systemctl restart proxy");
            CGlobal.Handler.UserLogout();
            RestartOrRebootAbak("systemctl restart abak_power");
        }

        private void ListOfUpdatedModulesClick_Handler(object sender, RoutedEventArgs e)
        {
            if (BlockList)
            {
                MessageBox.Show($"Один из модулей ещё не завершил обновление!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ListOfUpdateModules UpdatedWindow = new ListOfUpdateModules(CGlobal.Session.Connection);
            UpdatedWindow.Owner = this;
            UpdatedWindow.ShowDialog();
        }

        private void UpdateControllerClick_Handler(object sender, RoutedEventArgs e)
        {
            //Вызов окна обновления и восстановления ПО контроллера
            UpdateWindow updateWindow = new UpdateWindow(CGlobal.Session);
            try
            {
                updateWindow.Owner = this;
                updateWindow.ShowDialog();
            }
            catch
            {

            }
            if (updateWindow.IsFinished != true)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Update controller software"));

            CGlobal.Handler.UserLogout();
            Thread.Sleep(2000);
            RestartOrRebootAbak("reboot");

            MessageBox.Show(CGlobal.GetResourceValue("l_updateSuccess") + "Пожалуйста, дождитесь перезапуска контроллера.", 
                this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LicensesClick_Handler(object sender, RoutedEventArgs e)
        {
            try
            {
                LicensesWindow licensesWindow = new LicensesWindow(CGlobal.Session.Connection, CGlobal.Session.SSHClient);
                licensesWindow.Owner = this;
                licensesWindow.ShowDialog();
            }
            catch
            {

            }
        }

        private void ShowHelpFile_Handler(object sender, RoutedEventArgs e)
        {
            //Показывает файл со справкой
            System.Windows.Forms.Help.ShowHelp(null, "AbakConfigurator.chm");
        }

        private void showJournalsWindow_Handler(object sender, RoutedEventArgs e)
        {
            CGlobal.Handler.UserLog(2, string.Format("Check Event Protocol"));

            //Показывает окно с журналами и протоколами
            JournalsWindow journals = new JournalsWindow(CGlobal.Session.Connection);
            journals.ShowDialog();
        }

        private void showSequenceOfEventsWindow_Handler(object sender, RoutedEventArgs e)
        {
            CGlobal.Handler.UserLog(2, string.Format("Check Sequence of events"));

            SequenceOfEvents soe = new SequenceOfEvents();
            soe.ShowDialog();
        }

        private void CheckBackSlash(ref string name)
        {
            if (!name.Contains("/"))
                return;

            int index = name.IndexOf('/');
            string savedContent = name.Remove(0, index);
            name = name.Replace(savedContent, $"\\{savedContent}");
        }

        //Сброс настроек в CFG файле
        private void ResetFile()
        {
            CGlobal.Handler.UserLog(2, string.Format("Reset Codesys configuration"));

            Stream stream = CGlobal.Session.SSHClient.ReadFile("/etc/CODESYSControl.cfg");
            if (stream == null)
                stream = new MemoryStream();
            CIniFile ini = new CIniFile(stream);
            ini.NewLine = "\n";
            ini.ClearSection("cmpsrv");
            ini.AddKey("cmpsrv", "service.waittime", "2000");
            ini.RemoveKey("systarget", "nodenameunicode");
            ini.WriteValue("cmpredundancy", "plcident", "1");
            ini.WriteValue("cmpredundancy", "standbywaittime", "100");
            ini.WriteValue("cmpredundancy", "bootupwaittime", "5000");
            ini.WriteValue("cmpredundancy", "tcpwaittime", "2000");
            ini.WriteValue("cmpredundancy", "locktimeout", "50");
            ini.WriteValue("cmpredundancy", "bootproject", "Application");
            ini.WriteValue("cmpredundancy", "redundancytaskname", "MainTask");
            ini.WriteValue("cmpredundancy", "datawaittime", "100");
            ini.WriteValue("cmpredundancy", "datasyncalways", "0");
            ini.WriteValue("cmpredundancy", "debugmessages", "0");
            ini.WriteValue("cmpredundancy", "debugmessagestasktime", "0");
            ini.WriteValue("cmpredundancyconnectionip", "link1.ipaddresslocal", "0.0.0.0");
            ini.WriteValue("cmpredundancyconnectionip", "link1.ipaddresspeer", "0.0.0.0");
            ini.WriteValue("cmpredundancyconnectionip", "link1.port", "1205");
            ini.WriteValue("cmpredundancyconnectionip", "link2.ipaddresslocal", "0.0.0.0");
            ini.WriteValue("cmpredundancyconnectionip", "link2.ipaddresspeer", "0.0.0.0");
            ini.WriteValue("cmpredundancyconnectionip", "link2.port", "1206");
            CParam param = CGlobal.Config.FixedParamsList.Find(x => x.Tagname == "SERNUM") as CParam;
            if (!ini.IsKeyExists("systarget", "nodename"))
                ini.AddKey("systarget", "nodename", "ABAKPLC" + param.Value.ToString());
            else
                ini.WriteValue("systarget", "nodename", "ABAKPLC" + param.Value.ToString());
            ini.ExtraSpace = false;
            CGlobal.Session.SSHClient.WriteFile("/etc/CODESYSControl.cfg", ini.GetStream());
        }

        //Манипуляции с файлами для удаления Codesys
        private void DeleteCodesys()
        {
            CGlobal.Handler.UserLog(2, string.Format("Delete Codesys project"));

            bool isRunSwitchOn = CAuxil.IsRunSwitchOn();
            if (isRunSwitchOn)
            {
                CAuxil.SetTagValue("true", "CDS_FORCED");
            }

            Thread.Sleep(1000);
            ResetFile();
            //Удаление всех файлов (файл с паролями не удаляются нормально, только полная очистка каталога)
            CGlobal.Session.SSHClient.ExecuteCommand("find /var/opt/codesys -maxdepth 1 -type f -delete");
            //Копируем файл для ОРС 
            CGlobal.Session.SSHClient.ExecuteCommand("cp -r /opt/backup/IMAGES/PLC3/assembly/EmbeddedProfile_StandardNodes.xml /var/opt/codesys");
            //Подмена проекта на шаблонный
            CGlobal.Session.SSHClient.ExecuteCommand("rm -R /var/opt/codesys/PlcLogic");
            CGlobal.Session.SSHClient.ExecuteCommand("cp -r /opt/backup/PlcLogic  /var/opt/codesys");

            if (isRunSwitchOn)
            {
                CAuxil.SetTagValue("false", "CDS_FORCED");
                Thread.Sleep(1000);
                CAuxil.SetTagValue("true", "CDS_RESTART");
                Thread.Sleep(1000);
            }
        }

        //Кнопка для удаления Codesys
        private void DeleteCodeSysApp_Handler(object sender, RoutedEventArgs e)
        {
            string mes = "Удаление проекта CoDeSys приведёт к следующим последствиям:\n - Сброс пароля " +
               "\n - Сброс настроек резервирования \n - Потеря исходного кода проекта \n Вы уверены, что хотите продолжить?";
            if (MessageBox.Show(mes, "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            DeleteCodesys();
            MessageBox.Show(CGlobal.GetResourceValue("l_deleteCodeSysAppSuccess"), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrepareMicroSDForImage_Handler(object sender, RoutedEventArgs e)
        {
            //Алгоритм подготовки Micro SD карты для снятия образа системы контроллера

            PrepareSDWindow sDWindow = new PrepareSDWindow();
            sDWindow.Owner = this;

            if (sDWindow.ShowDialog() != true)
                return;

            String disk = sDWindow.GetDiskName;
            if (disk == "")
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_prepareSDSelectDrive"), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                //Подготовка скрипта
                List<String> makeimage_script = new List<String>();
                makeimage_script.Add("#!/bin/bash");
                makeimage_script.Add("LOG_FILE=/tmp/mountsd.log");
                makeimage_script.Add("export TERM=xterm");
                makeimage_script.Add("umount -l /dev/mmcblk0p1");
                makeimage_script.Add("echo \"Starting create image script\" >> $LOG_FILE");
                makeimage_script.Add("if ! /opt/scripts/tools/eMMC/beaglebone-black-make-microSD-flasher-from-eMMC.sh 1>/tmp/mmc.log");
                makeimage_script.Add("then");
                makeimage_script.Add("umount /tmp/rootfs");
                makeimage_script.Add("fi");

                //Преобразование в файл
                String script_file = AppDomain.CurrentDomain.BaseDirectory + "run";
                FileStream fs = new FileStream(script_file, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.NewLine = "\n";
                foreach (String s in makeimage_script)
                    sw.WriteLine(s);
                sw.Flush();
                fs.Close();

                ZipFile zip = new ZipFile();
                zip.Password = "matrix";
                zip.AddFile(script_file, "");
                zip.Save(disk + "run");

                File.Delete(script_file);

                MessageBox.Show(CGlobal.GetResourceValue("l_prepareSDPrepareDriveSuccess"), this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_prepareSDPrepareDriveError"), ex.Message);
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string UserDir
        {
            get { return userDir; }
            set { userDir = value; }
        }

        private void SaveLogInController_Handler(object sender, RoutedEventArgs e)
        {
            CSSHClient sshClient = new CSSHClient(CSettings.GetSettings().ConnectIP, CSettings.GetSettings().ConnectSSHport, CGlobal.Session.SshUser, CGlobal.Session.SshPassword);
            String UserDir = CAuxil.SelectFileToSaveOrOpen(true, false, true);
            if (String.Equals(UserDir, ""))
            {
                return;
            }
            UploadZipLog upZip = new UploadZipLog(sshClient, UserDir);
            upZip.Owner = this;
            upZip.ShowDialog();

            CGlobal.Handler.UserLog(2, string.Format("Save controller logs"));
        }

        private void RedundancySettings_Handler(object sender, RoutedEventArgs e)
        {
            try
            {
                RedundancyWindow redundancyWindow = new RedundancyWindow(CGlobal.Session.Connection,
                    CGlobal.Session.SSHClient);
                redundancyWindow.Owner = this;
                redundancyWindow.ShowDialog();
            }
            catch
            {

            }
        }

        private void DeleteLogsInController_Handler(object sender, RoutedEventArgs e)
        {
            string question = "Вы уверены, что хотите очистить логи контроллера?";
            string title = "Удаление логов";
            if (MessageBox.Show(question, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            const string sysLogsPath = "/var/log/";
            const string abakLogsPath = "/var/log/abak/";
            const string postgresqlLogsPath = "/var/log/postgresql/";

            CGlobal.Session.SSHClient.ExecuteCommand($"rm {sysLogsPath}*");
            CGlobal.Session.SSHClient.ExecuteCommand($"rm {abakLogsPath}*");
            CGlobal.Session.SSHClient.ExecuteCommand($"rm {postgresqlLogsPath}*");

            CGlobal.Handler.UserLog(2, string.Format("Delete controller logs"));
        }
        /// <summary>
        /// Функция меняет состояние WDT
        /// </summary>
        /// <param name="newState">
        /// 1 - on
        /// 2 - off
        /// </param>
        private void ChangeWdtState(int newState)
        {
            if (CGlobal.CurrState.AssemblyInt < 550)
            {
                if (newState == 1)
                    CGlobal.Session.SSHClient.ExecuteCommand("/opt/abak/A:/assembly/enable-wdt");
                else
                    CGlobal.Session.SSHClient.ExecuteCommand("/opt/abak/A:/assembly/disable-wdt");
            }
            else
            {
                Dictionary<int, string> actions = new Dictionary<int, string>();
                actions.Add(1, "NOTIFY watchdog_state_changed, 'true'");
                actions.Add(2, "NOTIFY watchdog_state_changed, 'false'");
                NpgsqlCommand cmd = new NpgsqlCommand(actions[newState], CGlobal.Session.Connection);
                cmd.ExecuteNonQuery();
            }
        }
        private void WDTClick_Handler(object sender, RoutedEventArgs e)
        {
            bool compatible_vers = (CGlobal.CurrState.AssemblyHi == 3 && CGlobal.CurrState.AssemblyLo > 3) || CGlobal.CurrState.AssemblyHi == 5;

            if (!compatible_vers)
            {
                MessageBox.Show(this, CGlobal.GetResourceValue("l_WDTSupport"), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CustomMsgBox customMsgBox = new CustomMsgBox("Измение состояние сторожевого таймера", "Выберите действие",
            "Включить", "Отключить", "Сторожевой таймер будет включён", "Сторожевой таймер будет отключён");
            customMsgBox.Owner = this;
            customMsgBox.ShowDialog();

            if (customMsgBox.ButtonEvent == 0 || customMsgBox.ButtonEvent == 3)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Change Watchdog settings"));
            ChangeWdtState(customMsgBox.ButtonEvent);
        }
        private void CodesysWdtClick_Handler(object sender, RoutedEventArgs e)
        {
            CustomMsgBox customMsgBox = new CustomMsgBox("Измение состояние сторожевого таймера Codesys", "Выберите действие",
            "Включить", "Отключить", "Сторожевой таймер Codesys будет включён", "Сторожевой таймер Codesys будет отключён");
            customMsgBox.Owner = this;
            customMsgBox.ShowDialog();

            if (customMsgBox.ButtonEvent == 0 || customMsgBox.ButtonEvent == 3)
                return;

            Dictionary<int, string> actions = new Dictionary<int, string>();
            actions.Add(1, "true");
            actions.Add(2, "false");
            CAuxil.SetTagValue($"{actions[customMsgBox.ButtonEvent]}", "CDS_WATHCDOG_ENABLE");
        }
        private void UpdateModulesSoftClick_Handler(object sender, RoutedEventArgs e)
        {
            //Вызов окна обновления ПО модулей
            UploadImageWindow updateWindow = new UploadImageWindow();
            updateWindow.Owner = this;
            updateWindow.ShowDialog();
            if (updateWindow.IsChanged)
            {
                CGlobal.Config.CImagesList.CopyList(updateWindow.GetImages());
                CGlobal.Config.SetModulesImagesData();
            }
        }

        private void modulesDataGridMouseDown_Handler(object sender, MouseButtonEventArgs e)
        {
            //Выбрали элемент в списке модулей
            this.selectedModule = this.modulesDataGrid.SelectedItem as CModule;
        }
        private bool IsParamEnabled()
        {
            for (int i = 0; i < CGlobal.Config.ControllerParams.AllParams.Count; ++i)
            {
                CBaseParam param = CGlobal.Config.ControllerParams.AllParams[i];
                //if (param.PLC31Only)
                //{
                //    return CGlobal.CurrState.PLCVersionInfo == 3.1f;
                //}

                //if (param.PLC3Only)
                //{
                //    return CGlobal.CurrState.PLCVersionInfo > 3;
                //}
            }

            return true;
        }
        private bool IsRunSwitchOff()
        {
            try
            {
                CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == "RUN_SWITCH_DIAG") as CParam;
                bool isrunstop = param.ValueString == "False" || param.ValueString == "false";
                return isrunstop;
            }
            catch
            {
                return false;
            }
        }

        private bool HeartbeatCheck()
        {
            //Принудительно отключаем сердцебиение
            ModulesList.EnableHeartBeat = false;
            //Отключаем чтение данных с модулей 
            if (EnableModuleUpdateTimer)
            {
                EnableModuleUpdateTimer = false;
                this.writeSynchronizationSettings();
            }

            //Нужно опередилть отключали ли тумблер Codesys
            if (IsRunSwitchOff())
                return true;

            return false;
        }



        private bool CanceledModule(ushort node_id)
        {
            if (!CanceledModules.Contains(node_id))
                return false;

            Dictionary<int, string> sql_update = new Dictionary<int, string>();
            sql_update.Add(1, "update update_info set state = 1, new_softver ='{0}', com_part ='Canceled by user', " +
                "channel_part ='Canceled by user' where id ='{1}'");
            sql_update.Add(2, "update update_info set state = 1, new_softver ='{0}', channel_part ='Canceled by user' where id ='{1}'");
            string sql = String.Format(sql_update[update_state], "", node_id);
            NpgsqlCommand cmd_update = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd_update.ExecuteNonQuery();

            return true;
        }

        private void ChooseAllModules()
        {
            foreach (CModule module in Config.Modules.GroupsList)
            {
                if (module.Module_Type == "Неизвестное устройство")
                    continue;
                if (MainTick.IsChecked == true)
                    ChoosedModules.Add(module);
                else
                    ChoosedModules.Remove(module);

                module.IsChecked = MainTick.IsChecked == true;
            }
            //Ну вдург все модули неизвестные
            if (ChoosedModules.Count() == 0)
                MainTick.IsChecked = false;
        }

        public void ChooseAllModulesButton(object sender, RoutedEventArgs e)
        {
            ChooseAllModules();
        }

        public void IsChanged(object sender, RoutedEventArgs e)
        {
            CModule module = modulesDataGrid.SelectedItem as CModule;
            if (module.Module_Type == "Неизвестное устройство")
            {
                module.IsChecked = false;
                MessageBox.Show(@"Модули, помеченные как ""Неизвестное устройство"", обновлять нельзя!",
                    "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Модуль выбрали, добавляем
            if (module.IsChecked == true)
                ChoosedModules.Add(module);
            else
                ChoosedModules.Remove(module); //Был выбран, но галочку убрали

            if (ChoosedModules.Count != 0 && ChoosedModules.Count != this.ModulesList.GroupsList.Count())
            {
                MainTick.IsChecked = null;
                return;
            }

            if (ChoosedModules.Count == 0)
                MainTick.IsChecked = false;
            else
                MainTick.IsChecked = true;
        }

        private void CheckUpdateValidation()
        {
            if(CGlobal.Handler.Auth.Account == "root")
            {
                return;
            }

            List<string> emptyModules = new List<string>(); 
            for(int i = 0; i < ChoosedModules.Count; i++)
            {
                CModule module = ChoosedModules[i];
                if (!module.IsModuleFlashed())
                {
                    ChoosedModules.Remove(module);
                    emptyModules.Add(module.NodeID.ToString());
                }
            }

            if(ChoosedModules.Count == 0)
            {
                throw new Exception("Недостаточно прав для обновления модулей без прошивки! Обновление отменено!");
            }
            if(emptyModules.Count() != 0)
            {
                string msg = $"Из операции обновления исключены следующие модули: ";
                foreach(string module in emptyModules)
                {
                    msg += $" '{module}';";
                }

                MessageBox.Show($"Недостаточно прав для обновления модулей без прошивки! {msg}", "Ошибка!", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearLists()
        {
            CanceledModules.Clear();
            CommImagesList.Clear();
            ChannelImagesList.Clear();
        }
        public void UpdateAllModules_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsChoosed())
                    return;

                CheckUpdateValidation();
                HeartbeatCheck();
                bool isRunSwitchOn = CAuxil.IsRunSwitchOn();
                ClearLists();

                update_state = 0;

                CustomMsgBox Box = new CustomMsgBox("Обновление модулей", "Выберите тип обновления",
                    "Автоматическое", "Ручное", "Автоматическое обновление до новейших версий ПО", "Ручной выбор из всех доступных версий ПО");
                Box.Owner = this;
                if (Box.ShowDialog() != true)
                    return;

                NpgsqlCommand cmd_delete = new NpgsqlCommand("delete from update_info", CGlobal.Session.Connection);
                cmd_delete.ExecuteNonQuery();

                bool result = Box.ButtonEvent == 1 ? FindTheNewestImages() : FindImages(ChoosedModules);
                //Если для модулей вообще нет обновлений и это ручной режим
                if ((result.ToString() == "False") || (Box.ButtonEvent == 2 && CommImagesList.Count() == 0))
                    return;

                if (CommImagesList.Count() == 0 && ChannelImagesList.Count() == 0)
                {
                    AfterWaitingWindow();
                    return;
                }
                amountBeforeUpdate = this.ModulesList.GroupsList.Count();
                if (isRunSwitchOn)
                {
                    //Не отключали, значит будем принудительно выключать сами
                    if (MessageBox.Show("Управление технологическим процессом будет приостановлено. Продолжить?",
                         "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    cdsForceFlag = true;
                    CAuxil.SetTagValue(true.ToString(), "CDS_FORCED");
                    Thread.Sleep(1000);
                }

                //Чтобы модули ушли в boot режим
                foreach (CModule module in this.ModulesList.GroupsList)
                {
                    string name = module.NameKey.ToLower();
                    if (name.Contains("boot"))
                        continue;

                    CGlobal.CurrState.Update_On = true;
                    CGlobal.CurrState.ModulesAmount = this.ModulesList.GroupsList.Count();
                    //Если хоть один не в этом режим, то надо слать команду в шину
                    CGlobal.Session.SSHClient.ExecuteCommand("cansend can0 000#81FF");
                    CGlobal.Session.SSHClient.ExecuteCommand("cansend can1 000#81FF");
                    Thread.Sleep(1000);
                    WaitingWindow = new WaitingWindow();
                    WaitingWindow.Owner = this;
                    WaitingWindow.Show();
                    return;
                }
                CGlobal.CurrState.Update_On = true;
                CGlobal.CurrState.ModulesAmount = this.ModulesList.GroupsList.Count();
                AfterWaitingWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось выполнить обновление по причине: {ex.InnerException.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfterWaitingWindow()
        {
            UpdateStart(CommImagesList);

            if (CGlobal.CurrState.Update_On = amountofrows != 0)
                return;

            ListOfUpdateModules UpdatedWindow = new ListOfUpdateModules(CGlobal.Session.Connection);
            UpdatedWindow.Owner = this;
            UpdatedWindow.ShowDialog();
        }
        private void ChangeCpuState(int button)
        {
            string nonModule = "systemctl disable canmanager;";
            string dedault = "systemctl enable canmanager;";
            if (CGlobal.CurrState.AssemblyInt >= 550)
            {
                CAuxil.SetTagValue((button == 1).ToString(), "NONMODULES_STATE");
            }
            else
            {
                const string assembly = "/opt/abak/A:/assembly/";
                const string singleCpu = "/opt/backup/SingleCPU/";
                string backup = $"/opt/backup/IMAGES/PLC{Convert.ToUInt16(CGlobal.CurrState.PLCVersionInfo)}/";

                nonModule += $"systemctl stop abak_power;" +
                    $"cp -r {singleCpu}abakbbb_io {assembly};" +
                    $"cp -r {singleCpu}abak_start {assembly};" +
                    $"cp -r {singleCpu}uEnv.txt /boot/;" +
                    $"sleep 1;";

                dedault += $"systemctl stop abak_power;" +
                    $"cp -r {backup}assembly/abakbbb_io {assembly};" +
                    $"cp -r {backup}assembly/abak_start {assembly};" +
                    $"cp -r {backup}boot/uEnv.txt /boot/;" +
                    $"sleep 1;";

                string sql = "update settings set value='{" +
                             $"\"single_cpu_state\":\"{button == 1}\"" +
                             "}' where name='cpu_state'";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd.ExecuteNonQuery();
            }

            Dictionary<int, string> actions = new Dictionary<int, string>
            {
                { 1, nonModule },
                { 2, dedault }
            };
            CAuxil.ExecuteSingleSSHCommand(actions[button] + ";systemctl enable canmanager") ;
            CGlobal.Handler.UserLogout();
            RestartOrRebootAbak("reboot");
            MessageBox.Show("Операция выполнена!", "Режим работы ПЛК", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void SingleCpuStatusClick_Handler(object sender, RoutedEventArgs e)
        {
            CustomMsgBox customMsgBox = new CustomMsgBox("Режим работы ПЛК", "Выберите режим работы",
                "Безмодульный", "Стандартный", "Работа с модулями и резервирование будут недоступны",
                "Работа с модулями и резервирование будут доступны");
            customMsgBox.Owner = this;
            customMsgBox.ShowDialog();
            if (customMsgBox.ButtonEvent == 0 || customMsgBox.ButtonEvent == 3)
                return;

            const string msg = "Контроллер будет перезагружен. Вы уверены, что хотите продолжить?";
            if (MessageBox.Show(msg, "Изменение режима работы ПЛК!", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            CGlobal.Handler.UserLog(2, string.Format("Change PLC mode"));

            ChangeCpuState(customMsgBox.ButtonEvent);
        }

        private void UpdateStart(Dictionary<int, int> give_me_dictionary)
        {
            try
            {
                update_stopped = modulesupdated = 0;
                update_state += 1;
                amountofrows = give_me_dictionary.Count();

                foreach (ushort key in give_me_dictionary.Keys)
                {
                    if (CanceledModule(key) && update_state == 2)
                    {
                        amountofrows -= 1;
                        continue;
                    }
                    FillTheQueue(key, give_me_dictionary[key]);
                }

                if (update_state == 1 && amountofrows == 0)
                    UpdateStart(ChannelImagesList);

                if (update_state == 2 && amountofrows == 0)
                    UpdatesFinished();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось выполнить обновление по причине: {ex.Message}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsChoosed()
        {
            if (ChoosedModules.Count != 0)
                return true;

            MessageBox.Show("Выберите модуль!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        /// <summary>
        ///Для всех модулей
        /// </summary>
        /// <param name="image"></param>
        private void FindPlc3Images(CModule module, ImageSelectionWindow updateWindow)
        {
            updateWindow.TheNewestPlc3();

            if (updateWindow.ModuleComImage != null)
            {
                if (CommImagesList.ContainsKey(module.NodeID))
                {
                    throw new Exception($"Модуль с номером {module.NodeID} уже добавлен");
                }
                CommImagesList.Add(module.NodeID, updateWindow.ModuleComImage.ID);
            }

            if (updateWindow.ModuleChannelImage != null)
            {
                if (ChannelImagesList.ContainsKey(module.NodeID))
                {
                    throw new Exception($"Модуль с номером {module.NodeID} уже добавлен");
                }
                ChannelImagesList.Add(module.NodeID, updateWindow.ModuleChannelImage.ID);
            }
        }

        private void FindPlc2Images(CModule module, ImageSelectionWindow updateWindow)
        {
            updateWindow.TheNewestPlc2();

            if (updateWindow.PLC2Image != null)
                CommImagesList.Add(module.NodeID, updateWindow.PLC2Image.ID);
        }

        //Функция для поиска новейших прошивок
        private bool FindTheNewestImages()
        {
            Dictionary<string, Delegate> ModuleType = new Dictionary<string, Delegate>();
            ModuleType["K3"] = new Action<CModule, ImageSelectionWindow>(FindPlc3Images);
            ModuleType["K2"] = new Action<CModule, ImageSelectionWindow>(FindPlc2Images);
            ObservableCollection<CModule> emptyModules = new ObservableCollection<CModule>();
            foreach (var module in ChoosedModules)
            {
                //Только по имени можно определить тип модуля
                string[] str = module.Module_Name.Split('.');
                if (!ModuleType.ContainsKey(str[0]))
                {
                    continue;
                }
                if(!module.IsModuleFlashed())
                {
                    emptyModules.Add(module);
                    continue;
                }
                ImageSelectionWindow updateWindow = new ImageSelectionWindow(module);
                ModuleType[str[0]].DynamicInvoke(module, updateWindow);
            }
            FindImages(emptyModules);
            return true;
        }

        //Функция для поиска всех возможных прошивок
        private bool FindImages(ObservableCollection<CModule> collection)
        {
            //Тут храним тип и железо модуля
            List<Tuple<string, string>> TypeHard = new List<Tuple<string, string>>();
            //Тут хранится сохраненная версия образа ком-платы
            int saved_com = 0;
            //Тут хранится сохраненная версия образа канальной платы
            int saved_channel = 0;
            //Флаг наличия для модуля образов
            bool iscompatible = true;
            foreach (var module in collection)
            {
                string[] module_plc_version = module.Module_Name.Split('.');

                Tuple<string, string> type_hard = new Tuple<string, string>(module.HardVersion, module.Module_Type);
                //Если тип модуля с этой версией железа уже есть, то ему записывается та же прошивка и окно не вызывается
                if (TypeHard.Contains(type_hard))
                {
                    //Модуль есть, а образов нет
                    if (!iscompatible)
                        continue;

                    if (!CommImagesList.ContainsKey(module.NodeID))
                        CommImagesList.Add(module.NodeID, saved_com);

                    if (!ChannelImagesList.ContainsKey(module.NodeID) && !module_plc_version.Contains("K2"))
                        ChannelImagesList.Add(module.NodeID, saved_channel);

                    continue;
                }

                ImageSelectionWindow updateWindow = new ImageSelectionWindow(module, module_plc_version[0]);
                if (updateWindow.Error)
                    continue;

                if (!updateWindow.IsImages)
                {
                    iscompatible = false;
                    MessageBox.Show("Для модулей типа: " + "'" + module.Module_Name + "'" + " образы не найдены", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Stop);
                    continue;
                }

                //Модуля нет, значит запоминаем
                TypeHard.Add(type_hard);

                updateWindow.Owner = this;
                if (updateWindow.ShowDialog() != true)
                    return false;

                try
                {
                    saved_com = updateWindow.ModuleComImage.ID;
                    CommImagesList.Add(module.NodeID, saved_com);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "Не удалось добавить прошивку коммукационной платы для модуля " + module.NodeID, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                try
                {
                    if (updateWindow.ModuleChannelImage == null)
                        continue;

                    saved_channel = updateWindow.ModuleChannelImage.ID;
                    ChannelImagesList.Add(module.NodeID, saved_channel);
                }
                catch
                {

                }
            }

            return true;
        }

        private void IsDictionaryEmpty()
        {
            bool isempty = UpdateActions.Count == 0 && UpdateStateActions.Count == 0;
            if (!isempty)
                return;

            UpdateActions.Clear();
            UpdateStateActions.Clear();

            UpdateActions["Завершено"] = new Action<CModule>(UpdateFinished);
            UpdateActions["Отменено пользователем"] = new Action<CModule>(UpdateCanceled);
            UpdateActions["Update error"] = new Action<CModule>(UpdateError);

            UpdateStateActions[1] = new Action(StartChannelUpdate);
            UpdateStateActions[2] = new Action(UpdatesFinished);
        }

        /// <summary>
        /// Отменяет процедуру обновления ПО модулей
        /// </summary>
        /// <param name="module"></param>

        private void stopModuleUpdate(CModule module = null)
        {
            save_update_state = update_state;
            BlockList = true;
            String sql;
            if (module == null)
            {
                sql = "select update_module_soft_stop(0)";
                ChannelImagesList.Clear();
                CommImagesList.Clear();
                ChooseAllModules();
            }
            else
            {
                SQLProgress(module.NodeID);
                sql = String.Format("select update_module_soft_stop({0})", module.NodeID);
                //ChannelImagesList.Remove(module.NodeID);
                //ComImagesList.Remove(module.NodeID);
                CanceledModules.Add(module.NodeID);
                module.IsChecked = false;
            }

            try
            {
                CPostgresAuxil.ExecuteNonQuery(CGlobal.Session.Connection, sql);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageStopUdateError"), ex.Message); //Ошибка инициализации процедуры обновления
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Останов процедуры обновления модуля
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void CancelUpdateModuleSoftClick_Handler(object sender, RoutedEventArgs e)
        {
            if (this.selectedModule == null)
                return;

            String message = String.Format(CGlobal.GetResourceValue("l_messageStopSingleModuleUdateQuestion"), this.selectedModule.NodeID);
            if (MessageBox.Show(message, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            CanceledModules.Add(selectedModule.NodeID);

            this.stopModuleUpdate(this.selectedModule);
        }

        private void CancelUpdateAllModuleSoftClick_Handler(object sender, RoutedEventArgs e)
        {
            String message = CGlobal.GetResourceValue("l_messageStopAllModulesUdateQuestion");
            if (MessageBox.Show(message, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            CGlobal.CurrState.Update_On = false;
            MainTick.IsChecked = false;

            this.stopModuleUpdate();

            UpdatesFinished();
        }

        private void StartUpdateAllModuleSoftClick_Handler(object sender, RoutedEventArgs e)
        {
            modulesInfoTabControl.Focus();
        }

        private void modulesDatagridMouseDoubleClick_Handler(object sender, MouseButtonEventArgs e)
        {
            if (CGlobal.CurrState.Update_On)
                return;

            TreeViewItem FindTviFromObjectRecursive(ItemsControl ic, object o)
            {
                //Search for the object model in first level children (recursively)
                TreeViewItem tvi = ic.ItemContainerGenerator.ContainerFromItem(o) as TreeViewItem;
                if (tvi != null)
                    return tvi;
                //Loop through user object models
                foreach (object i in ic.Items)
                {
                    //Get the TreeViewItem associated with the iterated object model
                    TreeViewItem tvi2 = ic.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                    if (tvi2 != null)
                    {
                        tvi = FindTviFromObjectRecursive(tvi2, o);
                        if (tvi != null)
                            return tvi;
                    }
                }
                return null;
            }

            TreeViewItem tv = this.groupsTreeView.ItemContainerGenerator.ContainerFromItem(this.groupsTreeView.SelectedItem) as TreeViewItem;
            if (tv == null)
                return;

            tv = FindTviFromObjectRecursive(this.groupsTreeView, this.modulesDataGrid.CurrentItem);
            if (tv != null)
            {
                tv.IsSelected = true;
                this.TreeViewMouseLeftButtonUp_Handler(this.groupsTreeView, null);
            }
        }

        private void GradientStop_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {

        }

        private void modulesDataGrid_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        //Функция для отмены закрытия конфигуратора во время обновления
        private void ClosingOff(object sender, CancelEventArgs e)
        {
            if (!CGlobal.CurrState.Update_On)
            {
                if (WindowState == WindowState.Maximized)
                {
                    Properties.Settings.Default.Top = RestoreBounds.Top;
                    Properties.Settings.Default.Left = RestoreBounds.Left;
                    Properties.Settings.Default.Height = RestoreBounds.Height;
                    Properties.Settings.Default.Width = RestoreBounds.Width;
                    Properties.Settings.Default.Maximized = true;
                }
                else
                {
                    Properties.Settings.Default.Top = Top;
                    Properties.Settings.Default.Left = Left;
                    Properties.Settings.Default.Height = Height;
                    Properties.Settings.Default.Width = Width;
                    Properties.Settings.Default.Maximized = false;
                }

                Properties.Settings.Default.Save();

                return;
            }

            e.Cancel = true; //Отменяется закрытие
            MessageBox.Show("Конфигуратор запрещенно закрывать во время обновления", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        //Перезагрузка модуля
        private void ResetModule_Click_1(object sender, RoutedEventArgs e)
        {
            bool cancel = this.selectedModule == null || !CGlobal.CurrState.IsRunning;
            if (cancel)
                return;

            string msg = $"Вы уверены, что хотитие перезагрузить '{selectedModule.Name}'?";
            string title = "Перезагрузка модуля";
            if (MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            string id = this.selectedModule.NodeID.ToString("X");
            string cmd = "";
            if (id.Length == 2)
                cmd = $"81{id}";
            else
                cmd = $"810{id}";

            CAuxil.ExecuteSingleSSHCommand($"cansend can0 000#{cmd}");
            CAuxil.ExecuteSingleSSHCommand($"cansend can1 000#{cmd}");
        }

        //Перезагрузка всех модулей
        private void RebootModulesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string msg = "Вы уверены, что хотитие перезагрузить модули?";
            string title = "Перезагрузка модулей";
            if (MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            CGlobal.Session.SSHClient.ExecuteCommand("cansend can0 000#81FF");
            CGlobal.Session.SSHClient.ExecuteCommand("cansend can1 000#81FF");
        }

        private void CodesysSettings_Click(object sender, RoutedEventArgs e)
        {
            CodesyscontrolWindow codesyscontrolWindow = new CodesyscontrolWindow(CGlobal.Session.SSHClient);
            codesyscontrolWindow.Owner = this;
            codesyscontrolWindow.Show();
        }
        private void RestartCodesys_Handler(object sender, RoutedEventArgs e)
        {
            bool isRunSwitchOn = CAuxil.IsRunSwitchOn();
            if (isRunSwitchOn)
            {
                string msg = "Службы контроллера будут перезапущены. Вы уверены, что хотите продолжить?";
                bool result = MessageBox.Show(msg, "Перезапуск Codesys", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (result)
                {
                    Thread.Sleep(1000);
                    CAuxil.SetTagValue("true", "CDS_RESTART");
                    Thread.Sleep(1000);
                }
            }
            else
            {
                string msg = "Переключатель \"Run/Stop\" находится в положении \"Stop\". Перезапуск Codesys невозможен!";
                MessageBox.Show(msg, "Перезапуск Codesys", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private static Collection<T> FindVisualChild<T>(DependencyObject current) where T : DependencyObject
        {
            if (current == null)
            {
                return null;
            }
            var children = new Collection<T>();
            FindVisualChild(current, children);
            return children;
        }

        private static void FindVisualChild<T>(DependencyObject current, Collection<T> children) where T : DependencyObject
        {
            if (current != null)
            {
                if (current.GetType() == typeof(T))
                { children.Add((T)current); }
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    FindVisualChild(VisualTreeHelper.GetChild(current, i), children);
                }
            }
        }
        private void ListViewScrollChanged_Handler(object sender, ScrollChangedEventArgs args)
        {
            //UpdateScreenParamsList();
        }

        private void ModuleListViewScrollChanged_Handler(object sender, ScrollChangedEventArgs args)
        {
            UpdateScreenParamsList();
        }

        private void UpdateScreenParamsList(List<CBaseParam> groupParams = null)
        {
            if (groupParams == null)
            {
                lock (Config.ScreenParamsList)
                {
                    Config.ScreenParamsList.Clear();

                    Collection<Expander> groups = FindVisualChild<Expander>(modulesTagsListView);
                    ListView listView = modulesTagsListView;
                    if (groups.Count == 0)
                    {
                        groups = FindVisualChild<Expander>(tagsListView);
                        listView = tagsListView;
                    }

                    visibleValues?.Clear();
                    foreach (Expander expander in groups)
                    {
                        ScrollViewer scroll_viewer = getScrollViewer(modulesTagsListView);
                        GeneralTransform expander_position = expander.TransformToAncestor(listView);
                        System.Windows.Point expander_point = expander_position.Transform(new System.Windows.Point(0, 0));
                        bool non_visible = ((expander_point.Y + expander.ActualHeight) < 0) || ((expander_point.Y - expander.ActualHeight) > scroll_viewer.ViewportHeight);

                        if (!non_visible || scroll_viewer.ComputedVerticalScrollBarVisibility != Visibility.Visible)
                        {
                            var newValues = FindVisualChild<ListItemParamControl>(expander);
                            if (visibleValues == null)
                            {
                                visibleValues = newValues;
                            }
                            else
                            {
                                foreach (var item in newValues)
                                {
                                    visibleValues.Add(item);
                                }
                            }
                            Collection<DescriptionParamControl> descValues = FindVisualChild<DescriptionParamControl>(expander);
                            foreach (ListItemParamControl param in visibleValues)
                            {
                                if (!param.IsUpdateBlocked)
                                {
                                    param.OnApplyTemplate();
                                    Config.ScreenParamsList.Add(param.param.Tagname);
                                }
                            }
                            foreach (DescriptionParamControl descParam in descValues)
                            {
                                descParam.OnApplyTemplate();
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (CBaseParam gParam in groupParams)
                {
                    Config.ScreenParamsList.Add(gParam.Tagname);
                }
            }
        }

        private void AccountMenu_Click_Profile(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }
        }

        private void AccountMenu_Click_Password(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null || !CGlobal.Handler.Auth.Authorized)
            {
                return;
            }

            var password_window = new PasswordChangeWindow() { Owner = this };
            password_window.ShowDialog();
        }

        private void AccountMenu_Click_Journal(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            UserJournalWindow window = new UserJournalWindow(true) { Owner = this };
            window.ShowDialog();
        }

        private void AccountMenu_Click_Exit(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            if (CGlobal.Handler.UserLogout(true))
            {
                stopConnection();
            }
        }

        private void ManagementMenu_Click_Users(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            UserManagementWindow window = new UserManagementWindow() { Owner = this };
            window.ShowDialog();
        }

        private void ManagementMenu_Click_Groups(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            GroupManagementWindow window = new GroupManagementWindow() { Owner = this };
            window.ShowDialog();
        }

        private void ManagementMenu_Click_Journal(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            UserJournalWindow window = new UserJournalWindow() { Owner = this };
            window.ShowDialog();
        }

        private void ManagementMenu_Click_Rules(object sender, RoutedEventArgs e)
        {
            if (CGlobal.Handler == null)
            {
                return;
            }

            RuleManagementWindow window = new RuleManagementWindow() { Owner = this };
            window.ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.Height != 0 && Properties.Settings.Default.Width != 0)
            {
                Top = Properties.Settings.Default.Top;
                Left = Properties.Settings.Default.Left;
                Height = Properties.Settings.Default.Height;
                Width = Properties.Settings.Default.Width;
            }

            if (Properties.Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }
        private void DeleteModulesDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CGlobal.CurrState.IsRunning)
            {
                return;
            }
            TruncateDescriptions();
        }
        private void TruncateDescriptions()
        {
            CGlobal.Config.TagsDescriptions.Clear();
            NpgsqlCommand cmd = GetCmdForTagsDescription("", "", "");
            cmd.ExecuteNonQuery();
            var descriptions = CGlobal.Config.ModulesParamsList.Where(x => x.Description != "").ToList();
            descriptions.ForEach(x => x.Description = "");
        }
        private void SaveModulesDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string msg = "";
            if (CGlobal.Config.TagsDescriptions.Count() == 0)
            {
                MessageBox.Show("Список пуст!", "Ошиба!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                string path = "";
                path = CAuxil.CreatePath("Описание тегов", "csv ", "Файлы с описанием тегов(*.csv)|*.csv");
                if (path == "")
                {
                    return;
                }
                SaveDescriptionInCSV(path, out msg);
            }
            catch (Exception ex)
            {
                if (msg != "")
                {
                    msg = $"Ошибка при сохранении файла файла: {ex.Message}";
                }
            }
            if (msg != "")
            {
                MessageBox.Show(msg, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show("Файл сохранён!", "Сохранение файла", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SaveDescriptionInCSV(string path, out string msg)
        {
            msg = "";
            string line = "";
            try
            {
                foreach (CModule module in ModulesList.GroupsList)
                {
                    foreach (CBaseParam param in module.AllParams)
                    {
                        if (!CGlobal.Config.TagsDescriptions.ContainsKey(param.Tagname))
                        {
                            continue;
                        }

                        line += $"{module.Name}, {param.Tagname}, {param.Name}, {CGlobal.Config.TagsDescriptions[param.Tagname][1]}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                msg = $"Ошибка при работе с файлом: {ex.Message}";
            }
            File.WriteAllText(path, line);
        }
        private void UploadModulesDescriptionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string msg = "";
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.CheckFileExists = true;
                if (openDialog.ShowDialog() != true)
                {
                    return;
                }
                UploadModulesDescription(openDialog.FileName, out msg);
            }
            catch (Exception ex)
            {
                if (msg != "")
                {
                    msg = $"Ошибка при открытии файла файла: {ex.Message}";
                }
            }
            if (msg != "")
            {
                MessageBox.Show(msg, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {

                MessageBox.Show("Данные из файла загружены!", "Открытие файла", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        void UploadModulesDescription(string path, out string msg)
        {
            msg = "";
            SortedDictionary<string, string[]> tagsDescriptions = new SortedDictionary<string, string[]>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    string[] splitLine = line.Split(',');
                    if(splitLine.Length != 3) 
                    {
                        msg = $"Ошибка при чтении файла! Строка {line} не соответствует формату";
                        return;
                    }
                    //key - tag, value - tag_localization + description
                    tagsDescriptions.Add(splitLine[0], new string[2] { splitLine[1], splitLine[2] });
                }
            }
            TruncateDescriptions();
            foreach (var data in tagsDescriptions)
            {
                NpgsqlCommand cmd = GetCmdForTagsDescription(data.Key, data.Value[0], data.Value[1]);
                cmd.ExecuteNonQuery();
            }    
            CGlobal.Config.TagsDescriptions = tagsDescriptions;
        }

        private void IECOpen_Click(object sender, RoutedEventArgs e)
        {
            IEC.IECWindow iec = new IEC.IECWindow { Owner = this };
            iec.Show();
        }

        private void GridViewColumnHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            const int min_size = 100;
            GridViewColumnHeader header = sender as GridViewColumnHeader;
            if (header != null && !double.IsNaN(header.Column.Width) && header.Column.ActualWidth < min_size)
            {
                header.Column.Width = min_size;
            }
        }
    }
}
