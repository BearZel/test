using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using Npgsql;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Threading;
using Renci.SshNet;
namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for BackupWindow.xaml
    /// </summary>
    public partial class BackupWindow : Window, INotifyPropertyChanged
    {
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private CSession session;
        public ObservableCollection<BackupParam> BackupParams { get; set; } = new ObservableCollection<BackupParam>();
        public class BackupParam
        {
            public string Name { get; set; }
            public string CopyCmd { get; set; }
            public string WriteCmd { get; set; }
            public int ID { get; set; }
            public bool State { get; set; }
        }
        public BackupWindow()
        {
            session = CGlobal.Session;
            LoadParams();
            InitializeComponent();
            BackupGrid.ItemsSource = BackupParams;
        }
        private void LoadParams()
        {
            const string sql = "select * from backup_table order by id";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, session.Connection);

            NpgsqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                const string message = "Ошибка во время загрузки настроек. Данные в базе не найдены";
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            while (reader.Read())
            {
                BackupParam bakcupParam = new BackupParam();
                bakcupParam.ID = Convert.ToInt32(reader["id"]);
                bakcupParam.Name = reader["name"].ToString();
                bakcupParam.CopyCmd = reader["copyCMD"].ToString();
                bakcupParam.WriteCmd = reader["writeCMD"].ToString();
                bakcupParam.State = Convert.ToBoolean(reader["state"]);
                BackupParams.Add(bakcupParam);
            }

            reader.Close();
        }
        private void PrepareFiles(List<BackupParam> choosenParams)
        {
            string cmd = "mkdir -p /tmp/backup;mkdir -p /tmp/backup/offline;" +
                "mkdir -p /tmp/backup/ethPorts;mkdir -p /tmp/backup/DB;" +
                "mkdir -p /tmp/backup/configs;" +
                "mkdir -p /tmp/backup/route;" +
                "rsync -av /opt/abak/A:/assembly/configs/. /tmp/backup/configs/.;" +
                "cp -rp /opt/abak/A:/assembly/route/. /tmp/backup/route/.";

            session.SSHClient.ExecuteCommand(cmd);

            Dictionary<string, string> paths = new Dictionary<string, string>();
            paths.Add("/tmp/backup/copyCmd.sh", "sleep 1;\ncp -rp /opt/backup/offline/. /tmp/backup/offline/.;\nsleep 2;");
            paths.Add("/tmp/backup/writeCmd.sh", "systemctl stop abak_power;\nsleep 10;\n" +
                "rsync -av /tmp/backup/configs/. /opt/abak/A:/assembly/configs/.\n" +
                "rsync -av /tmp/backup/route/. /opt/abak/A:/assembly/route/.");

            foreach (BackupParam backupParam in choosenParams)
            {
                paths["/tmp/backup/copyCmd.sh"] += backupParam.CopyCmd;
                paths["/tmp/backup/writeCmd.sh"] += backupParam.WriteCmd;
            }

            foreach (string key in paths.Keys)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(paths[key]);
                writer.Flush();
                stream.Position = 0;
                session.SSHClient.WriteFile(key, stream);
                session.SSHClient.ExecuteCommand($"chmod -R 755 {key}");
            }
        }
        private void SaveTags()
        {
            if (CGlobal.Session.SSHClient == null)
                return;

            const string path = "/tmp/backup/offline/tags.xml";
            //Создание документа с проектом
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(decl, root);
            XmlNode rootNode = doc.CreateNode("element", "root", "");
            XmlAttribute attr = doc.CreateAttribute("hardware");
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
            CGlobal.Session.SSHClient.WriteFile(path, mem);
        }
        private void LoadEthData()
        {
            CEthernetIterfacesList interfaces = new CEthernetIterfacesList(CGlobal.Session.SSHClient);
            interfaces.LoadInterfaces();
            foreach (CEthernetInterface eth in interfaces.InterfacesList)
            {
                CEthernetInterface ethCopy = eth;
                interfaces.UpdateDirectly(ref ethCopy, "/tmp/backup/ethPorts/");
            }
        }
        private void CreateBackup(string path)
        {
            if (CGlobal.Session.SSHClient == null)
            {
                return;
            }
            //Подготовка архива
            String archivePath = "/tmp/backup.tar.gz";
            CGlobal.Session.SSHClient.ExecuteCommand($"tar -czf " +
                $"{archivePath} /tmp/backup");
            //Загрузка файла из контроллера
            Stream file = CGlobal.Session.SSHClient.ReadFile(archivePath);
            if (file != null)
                CAuxil.CreateEncryptedFile(file, path);
        }
        private void ReadConfigFromController(string path, ref string msg, List<BackupParam> choosenParams)
        {
            PrepareFiles(choosenParams);
            SaveTags();
            LoadEthData();
            session.SSHClient.ExecuteCommand("/tmp/backup/copyCmd.sh &");
            CreateBackup(path);
        }
        private async void Accept_Click(object sender, RoutedEventArgs e)
        {
            List<BackupParam> choosenParams = BackupParams.Where(param => param.State).ToList();
            if (choosenParams.Count() == 0)
            {
                MessageBox.Show("Необходимо выбрать параметры!", "Ошибка!", MessageBoxButton.OK,
                 MessageBoxImage.Error);
                return;
            }

            string path = CAuxil.CreatePath("ABAK_Configuration", "abak_cfg", "Файлы конфигурации(*.abak_cfg)|*.abak_cfg");
            if (path == "")
                return;

            WaitingWindow waitingWindow = new WaitingWindow();
            waitingWindow.Owner = this;
            waitingWindow.Show();

            string msg = "";
            await Task.Run(() => ReadConfigFromController(path, ref msg, choosenParams));

            waitingWindow.Close();
            Thread.Sleep(100);
            if (msg == "")
                MessageBox.Show(CGlobal.GetResourceValue("l_configReadSuccess"), this.Title,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            
        }
    }
}
