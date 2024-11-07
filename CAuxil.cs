using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Controls;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.ComponentModel;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Collections.Generic;
using Npgsql;
using System.Xml;
using Newtonsoft.Json.Linq;
using AbakConfigurator;
using System.Reflection;
namespace AbakConfigurator
{
    public class CAuxil
    {
        /// <summary>
        /// Возвращает путь к папке с данными
        /// По умолчанию это c:\ProgramData\Abak
        /// </summary>
        /// <returns></returns>
        public static String AppDataPath
        {
            get
            {
                String dir = (Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Abak");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                return dir;
            }
        }
        public static void WriteStackTrace(Exception ex)
        {
            String path = AppDomain.CurrentDomain.BaseDirectory + "stacktrace.txt";
            FileStream stream = new FileStream(path, FileMode.Create);
            stream.Position = 0;
            StreamWriter writer = new StreamWriter(stream);
            writer.NewLine = "\n\r";

            writer.WriteLine(String.Format("Exception: {0}", ex.Message));

            String messageString = "";
            StackTrace st = new StackTrace(ex, true);
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                String s;
                if (sf.GetFileLineNumber() != 0)
                    s = String.Format("File {0}, Method {1}, Line {2}", Path.GetFileName(sf.GetFileName()), sf.GetMethod(), sf.GetFileLineNumber());
                else
                    s = String.Format("Method {0}", sf.GetMethod());

                writer.WriteLine(s);
                if (i == 0)
                    messageString = s;
            }
            writer.Flush();
            messageString = String.Format("{0}: {1}", messageString, ex.Message);
            MessageBox.Show(messageString, "Ошибка в работе конфигуратора", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        /// <summary>
        /// Функция проверяет что бы в вводимой строке были только цифры
        /// </summary>
        /// <param name="Text">Проверяемый текст</param>
        /// <param name="UnsignedVal">Флаг что должно быть целое безнаковое</param>
        /// <returns></returns>
        public static Boolean CheckStringForInt(String Text, Boolean UnsignedVal)
        {
            Regex regex;
            if (UnsignedVal)
                regex = new Regex("[^0-9]");
            else
                regex = new Regex("[^0-9-]");

            return !regex.IsMatch(Text);
        }

        /// <summary>
        /// Функция проверяет что бы в вводимой строке были только цифры и точки
        /// </summary>
        /// <param name="Text">Проверяемый текст</param>
        /// <returns></returns>
        public static Boolean CheckIPString(String Text)
        {
            Regex regex = new Regex("[^0-9.]");
            return !regex.IsMatch(Text);
        }

        /// <summary>
        /// Функция проверяет что бы вводимая строка соответсвовала числу с плавающей запятой
        /// </summary>
        /// <param name="Text">Проверяемый текст</param>
        /// <returns></returns>
        public static Boolean CheckStringForFloat(String Text)
        {
            Regex regex = new Regex("[^0-9-.]");
            return !regex.IsMatch(Text);
        }

        /// <summary>
        /// Функция конвертирует строку в Stream
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Stream StringToStream(string text, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, encoding);
            writer.NewLine = "\n";
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        /// <summary>
        /// Функция отрабатывает по строке в которой передается число с плавающей запятой
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static String AdaptFloat(String val)
        {

            String ds = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (ds == ",")
            {
                if (val.IndexOf('.') != -1)
                    return val.Replace(".", ",");
                else
                    return val;
            }
            else if (ds == ".")
            {
                if (val.IndexOf(',') != -1)
                    return val.Replace(",", ".");
                else
                    return val;
            }
            return val;
        }

        /// <summary>
        /// Функция показывает сколько мс прошло с 01.01.01 00:00:00
        /// </summary>
        /// <returns></returns>
        public static long GetTickCount()
        {
            return DateTime.Now.Ticks / 10000;
        }

        /// <summary>
        /// Загрузка имени контроллера
        /// </summary>
        /// <returns></returns>
        public static int LoadCFG(out string name, out string unicodeName, string plc = "")
        {
            //Путь к CFG файлу            
            string codesysControlCFG = $"/opt/backup/IMAGES/PLC{plc}/etc/CODESYSControl.cfg";
            name = "";
            unicodeName = "";
            int result = 0;
            Stream stream = CGlobal.Session.SSHClient.ReadFile(codesysControlCFG);
            if (stream == null)
                stream = new MemoryStream();
            CIniFile iniCFG = new CIniFile(stream);
            iniCFG.NewLine = "\n";
            string unicName = iniCFG.ReadValue("systarget", "nodenameunicode");
            string nodeName = iniCFG.ReadValue("systarget", "nodename");

            if (unicName != "")
            {
                unicodeName = unicName;
                result += 1;
            }
            if (nodeName != "")
            {
                name = nodeName;
                result += 2;
            }
            return result;
        }
        /// <summary>
        /// Функция возвращает цифру на конце строки
        /// </summary>
        /// <param name="s">строка для обработки</param>
        /// <returns></returns>
        public static int GetDigitFromEndOfString(String s)
        {
            int res = 0;
            Boolean hasNumber = false;

            foreach (Char c in s)
            {
                try
                {
                    res = res * 10 + Convert.ToInt32(Convert.ToString(c));
                    hasNumber = true;
                }
                catch
                {
                    res = 0;
                    hasNumber = false;
                }
            }

            if (!hasNumber)
            {
                throw new Exception(CGlobal.GetResourceValue("l_noNumberAthTheEnd"));
            }
            return res;
        }

        /// <summary>
        /// Функция масштабирования
        /// </summary>
        /// <param name="EUmin">Минимальное значение в инженерных единицах</param>
        /// <param name="EUmax">Максимальное значение в инженерных единицах</param>
        /// <param name="RAWmin">Минимальное значение в исходных единицах</param>
        /// <param name="RAWmax">Максимальное значение в исходных единицах</param>
        /// <param name="Raw">Текущее значение в исходных единицах</param>
        /// <returns></returns>
        public static Double Scale(Double EUmin, Double EUmax, Double RAWmin, Double RAWmax, Double Raw)
        {
            Double diff = RAWmax - RAWmin;
            if (diff == 0)
                return 0;

            double value = (EUmin + (Raw - RAWmin) * (EUmax - EUmin) / diff);
            return value;
        }

        /// <summary>
        /// Функция отрабатывает единичную SSH команду
        /// </summary>
        /// <param name="cmd">Команда передаваемая контроллеру</param>
        /// <param name="Error">Описание ошибки возникшей при выполнении команды</param>
        /// /// <param name="session">Сессия в рамках которой выполняется команда</param>
        static public String ExecuteSingleSSHCommand(String cmd, out String Error, CSession session = null)
        { 
            //Если связь с контроллером уже есть, то смысла нет ещё одно соединение устанавливать
            String result = CGlobal.Session.SSHClient.ExecuteCommand(cmd);
            Error = CGlobal.Session.SSHClient.LastError;
            return result;
        }
        public static void SetTagValue(string value, string tagName)
        {
            CParam param = CConfig.ParamByTag(tagName) as CParam;
            string sql = String.Format("execute update_fast('{0}', {1})", value, param.ID);
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
        }
        public static JObject ParseJson(string path)
        {
            Stream stream = CGlobal.Session.SSHClient.ReadFile(path);
            if (stream == null)
                return null;

            StreamReader readerJson = new StreamReader(stream);
            string jsonText = "";
            while (readerJson.Peek() >= 0)
            {
                string line = readerJson.ReadLine();
                if (line != "")
                {
                    jsonText += $"{line}"; ;
                }
            }
            if (jsonText != "")
                return JObject.Parse(jsonText);

            return null;
        }

        public static void SaveJson(string path, JObject o)
        {
            CGlobal.Session.SSHClient.WriteFile(path, StringToStream(o.ToString()));
        }

        public static string CheckNumbs(string value)
        {
            string[] str = value.Split(',');
            value = str[0] + "," + str[1].Remove(4);
            return value;
        }
        public static bool IsRunSwitchOn()
        {
            try
            {
                CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == "RUN_SWITCH_DIAG") as CParam;
                bool isOff = param.ValueString.ToLower() == "true";
                return isOff;
            }
            catch
            {
                return true;
            }
        }
        /// <summary>
        /// Функция отрабатывает единичную SSH команду
        /// </summary>
        /// <param name="cmd"></param>
        static public String ExecuteSingleSSHCommand(String cmd, CSession session = null)
        {
            String error = "";
            return CAuxil.ExecuteSingleSSHCommand(cmd, out error, session);
        }

        /**
         * Функция чтения файла по ftp
         */
        static public String FtpReadFile(String filename)
        {
            Uri serverUri = new Uri(filename);
            WebClient request = new WebClient();
            request.Credentials = new NetworkCredential("ftpincomsystem", "ftpabak3");//user, password
            try
            {
                byte[] newFileData = request.DownloadData(serverUri.ToString());
                System.Text.Encoding a = Encoding.GetEncoding(1251, new EncoderReplacementFallback(" "), new DecoderReplacementFallback(" "));
                return a.GetString(newFileData, 0, newFileData.Length);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }

        /// <summary>
        /// Функция показывает диалоговое окно для сохранения или открытия файла конфигурации
        /// </summary>
        /// <param name="save">если true значит надо сохранить, иначе открыть</param>
        /// <param name="project">если true значит работа с проектом, иначе импорт/экспорт</param>
        /// <returns></returns>
        public static String SelectFileToSaveOrOpen(Boolean save, Boolean project, Boolean isLog = false)
        {
            FileDialog dialog;

            if (save)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.AddExtension = true;
                if (project)
                    saveDialog.DefaultExt = "aprj";
                else if (!project & !isLog)
                    saveDialog.DefaultExt = "acf";
                else
                    saveDialog.DefaultExt = "tar.gz";
                saveDialog.OverwritePrompt = true;
                dialog = saveDialog;
            }
            else
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.CheckFileExists = true;
                dialog = openDialog;
            }
            if (project)
                dialog.Filter = String.Format("{0} (*.aprj)|*.aprj", CGlobal.GetResourceValue("l_configurationFiles"));
            else if (!project & !isLog)
                dialog.Filter = String.Format("{0} (*.acf)|*.acf|{1} (*.acr)|*.acr", CGlobal.GetResourceValue("l_configurationFiles"), CGlobal.GetResourceValue("l_cryptConfigurationFiles"));
            else
                dialog.Filter = String.Format("{0} (*.tar.gz)|*.tar.gz", CGlobal.GetResourceValue("l_controllerFiles"));

            if (dialog.ShowDialog() == true)
                return dialog.FileName;

            return "";
        }
        public static void CreateEncryptedFile(Stream stream, String path)
        {
            //Сжатие полученных данных
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            //Подготовка XML документа
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", "");
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(decl, root);
            XmlNode rootNode = doc.CreateNode("element", "root", "");
            doc.AppendChild(rootNode);
            //Формирование из бинарного массива строки hex символов
            //Формирование файла с резервными настройками
            memoryStream.Position = 0;
            memoryStream.Capacity = Convert.ToInt32(memoryStream.Length);
            String hex = CAuxil.ToHex(memoryStream.GetBuffer());
            //Сохранение hex строки в секцию CDATA XML документа
            XmlCDataSection cdata = doc.CreateCDataSection(hex);
            XmlNode updateNode = doc.CreateNode("element", "config", "");
            updateNode.AppendChild(cdata);
            rootNode.AppendChild(updateNode);
            //Расчёт MD5 суммы
            CXML.appendAttributeToNode(updateNode, "hash", CAuxil.ToHex(MD5.Create().ComputeHash(memoryStream)));
            //Сохранение файла
            doc.Save(path);
        }
        public static void DecryptFile(string path, out string error)
        {
            error = "";
            //Подгрузка документа
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNode rootNode = CXML.getRootNode(doc);
            XmlNode updateNode = rootNode.FirstChild;
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
                error = "Несовпадение хэш суммы";
                return;
            }

            if (!CGlobal.Session.SSHClient.WriteFile("/tmp/backupFile", new MemoryStream(zip)))
            {
                error = "Неудалось загрузить файл в контроллер";
                return;
            }

            CGlobal.Session.SSHClient.ExecuteCommand("tar -xmzf /tmp/backupFile -C /");
            if (CGlobal.Session.SSHClient.LastError != "")
            {
                error = String.Format("{0}: {1}", 
                    CGlobal.GetResourceValue("l_updWindControllerUnzipErr"),
                    CGlobal.Session.SSHClient.LastError);
                return;
            }
        }
        public static string CreatePath(string fileName, string defaultExt, string filter)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.FileName = fileName;
                saveDialog.DefaultExt = defaultExt;
                saveDialog.Filter = filter;
                saveDialog.AddExtension = true;
                if (saveDialog.ShowDialog() != true)
                    return "";

                FileStream file = File.Create(saveDialog.FileName);
                file.Close();
                return saveDialog.FileName;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Ошибка!", MessageBoxButton.OK,
                 MessageBoxImage.Error);
                return "";
            }
        }
        /// <summary>
        /// Функция показывает диалоговое окно для сохранения или открытия файла конфигурации
        /// </summary>
        /// <param name="save">если true значит надо сохранить, иначе открыть</param>
        /// <param name="filter">Фильтр файлов</param>
        /// <returns></returns>
        public static String SelectFileToSaveOrOpen(Boolean save, String filter)
        {
            FileDialog dialog;

            if (save)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.AddExtension = true;
                saveDialog.DefaultExt = "filter";
                saveDialog.OverwritePrompt = true;
                dialog = saveDialog;
            }
            else
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.CheckFileExists = true;
                dialog = openDialog;
            }
            dialog.Filter = String.Format("(*.{0})|*.{0}", filter);
            if (dialog.ShowDialog() == true)
                return dialog.FileName;

            return "";
        }

        public static bool IsColumnExists(string tableName, string columnName)
        {
            string query = $"SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE " +
                $"table_name = '{tableName}' AND column_name = '{columnName}')";
            NpgsqlConnection con = CGlobal.Session.CreateSQLConnection(CGlobal.DBUser, CGlobal.DBPassword, false, true);
            NpgsqlCommand cmd = new NpgsqlCommand(query, con);
            return (bool)cmd.ExecuteScalar();
        }

        /// <summary>
        /// Функция создает буфер заданного размера и из передаваемого буфера копирует в него данные
        /// </summary>
        /// <param name="buff"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] CreateNewBufferAndCopyData(byte[] buff, int length)
        {
            byte[] retBuff = new byte[length];

            for (int i = 0; i < length; i++)
                retBuff[i] = buff[i];

            return retBuff;
        }

        public static Image DrawToolButtonImage(String imageName)
        {
            //ImageSource source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            BitmapImage source = new BitmapImage();
            source.BeginInit();
            String uriString = "pack://application:,,/Resources/" + imageName;
            source.UriSource = new Uri(uriString);
            source.EndInit();

            Image image = new Image();
            image.Width = 24;
            image.Height = 24;
            image.Stretch = Stretch.Uniform;
            image.Source = source;

            return image;
        }

        public static DependencyObject GetDependencyObjectFromVisualTree(DependencyObject startObject, Type type)
        {
            //Walk the visual tree to get the parent(ItemsControl) 
            //of this control
            DependencyObject parent = startObject;
            Type contentType = typeof(ContentPresenter);
            while (parent != null)
            {
                if (type.IsInstanceOfType(parent))
                    break;
                else if (contentType.IsInstanceOfType(parent))
                {
                    //Здесь убирается пространство свободное вакруг элемента, а то некорректно отображается
                    ContentPresenter presenter = parent as ContentPresenter;
                    presenter.Margin = new Thickness(0);
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent;
        }

        /// <summary>
        /// Преобразует полученный массив в строку в которой кажды байт представлен в виде HEX строки
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string StreamToHexString(Stream stream)
        {
            stream.Position = 0;

            StringBuilder hex = new StringBuilder(Convert.ToInt32(stream.Length) * 2);
            int b;
            while ((b = stream.ReadByte()) != -1)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        /// <summary>
        /// Преобразует полученный массив в строку в которой кажды байт представлен в виде HEX строки
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string BuffToHexString(byte[] buff)
        {
            StringBuilder hex = new StringBuilder(Convert.ToInt32(buff.Length) * 2);
            foreach (byte b in buff)
                hex.AppendFormat("{0:x2}", b);

            return hex.ToString();
        }

        public static string ToHex(byte[] data)
        {
            return ToHex(data, "");
        }

        public static string ToHex(byte[] data, string prefix)
        {
            char[] lookup = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            int i = 0, p = prefix.Length, l = data.Length;
            char[] c = new char[l * 2 + p];
            byte d;
            for (; i < p; ++i) c[i] = prefix[i];
            i = -1;
            --l;
            --p;
            while (i < l)
            {
                d = data[++i];
                c[++p] = lookup[d >> 4];
                c[++p] = lookup[d & 0xF];
            }
            return new string(c, 0, c.Length);
        }

        public static byte[] FromHex(string str)
        {
            return FromHex(str, 0, 0, 0);
        }

        public static byte[] FromHex(string str, int offset, int step)
        {
            return FromHex(str, offset, step, 0);
        }

        public static byte[] FromHex(string str, int offset, int step, int tail)
        {
            byte[] b = new byte[(str.Length - offset - tail + step) / (2 + step)];
            byte c1, c2;
            int l = str.Length - tail;
            int s = step + 1;
            for (int y = 0, x = offset; x < l; ++y, x += s)
            {
                c1 = (byte)str[x];
                if (c1 > 0x60) c1 -= 0x57;
                else if (c1 > 0x40) c1 -= 0x37;
                else c1 -= 0x30;
                c2 = (byte)str[++x];
                if (c2 > 0x60) c2 -= 0x57;
                else if (c2 > 0x40) c2 -= 0x37;
                else c2 -= 0x30;
                b[y] = (byte)((c1 << 4) + c2);
            }
            return b;
        }

        /// <summary>
        /// Функция расчитывает контрольную сумму MD5 и возвращает её в виде строки
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static String ComputeMD5(Stream stream)
        {
            stream.Position = 0;
            MD5 md5 = new MD5CryptoServiceProvider();
            String md5_string = "";
            byte[] hashenc = md5.ComputeHash(stream);
            foreach (var b in hashenc)
                md5_string += b.ToString("x2");
            md5.Dispose();
            return md5_string;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader">
        /// Должен быть json или reader
        /// </param>
        /// <param name="key"></param>
        /// <exception cref="JsonException"> 
        /// Не удалось прочитать параметр из файла
        /// </exception>
        /// <exception cref="Exception">
        /// Параметр key не имеет значения
        /// </exception>
        /// <returns></returns>
        public static string GetStrFromReader(object reader, string key, string defValue = null)
        {
            string value = "";
            try
            {
                if (reader is JToken json)
                {
                    value = json[key].ToString();
                }
                if (reader is NpgsqlDataReader pgsql)
                {
                    value = pgsql[key].ToString();
                }
                if (value != "")
                {
                    return value;
                }
            }
            catch
            {
                if (defValue != null)
                {
                    return defValue;
                }

                throw new Exception($"Не удалось считать параметр {key}. ");
            }
            if (defValue != null)
            {
                return defValue;
            }

            throw new Exception($"Параметр {key} не имеет значения");
        }
    }

    public class CItemsList
    {
        protected ArrayList items = new ArrayList();

        public CItemsList()
        {
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        public void RemoveAt(int index)
        {
            this.items.RemoveAt(index);
        }

        public int AddItem(Object obj)
        {
            return this.items.Add(obj);
        }

        public int IndexOf(Object obj)
        {
            return this.items.IndexOf(obj);
        }

        public Object this[int index]
        {
            get
            {
                return this.items[index];
            }
        }
    }

    public class VisibilityConverter : IValueConverter
    {
        #region Члены IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.ToString() == "Visible")
            {
                Boolean visible = (Boolean)value;
                if (visible)
                    return Visibility.Visible;
                else
                    return Visibility.Hidden;
            }
            else if (parameter.ToString() == "Collapsed")
            {
                Boolean visible = (Boolean)value;
                if (visible)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class BindingProxy : Freezable
    {
        #region Overrides of Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object),
                                         typeof(BindingProxy));
    }

    public class DateTimeConverter : IValueConverter
    {
        public static String DateTimeString(DateTime value)
        {
            return String.Format("{0}.{1}", value.ToString("dd.MM.yyyy HH:mm:ss"), value.Millisecond);
        }

        #region Члены IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DateTimeConverter.DateTimeString((DateTime)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        #endregion
    }

    public class EnumToItemsSource : MarkupExtension
    {
        private readonly Type _type;

        public EnumToItemsSource(Type type)
        {
            _type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _type.GetMembers().SelectMany(member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>()).Select(x => x.Description).ToList();
        }
    }

    public class DebugDummyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }


}
   