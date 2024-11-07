using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using System.Xml.XPath;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс описывающий образ обновления модуля
    /// </summary>
    public class CImage
    {
        //Идентификатора в базе данных
        private Int32 id;
        //Название модуля, понятное человеку
        private String name;
        //Тип модуля
        private UInt32 type;
        //Хэш сумма образа
        private String md5;
        //Версия ПО в образе
        private String version;
        //Путь к бинарнику в контроллере
        private String link;
        //Версия железа/кода ацп
        private String hard_adc = "";
        //Список версий ПО которые можно обновлять
        private List<String> hard_adc_list;
        //Список версий плат которые можно обновлять


        /// <summary>
        /// Загрузка настроек прошивки для обновления модуля
        /// </summary>
        /// <param name="reader">postgresql reader для получения информации</param>
        public void Load(NpgsqlDataReader reader)
        {
            this.id = Convert.ToInt32(reader["id"]);
            this.name = Convert.ToString(reader["name"]);
            this.type = Convert.ToUInt32(reader["type"]);
            this.version = Convert.ToString(reader["version"]);
            this.link = Convert.ToString(reader["link"]);
            this.hard_adc = Convert.ToString(reader["hard_adc"]);
        }

        public String Name => this.name;
        public String Version => this.version;
        public Int32 ID => this.id;
        public String Link => this.link;
        public String Hardware => this.hard_adc;
    }

    /// <summary>
    /// Окно управления списком обновлений
    /// </summary>
    public partial class ModulesUpdatesWindow : Window
    {
        //Пароль для распаковки образа
        private const String password = "module";
        //Путь к папке в которой храняться прошивки модулей
        private String images_path = String.Format("{0}/modules", CGlobal.AbakPath);
        //Флаг об изменении
        public bool IsChanged = false;
        //Список образов с прошивками модулей
        private ObservableCollection<CImage> imagesList = new ObservableCollection<CImage>();
        public ModulesUpdatesWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Функция загрузки информации о прошивках модулей, хранящихся в контроллере
        /// </summary>
        private void Load()
        {
            this.imagesList.Clear();

            String sql = "select * from module_update_table";

            SelectDelegate select = (reader) =>
            {
                CImage image = new CImage();
                try
                {
                    image.Load(reader);
                    this.imagesList.Add(image);
                }
                catch
                {
                    //Неудача, в список не добавляется
                    image = null;
                }
            };
            CPostgresAuxil.Select(CSession.Session.Connection, select, sql);
        }

        /// <summary>
        /// Извлекает из архива описательный JSON файл
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private JObject getInfoObject(ZipFile zip)
        {
            try
            {
                ZipEntry info = zip["image.json"];
                MemoryStream ms = new MemoryStream();
                info.ExtractWithPassword(ms, password);
                ms.Position = 0;

                StreamReader sr = new StreamReader(ms);
                return JObject.Parse(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageDescriptive"), ex.Message); //в описательной части
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Извлекает из архива образ прошивки
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private MemoryStream getModuleImage(ZipFile zip, String md5, string image_name)
        {
            try
            {
                MemoryStream update = new MemoryStream();
                ZipEntry image_entry = zip[image_name];
                image_entry.ExtractWithPassword(update, password);

                //Проверка md5 контрольной суммы
                String md5_string = CAuxil.ComputeMD5(update);
                if (md5 != md5_string)
                    throw new Exception(CGlobal.GetResourceValue("l_messageInvalidHash")); //Несовпадение хэш суммы!

                update.Position = 0;
                return update;
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageUpdateFile"), ex.Message); //в файле обновления
                throw new Exception(message);
            }
        }

        private CModuleTemplate getModuleTemplate(ZipFile zip, String templateName)
        {
            CModuleTemplate result = null;
            try
            {
                ZipEntry templateEntry = zip[templateName];
                if (templateEntry == null)
                    return result;

                MemoryStream ms = new MemoryStream();
                templateEntry.ExtractWithPassword(ms, password);

                //Тестовая загрузка шаблона
                ms.Position = 0;
                XPathDocument doc = new XPathDocument(ms);

                result = new CModuleTemplate(doc);

            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageTemplateFile"), ex.Message); //в шаблоне модуля
                throw new Exception(message);
            }

            return result;
        }

        /// <summary>
        /// Загрузка файла с прошивкой в контроллер
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        private void writeImageIntoController(Stream image, String path)
        {
            image.Position = 0;
            try
            {
                CSession.Session.SSHClient.WriteFile(path, image, true);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageOnLoaImagedFile"), ex.Message); //при загрузке образа модуля в контроллер
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Добавление шаблона модуля в контроллер
        /// </summary>
        /// <param name="template"></param>
        private void addTemplateIntoContrioller(CModuleTemplate template)
        {
            if (template == null)
                return;

            try
            {
                template.AddIntoController(CSession.Session.Connection);
            }
            catch (Exception ex)
            {
                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageOnLoadTemplateFile"), ex.Message); //при загрузке шаблона модуля в контроллер
                throw new Exception(message);
            }

        }

        private void LoadXml(ZipFile zip)
        {
            foreach (var file in zip)
            {
                string [] xml = file.FileName.Split('.');
                if (!xml.Contains("xml"))
                    continue;

                CModuleTemplate template = this.getModuleTemplate(zip, file.FileName);
                //Загрузка шаблона модуля в контроллер
                this.addTemplateIntoContrioller(template);
            }
        }
        
        private string CheckName(String fileName)
        {
            string title = "Ошибка загрузки шаблона";
            string message = "Не удалось загрузить шаблон.";
            string[] moduleName = fileName.Split('.');
            if (moduleName.Length != 4)
            {
                MessageBox.Show(title, $"{message} Неверная длина строки!", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }

            List<char> range = new List<char>() { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            //Проверка букв
            for (int i = 0; i < 2; i++)
            {
                bool letters = !range.Contains(moduleName[i][0]) && !range.Contains(moduleName[i][1]);
                if (letters)
                    continue;

                MessageBox.Show(title, $"{message} Неверный формат строки!", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }

            //Проверка цифр
            for (int i = 2; i < 4; i++)
            {
                bool numbers = range.Contains(moduleName[i][0]) && range.Contains(moduleName[i][1]);

                if (numbers)
                    continue;

                MessageBox.Show(title, $"{message} Неверный формат строки!", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";

            }
            return fileName;
        }

        private void AddButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.DefaultExt = "mimg";
            dialog.Filter = String.Format("{0} (*.mimg)|*.mimg", CGlobal.GetResourceValue("l_configurationFiles"));

            if (dialog.ShowDialog() != true)
                return;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                try
                {
                    using (ZipFile zip = ZipFile.Read(dialog.FileName))
                    {
                        LoadXml(zip);
                        //Информационный json файл
                        JObject info_obj = this.getInfoObject(zip);
                        int counter = Convert.ToInt32(info_obj["count"]);
                        for (int i = 1; i < counter + 1; i++)
                        {
                            //Для удобства
                            int module_type = Convert.ToInt32(info_obj[i.ToString()][0]["module"]);
                            string module_name = info_obj[i.ToString()][0]["name"].ToString();
                            string version = info_obj[i.ToString()][0]["version"].ToString();
                            string image_name = module_name + "-" + version + "-" + i.ToString();
                            //Сама прошивка
                            MemoryStream update = this.getModuleImage(zip, info_obj[i.ToString()][0]["hash"].ToString(), image_name);
                            //Всё прогрузили, теперь надо залить в контроллер
                            //Загрузка по SSH файла в контроллер, файл сохраняется под имененм шаблона и версии
                            String path = String.Format("{0}/{1}-{2}-{3}", this.images_path, module_name, version, i.ToString());
                            this.writeImageIntoController(update, path);
                            //Формирование записи в таблице с обновлениями
                            try
                            {
                                List<String> sql_list = new List<string>();
                                //Удаление такой записи, если есть
                                String sql = String.Format("delete from module_update_table where type={0} and version='{1}'", module_type, version);
                                sql_list.Add(sql);
                                sql = "insert into module_update_table(info, link, name, type, version)";
                                sql = String.Format("{0} values('{1}','{2}','{3}',{4},'{5}')", sql, info_obj[i.ToString()][0].ToString(), path, module_name, module_type.ToString(), version);
                                sql_list.Add(sql);
                                CPostgresAuxil.ExecuteNonQuery(CSession.Session.Connection, sql_list);
                                HardOrADC(info_obj, i, module_type, version);
                            }
                            catch (Exception ex)
                            {
                                String message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageUpdateWriting"), ex.Message); //при формировании записи об обновлени
                                throw new Exception(message);
                            }
                            //Обновление списка
                            this.Load();
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Произошла ошибка при распаковке архива
                    String message = String.Format("{0} {1}", CGlobal.GetResourceValue("l_messageImageProcessingError"), ex.Message); //Ошибка обработки образа
                    MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
            IsChanged = true;
        }
        
        private void HardOrADC(JObject info_obj, int counter, int module_type, string version)
        {
            string act;
            //Если ком-плата (версия = 0), то в базу записываются допустимые версии платы.
            //Если канальная плата, то пишем диапазон кодов АЦП
            if (module_type != 0)
                act = "adc";
            else
                act = "hardware";

            JArray items = (JArray)info_obj[counter.ToString()][0][act];
            string values = "";
            
            for (int length = 0; length < items.Count; length++)
            {
                if(items.Count - length != 1 && items.Count - length != 0)
                    values = values + $"{info_obj[counter.ToString()][0][act][length]}" + ",";
                else
                    values = values + $"{info_obj[counter.ToString()][0][act][length]}";
            }
            string sql = $"update module_update_table set hard_adc = '{values}' where version='{version}' and type = {module_type} ";
            CPostgresAuxil.ExecuteNonQuery(CSession.Session.Connection, sql);
        }

        private void RemoveButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            StartDeleting();
        }

        private void DeleteImage(CImage image)
        {
            try
            {
                //Удаление файла
                CGlobal.Session.SSHClient.ExecuteCommand(String.Format("rm {0}", image.Link));

                //Удаление записи из таблицы
                String sql = String.Format("delete from module_update_table where id={0}", image.ID);
                CPostgresAuxil.ExecuteNonQuery(CSession.Session.Connection, sql);

                this.Load();
            }
            catch (Exception ex)
            {
                string message = String.Format("{0}: {1}", CGlobal.GetResourceValue("l_messageDeleteImageError"), ex.Message); //Ошибка при удалении обновления модуля из контроллера
                MessageBox.Show(message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            this.Load();
        }

        public ObservableCollection<CImage> ImagesList => this.imagesList;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Load();
        }

        private void dataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var dataGrid = (DataGrid)sender;
                StartDeleting();
                this.Load();
            }
        }
        private void DeleteToolButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            StartDeleting();
        }

        private void StartDeleting()
        {
            if (this.dataGrid.SelectedItems == null)
                return;

            Dictionary<bool, string> ChoosedITems = new Dictionary<bool, string>()
            {
                {true,  "Вы действительно хотите удалить выбранный образ"},
                {false,  "Вы действительно хотите удалить выбранные образы"}
            };

            string message = ChoosedITems[this.dataGrid.SelectedItems.Count == 1];

            if (MessageBox.Show(message, CGlobal.GetResourceValue("l_deleteTemplate"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            //Если удалять шаблон, то коллекция меняется на ходу и привет ошибка
            ObservableCollection<CImage> SelectedTemplates = new ObservableCollection<CImage>();

            foreach (var item in this.dataGrid.SelectedItems)
            { 
                CImage template = item as CImage;
                SelectedTemplates.Add(template);
            }

            foreach (CImage template in SelectedTemplates)
                DeleteImage(template);
        }
    }
}
