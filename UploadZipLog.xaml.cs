using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using AbakConfigurator;
using Npgsql;
using Newtonsoft.Json.Linq;
using Ionic.Zip;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для UploadZipLog.xaml
    /// </summary>
    public partial class UploadZipLog : Window, INotifyPropertyChanged
        
    {
        //SSH клиент
        private CSSHClient sshClient;
        //Контекст потока для работы с пользовательским интерфейсом приложения из других потоков 
        private SynchronizationContext uiContext;
        //Путь расположения архива логов контроллера
        private string resultPath = "/tmp";
        //Пусть расположения файлов .log контроллера
        private string logPath = "/var/log";
        //Текущая выполняемая операция
        private String currentOperation = "";
        //Строка прогресса
        private string progressString = "";
        //Таймер обновления данных на экране
        private DispatcherTimer timer = new DispatcherTimer();
        //Номер точки
        private int pointNumber = 0;
        //Маркер запрета закрытия окна
        private bool notCloseWindow = false;


        public UploadZipLog(CSSHClient sshClient, string userDir)
        {
            InitializeComponent();
            this.sshClient = sshClient;
            //Инициализация таймера обновляющего экран
            this.timer.IsEnabled = false;
            NotCloseWindow = true;
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            this.timer.Tick += new EventHandler(this.timer_Tick);
            

            this.uiContext = SynchronizationContext.Current;
            startMakeZip(userDir);            
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (this.pointNumber == 0)
                this.ProgressString = "";
            else
                this.ProgressString = this.ProgressString + ".";

            this.pointNumber++;
            if (this.pointNumber >= 50)
                this.pointNumber = 0;
        }

        private void stopOperation(object message)
        {
            this.CurrentOperation = message.ToString();
            this.ProgressString = "";
            this.timer.IsEnabled = false;
            
        }

        public string ProgressString
        {
            get
            {
                return this.progressString;
            }
            set
            {
                this.progressString = value;
                this.OnPropertyChanged("ProgressString");
            }
        }

        public string CurrentOperation
        {
            get
            {
                return this.currentOperation;
            }
            set
            {
                this.currentOperation = value;
                this.OnPropertyChanged("CurrentOperation");
            }
        }

        private void SetCurrentOperationValue(object state)
        {
            this.CurrentOperation = state.ToString();
        }


        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public string LogPath { get => logPath; set => logPath = value; }
        public string ResultPath { get => resultPath; set => resultPath = value; }

        public bool NotCloseWindow { get => notCloseWindow; set => notCloseWindow = value; }
        

        /// <summary>
        /// Сохранение из памяти в файл
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="stream"></param>
        public bool SaveStreamToFile(string filename, Stream stream)

        {
            if (stream.Length != 0)
                using (FileStream fileStream = File.Create(filename, (int)stream.Length))
                {
                    // Размещает массив общим размером равным размеру потока

                    byte[] data = new byte[stream.Length];

                    stream.Read(data, 0, (int)data.Length);
                    fileStream.Write(data, 0, data.Length);
                    return true;
                }
            return false;

        }



        private void makeZipLog(string userDir)
        {
            // результат сохранения
            bool result;
           
            this.timer.IsEnabled = true;
            this.uiContext.Post(this.SetCurrentOperationValue, CGlobal.GetResourceValue("l_createZipLog"));
            //Формируем архив логов в контроллере
            sshClient.ExecuteCommand("cd /var/log");
            string archivePath = String.Format("{0}.tar.gz", this.ResultPath + "/abak_logs");
            sshClient.ExecuteCommand(String.Format("tar -czf {0} {1}", archivePath, this.LogPath));

            this.uiContext.Post(this.SetCurrentOperationValue, CGlobal.GetResourceValue("l_readyToCopyZip"));
            //Читаем в память
            MemoryStream memoryStream = new MemoryStream();
            Stream file = sshClient.ReadFile(archivePath);
            if(file == null)
            {
                this.uiContext.Post(this.stopOperation, CGlobal.GetResourceValue("l_notReadyToFile"));
                return;
            }
            file.CopyTo(memoryStream);
            memoryStream.Position = 0;
            this.uiContext.Post(this.SetCurrentOperationValue, CGlobal.GetResourceValue("l_createDirToConfigurator"));
            String logDir = String.Format(userDir);
            string path = logDir;
            //Сохранение файла
            this.uiContext.Post(this.SetCurrentOperationValue, CGlobal.GetResourceValue("l_savingToLog"));
            result = SaveStreamToFile(path, memoryStream);
            if (result)
            {
                this.uiContext.Post(this.SetCurrentOperationValue, CGlobal.GetResourceValue("l_deleteToarchiv"));
                sshClient.ExecuteCommand(String.Format("rm {0}", archivePath));
            }
        }

       

        private async void startMakeZip(string userDir)
        {

            
            try
            {
                await Task.Run(() => makeZipLog(userDir));
            }
            finally
            {
                this.stopOperation("");
                NotCloseWindow = false;
                this.Close();

                MessageBox.Show(this,CGlobal.GetResourceValue("l_uploadLogFileSuccesfull") + " "+ userDir, CGlobal.GetResourceValue("l_uploadLogFile"), MessageBoxButton.OK, MessageBoxImage.Information);
                
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = this.NotCloseWindow;
        }
    }
}

    

