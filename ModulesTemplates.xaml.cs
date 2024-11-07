using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.XPath;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс работающий с шаблонами модулей
    /// </summary>
    public class CModuleTemplate
    {
        //ID модуля
        private Int32 id;
        //Название модуля
        private String moduleName;
        //Тип модуля
        private UInt32 moduleType;
        //Ревизия модуля
        private UInt32 moduleRevision;
        //Шаблон модуля 
        private XPathDocument doc;

        /// <summary>
        /// Парсит полученный XML документ
        /// </summary>
        /// <param name="doc"></param>
        private void parseDocument(XPathDocument doc)
        {
            XPathNavigator nav = doc.CreateNavigator();

            //Узел с описанием модуля
            XPathNavigator moduleNode = nav.SelectSingleNode("//module");
            //Название модуля
            String name = CXML.getAttributeValue(moduleNode, "name", "");
            if (name == "")
                throw new Exception("В шаблоне не указано имя модуля.");
            this.moduleName = name;
            //Тип модуля
            String type = CXML.getAttributeValue(moduleNode, "type", "");
            if (type == "")
                throw new Exception("В шаблоне не указан тип модуля.");
            this.moduleType = Convert.ToUInt32(type);
            //Ревизия модуля
            String revision = CXML.getAttributeValue(moduleNode, "revision", "");
            if (revision == "")
                throw new Exception("В шаблоне не указана ревизия модуля.");
            this.moduleRevision = Convert.ToUInt32(revision);

            this.doc = doc;
        }

        public CModuleTemplate(XPathDocument doc)
        {
            this.parseDocument(doc);
        }

        public CModuleTemplate(String path)
        {
            XPathDocument doc = new XPathDocument(path);
            this.parseDocument(doc);
        }

        public Int32 ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }

        public String ModuleName
        {
            get
            {
                return this.moduleName;
            }
            set
            {
                this.moduleName = value;
            }
        }

        public UInt32 ModuleType
        {
            get
            {
                return this.moduleType;
            }
            set
            {
                this.moduleType = value;
            }
        }

        public UInt32 ModuleRevision
        {
            get
            {
                return this.moduleRevision;
            }
            set
            {
                this.moduleRevision = value;
            }
        }

        public String XML
        {
            get
            {
                return this.doc.CreateNavigator().InnerXml;
            }
        }

        /// <summary>
        /// Добавление модуля в базу контроллера
        /// </summary>
        public void AddIntoController(NpgsqlConnection connection)
        {
            //Формирование SQL команды для записи шаблона в базу данных
            string sql = "insert into modules_templates(name, type, revision, template) values" +
                $"('{ModuleName}', {ModuleType}, {ModuleRevision}, '{XML}')";
            NpgsqlCommand command = new NpgsqlCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }


    /// <summary>
    /// Окно управления шаблонами модулей контроллера
    /// </summary>
    public partial class ModulesTemplates : Window
    {
        //Список шаблонов
        private ObservableCollection<CModuleTemplate> templatesList = new ObservableCollection<CModuleTemplate>();

        public ModulesTemplates()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Добавление нового шаблона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddToolButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            openDialog.DefaultExt = "xml";
            openDialog.Filter = "Файлы шаблонов модулей (*.xml)|*.xml";
            openDialog.Multiselect = true;

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    foreach (string name in openDialog.FileNames)
                    {
                        CSession session = new CSession();
                        NpgsqlConnection connection = session.CreateSQLConnection(CGlobal.DBUser,
                CGlobal.DBPassword, false, true);
                        //Чтение файла шаблона
                        CModuleTemplate template = new CModuleTemplate(name);

                        string sql = $"delete from modules_templates where(type={template.ModuleType} and revision={template.ModuleRevision})";
                        NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                        command.ExecuteNonQuery();
                        connection.CloseAsync();
                        //Добавление шаблона в контроллер
                        template.AddIntoController(session.Connection);
                    }
                    //Обновление списка шаблонов
                    this.loadTemplates();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_addingTemplate"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Подгрузка списка шаблонов из базы данных
        /// </summary>
        private void loadTemplates()
        {
            this.templatesList.Clear();

            String sql = "select * from modules_templates order by name";
            try
            {
                NpgsqlCommand command = new NpgsqlCommand(sql, CGlobal.Session.Connection);
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    String xml = reader["template"].ToString();
                    try
                    {
                        //Чтение и разбор содержимого XML файла шаблона
                        XPathDocument doc = new XPathDocument(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
                        CModuleTemplate template = new CModuleTemplate(doc);
                        template.ID = Convert.ToInt32(reader["id"]);

                        this.templatesList.Add(template);
                    }
                    catch(Exception ex)
                    {
                        reader.Close();
                        MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_templates"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                reader.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_templates"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            this.loadTemplates();
        }

        private void windowClosed(object sender, EventArgs e)
        {

        }

        public ObservableCollection<CModuleTemplate> TemplatesList
        {
            get
            {
                return this.templatesList;
            }
        }

        private void DeleteTemplate(int id)
        {
            CSession session = new CSession();
            NpgsqlConnection connection = session.CreateSQLConnection(CGlobal.DBUser,
                CGlobal.DBPassword, false, true);
            //Формирование SQL запроса на удаление
            try
            {
                String sql = String.Format("delete from modules_templates where id={0}", id);
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();

                this.loadTemplates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_deleteTemplate"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            connection.CloseAsync();
        }

        private void dataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                StartDeleting();
                this.loadTemplates();
            }
        }

        private void DeleteToolButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            StartDeleting();
        }
        
        private void StartDeleting()
        {
            if (this.templatesGrid.SelectedItems == null)
                return;

            Dictionary<bool, string> ChoosedITems = new Dictionary<bool, string>()
            {
                {true,  "Вы действительно хотите удалить выбранный шаблон"},
                {false,  "Вы действительно хотите удалить выбранные шаблоны"}
            };

            string message = ChoosedITems[this.templatesGrid.SelectedItems.Count == 1];

            if (MessageBox.Show(message, CGlobal.GetResourceValue("l_deleteTemplate"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            //Если удалять шаблон, то коллекция меняется на ходу и привет ошибка
            ObservableCollection<CModuleTemplate> SelectedTemplates = new ObservableCollection<CModuleTemplate>();

            foreach (var item in this.templatesGrid.SelectedItems)
            {
                CModuleTemplate template =  item as CModuleTemplate;
                SelectedTemplates.Add(template);
            }

            foreach (CModuleTemplate template in SelectedTemplates)
            {
                DeleteTemplate(template.ID);
            }
        }
    }
}
