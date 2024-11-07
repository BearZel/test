using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.XPath;
namespace AbakConfigurator
{
    public class CImagesList
    {

        /// <summary>
        /// Код COMM-платы
        /// </summary>
        public const uint CommType = 0;
        /// <summary>
        /// Список продуктов
        /// </summary>
        private List<CImage> products = null;
        public CImagesList()
        {
            products = new List<CImage>();
        }
        /// <summary>
        /// Возвращает информацию о продукте по диапазону
        /// </summary>
        /// <param name="adc"></param>
        /// <returns>
        /// ProductData
        /// </returns>
        public List<CImage> GetCompatibleProductByAdc(uint adc)
        {
            return products.Where(x => x.IsInAdcRange(adc)).ToList();
        }
        /// <summary>
        /// Функция преобразует версию железа в double.
        /// Для К3.1 отсекается часть COMM-платы
        /// </summary>
        /// <param name="hardware"></param>
        /// <param name="moduleVesrsion"></param>
        /// <returns>
        /// double
        /// </returns>
        private double GetHardwareInDouble(string hardware, ProductVersion moduleVesrsion)
        {
            //Проверка только для К3.1, потому что К2 одноплатный модуль
            if (moduleVesrsion == ProductVersion.K31)
            {
                //XX.YY.ZZ.WW, где XX.YY - Мажор и минор COMM-платы, ZZ.WW - Мажор и минор Канальной платы (нужна она)
                hardware = hardware.Remove(0, 6);
            }

            return Convert.ToDouble(CAuxil.AdaptFloat(hardware));
        }
        /// <summary>
        /// Возвращает все прошивки по коду ацп и железу
        /// </summary>
        /// <param name="adc"></param>
        /// Код ацп модуля
        /// <param name="hardware">
        /// Аппаратная версия модуля
        /// </param>
        /// <param name="moduleVesrsion">
        /// Версия модуля (нужна, потому что версии железа могут совпадать)
        /// </param>
        /// <returns></returns>
        public List<CImage> GetImagesByAdcAndHard(uint adc, string hardware, ProductVersion moduleVesrsion)
        {
            double hw = GetHardwareInDouble(hardware, moduleVesrsion);
            return products.Where(x => x.IsInAdcRange(adc) && x.HardwareList.Contains(hw)).ToList();
        }
        /// <summary>
        /// Возвращает все прошивки по коду продукта
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns>
        /// ProductData
        /// </returns>
        public List<CImage> GetImagesByCode(string productCode)
        {
            return products.Where(x => x.IsModuleTypeEqual(productCode)).ToList();
        }
        /// <summary>
        /// Добавить информацию о продукте
        /// </summary>
        /// <param name="product"></param>
        public void AddProduct(CImage product)
        {
            if (!products.Contains(product))
            {
                products.Add(product);
            }
        }
        /// <summary>
        /// Метод копирует список коллекций
        /// </summary>
        /// <param name="list"></param>
        public void CopyList(List<CImage> list)
        {
            products = list;
        }
    }

    /// <summary>
    /// Класс описывающий образ обновления модуля
    /// </summary>
    public class CImage : ICloneable
    {
        private class AdcRange
        {
            //1 - для COMM плат
            private uint low = 1;
            private uint high = 1;
            /// <summary>
            /// Функция заполняет диапазон кодов АЦП
            /// </summary>
            /// <param name="adcRange"></param>
            public void SetRange(string adcRange)
            {
                string[] tmp = adcRange.Split(',');

                //Точки и запятые только у COMM платы
                if (tmp.Any(value => CAuxil.CheckStringForInt(value, true)))
                {
                    low = uint.Parse(tmp[0]);
                    high = uint.Parse(tmp[1]);
                }
            }
            public bool CheckRange(uint adc)
            {
                return adc >= low && adc <= high;
            }
            public uint Low => low;
            public uint High => high;
        }
        /// <summary>
        /// Диапазон кодов АЦП для проверки
        /// </summary>
        private AdcRange adcRange = new AdcRange();
        private string hash = "";
        private string type = "";
        private int id = 1;
        private string name;
        private string version;
        private string link;
        private string hard_adc = "";
        private int subtype = -1;
        private string hardware = "-";
        private string product = "";
        private string orderCode = "";
        private string filePath = CGlobal.AbakPath + "/modules";
        /// <summary>
        /// Код продукта (артикул)
        /// </summary>
        public string OrderCode => orderCode;
        /// <summary>
        /// Имя модуля
        /// </summary>
        public string Name => name;
        /// <summary>
        /// Версия образа
        /// </summary>
        public string Version { get => version; set => version = value; }
        /// <summary>
        /// Тип продукта (К2/К3/COMM)
        /// </summary>
        public string Product => product;
        /// <summary>
        /// id образа
        /// </summary>
        public int ID => id;
        /// <summary>
        /// Подтип модуля 
        /// -1 - К2
        /// 0 - К3
        /// 10 - К3.1
        /// </summary>
        public int SubType => subtype;
        /// <summary>
        /// Путь к образу в линуксе
        /// </summary>
        public string Link => link;
        /// <summary>
        /// Диапазон кодов АЦП
        /// </summary>
        public string HardAdc => hard_adc;
        /// <summary>
        /// Аппаратная версия
        /// </summary>
        public List<double> HardwareList
        {
            get
            {
                List<string> list = hardware.Split(',').ToList();
                return list.Select(hard => Convert.ToDouble(CAuxil.AdaptFloat(hard.ToString()))).ToList();
            }
        }
        public string Hardware => hardware;
        /// <summary>
        /// ХЭШ образа
        /// </summary>
        public string Hash => hash;
        /// <summary>
        /// Тип модуля
        /// </summary>
        public string Type => type;
        //private Dictionary<string, int> productSubtype = new Dictionary<string, int>()
        //{
        //    {"k2", -1}, {"k3",}
        //};
        public void LoadFromDB(NpgsqlDataReader reader)
        {
            name = CAuxil.GetStrFromReader(reader, "name");
            type = CAuxil.GetStrFromReader(reader, "type");
            version = CAuxil.GetStrFromReader(reader, "version");
            id = Convert.ToInt32(CAuxil.GetStrFromReader(reader, "id"));
            link = CAuxil.GetStrFromReader(reader, "link");
            hard_adc = CAuxil.GetStrFromReader(reader, "hard_adc");
            hardware = CAuxil.GetStrFromReader(reader, "hardware");
            adcRange.SetRange(hard_adc);
            orderCode = CAuxil.GetStrFromReader(reader, "order_code", "");
            //На будущее если будут новые изменения или типы/продукты
            string[] str = name.ToLower().Split('.');
            Dictionary<string, Delegate> product = new Dictionary<string, Delegate>();
            product["k3"] = new Action<string>(SetparamsForChannelK3);
            product["comm"] = new Action<string>(SetparamsForCom);
            product["k2"] = new Action<string>(SetparamsForK2);
            if (product.ContainsKey(str[0]))
            {
                product[str[0]].DynamicInvoke(name);
            }
        }
        /// <summary>
        /// Функция возвращает код АЦП/версию железа, в зависимости от dataType
        /// </summary>
        /// <param name="jToken"></param>
        /// <param name="dataType"></param>
        /// <returns>
        /// string
        /// </returns>
        /// <exception cref="Exception"></exception>
        private string GetHardOrADC(JToken jToken, string dataType)
        {
            try
            {
                JArray jArray = (JArray)jToken[dataType];
                string values = jArray[0].ToString();
                for (int i = 1; i < jArray.Count; i++)
                {
                    values += $",{jArray[i]}";
                }
                return values;
            }
            catch
            {
                throw new Exception($"Не удалось обработать массив {dataType}");
            }
        }
        /// <summary>
        /// Считать данные об образе из json
        /// </summary>
        /// <param name="jToken"></param>
        /// <param name="counter">
        /// В новых коллекциях нет порядкого номера (наныли стмщики)
        /// </param>
        public void LoadFromJSOn(JToken jToken)
        {
            name = CAuxil.GetStrFromReader(jToken, "name");
            type = CAuxil.GetStrFromReader(jToken, "module");
            version = CAuxil.GetStrFromReader(jToken, "version");
            hash = CAuxil.GetStrFromReader(jToken, "hash");
            //Если ком-плата (версия = 0), то в базу записываются допустимые версии платы.
            //Если канальная плата, то пишем диапазон кодов АЦП
            hardware = GetHardOrADC(jToken, "hardware");
            //У COMM-платы код ацп всегда совпадает с версией железа
            hard_adc = type != "0" ? GetHardOrADC(jToken, "adc") : hardware;
            adcRange.SetRange(hard_adc);
            //Для COMM-платы нет артикула
            orderCode = type == "0" ? "" : CAuxil.GetStrFromReader(jToken, "order_code", "");
            link = $"{filePath}/{name}_{orderCode}_{version}";
            id = GetHashCode();
        }
        public object Clone()
        {
            CImage image = new CImage();
            image.Assign(this);

            return image;
        }
        public void Assign(CImage image)
        {
            type = image.Type;
            id = image.ID;
            name = image.Name;
            version = image.Version;
            link = image.Link;
            hard_adc = image.HardAdc;
            subtype = image.SubType;
            product = image.Product;
            orderCode = image.OrderCode;
            hardware = image.GetHardwareAsString();
        }
        /// <summary>
        /// Функция возвращает список версий железа в виде строки
        /// </summary>
        /// <returns>
        /// string
        /// </returns>
        private string GetHardwareAsString()
        {
            return hardware;
        }
        /// <summary>
        /// Функция проверяет в диапазоне ли код АЦП
        /// </summary>
        /// <param name="adc"></param>
        /// <returns>
        /// bool
        /// </returns>
        public bool IsInAdcRange(uint adc)
        {
            return adcRange.CheckRange(adc);
        }
        /// <summary>
        /// Функция проверяет соовпадают ли коды продуктов
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public bool IsModuleTypeEqual(string productCode)
        {
            return productCode == type;
        }
        /// <summary>
        /// Функция выставляет продукт и подтип
        /// </summary>
        private void SetProductSubtype()
        {
            //Формат - 
            //if(name.cont)
            //string[] str = name.ToLower().Split('.');
            //switch (str[0])
            //{
            //    case "k2":
            //        product = "k2";
            //        break;

            //    case "k3";
            //        string[] str = name.Split('.');
            //        if (Convert.ToInt32(str[2]) < 9)
            //            subtype = 0;
            //        else
            //            subtype = 10;

            //        product = "k3";
            //        break;

            //}
        }
        private void SetparamsForK2(string name)
        {
            product = "k2";
        }
        private void SetparamsForCom(string name)
        {
            subtype = 0;
            product = "k3";
        }
        private void SetparamsForChannelK3(string name)
        {
            string[] str = name.Split('.');
            if (Convert.ToInt32(str[2]) < 9)
                subtype = 0;
            else
                subtype = 10;

            product = "k3";
        }


        public override bool Equals(object obj)
        {
            if (obj is CImage image)
            {
                return GetHashCode() == image.GetHashCode();
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() + Version.GetHashCode() + HardAdc.GetHashCode() + HardwareList.GetHashCode();
        }
    }

    /// <summary>
    /// Окно управления списком обновлений
    /// </summary>
    public partial class UploadImageWindow : Window
    {
        //
        private const string tagCollection = "MODULES_COLLECTION_VERSION";
        //Словарь хранит ключ - hash, значение - имя файла
        //hash ключ, потому что в json есть только он. Имена файлов могут меняться
        private Dictionary<string, Stream> filesHash = new Dictionary<string, Stream>();
        //Версия коллекции
        private double collectionVer = 0.0;
        //Пароль для распаковки образа
        private const String password = "module";
        //Флаг об изменении
        public bool IsChanged = false;
        //Список образов с прошивками плат
        private List<CImage> imagesList = new List<CImage>();
        //Временный список образов с прошивками плат
        private List<CImage> tmpImagesList = new List<CImage>();
        /// <summary>
        /// Временный список шаблонов
        /// </summary>
        private List<CModuleTemplate> templates = new List<CModuleTemplate>();
        public UploadImageWindow()
        {
            this.LoadFromDB();
            InitializeComponent();
            SetModulesImages();
        }
        /// <summary>
        /// Функция загрузки информации о прошивках плат, хранящихся в контроллере
        /// </summary>
        private void LoadFromDB()
        {
            this.imagesList.Clear();

            String sql = "select * from module_update_table";

            SelectDelegate select = (reader) =>
            {
                CImage image = new CImage();
                image.LoadFromDB(reader);
                this.imagesList.Add(image);

            };
            CPostgresAuxil.Select(CGlobal.Session.Connection, select, sql);
        }
        private async void AddButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.DefaultExt = "mimg";
            dialog.Filter = String.Format("{0} (*.mimg)|*.mimg", CGlobal.GetResourceValue("l_configurationFiles"));
            if (dialog.ShowDialog() != true)
                return;

            WaitingWindow waitingWindow = new WaitingWindow();
            bool isCollectionOK = false;
            try
            {
                waitingWindow.Show();
                ZipFile zip = ZipFile.Read(dialog.FileName);
                //Информационный json файл
                JObject jObject = this.GetInfoObject(zip);
                //Версия коллекции
                SetCollectionVersion(jObject);
                //Выгружаем во временные списки для безопасности
                LoadSortFiles(zip);
                //Получаем sql команду
                string sql = FillSqlCmdFromjson(jObject);
                isCollectionOK = sql != "";
                if (!isCollectionOK)
                {
                    return;
                }
                //Грузим в ЦПУ
                await UpdateAndUpload(sql);
                imagesList = tmpImagesList;
                //Прошивки плат => прошивки модулей
                SetModulesImages();
                //Обновляется тег в базе
                CAuxil.SetTagValue(collectionVer.ToString(), tagCollection);
                IsChanged = true;
                MessageBox.Show("Для применения настроек необходимо перезапустить службы контроллера или " +
                    "модули ввода/вывода",
                   "Обновление коллекции", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string msg = !isCollectionOK ? "Ошибка в коллекции прошивок! {0}" :
                    "Во время загрузки произошла ошибка!" +
                    "\nОшибка: {0}." +
                    "\nРекомендуется повторить процедуру обновления коллекции";
                MessageBox.Show(string.Format(msg, ex.Message), this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            waitingWindow?.Close();
        }
        /// <summary>
        /// Извлекает из архива описательный JSON файл
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        private JObject GetInfoObject(ZipFile zip)
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
                throw new Exception($"Не удалось прочитать json файл. Ошибка:{ex.Message}");
            }
        }
        private void SetCollectionVersion(JObject jObject)
        {
            string tmp = CAuxil.GetStrFromReader(jObject, "version");
            double.TryParse(CAuxil.AdaptFloat(tmp), out collectionVer);
            if (collectionVer == 0)
            {
                throw new Exception("Не удалось прочитать версию версию коллекции");
            }
        }
        /// <summary>
        /// Функция выгружает xml-файлы и образы во временные спписки
        /// </summary>
        /// <param name="zip"></param>
        private void LoadSortFiles(ZipFile zip)
        {
            templates.Clear();
            tmpImagesList.Clear();
            filesHash.Clear();
            foreach (ZipEntry file in zip)
            {
                MemoryStream ms = new MemoryStream();
                ZipEntry fileEntry = zip[file.FileName];
                fileEntry.ExtractWithPassword(ms, password);
                ms.Position = 0;
                if (file.FileName.Contains("xml") && !file.FileName.Contains("json"))
                {
                    FillTemplatesTmp(ms, file.FileName);
                }
                else
                {
                    FillHashTmp(ms, file.FileName);
                }
            }
        }
        /// <summary>
        /// Функция формирует sql команду на основе данных из json
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns>
        /// sql cmd
        /// </returns>
        private string FillSqlCmdFromjson(JObject jObject)
        {
            string sql = "";
            IJEnumerable<JToken> json = collectionVer > 120 ? jObject.Last.Values() : jObject.Values();
            foreach (JToken jToken in json.Where(x => x.HasValues))
            {
                //Преобразование токена, они будут отличаться в старых и новых коллекциях
                JToken jt = collectionVer > 120 ? jToken : jToken.First;
                try
                {
                    CImage cImage = new CImage();
                    cImage.LoadFromJSOn(jt);
                    //Сохранение имён для сообщений об ошибке
                    if (!filesHash.ContainsKey(cImage.Hash))
                    {
                        throw new Exception($"Не найдены образы с хэшом {cImage.Hash}!");
                    }
                    tmpImagesList.Add(cImage);
                    sql += "insert into module_update_table(" +
                    "id, " +
                    "info, " +
                    "link, " +
                    "name, " +
                    "type, " +
                    "version, " +
                    "hardware, " +
                    "hard_adc, " +
                    "order_code " +
                    ") " +
                    "values (" +
                    $"'{cImage.ID}'," +
                    $"'{jt}'," +
                    $"'{cImage.Link}'," +
                    $"'{cImage.Name}'," +
                    $"'{cImage.Type}'," +
                    $"'{cImage.Version}'," +
                    $"'{cImage.Hardware}'," +
                    $"'{cImage.HardAdc}'," +
                    $"'{cImage.OrderCode}'" +
                    $");";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{ex.Message}\nОшибка в образе для токена \n{jt}", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return "";
                }
            }
            sql = "delete from module_update_table\n; delete from modules_templates;\n" + sql;
            return sql;
        }
        /// <summary>
        /// Обновляются все списки, данные в базе и линуксе
        /// </summary>
        /// <param name="sqlCmd"></param>
        private async Task UpdateAndUpload(string sqlCmd)
        {
            await Task.Run(() => UpdateDB(sqlCmd));
            await Task.Run(() => WriteImagesIntoController());
        }
        /// <summary>
        /// 
        /// </summary>
        private void UpdateDB(string sqlCmd)
        {

            CSession session = new CSession();
            CPostgresAuxil.ExecuteNonQuery(session.Connection, sqlCmd);
            foreach (CModuleTemplate template in templates)
            {
                template.AddIntoController(session.Connection);
            }
        }
        /// <summary>
        /// Загрузка файлов с прошивкой в контроллер
        /// </summary>
        /// <param name="image"></param>
        /// <param name="path"></param>
        private void WriteImagesIntoController()
        {

            CGlobal.Session.SSHClient.ExecuteCommand("rm /opt/abak/A:/modules/*");
            foreach (CImage cImage in tmpImagesList)
            {
                if (!CGlobal.Session.SSHClient.WriteFile(cImage.Link, filesHash[cImage.Hash]))
                {
                    throw new Exception($"Не удалось загрузить образ для {cImage.Name}/{cImage.OrderCode}");
                }
            }
        }
        /// <summary>
        /// Выгружает из zip шаблоны во временный список
        /// </summary>
        /// <param name="zip"></param>
        private void FillTemplatesTmp(MemoryStream ms, string fileName)
        {
            try
            {
                XPathDocument doc = new XPathDocument(ms);
                CModuleTemplate template = new CModuleTemplate(doc);
                templates.Add(template);
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось обработать шаблон {fileName}. Ошибка: {ex.Message}");
            }
        }
        List<string> list = new List<string>();
        private void FillHashTmp(MemoryStream ms, string fileName)
        {

            string md5 = CAuxil.ComputeMD5(ms);
            //До 120 такое было нормой, а сейчас считаем багом в коллекции
            if (!filesHash.ContainsKey(md5))
            {
                ms.Position = 0;
                filesHash.Add(CAuxil.ComputeMD5(ms), ms);
            }
            else
            {
                if (collectionVer > 120)
                {
                    list.Add(fileName);
                    throw new Exception($"Не удалось добавить файл {fileName}. " +
                        $"Hash {md5} уже добавлен в список для модуля {filesHash[md5]}");
                }
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            this.LoadFromDB();
        }
        /// <summary>
        /// Функция объеденяет прошивки плат в прошивки модулей
        /// </summary>
        private void SetModulesImages()
        {
            List<CImage> commImages = imagesList.Where(image => image.Name.ToLower().Contains("comm")).ToList();
            List<CImage> channelImages = imagesList.Where(image => image.Name.ToLower().Contains("k3")).ToList();
            //k2 просто копируем, у них прошивика платы = прошиваке модуля
            ModulesImages = new ObservableCollection<CImage>(imagesList.Where(image => image.Name.ToLower().Contains("k2")));
            foreach (CImage image in channelImages)
            {
                string[] splittedChannelVersion = image.Version.Split('.');
                //Первые две цифры - совместимость с ком-платой
                int channelVersion = Convert.ToInt32(splittedChannelVersion[0] + splittedChannelVersion[1]);
                foreach (CImage commImage in commImages)
                {
                    string[] splittedComVersion = commImage.Version.Split('.');
                    int commVersion = Convert.ToInt32(splittedComVersion[0] + splittedComVersion[1]);
                    //Должны быть только одного подтипа
                    if (commImage.SubType != image.SubType)
                        continue;

                    if (channelVersion > commVersion)
                        continue;

                    CImage copyImage = image.Clone() as CImage;
                    copyImage.Version = splittedComVersion[0] + "." + splittedComVersion[1] +
                        "." + splittedChannelVersion[2] + "." + splittedChannelVersion[3];


                    ModulesImages.Add(copyImage);
                }
            }

            dataGrid.ItemsSource = ModulesImages;
        }
        /// <summary>
        /// Список прошивок модуля (COMM-плата + Канальная плата)
        /// </summary>
        public ObservableCollection<CImage> ModulesImages = new ObservableCollection<CImage>();
        /// <summary>
        /// //Функция возвращает список прошивок плат
        /// </summary>
        /// <returns>
        /// List<CImage>
        /// </returns>
        public List<CImage> GetImages()
        {
            return imagesList;
        }
    }
}
