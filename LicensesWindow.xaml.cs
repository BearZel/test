using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Win32;
using System.Security.Cryptography;
using Npgsql;
using System.Threading;

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for LicensesWindow.xaml
    /// </summary>
    public partial class LicensesWindow : Window
    {
        //SSH клиент
        private string runTime = "PP.16.04.00.00";
        private string key = "";
        private string sernum = "";
        private CSSHClient sshClient = null;
        private NpgsqlConnection connection;
        private Dictionary<int, string> words = new Dictionary<int, string>
        {
            { 0, "incomsystem" }, { 1, "controller" }, { 2, "redundancy" },
            { 3, "cryptography" }, { 4, "license" }, { 5, "generator" }
        };
        public ObservableCollection<License> LicensesInFile { get; set; }
        public ObservableCollection<License> LicensesInCPU { get; set; }
        public LicensesWindow(NpgsqlConnection connection, CSSHClient sshClient)
        {
            this.sshClient = sshClient;
            this.connection = connection;
            LicensesInFile = new ObservableCollection<License>();
            LicensesInCPU = new ObservableCollection<License>();
            InitializeComponent();
            string[] str = CGlobal.CurrState.Serial.Split(':');
            str = str[1].Split(' ');
            sernum = str[1];

        }
        private void UploadFile()
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            openDialog.Filter = "Лицензия (*.xml)|*.xml";
            if (openDialog.ShowDialog() != true)
                return;

            if (CheckLicenses(openDialog.FileName))
            {
                Thread.Sleep(100);
                if (CGlobal.CurrState.AssemblyInt < 530)
                    DownloadLicensesFromDB();
                else
                    DownloadLicensesFromXML();

                LicensesGrid.ItemsSource = LicensesInCPU;
            }
        }
        private void DeleteKey(ref string xml)
        {
            if (!xml.Contains("license_key"))
                return;

            int index = xml.IndexOf("license_key");
            //license_key= - 12 символов
            key = xml.Substring(index + 12);
            xml = xml.Remove(index);
        }
        private bool CheckLicenses(string path)
        {
            string msg = "Не удалось загрузить файл в ПЛК. Ошибка:";
            try
            {
                string xml = File.ReadAllText(path, Encoding.GetEncoding(1251));
                //Ключ удаляется, потому что не выходит читать XML
                DeleteKey(ref xml);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                XmlElement node = doc.DocumentElement;
                XPathNavigator nav = doc.CreateNavigator();
                string sernum = node.GetAttribute("sernum");
                if (sernum != this.sernum)
                {
                    MessageBox.Show("Несовпадение серийного номера!", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return false;
                }
                ObservableCollection<License> licensesInFile = new ObservableCollection<License>();
                if (!ReadXML(nav, ref licensesInFile))
                    return false;

                LicensesInFile = licensesInFile;
                if (key != CheckFile())
                {
                    MessageBox.Show($"Файл был изменён!", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                SendDataToCPU(doc);
                Thread.Sleep(1000);
                MessageBox.Show("Лицензия успешно загружена в контроллер", "Лицензии ПЛК", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(msg + ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }
        private bool ReadXML(XPathNavigator nav, ref ObservableCollection<License> collection)
        {
            //Проверка что файл не повреждён
            int counter = 0;
            foreach (XPathNavigator ruleNode in nav.Select("//license"))
            {
                counter++;
                License license = new License();
                license.Num = counter;
                license.Description = CXML.getAttributeValue(ruleNode, "name", "");
                license.Name = CXML.getAttributeValue(ruleNode, "code", "");
                license.Enable = Convert.ToBoolean(CXML.getAttributeValue(ruleNode, "enable", "false"));
                collection.Add(license);
                string runTime = CXML.getAttributeValue(ruleNode, "runTime", "").ToLower();
                if (runTime == "true")
                {
                    this.runTime = runTime;
                }
            }

            if (collection.Count() == 0)
            {
                string msg = "Не удалось загрузить файл в ПЛК. Ошибка:";
                MessageBox.Show(msg + "Список пуст!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }
        private void SendDataToCPU(XmlDocument doc)
        {
            XmlNodeList itemNodes = doc.SelectNodes("//license");
            string content = "<?xml version=\"1.0\" encoding=\"1251\"?>\n" +
                $"<licenses sernum=\"{sernum}\">\n";
            foreach (XmlNode itemNode in itemNodes)
            {
                if (itemNode != null)
                    content += "\t" + itemNode.OuterXml + "\n";
            }
            content += "</licenses>\n";
            sshClient.WriteFile($"/tmp/{sernum}.xml", CAuxil.StringToStream(content, Encoding.GetEncoding(1251)));
            string linuxCMD = $"echo \"license_key={key}\" >> /tmp/{sernum}.xml";
            CAuxil.ExecuteSingleSSHCommand(linuxCMD);
            string sql = $"NOTIFY license_changed, '/tmp/{sernum}.xml'";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }
        private string CheckFile()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("licenses");
            XmlAttribute attribute = doc.CreateAttribute("sernum");
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "1251", "");
            attribute.Value = sernum;
            root.Attributes.Append(attribute);
            doc.AppendChild(decl);
            doc.AppendChild(root);

            foreach (License lic in LicensesInFile)
            {
                XmlNode ruleNode = doc.CreateNode("element", "license", "");
                XmlAttribute attr = doc.CreateAttribute("code");
                attr.Value = lic.Name;
                ruleNode.Attributes.Append(attr);
                root.AppendChild(ruleNode);

                attr = doc.CreateAttribute("name");
                attr.Value = lic.Description;
                ruleNode.Attributes.Append(attr);
                root.AppendChild(ruleNode);

                attr = doc.CreateAttribute("enable");
                attr.Value = lic.Enable.ToString().ToLower();
                ruleNode.Attributes.Append(attr);
                root.AppendChild(ruleNode);

                root.AppendChild(ruleNode);
            }
            string path = CAuxil.AppDataPath + $"\\{sernum}.xml";
            File.WriteAllText(path, string.Empty);
            WriteWithEncoding(doc, path);
            WriteWords(path, CAuxil.AppDataPath + "\\tmp.txt");
            byte[] md5b;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(CAuxil.AppDataPath + "\\tmp.txt"))
                {
                    md5b = md5.ComputeHash(stream);
                }
            }
            string md5Text = ToHex(md5b);
            File.AppendAllText(path, "license_key=" + md5Text);
            File.Delete(path);
            File.Delete(CAuxil.AppDataPath + "\\tmp.txt");
            return md5Text;
        }
        private void WriteWithEncoding(XmlDocument doc, string path)
        {
            XmlNodeList itemNodes = doc.SelectNodes("//license");
            string content = "<?xml version=\"1.0\" encoding=\"1251\"?>\n" +
                $"<licenses sernum=\"{sernum}\">\n";
            foreach (XmlNode itemNode in itemNodes)
            {
                if (itemNode != null)
                    content += "\t" + itemNode.OuterXml + "\n";
            }
            content += "</licenses>";
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(ms, Encoding.GetEncoding(1251));
                writer.WriteLine(content);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    ms.CopyTo(fs);
                    fs.Flush();
                }
            }
        }
        private void WriteWords(string licPath, string tmpPath)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter writer = new StreamWriter(ms, Encoding.GetEncoding(1251));
                int i = 0;
                foreach (string line in System.IO.File.ReadLines(licPath, Encoding.GetEncoding(1251)))
                {
                    writer.WriteLine(line);
                    if (i < 3)
                        writer.WriteLine(words[i++]);
                }
                writer.WriteLine(words[3] + words[4] + words[5]);
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                using (FileStream fs = new FileStream(tmpPath, FileMode.OpenOrCreate))
                {
                    ms.CopyTo(fs);
                    fs.Flush();
                }
            }
        }
        public static string ToHex(byte[] data)
        {
            string hex = string.Empty;
            foreach (byte c in data)
            {
                hex += c.ToString("X2");
            }
            return hex;
        }

        private bool DownloadLicensesFromDB()
        {
            Thread.Sleep(900);
            string sql = "select * from licenses_table";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (!reader.HasRows)
                {
                    const string message = "Ошибка во время загрузки настроек. Данные в базе не найдены";
                    MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    reader.Close();
                    return false;
                }
                int counter = 0;
                LicensesInCPU.Clear();
                while (reader.Read())
                {
                    counter++;
                    License license = new License();
                    license.Num = counter;
                    license.Name = reader["name"].ToString();
                    license.Enable = Convert.ToBoolean(reader["value"]);
                    license.Description = reader["description"].ToString();
                    LicensesInCPU.Add(license);
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                MessageBox.Show("Ошибка при работе базы данных." + ex.Message, "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            reader.Close();
            LicensesGrid.ItemsSource = LicensesInCPU;
            return true;
        }
        private bool DownloadLicensesFromXML()
        {
            const string path = "/dev/shm/licenses.xml";
            Stream stream = CGlobal.Session.SSHClient.ReadFile(path);
            if (stream == null)
                return false;

            StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251), true);
            XmlTextReader xmlReader = new XmlTextReader(reader);
            XPathDocument doc = new XPathDocument(xmlReader);
            XPathNavigator nav = doc.CreateNavigator();
            ObservableCollection<License> licensesInFile = new ObservableCollection<License>();
            if (!ReadXML(nav, ref licensesInFile))
                return false;

            LicensesInCPU = licensesInFile;
            return true;
        }
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            UploadFile();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool state = false;
            if (CGlobal.CurrState.AssemblyInt < 530)
                state = DownloadLicensesFromDB();
            else
                state = DownloadLicensesFromXML();

            if (!state)
                Close();

            LicensesGrid.ItemsSource = LicensesInCPU;
        }
        private void UploadKey(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            if (openDialog.ShowDialog() != true)
                return;

            FileStream fileStream = new FileStream(openDialog.FileName, FileMode.Open);
            switch (runTime)
            {
                //MasterSCADA  
                case "PP.16.02.00.00":
                    CGlobal.Session.SSHClient.WriteFile("/opt/abak/A:/assembly/mplc.key", fileStream);
                    break;
                //Круг2000
                case "PP.16.04.00.00":
                    string crugPath = $"/opt/crug2000/{openDialog.SafeFileName}";
                    string abakStartPath = "/opt/abak/A:/assembly/abak_start";
                    CGlobal.Session.SSHClient.WriteFile($"{crugPath}", fileStream);
                    CGlobal.Session.SSHClient.ExecuteCommand($"sudo -u disrt {crugPath}");
                    Stream stream = CGlobal.Session.SSHClient.ReadFile(abakStartPath);
                    StreamReader streamReader = new StreamReader(stream);
                    string abakStart = "";
                    bool isCrugExists = false;
                    while (streamReader.Peek() >= 0)
                    {
                        string line = streamReader.ReadLine();
                        abakStart += line + "\n";
                        if (line != "#crug2000")
                            continue;

                        isCrugExists = true;
                        abakStart += crugPath;
                        break;
                    }

                    if (!isCrugExists)
                    {
                        CGlobal.Session.SSHClient.ExecuteCommand("printf '%s\\n' c#R$1U2g_paSwRd c#R$1U2g_paSwRd v 1 2 3 4 y | " +
                            "adduser --home /opt/crug2000 disrt");
                        abakStart += $"\n#crug2000\nsudo -u disrt {crugPath}";
                    }
                    CGlobal.Session.SSHClient.WriteFile($"{abakStartPath}", CAuxil.StringToStream(abakStart));
                    break;
            }

            fileStream.Close();
            MessageBox.Show("Файл загружен в контроллер", "Загрузка", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    public class License
    {
        private string name;
        private int num;
        private bool enable;
        private string description;
        public string Name { get => name; set => name = value; }
        public bool Enable { get => enable; set => enable = value; }
        public int Num { get => num; set => num = value; }
        public string Description { get => description; set => description = value; }
    }
}
