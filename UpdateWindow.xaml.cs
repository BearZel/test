using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.XPath;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс описывающий архив контроллера
    /// </summary>
    public class CBackupInfo
    {
        //Серийный номер контроллера
        private String serial;
        //Номер сборки
        private String assembly;
        //Описание контроллера
        private String description;
        //Дата создания
        private DateTime createDate;
        //Путь к файлу
        private String path;

        public String Serial
        {
            get
            {
                return this.serial;
            }
            set
            {
                this.serial = value;
            }
        }

        public String Assembly
        {
            get
            {
                return this.assembly;
            }
            set
            {
                this.assembly = value;
            }
        }

        public String Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        public DateTime CreateDate
        {
            get
            {
                return this.createDate;
            }
            set
            {
                this.createDate = value;
            }
        }

        public String Path
        {
            get
            {
                return this.path;
            }
            set
            {
                this.path = value;
            }
        }
    }

    /// <summary>
    /// Логика взаимодействия для UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window, INotifyPropertyChanged
    {
        public bool IsFinished = false;
        private const String BACKUP_FILE = "/tmp/backup.tar.gz";
        private const String UPDATE_FILE = "/update";
        //Нужен перезапуск
        private bool isReboot;

        public bool IsReboot { get => isReboot; set => isReboot = value; }
        public enum BACKUP_OPERATION
        {
            //Нет никакой операции
            NONE,
            //Создание архивной копии ПО и настроек
            CREATE_BACKUP,
            //Восстановление ПО и настроек из бэкапа
            RESTORE_BACKUP,
            //Обновление ПО
            UPDATE_SOFTWARE
        }

        //Указатель на класс с открытой сессией
        private CSession session = null;
        //Таймер обновления данных на экране
        private DispatcherTimer timer = new DispatcherTimer();
        //Название текущей операции
        private String operationName = "";
        //Строка с точками для отображения выполнения операции
        private String statusRunning = "";
        //Номер точки
        private Byte pointNumber = 0;
        //Текущая выполняемая операция
        private BACKUP_OPERATION currentOperation = BACKUP_OPERATION.NONE;
        //XML документ с информацией о контроллере
        private XmlDocument infoDoc = null;
        //Список резервных копий
        private ObservableCollection<CBackupInfo> backupsList = new ObservableCollection<CBackupInfo>();
        //Контекст потока для работы с пользовательским интерфейсом приложения из других потоков
        private SynchronizationContext uiContext;
        //Серийный номер абака
        private String abakAssembly;
        private bool saveNetworkSettings = false;

        public UpdateWindow(CSession session)
        {
            InitializeComponent();

            this.uiContext = SynchronizationContext.Current;

            this.session = session;

            //Инициализация таймера обновляющего экран
            this.timer.IsEnabled = false;
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            this.timer.Tick += new EventHandler(this.timer_Tick);

            this.getAbakAssembly();
            this.loadBackups(null);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.OnPropertyChanged("OperationName");
            this.OnPropertyChanged("StatusRunning");
        }

        /// <summary>
        /// Функция читает номер сборки из контроллера
        /// что бы потом по нему отфильтровать список
        /// </summary>
        void getAbakAssembly()
        {
            String sql = "select value from fast_table where tag='ASSEMBLY'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (!reader.Read())
                {
                    this.abakAssembly = "";
                    return;
                }

                this.abakAssembly = reader["value"] as String;
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Функция подгружает список сделанных резервных копий
        /// </summary>
        private void loadBackups(object state)
        {
            this.backupsList.Clear();
            String backupsDir = String.Format("{0}\\backups", Directory.GetCurrentDirectory());
            if (!Directory.Exists(backupsDir))
                return;

            string[] files = Directory.GetFiles(backupsDir, "*.backup");
            foreach(string file in files)
            {
                try
                {
                    //Подгрузка XML документа описывающего резервную копию
                    XPathDocument doc = new XPathDocument(file);
                    XPathNavigator nav = doc.CreateNavigator();
                    XPathNavigator rootNode = CXML.getRootNode(nav);

                    CBackupInfo backup = new CBackupInfo();
                    backup.Path = file;
                    backup.Serial = CXML.getAttributeValue(rootNode, "SERNUM", "");
                    backup.Assembly = CXML.getAttributeValue(rootNode, "ASSEMBLY", "");
                    backup.Description = CXML.getAttributeValue(rootNode, "INFO_STRING", "");
                    backup.CreateDate = Convert.ToDateTime(CXML.getAttributeValue(rootNode, "dt", ""));

                    this.backupsList.Add(backup);
                }
                catch
                {
                    //Не получилось файл распарсить
                }
            }
        }

        private void showErrorMessage(object state)
        {
            String message = state.ToString();
            MessageBox.Show(this, message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void showInfoMessage(object state)
        {
            AfterRestoreWindow afterRestore = new AfterRestoreWindow();
            afterRestore.Owner = this;
            afterRestore.ShowDialog();

            if (afterRestore.RestartService.IsChecked == true)
                this.session.SSHClient.ExecuteCommand("systemctl restart abak_power");
            else if (afterRestore.RestartController.IsChecked == true)
            {
                try
                {
                    this.session.SSHClient.ExecuteCommand("reboot");
                    
                }
                catch
                {
                    //Здесь исключение возникает, т.к. контроллер отваливается
                }
                this.Close();
            }
        }

        private void startOperation(String name, BACKUP_OPERATION operation)
        {
            this.operationName = name;
            this.pointNumber = 0;
            this.currentOperation = operation;
            this.OnPropertyChanged("OperationName");
            this.OnPropertyChanged("StatusRunning");
            this.OnPropertyChanged("CommandNotRunning");
            this.OnPropertyChanged("CommandRunning");
            this.timer.IsEnabled = true;
        }

        private void stopOperation()
        {
            string sql = "NOTIFY watchdog_state_changed, 'true'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.ExecuteNonQuery();

            this.currentOperation = BACKUP_OPERATION.NONE;
            this.OnPropertyChanged("CommandNotRunning");
            this.OnPropertyChanged("CommandRunning");
            this.timer.IsEnabled = false;
        }

        /// <summary>
        /// Функция вытаскивает из контроллера информацию
        /// </summary>
        private Boolean getControllerInfo()
        {
            try
            {
                this.infoDoc = new XmlDocument();
                XmlDeclaration decl = this.infoDoc.CreateXmlDeclaration("1.0", "utf-8", "");
                XmlElement root = this.infoDoc.DocumentElement;
                this.infoDoc.InsertBefore(decl, root);
                XmlNode rootNode = this.infoDoc.CreateNode("element", "root", "");
                this.infoDoc.AppendChild(rootNode);

                String sql = "select tag, value from fast_table where(tag='SERNUM' or tag='ASSEMBLY' or tag='INFO_STRING')";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, this.session.Connection);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        String tag = reader["tag"] as String;
                        String value = reader["value"] as String;

                        XmlAttribute attr = this.infoDoc.CreateAttribute(tag);
                        attr.Value = value;
                        rootNode.Attributes.Append(attr);
                    }
                }
                finally
                {
                    reader.Close();
                }
                return true;
            }
            catch(Exception ex)
            {
                this.uiContext.Post(this.showErrorMessage, String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerInfoErr"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Функция сохраняет окончательный XML документ
        /// </summary>
        /// <param name="stream"></param>
        private void saveBackupFile(Stream stream)
        {
            //Сжатие полученных данных
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            //Расчёт MD5 суммы
            XmlAttribute attr = this.infoDoc.CreateAttribute("hash");
            attr.Value = CAuxil.ToHex(MD5.Create().ComputeHash(memoryStream));

            //Формирование из бинарного массива строки hex символов
            //Формирование файла с резервными настройками
            this.operationName = CGlobal.GetResourceValue("l_updWindOperationGenerateFile");
            memoryStream.Position = 0;
            memoryStream.Capacity = Convert.ToInt32(memoryStream.Length);
            String hex = CAuxil.ToHex(memoryStream.GetBuffer());

            //Сохранение hex строки в секцию CDATA XML документа
            XmlCDataSection cdata = this.infoDoc.CreateCDataSection(hex);
            XmlNode rootNode = CXML.getRootNode(this.infoDoc);

            XmlNode backupNode = this.infoDoc.CreateNode("element", "backup", "");
            backupNode.Attributes.Append(attr);
            backupNode.AppendChild(cdata);
            rootNode.AppendChild(backupNode);

            DateTime dt = DateTime.Now;
            //В XML файл записывается время создания резервной копии
            attr = this.infoDoc.CreateAttribute("dt");
            attr.Value = dt.ToString("dd.MM.yyyy HH:mm");
            rootNode.Attributes.Append(attr);

            //Формирование имени файла в который сохранится резервная копия
            String backupsDir = String.Format("{0}\\backups", Directory.GetCurrentDirectory());
            if (!Directory.Exists(backupsDir))
                Directory.CreateDirectory(backupsDir);
            String dt_string = dt.ToString("ddMMyyyyHHmm");
            String path = String.Format("{0}\\abak_{1}_{2}.backup", backupsDir, CXML.getAttributeValue(rootNode, "SERNUM", "0"), dt_string);

            //Сохранение файла
            this.infoDoc.Save(path);
        }

        /// <summary>
        /// Восстановление резервных настроек в контроллере
        /// </summary>
        /// <param name="backupInfo"></param>
        private void downloadBackup(CBackupInfo backupInfo)
        {
            try
            {
                //Подгрузка документа
                XmlDocument doc = new XmlDocument();
                doc.Load(backupInfo.Path);
                XmlNode rootNode = CXML.getRootNode(doc);
                XmlNode backupNode = rootNode.FirstChild;

                //Получение сжатого файла с резервной копией
                XmlCDataSection zipSection = backupNode.FirstChild as XmlCDataSection;
                byte[] zip = CAuxil.FromHex(zipSection.Value);
                //Расчёт хэш суммы полученного массива
                MD5 md5 = MD5.Create();
                String md5Hash = CAuxil.ToHex(md5.ComputeHash(zip));

                //Получение хэш суммы из XML документа
                String hash = CXML.getAttributeValue(backupNode, "hash", "");
                if (md5Hash != hash)
                {
                    //Несовпадение хэш сумм
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_updWindInvalidHash"));
                    this.stopOperation();
                    return;
                }

                //Загрузка файла в контроллер
                this.operationName = CGlobal.GetResourceValue("l_updWindSendFile");
                MemoryStream memoryStream = new MemoryStream(zip);
                if (!this.session.SSHClient.WriteFile(UpdateWindow.BACKUP_FILE, new MemoryStream(zip)))
                {
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_updWindControllerSendFileErr"));
                    this.stopOperation();
                    return;
                }

                //Распаковка архива
                this.operationName = CGlobal.GetResourceValue("l_updWindUnzipArchive");
                this.session.SSHClient.ExecuteCommand(String.Format("tar -xmzf {0} -C /tmp", UpdateWindow.BACKUP_FILE));
                if (this.session.SSHClient.LastError != "")
                {
                    String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerUnzipErr"), this.session.SSHClient.LastError);
                    this.uiContext.Post(this.showErrorMessage, message);
                    this.stopOperation();
                    return;
                }

                //Восстановление настроек контроллера
                this.operationName = CGlobal.GetResourceValue("l_updWindRestoreController");
                //Временно закрывается SQL соединение с базой
                this.session.CloseSQL();
                String cmd = "/tmp/backup/run";
                if (this.saveNetworkSettings)
                {
                    //Добавление ключа сохранения сетевых настроек нетронутыми
                    //sn - save network
                    cmd += " -sn";
                }
                    
                this.session.SSHClient.ExecuteCommand(cmd);
                if (this.session.SSHClient.LastError != "")
                {
                    String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerRestoreErr"), this.session.SSHClient.LastError);
                    this.uiContext.Post(this.showErrorMessage, message);
                    this.stopOperation();
                    return;
                }

                //Заново открывается соединение с базой
                this.session.OpenSQL();
                this.uiContext.Post(this.showInfoMessage, null);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindArchiveErr"), ex.Message);
                this.uiContext.Post(this.showErrorMessage, message);
            }
            this.stopOperation();
        }

        private void uploadBackupData()
        {
            String cmd = "/opt/abak/A:/assembly/backup";
            this.session.SSHClient.ExecuteCommand(cmd);
            if (this.session.SSHClient.LastError != "")
            {
                this.stopOperation();
                //Ошибка при создании резервной копии
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindCreateArchiveErr"), this.session.SSHClient.LastError);
                this.uiContext.Post(this.showErrorMessage, message);
                return;
            }

            //Копирование резервных настроек
            this.operationName = CGlobal.GetResourceValue("l_updWindCopyBackup");
            //Чтение файла с настройками из контроллера
            Stream file = this.session.SSHClient.ReadFile(UpdateWindow.BACKUP_FILE); 
            if (file == null)
            {
                this.stopOperation();
                //Не удалось прочитать файл с резервной копией
                this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_updWindReadArchiveErr"));
                return;
            }
            //Формирование специального файла с резервной копией
            this.saveBackupFile(file);
            //Останов процесса 
            this.stopOperation();
            //Обновление списка резервных копий
            this.uiContext.Post(this.loadBackups, null);
        }

        private void parseAssembly(String assembly, out int hi, out int mid, out int lo)
        {
            string[] vs = assembly.Split('.');
            hi = Convert.ToInt32(vs[0]);
            lo = Convert.ToInt32(vs[1]);
            if (vs.Length == 3)
            {
                mid = Convert.ToInt32(vs[1]);
                lo = Convert.ToInt32(vs[2]);
            }
            else
            {
                mid = -1;
                lo = Convert.ToInt32(vs[1]);
            }
        }

        private Boolean compareAssemblies(String assembly1, String assembly2)
        {
            int hi1, hi2, mid1, mid2, lo1, lo2;

            this.parseAssembly(assembly1, out hi1, out mid1, out lo1);
            this.parseAssembly(assembly2, out hi2, out mid2, out lo2);

            if ((hi1 != hi2) || (lo1 != lo2) || (mid1!= mid2))
                return false;

            return true;
        }

        /// <summary>
        /// Функция обновления контроллера
        /// </summary>
        /// <param name="path"></param>
        private void updateController(String path)
        {
            try
            {
                //Подгрузка документа
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode rootNode = CXML.getRootNode(doc);
                XmlNode updateNode = rootNode.FirstChild;

                //Полный путь к срипту обновления
                String script_path = CXML.getAttributeValue(rootNode, "path", "");
                if (script_path == "")
                {
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_undefinedUpdateScript"));
                    this.stopOperation();
                    return;
                }
                script_path = String.Format("{0}/update", script_path);
                //Версий контроллера
                //Получение версии прописанной в пакете обновления
                String oldAssembly = CXML.getAttributeValue(rootNode, "old_assembly", "");
                if (oldAssembly == "")
                {
                    //Нет старой версии
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_undefinedOldAssembly"));
                    this.stopOperation();
                    return;
                }

                //Сравнение текущей версии контроллера и старой версии в пакете обновления
                try
                {
                    if (!this.compareAssemblies(this.abakAssembly, oldAssembly))
                    {
                        this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_noAssemblyMatch"));
                        this.stopOperation();
                        return;
                    }
                }
                catch(Exception ex)
                {
                    this.uiContext.Post(this.showErrorMessage, ex.Message);
                    this.stopOperation();
                    return;
                }

                //Получение сжатого файла с резервной копией
                XmlCDataSection zipSection = updateNode.FirstChild as XmlCDataSection;
                byte[] zip = CAuxil.FromHex(zipSection.Value);
                //Расчёт хэш суммы полученного массива
                MD5 md5 = MD5.Create();
                String md5Hash = CAuxil.ToHex(md5.ComputeHash(zip));

                //Получение хэш суммы из XML документа
                String hash = CXML.getAttributeValue(updateNode, "hash", "");
                if (md5Hash != hash)
                {
                    //Несовпадение хэш сумм
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_updWindInvalidHash"));
                    this.stopOperation();
                    return;
                }

                //Загрузка файла в контроллер
                this.operationName = CGlobal.GetResourceValue("l_updWindSendFile");
                MemoryStream memoryStream = new MemoryStream(zip);
                if (!this.session.SSHClient.WriteFile(UpdateWindow.UPDATE_FILE, new MemoryStream(zip)))
                {
                    this.uiContext.Post(this.showErrorMessage, CGlobal.GetResourceValue("l_updWindControllerSendFileErr"));
                    this.stopOperation();
                    session.SSHClient.ExecuteCommand("systemctl restart abak_power");
                    return;
                }

                //Распаковка архива
                this.operationName = CGlobal.GetResourceValue("l_updWindUnzipArchive");
                this.session.SSHClient.ExecuteCommand(String.Format("tar -xmzf {0} -C /", UpdateWindow.UPDATE_FILE));
                if (this.session.SSHClient.LastError != "")
                {
                    String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerUnzipErr"), this.session.SSHClient.LastError);
                    this.uiContext.Post(this.showErrorMessage, message);
                    this.stopOperation();
                    session.SSHClient.ExecuteCommand("systemctl restart abak_power");
                    return;
                }

                //Удаление исходного файла
                this.session.SSHClient.ExecuteCommand(String.Format("rm {0}", UpdateWindow.UPDATE_FILE));

                //Запуск скрипта обновления ПО
                this.operationName = CGlobal.GetResourceValue("l_updatingController");
                this.session.SSHClient.ExecuteCommand(script_path);
                if (this.session.SSHClient.LastError != "")
                {
                    String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindControllerUpdateErr"), this.session.SSHClient.LastError);
                    this.uiContext.Post(this.showErrorMessage, message);
                    this.stopOperation();
                    session.SSHClient.ExecuteCommand("systemctl restart abak_power");
                    return;
                }
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindArchiveErr"), ex.Message);
                this.uiContext.Post(this.showErrorMessage, message);
            }
            this.stopOperation();
            IsFinished = true;
        }

        /// <summary>
        /// Загрузить обновления в контроллер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UploadUpdateButton_Handler(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            openDialog.Filter = String.Format("{0} (*.update)|*.update", CGlobal.GetResourceValue("l_updateFiles"));
            if (openDialog.ShowDialog() != true)
                return;
            if(CGlobal.CurrState.PLCVersionInfo == 3)
            {
                string sql = "NOTIFY watchdog_state_changed, 'false'";
                NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                cmd.ExecuteNonQuery();
            }

            this.startOperation(CGlobal.GetResourceValue("l_updateWindowUpdateUploadButton"), BACKUP_OPERATION.UPDATE_SOFTWARE);
            await Task.Run(() => updateController(openDialog.FileName));
            if (!IsFinished)
                return;

            this.Close();
        }

        /// <summary>
        /// Флаг показывает что команда не выполняется
        /// </summary>
        public Boolean CommandNotRunning
        {
            get
            {
                return this.currentOperation == BACKUP_OPERATION.NONE;
            }
        }

        public Boolean CommandRunning
        {
            get
            {
                return this.currentOperation != BACKUP_OPERATION.NONE;
            }
        }

        public String OperationName
        {
            get
            {
                return this.operationName;
            }
        }

        public String StatusRunning
        {
            get
            {
                if (this.pointNumber == 0)
                    this.statusRunning = "";
                else
                    this.statusRunning = this.statusRunning + ".";
                this.pointNumber++;
                if (this.pointNumber >= 50)
                    this.pointNumber = 0;

                return this.statusRunning;
            }
        }

        public ObservableCollection<CBackupInfo> BackupsList
        {
            get
            {
                return this.backupsList;
            }
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DeleteButton_Handler(object sender, RoutedEventArgs e)
        {
            //Удаление выбранного файла
            if (this.backupsListView.SelectedItem == null)
                return;

            CBackupInfo backupInfo = this.backupsListView.SelectedItem as CBackupInfo;
            if (MessageBox.Show(this, CGlobal.GetResourceValue("l_updWindDeleteQuestion"), this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                File.Delete(backupInfo.Path);
                this.loadBackups(null);
            }
            catch(Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_updWindDeleteFileErr"), ex.Message);
                MessageBox.Show(this, message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загрузить резервную копию в контроллер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UploadBackupButton_Handler(object sender, RoutedEventArgs e)
        {
            //Получение информации о контроллере
            String operName = CGlobal.GetResourceValue("l_updWindControllerInfo");
            this.startOperation(operName, BACKUP_OPERATION.CREATE_BACKUP);
            if (!this.getControllerInfo())
                return;

            //Формирование запроса на создание резерной копии ПО и настроек
            this.operationName = CGlobal.GetResourceValue("l_updWindCreateBackup");
            await Task.Run(() => uploadBackupData());
        }

        /// <summary>
        /// Загрузить резервную копию из контроллера,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DownloadBackupButton_Handler(object sender, RoutedEventArgs e)
        {
            if (this.backupsListView.SelectedItem == null)
                return;

            RestoreOptionsWindow restoreOptions = new RestoreOptionsWindow();
            restoreOptions.Owner = this;
            if (restoreOptions.ShowDialog() != true)
                return;

            this.saveNetworkSettings = (bool)restoreOptions.DoNotOverwriteNetwork.IsChecked;

            //Подготовка резервных настроек к загрузке в контроллер
            this.startOperation(CGlobal.GetResourceValue("l_updWindPrepareRestoreController"), BACKUP_OPERATION.RESTORE_BACKUP);
            CBackupInfo backupInfo = this.backupsListView.SelectedItem as CBackupInfo;
            await Task.Run(() => downloadBackup(backupInfo));
        }
    }
}
;