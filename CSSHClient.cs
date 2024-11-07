using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet;
using System.IO;
using System.Windows;
using System.Threading.Tasks;

namespace AbakConfigurator
{
    public class CSSHClient : IDisposable
    {
        //IP адрес с котороым идет соединение
        private string host = "";
        private UInt16 port = CSettings.DEFAULT_SSH_PORT;

        private String user;
        private String passw;

        //Класс клиента с которым идет работа
        private SshClient client = null;
        //Класс для отработки асинхронных команд
        private SshCommand asynchCmd = null;
        //Класс для чтения и записи файлов
        private ScpClient scp = null;

        //Описание последней ошибки
        private String lastError;

        public CSSHClient(string Host)
        {
            this.host = Host;
        }

        public CSSHClient(string Host, UInt16 Port, String user, String password)
        {
            this.host = Host;
            this.port = Port;
            this.user = user;
            this.passw = password;

            this.client = new SshClient(this.host, this.port, this.user, this.passw);
        }

        public String LastError
        {
            get
            {
                return this.lastError;
            }
        }

        /*
        * Установка соединения с контроллером
        */
        private Boolean Connect()
        {
            try
            {
                this.client.Connect();
                return true;
            }
            catch
            {
                return false;
            }
        }
        /*
        * Разрыв соединения с клиентом
        */
        public void Disconnect()
        {
            if (this.client != null)
            {
                this.client.Disconnect();
                this.client = null;
            }

            if (this.scp != null)
            {
                this.scp.Disconnect();
                this.scp = null;
            }
        }

        /*
        * Установка соединения с контроллером
        */
        public void Dispose()
        {

        }

        private SshClient sshClient
        {
            get
            {
                if (!this.client.IsConnected)
                    this.client.Connect();

                return this.client;
            }
        }

        /// <summary>
        /// Функция исполняет команду
        /// </summary>
        /// <param name="command"> Исполняемая команда</param>
        /// <returns>Возвращает ответ из консоли</returns>
        public string ExecuteCommand(string command)
        {
            SshCommand cmd = this.sshClient.CreateCommand(command);
            String result = cmd.Execute();
            if (!string.IsNullOrEmpty(cmd.Error))
            {
                this.lastError = String.Format("При выполнении команды:\n{0}\nВозникла ошибка:\n{1}", command, cmd.Error);
                //MessageBox.Show(lastError, "Ошибка выполнения SSH команды", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }

            this.lastError = "";
            if (string.IsNullOrEmpty(result))
                return "";
            return result.Substring(0, result.Length - 1);
        }

        /// <summary>
        /// Запускает на выполнение команду в асинхронном режиме
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public IAsyncResult BeginExecute(string command, AsyncCallback callback = null)
        {
            if (this.asynchCmd != null)
                throw new Exception("Контроллер уже занят выполнением операции!");
            this.asynchCmd = this.sshClient.CreateCommand(command);
            return this.asynchCmd.BeginExecute(callback);
        }

        public void EndExecute(IAsyncResult result)
        {
            if (this.asynchCmd == null)
                return;

            this.asynchCmd.EndExecute(result);
            this.asynchCmd = null;
        }

        private ScpClient scpClient
        {
            get 
            {
                if (this.scp == null)
                {
                    this.scp = new ScpClient(this.client.ConnectionInfo);
                    this.scp.Connect();
                }

                return this.scp;
            }
        }

        /// <summary>
        /// Функция записи файла в контроллер
        /// </summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <param name="stream">Содержимое файла</param>
        /// <param name="throw_ex">Флаг разрешающий выброс исключения</param>
        public Boolean WriteFile(String path, Stream stream, Boolean throw_ex = false)
        {
            stream.Position = 0;
            try
            {
                this.scpClient.Upload(stream, path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Функция чтения содержимого файла мз контроллера
        /// </summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <returns>Возвращает буфер с данными</returns>
        public Stream ReadFile(String path)
        {
            MemoryStream stream = new MemoryStream();
            try
            {
                this.scpClient.Download(path, stream);
                stream.Position = 0;
            }
            catch
            {
                stream = null;
            }
            return stream;
        }

        public List<string> ListDirectory(string path)
        {
            var res = ExecuteCommand("ls " + path);
            return res.Split('\n').ToList();
        }
    }
}
