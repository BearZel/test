using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Npgsql;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AbakConfigurator.Secure;

namespace AbakConfigurator
{
    public class SaveIP
    {
        private static string IP;
        private static bool? IsUSB = false;
        static SaveIP()
        {
            IP = "";
        }

        public static void SetIP(string IPadr)
        {
            IP = IPadr;
        }

        public static string GetIP()
        {
            return IP;
        }

        public static void SetUSB(bool? usb)
        {
            IsUSB = usb;
        }
        public static bool? GetUSB()
        {
            return IsUSB;
        }
    }
    /// <summary>
    /// Класс описывающий альтернативное значение параметра
    /// </summary>
    public class COption
    {
        //Локализация 
        private string locale;
        //Значение для опции
        private String value;

        public string Locale { get => locale; set => locale = value; }
        public string Value { get => value; set => this.value = value; }
        public string GuiValue
        {
            get
            {
                return CConfig.Localization.GetLocaleValue(this.locale);
            }
        }
    }

    /// <summary>
    /// Набор опций
    /// </summary>
    public class COptionSet
    {
        //Имя записи в базе
        private String name;
        //Флаг того что опция не должна сохраняться в конфигурации
        private Boolean temporary = false;
        //Флаг учёта регистра значений
        private Boolean case_sensitive = false;

        private Dictionary<String, COption> options = new Dictionary<string, COption>();

        public Dictionary<String, COption> Options
        {
            get
            {
                return this.options;
            }
        }

        public string Name { get => name; set => name = value; }
        public Boolean CaseSensitive { get => case_sensitive; set => case_sensitive = value; }
        private Boolean Temporary { get => temporary; set => temporary = value; }
    }

    /// <summary>
    /// Тип данных параметра
    /// </summary>
    public enum ParamType
    {
        BOOLEAN = 0,
        INT = 1,
        UINT = 2,
        FLOAT = 3,
        STRING = 4
    }

    /// <summary>
    /// Базовый параметр
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class CBaseParam : INotifyPropertyChanged, ICloneable
    {
        //Флаг показывающий что параметр конфигурируемый
        protected Boolean config;
        //Флаг только ПЛК3
        protected Boolean plc3only;
        //Флаг только ПЛК3
        protected Boolean plc31rockchiponly;
        //Флаг только ПЛК3.1
        protected Boolean plc31only;
        //Маска флагов групп ПЛК
        protected uint plcuse;
        //Флаг что параметр берётся с другого ЦПУ
        protected Boolean diagnosticToPair;
        //Флаг невидимого параметра
        protected Boolean visible;
        //Флаг только чтение
        protected Boolean readOnly;
        //Флаг отображения hex
        protected Boolean hex;
        //Флаг отображения версии
        protected Boolean isversion;
        //Тип параметра
        protected ParamType type;
        //Название параметра
        protected String name;
        //Ключ имени параметра в таблице локализации, за это берётся название параметра в английской локали
        protected String nameKey;
        //Имя группы которой принадлежит тег
        protected String groupName;
        //Ссылка на группу которой принадлежит параметр
        protected CBaseGroup group;
        //Флаг изменения значения из конфигуратора
        protected Boolean manualChanged = false;
        //Текущее значение параметра
        protected object value;
        //Значение параметра на запись
        protected object writeValue;
        //Название тега
        protected String tagname;
        //Указатель на опции значений параметра
        protected COptionSet options = null;
        //Флаг лимитов в шаблоне
        protected bool limit;
        //Нижнее значение лимита (если есть)
        protected int lowLimit;
        //Верхнее значение лимита (если есть)
        protected int hiLimit;
        //Флаг выхода за диапазон (для старых систем)
        protected int index;
        //Подсказка
        protected String hint;
        //CPU
        protected bool cpuTag = false;
        private string description = "";
        private bool descriptionChanged = false;
        public string Description { get => description; set => this.description = value; }
        public bool DescriptionChanged { get => descriptionChanged; set => this.descriptionChanged = value; }
        protected bool exceed;
        protected string new_name = "";
        protected int PrecisionDecimalPoint = -1;
        public static ParamType ParamTypeFromString(String typeString)
        {
            switch (typeString)
            {
                case "BOOLEAN":
                    return ParamType.BOOLEAN;

                case "INT":
                    return ParamType.INT;

                case "UINT":
                    return ParamType.UINT;

                case "FLOAT":
                    return ParamType.FLOAT;

                default:
                    return ParamType.STRING;

            }
        }

        protected abstract CBaseParam createParam();

        public virtual void Assign(CBaseParam param)
        {
            //Флаг показывающий что параметр конфигурируемый
            this.config = param.config;
            //Флаг только чтение
            this.readOnly = param.readOnly;
            //Тип параметра
            this.type = param.type;
            //Название параметра
            this.name = param.name;
            //Индекс имени параметра в таблице локализации
            this.nameKey = param.nameKey;
            //Имя группы которой принадлежит тег
            this.groupName = param.groupName;
            //Ссылка на группу
            this.group = param.group;
            //Флаг изменения значения из конфигуратора
            this.manualChanged = param.manualChanged;
            //Текущее значение параметра
            this.value = param.value;
            //Значение параметра на запись
            this.writeValue = param.writeValue;
            //Cpu tag
            cpuTag = param.cpuTag;
            //Имя тега
            this.tagname = param.tagname;
            //Список опций
            this.options = param.options;
            //Флаг лимитов в шаблоне
            this.limit = param.limit;
            //Нижнее значение лимита (если есть)
            this.lowLimit = param.lowLimit;
            //Верхнее значение лимита (если есть)
            this.hiLimit = param.hiLimit;

            this.index = param.index;
        }

        public virtual object Clone()
        {
            CBaseParam param = this.createParam();
            param.Assign(this);

            return param;
        }

        /// <summary>
        /// Функция сохраняет настройки параметра в XML файле
        /// </summary>
        /// <param name="saveNode">Узел XML документа в который сохраняются настройки</param>
        public virtual XmlNode Save(XmlNode saveNode)
        {
            XmlNode param = saveNode.OwnerDocument.CreateElement("element", "param", "");
            saveNode.AppendChild(param);

            //Имя параметра в человеческом виде в текущей локали, для того что бы в файле видеть к что за группа
            XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute("name");
            attr.Value = this.name;
            param.Attributes.Append(attr);
            //Индекс имени параметра
            attr = saveNode.OwnerDocument.CreateAttribute("nameIndex");
            attr.Value = this.nameKey;
            param.Attributes.Append(attr);
            //Тег параметра
            attr = saveNode.OwnerDocument.CreateAttribute("tagname");
            attr.Value = this.tagname;
            param.Attributes.Append(attr);
            //CPU
            attr = saveNode.OwnerDocument.CreateAttribute("cpuTag");
            attr.Value = cpuTag.ToString();
            param.Attributes.Append(attr);
            //Флаг только чтение
            attr = saveNode.OwnerDocument.CreateAttribute("readonly");
            attr.Value = this.readOnly.ToString();
            param.Attributes.Append(attr);
            //Флаг конфигурирования
            attr = saveNode.OwnerDocument.CreateAttribute("config");
            attr.Value = this.config.ToString();
            param.Attributes.Append(attr);
            //Тип параметра
            attr = saveNode.OwnerDocument.CreateAttribute("type");
            attr.Value = this.type.ToString();
            param.Attributes.Append(attr);
            //Текущее значение параметра
            attr = saveNode.OwnerDocument.CreateAttribute("value");
            if (this.value != null)
                attr.Value = this.value.ToString();
            else
                attr.Value = "";
            param.Attributes.Append(attr);

            //Сохранение опций
            if (this.options != null)
            {
                XmlNode optionsNode = saveNode.OwnerDocument.CreateElement("element", "options", "");
                param.AppendChild(optionsNode);
                //Название набора опций
                attr = optionsNode.OwnerDocument.CreateAttribute("name");
                attr.Value = this.options.Name;
                optionsNode.Attributes.Append(attr);

                foreach (KeyValuePair<String, COption> kvp in this.Options.Options)
                {
                    XmlNode optionNode = saveNode.OwnerDocument.CreateElement("element", "option", "");
                    optionsNode.AppendChild(optionNode);

                    //значение для опции
                    attr = saveNode.OwnerDocument.CreateAttribute("value");
                    attr.Value = kvp.Value.Value;
                    optionNode.Attributes.Append(attr);
                    //Локализация 
                    attr = saveNode.OwnerDocument.CreateAttribute("locale");
                    attr.Value = kvp.Value.Locale.ToString();
                    optionNode.Attributes.Append(attr);
                    //Значение выводимое в пользовательский интерфейс, это что бы просто было видно что там за значение привязывается
                    attr = saveNode.OwnerDocument.CreateAttribute("guivalue");
                    attr.Value = kvp.Value.GuiValue;
                    optionNode.Attributes.Append(attr);
                }
            }

            return param;
        }

        public virtual void Load(XPathNavigator loadNode)
        {
            //Индекс имени параметра
            this.nameKey = CXML.getAttributeValue(loadNode, "nameIndex", "");
            //Имя параметра в текущей локали по индексу
            this.Name = CConfig.Localization.GetLocaleValue(this.nameKey);
            //cpu tag
            cpuTag = Convert.ToBoolean(CXML.getAttributeValue(loadNode, "cpuTag", "false"));
            //Имя тега
            this.tagname = CXML.getAttributeValue(loadNode, "tagname", "");
            //Текущее значение
            this.WriteValue = CXML.getAttributeValue(loadNode, "value", "");
            //Тип параметра
            this.type = CBaseParam.ParamTypeFromString(CXML.getAttributeValue(loadNode, "type", ""));
            //Только чтение
            this.readOnly = System.Convert.ToBoolean(CXML.getAttributeValue(loadNode, "readonly", ""));
            //Флаг конфигурирования
            this.config = System.Convert.ToBoolean(CXML.getAttributeValue(loadNode, "config", ""));

            this.index = Convert.ToInt32(CXML.getAttributeValue(loadNode, "index", "0x0"), 16);

            XPathNavigator optionsNode = loadNode.SelectSingleNode("options");
            if (optionsNode != null)
            {
                String optionsSetName = CXML.getAttributeValue(optionsNode, "name", "");
                COptionSet optionsSet;
                if (CConfig.Options.OptionSets.TryGetValue(optionsSetName, out optionsSet))
                {
                    this.options = optionsSet;
                    return;
                }

                //Не нашлось набора с таким именем, значит надо его создать
                optionsSet = new COptionSet();
                optionsSet.Name = optionsSetName;
                CConfig.Options.OptionSets.Add(optionsSet.Name, optionsSet);

                //Перебор опций
                XPathNodeIterator options = optionsNode.Select("option");
                foreach (XPathNavigator node in options)
                {
                    COption option = new COption();

                    option.Value = CXML.getAttributeValue(node, "value", "");
                    option.Locale = CXML.getAttributeValue(node, "locale", "");

                    optionsSet.Options.Add(option.Value, option);
                }

                this.options = optionsSet;
            }
        }

        public bool Config { get => config; set => config = value; }
        public bool HexCode { get => hex; set => hex = value; }
        public bool IsVersion { get => isversion; set => isversion = value; }
        public bool ReadOnly { get => readOnly; set => readOnly = value; }
        public bool PLC3Only { get => plc3only; set => plc3only = value; }
        public bool PLC31rockchiponly { get => plc31rockchiponly; set => plc31rockchiponly = value; }
        public bool PLC31Only { get => plc31only; set => plc31only = value; }
        public uint PLCUse { get => plcuse; set => plcuse = value; }
        public bool DiagnosticToPair { get => diagnosticToPair; set => diagnosticToPair = value; }
        public bool Visible { get => visible; set => visible = value; }
        public ParamType Type { get => type; set => type = value; }
        public String NameKey { get => nameKey; set => nameKey = value; }
        public CBaseGroup Group { get => group; set => group = value; }
        public bool IsLimit { get => limit; }
        public int HiLimit { get => hiLimit; }
        public bool Exceed { get => exceed; set => exceed = value; }
        public int LowLimit { get => lowLimit; }
        public int Index { get => index; }
        public String Hint { get => hint; }
        public String TypeString
        {
            get
            {
                switch (this.type)
                {
                    case ParamType.BOOLEAN:
                        return "Boolean";
                    case ParamType.INT:
                        return "Int";
                    case ParamType.UINT:
                        return "UInt";
                    case ParamType.FLOAT:
                        return "Float";
                    case ParamType.STRING:
                        return "String";
                    default:
                        return "Unknown";
                }
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (this.name == value)
                    return;

                this.name = value;
                this.OnPropertyChanged("Name");
            }
        }

        public string GroupName { get => groupName; set => groupName = value; }
        public bool ManualChanged { get => manualChanged; set => manualChanged = value; }

        [JsonProperty]
        public string Tagname { get => tagname; set => tagname = value; }
        public bool CpuTag { get => cpuTag; set => cpuTag = value; }
        private string HexConverter(string value)
        {
            string[] i_version = new string[4];
            char[] hex = value.ToCharArray();
            int count = 0;
            if (hex.Count() == 7)
            {
                int first = int.Parse(hex[0].ToString(), System.Globalization.NumberStyles.HexNumber);
                if (first > 9)
                    i_version[0] = first.ToString();
                else
                    i_version[0] = "0" + first.ToString();
                count = 1;
            }
            else
                count = 0;

            for (int i = count; i < hex.Count(); i++)
            {
                string str = hex[i].ToString();
                i++;
                int numb = int.Parse(str + hex[i].ToString(), System.Globalization.NumberStyles.HexNumber);
                if (numb > 9)
                    i_version[count] = numb.ToString();
                else
                    i_version[count] = "0" + numb.ToString();
                count++;

            }
            return i_version[0].ToString() + "." + i_version[1].ToString() + "." + i_version[2].ToString() + "." + i_version[3].ToString();
        }
        //[JsonProperty]
        public object Value
        {
            get
            {
                if ((value == null) || (value.ToString() == ""))
                    return "";
                try
                {
                    switch (this.type)
                    {
                        case ParamType.BOOLEAN:
                            return Convert.ToBoolean(this.value);

                        case ParamType.FLOAT:
                            return Math.Round(Convert.ToDouble(CAuxil.AdaptFloat(this.value.ToString())), 3);

                        case ParamType.INT:
                            return Convert.ToInt64(this.value);

                        case ParamType.UINT:
                            return Convert.ToUInt64(this.value);

                        default:
                            return Convert.ToString(value);
                    }
                }
                catch
                {
                    try
                    {
                        string[] ishex = value.ToString().Split();
                        if (ishex.Contains("HEXConvert"))
                            return HexConverter(ishex[1]);

                        if (ishex.Contains("HEX"))
                            return ishex[1];

                        return "";
                    }
                    catch
                    {
                        return "";
                    }
                }
            }
            set
            {
                this.value = value;
                this.OnPropertyChanged("Value");
                this.OnPropertyChanged("GuiValue");
            }
        }

        /// <summary>
        /// Возвращает строковое значение параметра для записи в SQL
        /// Для чисел с плавающей запятой разделитель принудительно ставится "."
        /// </summary>
        public String SQLValue
        {
            get
            {
                if ((value == null) || (value.ToString() == ""))
                    return "";

                try
                {
                    switch (this.type)
                    {
                        case ParamType.FLOAT:
                            this.new_name = this.value.ToString().Replace(",", ".");
                            return new_name;

                        default:
                            this.new_name = this.value.ToString();
                            return new_name;
                    }

                }
                catch
                {
                    return "";
                }
            }
        }

        public void RefreshGUIValue()
        {
            this.OnPropertyChanged("GuiValue");
            this.OnPropertyChanged("Exceed");
        }

        protected virtual object getScaleValue()
        {
            return this.Value;
        }

        protected virtual void setScaleValue(object value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Отмасштабированное значение параметра
        /// </summary>
        public object
            ScaleValue
        {
            get
            {
                try
                {
                    return this.getScaleValue();
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                this.setScaleValue(value);
            }

        }

        [JsonProperty]
        public String ValueString
        {
            get
            {
                return this.Value.ToString();
            }
        }

        public object WriteValue
        {
            get
            {
                return this.writeValue;
            }
            set
            {
                if (this.writeValue == value)
                    return;

                this.writeValue = value;
                if (!CGlobal.CurrState.IsRunning)
                    this.Value = value;
                this.OnPropertyChanged("WriteValue");
            }
        }

        /// <summary>
        /// Значение для отображения в пользовательском интерфейсе
        /// </summary>
        public String GuiValue
        {

            get
            {

                if (this.ScaleValue == null)
                    return "";

                if (this.options == null)
                    return this.ScaleValue.ToString();


                COption option;
                String val = this.ScaleValue.ToString();
                if (!this.options.CaseSensitive)
                    val = val.ToLower();
                if (!this.options.Options.TryGetValue(val, out option))
                    return this.ScaleValue.ToString();
                else
                    return option.GuiValue;
            }
        }

        public COptionSet Options
        {
            get
            {
                return this.options;
            }
            set
            {
                this.options = value;
            }
        }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Класс описывающий единичный параметр контроллера
    /// </summary>
    public class CParam : CBaseParam
    {
        //Идентификатор параметра в базе
        private Int32 id;

        public int ID { get => id; set => id = value; }

        protected override CBaseParam createParam()
        {
            return new CParam();
        }

        public override void Assign(CBaseParam param)
        {
            base.Assign(param);

            CParam p = param as CParam;

            //Идентификатор параметра в базе
            this.id = p.id;
            //Название тега
            this.tagname = p.tagname;
        }

        /// <summary>
        /// Свойство видимости параметра в списке окна
        /// </summary>
        public Boolean Visible
        {
            get
            {
                return this.readOnly;
                //if ((!CGlobal.CurrState.IsRunning) && (this.NoConfig || this.ReadOnly))
                //    return false;
                //else
                //    return true;
            }
        }
    }

    /// <summary>
    /// Базовая группа
    /// </summary>
    public abstract class CBaseGroup : INotifyPropertyChanged
    {
        public const string PARAMS = "params";
        public const string MODULES = "modules";

        //Родительская группа
        private CBaseGroup parentGroup = null;
        //Список групп
        protected ObservableCollection<CBaseGroup> groupsList = new ObservableCollection<CBaseGroup>();
        //Список параметров группы
        private List<CBaseParam> paramsList = new List<CBaseParam>();
        //Индекс названия группы
        protected String nameKey = "";
        //Ссылка на таблицу локализации приложения
        protected String langReference = "";
        //Описатель типа группы
        protected String groupType;
        //Строка указывающая на изображение
        private String imageSource = null;

        //Название группы
        protected String name;

        public CBaseGroup()
        {
        }

        public CBaseGroup(CBaseGroup parentGroup)
        {
            this.parentGroup = parentGroup;
        }

        protected virtual String getName()
        {
            return this.name;
        }

        public String Name
        {
            get
            {
                return this.getName();
            }
            set
            {

                if (this.name == value)
                    return;

                this.name = value;
                //this.DebugName = this.name;
                this.OnPropertyChanged("Name");
            }
        }

        public virtual void Clear()
        {
            foreach (CBaseGroup group in this.groupsList)
                group.Clear();
            this.groupsList.Clear();
            this.paramsList.Clear();
        }

        protected abstract CBaseParam createParam();

        protected abstract CBaseGroup createGroup();

        public virtual void Load(NpgsqlConnection connection)
        {

        }
        public virtual void Load(XPathNavigator loadNode)
        {
            if (this.name == null)
            {
                //Индекc названия из таблицы локализации
                this.nameKey = CXML.getAttributeValue(loadNode, "nameIndex", "");
                //Имя берётся из таблицы локализации
                this.name = CConfig.Localization.GetLocaleValue(this.nameKey);
            }

            XPathNavigator paramsNode = loadNode.SelectSingleNode("params");
            //Подгрузка параметров
            foreach (XPathNavigator paramNode in paramsNode.Select("param"))
            {
                CBaseParam param = this.createParam();
                param.Load(paramNode);
                param.GroupName = this.Name;
                param.Group = this;

                this.ParamsList.Add(param);
            }

            //Подгрузка подгрупп
            XPathNavigator groupsNode = loadNode.SelectSingleNode("groups");
            foreach (XPathNavigator groupNode in groupsNode.Select("group"))
            {
                CBaseGroup group = this.createGroup();
                group.Load(groupNode);
                this.groupsList.Add(group);
            }
        }

        /// <summary>
        /// Функция сохраняет содержимое группы в XML узле
        /// </summary>
        /// <param name="saveNode"></param>
        public virtual XmlNode Save(XmlNode saveNode)
        {
            XmlNode group = saveNode.OwnerDocument.CreateElement("element", "group", "");
            saveNode.AppendChild(group);

            //Имя параметра в человеческом виде в текущей локали, для того что бы в файле видеть что за группа
            XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute("name");
            attr.Value = this.name;
            group.Attributes.Append(attr);
            //Номер индекса в таблице локализации
            attr = saveNode.OwnerDocument.CreateAttribute("nameIndex");
            attr.Value = this.nameKey.ToString();
            group.Attributes.Append(attr);
            //Тип группы
            if (this.groupType != null)
            {
                attr = saveNode.OwnerDocument.CreateAttribute("type");
                attr.Value = this.groupType;
                group.Attributes.Append(attr);
            }

            //Сохранение вложенных групп
            XmlNode groupsNode = saveNode.OwnerDocument.CreateElement("element", "groups", "");
            group.AppendChild(groupsNode);
            foreach (CBaseGroup pGroup in this.groupsList)
                pGroup.Save(groupsNode);

            //Сохранение параметров группы
            XmlNode paramsNode = saveNode.OwnerDocument.CreateElement("element", "params", "");
            group.AppendChild(paramsNode);
            foreach (CBaseParam param in this.ParamsList)
                param.Save(paramsNode);

            return group;
        }

        private void fillParamsList(List<CBaseParam> list)
        {
            foreach (CBaseParam param in this.ParamsList)
                list.Add(param);
        }

        public void FillParamsList(List<CBaseParam> list)
        {
            this.fillParamsList(list);
            foreach (CBaseGroup group in this.groupsList)
                group.FillParamsList(list);
        }

        private void fillParamsList(Dictionary<String, CBaseParam> list)
        {
            foreach (CBaseParam param in this.ParamsList)
            {
                if (!list.ContainsKey(param.Tagname) && !list.ContainsValue(param))
                {
                    list.Add(param.Tagname, param);
                }
            }
        }

        /// <summary>
        /// Заполняет список параметрами группы
        /// </summary>
        /// <param name="list"></param>
        public void FillParamsList(Dictionary<String, CBaseParam> list)
        {
            this.fillParamsList(list);

            foreach (CBaseGroup group in this.groupsList)
                group.FillParamsList(list);
        }

        /// <summary>
        /// Из переданного списка удаляются все параметры группы
        /// </summary>
        /// <param name="list"></param>
        private void removeParamsFromParamsList(Dictionary<String, CBaseParam> list)
        {
            foreach (CBaseParam param in this.paramsList)
                list.Remove(param.Tagname);
        }

        public void RemoveParamsFromParamsList(Dictionary<String, CBaseParam> list)
        {
            this.removeParamsFromParamsList(list);

            foreach (CBaseGroup group in this.groupsList)
                group.RemoveParamsFromParamsList(list);
        }

        public CBaseGroup ParentGroup { get => parentGroup; set => parentGroup = value; }

        public List<CBaseParam> ParamsList
        {
            get
            {
                return this.paramsList;
            }
        }

        /// <summary>
        /// Список всех параметров группы включая вложенные
        /// </summary>
        public List<CBaseParam> AllParams
        {
            get
            {
                List<CBaseParam> list = new List<CBaseParam>();

                list.AddRange(this.paramsList);
                foreach (CBaseGroup group in this.groupsList)
                    list.AddRange(group.AllParams);

                return list;
            }
        }

        public ObservableCollection<CBaseGroup> GroupsList
        {
            get
            {
                return this.groupsList;
            }
        }

        public String NameKey { get => nameKey; set => nameKey = value; }
        public String LangReference { get => langReference; set => langReference = value; }
        public String GroupType { get => groupType; set => groupType = value; }

        public string ImageSource
        {
            get
            {
                return this.imageSource;
            }
            set
            {
                if (this.imageSource == value)
                    return;

                this.imageSource = value;
                this.OnPropertyChanged("ImageSource");
            }
        }

        public void ChangeLanguage()
        {
            if (nameKey != "")
            {
                //Смена имени группы
                this.Name = CConfig.Localization.GetLocaleValue(this.nameKey);
            }
            else
            {
                //Если нет индекса в таблице локализации, то проверяется ссылка на таблицу локализации приложения
                if (this.langReference != "")
                    this.Name = CGlobal.GetResourceValue(this.langReference);
            }
            //Смена имени всех групп входящих в эту группу
            foreach (CBaseGroup group in this.groupsList)
                group.ChangeLanguage();
            //Смена имени параметров этой группы
            foreach (CBaseParam param in this.ParamsList)
                param.Name = CConfig.Localization.GetLocaleValue(param.NameKey);
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Класс группа параметров
    /// </summary>
    public class CParamsGroup : CBaseGroup
    {
        //Идентификатор группы в базе
        private Int32 id;
        //Уровень группы
        private Int32 level;
        //Правый и левый индексы
        private Int32 lid;
        private Int32 rid;
        //Флаг запрещающий изменения в группе
        private Boolean read_only;

        public CParamsGroup(CParamsGroup parentGroup)
            : base(parentGroup)
        {
        }

        protected override CBaseParam createParam()
        {
            return new CParam();
        }

        protected override CBaseGroup createGroup()
        {
            return new CParamsGroup(this);
        }

        public override void Load(NpgsqlConnection connection)
        {
            this.groupsList.Clear();

            //Подготовка SQL запроса с учётом колонок локализации
            String sql = CConfig.PreinitSQL();
            if (this.level == 0)
            {
                //Выборка групп 1-го уровня
                sql += "g.id, g.level, g.lid, g.rid, g.readonly, g.name from groups_table g inner join localization l on g.name=l.id where g.level = 1";
            }
            else
            {
                this.LoadParams(connection);

                sql += "g.id, g.level, g.lid, g.rid, g.readonly, g.name from groups_table g inner join localization l on g.name=l.id " +
                             "where (g.level={0} and g.lid>{1} and g.rid<{2})";
                sql = String.Format(sql, this.level + 1, this.lid, this.rid);
            }

            //Выборка
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            try
            {
                //Если не получилось группы прочитать, то и делать тут нечего
                if (!reader.HasRows)
                    return;

                //Перебор групп
                while (reader.Read())
                {
                    CParamsGroup group = new CParamsGroup(null);
                    try
                    {
                        group.SetValuesFromSQL(reader);
                        this.groupsList.Add(group);
                        group.ParentGroup = this;
                    }
                    catch
                    { }
                }
            }
            finally
            {
                reader.Close();
            }

            foreach (CParamsGroup group in this.groupsList)
            {
                group.Load(connection);
            }
        }

        public void Load(List<CConfig.CParamsGroupRecord> groupsList, List<CConfig.CParamRecord> paramsList, bool mustAllReadOnly = false)
        {
            this.groupsList.Clear();

            IEnumerable<CConfig.CParamsGroupRecord> groups;
            if (this.level == 0)
            {
                //Выборка групп первого уровня
                groups = from x in groupsList where x.level == 1 select x;
            }
            else
            {
                this.loadParams(paramsList, mustAllReadOnly);
                //Выборка остальных групп
                groups = from x in groupsList where (x.level == (this.level + 1)) && (x.lid > this.lid) && (x.rid < this.rid) select x;
            }


            foreach (CConfig.CParamsGroupRecord groupRecord in groups)
            {
                CParamsGroup group = new CParamsGroup(null);
                try
                {
                    group.SetValuesFromRecord(groupRecord);
                    this.groupsList.Add(group);
                    group.ParentGroup = this;
                }
                catch
                {

                }
            }

            foreach (CParamsGroup group in this.groupsList)
            {
                group.Load(groupsList, paramsList, mustAllReadOnly);
            }
        }

        public override void Load(XPathNavigator loadNode)
        {
            if (this.name == null)
            {
                //Индекc названия из таблицы локализации
                this.nameKey = CXML.getAttributeValue(loadNode, "nameIndex", "");
                //Имя берётся из таблицы локализации
                this.name = CConfig.Localization.GetLocaleValue(this.nameKey);
            }

            XPathNavigator paramsNode = loadNode.SelectSingleNode("params");
            //Подгрузка параметров
            foreach (XPathNavigator paramNode in paramsNode.Select("param"))
            {
                CParam param = new CParam();
                param.Load(paramNode);
                param.GroupName = this.name;
                param.Group = this;

                this.ParamsList.Add(param);
            }

            //Подгрузка подгрупп
            XPathNavigator groupsNode = loadNode.SelectSingleNode("groups");
            foreach (XPathNavigator groupNode in groupsNode.Select("group"))
            {
                CParamsGroup group = new CParamsGroup(this);
                group.Load(groupNode);
                this.groupsList.Add(group);
            }
        }

        /// <summary>
        /// Выборка списка параметров группы
        /// </summary>
        /// <param name="connection"></param>
        public void LoadParams(NpgsqlConnection connection)
        {
            bool isColumnPlc3Exists = CAuxil.IsColumnExists("params_table", "plc3only");
            bool isColumnPlc31Exists = CAuxil.IsColumnExists("params_table", "plc31only");
            string plc3Only = isColumnPlc3Exists ? "p.plc3only," : "";
            string plc31Only = isColumnPlc31Exists ? "p.plc31only," : "";

            String sql = CConfig.PreinitSQL();
            sql += $"p.id, p.config, p.tag, p.readonly, {plc3Only}{plc31Only} pt.code as type, p.param_name, os.name as opset_name from params_table p " +
                "inner join param_type_table pt on p.type=pt.id left join options_sets os on os.id=p.options_set " +
                "inner join localization l on l.id=p.param_name " +
                "where p.group_id = {0}";
            sql = String.Format(sql, this.id);

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                //Если не получилось группы прочитать, то и делать тут нечего
                if (!reader.HasRows)
                    return;

                //Перебор групп
                while (reader.Read())
                {
                    CParam param = new CParam();
                    param.PLC3Only = Convert.ToBoolean(CAuxil.GetStrFromReader(reader, "plc3only", "false"));
                    //p.plc31only
                    param.PLC31Only = Convert.ToBoolean(CAuxil.GetStrFromReader(reader, "plc31only", "false"));
                    //p.id
                    param.ID = Convert.ToInt32(reader["id"]);
                    //p.config
                    param.Config = Convert.ToBoolean(reader["config"]);
                    //p.tag
                    param.Tagname = Convert.ToString(reader["tag"]);
                    //p.read_only
                    param.ReadOnly = Convert.ToBoolean(reader["readonly"]);
                    //p.type
                    param.Type = (ParamType)Convert.ToInt32(reader["type"]);
                    //Индекс имени параметра
                    param.NameKey = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);
                    //Имя параметра
                    param.Name = CConfig.Localization.GetLocaleValue(param.NameKey);
                    //Имя группы
                    param.GroupName = this.name;
                    param.Group = this;
                    //Опции
                    String option_set_name = Convert.ToString(reader["opset_name"]);
                    if (option_set_name != "")
                    {
                        //Что то вроде есть
                        COptionSet optionSet;
                        if (CConfig.Options.OptionSets.TryGetValue(option_set_name, out optionSet))
                            param.Options = optionSet;
                    }

                    this.ParamsList.Add(param);
                }
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Выборка списка параметров группы
        /// </summary>
        /// <param name="paramsList"></param>
        /// <param name="mustAllReadOnly"></param>
        private void loadParams(List<CConfig.CParamRecord> paramsList, bool mustAllReadOnly)
        {
            IEnumerable<CConfig.CParamRecord> params_list = from x in paramsList where x.group_id == this.id select x;
            foreach (CConfig.CParamRecord paramRecord in params_list)
            {
                CParam param = new CParam();

                //p.plc3only
                param.PLC3Only = paramRecord.plc3only;
                //p.plc31only
                param.PLC31Only = paramRecord.plc31only;
                //p.diagnosticToPair
                param.DiagnosticToPair = paramRecord.diagnosticToPair;
                //p.id
                param.ID = paramRecord.id;
                //p.config
                param.Config = paramRecord.config;
                //p.tag
                param.Tagname = paramRecord.tag;
                //p.read_only
                if (CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType >= GroupTypeEnum.Spectator || mustAllReadOnly)
                {
                    param.ReadOnly = true;
                }
                else
                {
                    param.ReadOnly = paramRecord.read_only;
                }
                //p.type
                param.Type = (ParamType)paramRecord.type;
                //Индекс имени параметра
                param.NameKey = paramRecord.param_name;
                //Имя параметра
                param.Name = CConfig.Localization.GetLocaleValue(param.NameKey);
                //Имя группы
                param.GroupName = this.name;
                param.Group = this;
                //Опции
                String option_set_name = paramRecord.opset_name;
                if (option_set_name != "")
                {
                    //Что то вроде есть
                    COptionSet optionSet;
                    if (CConfig.Options.OptionSets.TryGetValue(option_set_name, out optionSet))
                        param.Options = optionSet;
                }

                this.ParamsList.Add(param);
            }

        }

        public void SetValuesFromSQL(NpgsqlDataReader reader)
        {
            //g.id
            this.id = Convert.ToInt32(reader["id"]);
            //g.level
            this.level = Convert.ToInt32(reader["level"]);
            //g.lid
            this.lid = Convert.ToInt32(reader["lid"]);
            //g.rid
            this.rid = Convert.ToInt32(reader["rid"]);
            //g.readonly
            this.read_only = Convert.ToBoolean(reader["readonly"]);
            //Индекс названия группы
            this.nameKey = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);
            //Имя берётся из таблицы локализации
            this.name = CConfig.Localization.GetLocaleValue(this.nameKey);
        }

        public void SetValuesFromRecord(CConfig.CParamsGroupRecord groupRecord)
        {
            //g.id
            this.id = groupRecord.id;
            //g.level
            this.level = groupRecord.level;
            //g.lid
            this.lid = groupRecord.lid;
            //g.rid
            this.rid = groupRecord.rid;
            //g.readonly
            this.read_only = groupRecord.readnly;
            //Индекс названия группы
            this.nameKey = groupRecord.name;
            //Имя берётся из таблицы локализации
            this.name = CConfig.Localization.GetLocaleValue(this.nameKey);
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

        public Int32 Level
        {
            get
            {
                return this.level;
            }
            set
            {
                this.level = value;
            }
        }

        public Int32 LID
        {
            get
            {
                return this.lid;
            }
            set
            {
                this.lid = value;
            }
        }

        public Int32 RID
        {
            get
            {
                return this.rid;
            }
            set
            {
                this.rid = value;
            }
        }

        public Boolean ReadOnly
        {
            get
            {
                return this.read_only;
            }
            set
            {
                this.read_only = value;
            }
        }

        /// <summary>
        /// Отладочное имя
        /// </summary>
        public String DebugName
        {
            get
            {
                String s = String.Format("{0} - (id={1}, lev={2}, lid={3}, rid={4})", this.Name, this.id, this.level, this.lid, this.rid);
                return s;
            }
            set
            {
                this.OnPropertyChanged("DebugName");
            }
        }
    }

    /// <summary>
    /// Класс описывающий конфигурацию контроллера
    /// </summary>
    public class CConfig : INotifyPropertyChanged
    {
        /// <summary>
        /// Класс
        /// </summary>
        public class CLocaleRecord
        {
            private Dictionary<string, string> values = new Dictionary<string, string>();
            private Boolean temporary = false;

            /// <summary>
            /// Первый параметр - ключ записи обозначанет язык: en, ru и т.д.
            /// Второй параметр - непосредственно само значение в нужной локализации
            /// </summary>
            public Dictionary<string, string> Values
            {
                get
                {
                    return this.values;
                }
            }

            /// <summary>
            /// Флаг показывает что эта запись непостоянная и не должна сохраняться в конфигурацию
            /// </summary>
            public Boolean Temporary
            {
                get
                {
                    return this.temporary;
                }
                set
                {
                    this.temporary = value;
                }
            }
        }

        /// <summary>
        /// Класс отвечающий за локализацию конфигурации
        /// </summary>
        public class CLocalization
        {
            /// <summary>
            /// Список локалей конфигурации
            /// </summary>
            private Dictionary<String, CLocaleRecord> locales = new Dictionary<String, CLocaleRecord>();

            //Список колонок локализаций
            private List<String> columns = new List<String>();

            public CLocalization()
            {
                this.columns.Clear();
                this.columns.Add("ru");
                this.columns.Add("en");
            }

            public void Clear()
            {
                this.locales.Clear();
            }

            public String GetLocaleValue(String index)
            {
                switch (CGlobal.Settings.Language)
                {
                    case CGlobal.RU:
                        if (this.locales.ContainsKey(index))
                            return this.locales[index].Values["ru"];
                        else
                            return "";
                    //По умолчанию берётся английский
                    default:
                        if (this.locales.ContainsKey(index))
                            return this.locales[index].Values["en"];
                        else
                            return "";
                }
            }

            /// <summary>
            /// Функция сохраняет таблицу локализации в XML файл
            /// </summary>
            /// <param name="saveNode">узел XML документа в котором размещается таблица локализации</param>
            public void Save(XmlNode saveNode)
            {
                //Список локалей
                XmlNode locales = saveNode.OwnerDocument.CreateElement("element", "locales", "");
                saveNode.AppendChild(locales);

                foreach (KeyValuePair<String, CLocaleRecord> KeyPair in this.locales)
                {
                    XmlNode locale = saveNode.OwnerDocument.CreateElement("element", "locale", "");
                    locales.AppendChild(locale);

                    //Узла с названиями параметров с привязкой по локализациям
                    CLocaleRecord record = KeyPair.Value;
                    foreach (KeyValuePair<string, string> WordsPair in record.Values)
                    {
                        //Наменование
                        XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute(WordsPair.Key);
                        attr.Value = WordsPair.Value;
                        locale.Attributes.Append(attr);
                    }
                }
            }

            /// <summary>
            /// Подгрузка настроек из XML файла
            /// </summary>
            /// <param name="loadNode">Узел в котором размещены настройки</param>
            public void Load(XPathNavigator loadNode)
            {
                //Загрузка содержимого ветки locales
                XPathNodeIterator nodes = loadNode.Select("//locales//locale");
                foreach (XPathNavigator locale in nodes)
                    this.GetLocalesFromXmlNode(locale);
            }

            public Dictionary<String, CLocaleRecord> Locales
            {
                get
                {
                    return this.locales;
                }
            }

            public List<String> Columns
            {
                get
                {
                    return this.columns;
                }
            }

            /// <summary>
            /// Функция извлекает из переданного xml узла атрибуты которые соотвествуют колонкам локалей
            /// и формирует из них новую запись
            /// </summary>
            /// <param name="node">Узел для обработки</param>
            /// <returns>Индекс записи в таблице локализации</returns>
            public String GetLocalesFromXmlNode(XPathNavigator node, String key = "")
            {
                CLocaleRecord record = new CLocaleRecord();

                foreach (String columnname in this.columns)
                {
                    String localeValue = CXML.getAttributeValue(node, columnname, "");
                    record.Values.Add(columnname, localeValue);
                }

                return this.AddLocaleRecord(record, key);
            }

            public String GetLocalesFromDataBaseRecord(NpgsqlDataReader reader, String key = "")
            {
                CLocaleRecord record = new CLocaleRecord();

                foreach (String col in CConfig.Localization.Columns)
                    record.Values.Add(col, Convert.ToString(reader[col]));

                return this.AddLocaleRecord(record, key);
            }

            public String AddLocaleRecord(CLocaleRecord record, String key)
            {
                CLocaleRecord rec = null;
                if (key == "")
                    key = record.Values["en"];
                else
                    key = String.Format("{0}.{1}", key, record.Values["en"]);

                if (!this.locales.TryGetValue(key, out rec))
                    this.locales.Add(key, record);

                return key;
            }
        }

        /// <summary>
        /// Класс для временного хранения данных о группе
        /// </summary>
        public class CParamsGroupRecord
        {
            public String ru;
            public String en;
            public Int32 id;
            public Int32 level;
            public Int32 lid;
            public Int32 rid;
            public Boolean readnly;
            public String name;
        }

        /// <summary>
        /// Класс для временного хранения данных о параметре
        /// </summary>

        public class CParamRecord
        {
            public String ru;
            public String en;
            public Int32 id;
            public Boolean config;
            public String tag;
            public Boolean read_only;
            public Int32 type;
            public String param_name;
            public String opset_name;
            public Int32 group_id;
            public Boolean plc3only;
            public Boolean plc31rockchiponly;
            public Boolean plc31only;
            public Boolean visible;
            public bool diagnosticToPair;
            public string tmpValue;
            public uint plcuse;
        }

        /// <summary> Группы контроллеров (числовое значение - это порядковый номер бита в маске, от младшего бита к старшему) </summary>
        public enum ControllerGroups
        {
            Version2 = 1,
            Version3 = 2,
            Mentorel = 3,
            RockChip = 4,
        }

        /// <summary>
        /// Список опций конфигурации
        /// </summary>
        public class COptions
        {
            //Список наборов опций
            private Dictionary<String, COptionSet> optionSets = new Dictionary<string, COptionSet>();

            public Dictionary<string, COptionSet> OptionSets
            {
                get
                {
                    return this.optionSets;
                }
            }

            /// <summary>
            /// Загрузка списка опциональных значений
            /// </summary>
            /// <param name="connection"></param>
            public void Load(NpgsqlConnection connection)
            {
                //Подготовка запроса с учётом колонок в таблице локализации
                String sql = CConfig.PreinitSQL();
                sql += "o.name, o.case_sensitive, e.* from options_enums e inner join options_sets o on o.id=e.option_set_id inner join localization l on l.id=e.locale order by e.val_order";

                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    this.Load(reader);
                }
                finally
                {
                    reader.Close();
                }
            }

            public void Load(NpgsqlDataReader reader)
            {
                if (!reader.HasRows)
                    return;

                while (reader.Read())
                {
                    COptionSet optionSet;
                    String setName = Convert.ToString(reader["name"]);
                    Boolean case_sensitive = Convert.ToBoolean(reader["case_sensitive"]);
                    if (!this.optionSets.TryGetValue(setName, out optionSet))
                    {
                        optionSet = new COptionSet();
                        optionSet.Name = setName;
                        optionSet.CaseSensitive = case_sensitive;
                        this.optionSets.Add(optionSet.Name, optionSet);
                    }

                    COption option = new COption();
                    if (optionSet.CaseSensitive)
                        option.Value = Convert.ToString(reader["value"]);
                    else
                        option.Value = Convert.ToString(reader["value"]).ToLower();
                    option.Locale = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);

                    optionSet.Options.Add(option.Value, option);
                }
            }

            public void Clear()
            {
                this.optionSets.Clear();
            }
        }

        //Список групп
        private ObservableCollection<CBaseGroup> groupsList = new ObservableCollection<CBaseGroup>();
        //Список параметров для отображения в интерфейсе, параметры контроллера
        private ObservableCollection<CBaseParam> visibleParamsList = new ObservableCollection<CBaseParam>();
        //Список конфигурационных параметров для отображения в интерфейсе, параметры модулей
        private ObservableCollection<CBaseParam> modulesParamsList = new ObservableCollection<CBaseParam>();

        //Список выбранных параметров
        private ObservableCollection<CBaseParam> selectedParamsList = new ObservableCollection<CBaseParam>();
        //Список параметров которые всегда должны быть в опросе
        private List<CBaseParam> fixedParamsList = new List<CBaseParam>();
        //Список видимых на экране параметров (адреса)
        private List<String> screenParamsList = new List<String>();
        //Firewall
        private bool firewallState = true;
        //Настройки контроллера
        private CParamsGroup controllerParams;
        //Диагностика партнёра
        private CParamsGroup pairControllerParams;
        //Список модулей контроллера
        private CModulesList modules;
        //Флаг разрешающий обновление модулей по CAN
        private Boolean enable_module_update = false;
        //Флаг одиночного ЦПУ
        private bool isCpuSingle = false;
        //Версия коллекци прошивок модулей
        private string collectionVersion = "";
        //версия сборки ЦПУ
        private string cpuVersion = "";
        //Таблица локализации параметров
        public static CLocalization Localization = new CLocalization();
        //Таблица со списком опций
        public static COptions Options = new COptions();
        //Полный список параметров 
        public static Dictionary<String, CBaseParam> ParamsList = new Dictionary<String, CBaseParam>();
        public static Dictionary<int, string> PairParamsList = new Dictionary<int, string>();
        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CConfig()
        {
            //Группа с параметрами контроллера
            this.controllerParams = new CParamsGroup(null);
            this.controllerParams.LangReference = "l_controllerParams";
            this.controllerParams.Name = CGlobal.GetResourceValue("l_controllerParams");
            this.controllerParams.GroupType = CBaseGroup.PARAMS;
            this.groupsList.Add(this.controllerParams);
            //Группа соседнего ЦПУ
            pairControllerParams = new CParamsGroup(null);
            pairControllerParams.Name = "Параметры партнёра";
            this.pairControllerParams.GroupType = CBaseGroup.PARAMS;
            this.groupsList.Add(this.pairControllerParams);
            //Группа модулей контроллера
            //Группа модулей контроллера
            this.modules = new CModulesList();
            this.modules.LangReference = "l_modules";
            this.modules.Name = CGlobal.GetResourceValue("l_modules");
            this.modules.GroupType = CBaseGroup.MODULES;
            this.groupsList.Add(this.modules);

        }

        public static CBaseParam ParamByTag(String tagname)
        {
            CBaseParam param;
            if (ParamsList.TryGetValue(tagname, out param))
                return param;

            return null;
        }

        private void addParamToFixedList(String tagname)
        {
            CBaseParam param;
            if (!ParamsList.TryGetValue(tagname, out param))
                return;

            this.fixedParamsList.Add(param);
        }

        /// <summary>
        /// Функция предварительной подготовки SQL запроса
        /// </summary>
        /// <returns></returns>
        public static String PreinitSQL()
        {
            String sql = "select ";
            foreach (String col in CConfig.Localization.Columns)
                sql += String.Format("l.{0}, ", col);
            return sql;
        }

        /// <summary>
        /// Функция подгружает настройки контроллера
        /// </summary>
        /// <param name="connection"></param>
        private void loadControllerSettings(NpgsqlConnection connection)
        {
            this.EnableModuleUpdate = false;
            //Выборка
            NpgsqlCommand cmd = new NpgsqlCommand("select * from settings where name='cpu_state'", connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    this.loadControllerSettings(reader);
                }
                finally
                {
                    reader.Close();
                }
            }
            catch
            {

            }
        }

        private void loadControllerSettings(NpgsqlDataReader reader)
        {
            //Если не получилось группы прочитать, то и делать тут нечего
            if (!reader.HasRows)
                return;

            //Получение данных
            if (!reader.Read())
                return;

            try
            {
                JObject settings = JObject.Parse(reader["value"].ToString());
                this.EnableModuleUpdate = true;
                //Только для 5.0.3
                //Появилось в 6.14.26
                IsCpuSingle = Convert.ToBoolean(settings["single_cpu_state"]);
            }
            catch
            {

            }
        }
        /// <summary>
        /// Функция возвращает полный список всех групп контроллера
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private List<CParamsGroupRecord> loadControllerGroups(NpgsqlConnection connection)
        {
            List<CParamsGroupRecord> list = new List<CParamsGroupRecord>();

            String sql = CConfig.PreinitSQL();
            sql += "g.id, g.level, g.lid, g.rid, g.readonly, g.name from groups_table g inner join localization l on g.name=l.id";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    list = this.loadControllerGroups(reader);
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        private List<CParamsGroupRecord> loadControllerGroups(NpgsqlDataReader reader)
        {
            List<CParamsGroupRecord> list = new List<CParamsGroupRecord>();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    try
                    {
                        CParamsGroupRecord record = new CParamsGroupRecord();
                        record.ru = reader["ru"] as String;
                        record.en = reader["en"] as String;
                        record.id = Convert.ToInt32(reader["id"]);
                        record.level = Convert.ToInt32(reader["level"]);
                        record.lid = Convert.ToInt32(reader["lid"]);
                        record.rid = Convert.ToInt32(reader["rid"]);
                        record.readnly = Convert.ToBoolean(reader["readonly"]);
                        record.name = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);

                        list.Add(record);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            return list;
        }
        private bool IsVisible(NpgsqlDataReader reader)
        {
            try
            {
                return Convert.ToBoolean(reader["visible"]);
            }
            catch
            {
                return true;
            }
        }
        private bool IsParamBlocked(CParamRecord param)
        {
            var array = new[]{param.plcuse};
            bool accessAllowed = false;
            int checksCount = 0;

            if (WorkerWithBitAndByte.ArrUint2Bite(array, (int)ControllerGroups.Version2))
            {
                checksCount++;
                accessAllowed = CGlobal.CurrState.PLCVersionInfo < 3f;
            }
            if (param.plc3only || WorkerWithBitAndByte.ArrUint2Bite(array, (int)ControllerGroups.Version3))
            {
                checksCount++;
                accessAllowed = accessAllowed || CGlobal.CurrState.PLCVersionInfo.Equals(3f);
            }
            if (param.plc31only || WorkerWithBitAndByte.ArrUint2Bite(array, (int)ControllerGroups.Mentorel))
            {
                checksCount++;
                accessAllowed = accessAllowed || CGlobal.CurrState.PLCVersionInfo >= 3.1f;
            }
            if (param.plc31rockchiponly || WorkerWithBitAndByte.ArrUint2Bite(array, (int)ControllerGroups.RockChip))
            {
                checksCount++;
                accessAllowed = accessAllowed || CGlobal.CurrState.IsRockChip;
            }

            if(checksCount <= 0)
                return false;

            return !accessAllowed;
        }
        //Тут скачиваются теги ЦПУ
        private List<CParamRecord> loadControllerParams(NpgsqlDataReader reader)
        {
            List<CParamRecord> list = new List<CParamRecord>();

            //Если не получилось группы прочитать, то и делать тут нечего
            if (!reader.HasRows)
                return list;
            //Эти теги не нужно видеть пользователю
            List<string> hiddenTags = new List<string>
            {
                "CDS_STOPPING",
                "CONTRACTHOUR"
            };
            //Перебор групп
            while (reader.Read())
            {
                try
                {
                    CParamRecord param = new CParamRecord();
                    //p.tag
                    param.tag = Convert.ToString(reader["tag"]);
                    //p.plc3
                    param.plc3only = Convert.ToBoolean(CAuxil.GetStrFromReader(reader, "plc3only", "false"));
                    //p.plc31rockchiponly
                    param.plc31rockchiponly = Convert.ToBoolean(CAuxil.GetStrFromReader(reader, "plc31rockchiponly", "false"));
                    //p.plc31
                    param.plc31only = Convert.ToBoolean(CAuxil.GetStrFromReader(reader, "plc31only", "false"));
                    //p.plcuse
                    param.plcuse = Convert.ToUInt32(CAuxil.GetStrFromReader(reader, "plcuse", "0"));
                    //p.visible
                    param.visible = IsVisible(reader);
                    if (IsParamBlocked(param) || hiddenTags.Contains(param.tag))
                    {
                        continue;
                    }
                    //p.diagnosticToPair
                    param.diagnosticToPair = Convert.ToBoolean(reader["diagnostic_to_pair"]);
                    //p.id
                    param.id = Convert.ToInt32(reader["id"]);
                    //p.config
                    param.config = Convert.ToBoolean(reader["config"]);
                    //p.read_only
                    param.read_only = Convert.ToBoolean(reader["readonly"]);
                    //p.type
                    param.type = Convert.ToInt32(reader["type"]);
                    //Опции
                    param.opset_name = Convert.ToString(reader["opset_name"]);
                    //ru
                    param.ru = Convert.ToString(reader["ru"]);
                    //en
                    param.en = Convert.ToString(reader["en"]);
                    //Индекс имени параметра
                    param.param_name = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);
                    //Индекс группы
                    param.group_id = Convert.ToInt32(reader["group_id"]);
                    //значение

                    list.Add(param);
                }
                catch
                {

                }
            }

            return list;
        }

        private string GetCmd(NpgsqlConnection connection)
        {
            bool isColumnPlc3Exists = CAuxil.IsColumnExists("params_table", "plc3only");
            bool isColumnPlc31Exists = CAuxil.IsColumnExists("params_table", "plc31only");
            bool isColumnRockchipExists = CAuxil.IsColumnExists("params_table", "plc31rockchiponly");
            bool isColumnPlcUseExists = CAuxil.IsColumnExists("params_table", "plcuse");
            string plc3Only = isColumnPlc3Exists ? "p.plc3only," : "";
            string plc31Only = isColumnPlc31Exists ? "p.plc31only," : "";
            string plc31RockchipOnly = isColumnRockchipExists ? "p.plc31rockchiponly," : "";
            string plcUse = isColumnPlcUseExists ? "p.plcuse," : "";
            string cmd = $"p.id, p.config, p.tag, p.readonly,p.value, {plc3Only}{plc31Only}{plc31RockchipOnly}{plcUse} p.diagnostic_to_pair, p.param_name, p.group_id, pt.code as type, os.name as opset_name from params_table p " +
                                  "inner join param_type_table pt on p.type=pt.id left join options_sets os on os.id=p.options_set " +
                                  "inner join localization l on l.id=p.param_name";
            return cmd;
        }
        private void GetParamsFromDB(NpgsqlConnection connection, out string plcVer, out string boardVer)
        {
            string sql = "select value,tag from fast_table where tag in ('PLC_VERSION', 'BOARD_VERSION')";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            boardVer = "";
            plcVer = "";
            while (reader.Read())
            {
                string name = reader["tag"].ToString();
                if (name == "PLC_VERSION")
                {
                    plcVer = reader["value"].ToString();
                }
                if (name == "BOARD_VERSION")
                {
                    boardVer = reader["value"].ToString();
                }
            }

            reader.Close();
        }
        private void PlcVersionControl(NpgsqlConnection connection)
        {
            GetParamsFromDB(connection, out string plcVer, out string boardVer);
            if (plcVer == "" && boardVer == "")
            {
                throw new Exception("Ошибка в чтении базовых параметров");
            }
            bool is31 = boardVer.Contains("3.1");
            if (is31)
            {
                CGlobal.CurrState.PLCVersionInfo = 3.1f;
            }
            else
            {
                CGlobal.CurrState.PLCVersionInfo = plcVer == "2" ? 2 : 3;
            }

        }
        /// <summary>
        /// Функция подгружает струткуру параметров контроллера, с опциями, локализаией и т.д.
        /// </summary>
        private void loadControllerStructure(NpgsqlConnection connection)
        {
            PlcVersionControl(connection);
            //Подготовка списка SQL запросов
            Queue<CSQlPair> sqlQueue = new Queue<CSQlPair>();

            //Подгрузка настроек контроллера
            CSQlPair pair = new CSQlPair("select * from settings where name='cpu_state'",
                reader =>
                {
                    this.loadControllerSettings(reader);
                });
            sqlQueue.Enqueue(pair);

            //Загрузка списка опций
            String sql = CConfig.PreinitSQL();
            sql += "o.name, o.case_sensitive, e.* from options_enums e inner join options_sets o on o.id=e.option_set_id inner join localization l on l.id=e.locale order by e.val_order";
            pair = new CSQlPair(sql,
                reader =>
                {
                    CConfig.Options.Load(reader);
                });
            sqlQueue.Enqueue(pair);

            //Подгрузка списка групп контроллера
            sql = CConfig.PreinitSQL();
            sql += "g.id, g.level, g.lid, g.rid, g.readonly, g.name from groups_table g inner join localization l on g.name=l.id";
            List<CParamsGroupRecord> groups_list = null;
            pair = new CSQlPair(sql,
                reader =>
                {
                    groups_list = this.loadControllerGroups(reader);
                });
            sqlQueue.Enqueue(pair);

            //Подгрузка списка параметров контроллера
            sql = CConfig.PreinitSQL();

            sql += GetCmd(connection);

            List<CParamRecord> params_list = null;
            pair = new CSQlPair(sql,
                reader =>
                {
                    params_list = this.loadControllerParams(reader);
                });
            sqlQueue.Enqueue(pair);
            //Вызов всех запросов
            CPostgresAuxil.Select(connection, sqlQueue);
            //Загрузка управляющих контроллером параметров
            this.controllerParams.Load(groups_list, params_list);

            List<CParamRecord> pairParamsList = params_list.ToList();
            //Параметры партнёра
            foreach (CParamRecord param in pairParamsList)
            {
                param.tag = $"PAIR_PARAM_{param.tag}";
            }
            pairControllerParams.Load(groups_list, params_list.ToList(), true);
        }
        private void FillLists()
        {
            //Формируется полный список параметров
            ParamsList.Clear();
            PairParamsList.Clear();
            pairControllerParams.FillParamsList(ParamsList);
            foreach (CBaseParam value in ParamsList.Values)
            {
                CParam param = value as CParam;
                if (param != null)
                {
                    PairParamsList.Add(param.ID, param.Tagname);
                }
            }

            this.controllerParams.FillParamsList(ParamsList);
            this.modules.FillParamsList(ParamsList);

            //Формируется список параметров которые всегда дожны быть в опросе
            this.addParamToFixedList("HOUR");
            this.addParamToFixedList("MINUTE");
            this.addParamToFixedList("SECOND");
            this.addParamToFixedList("MILLISECONDS");
            this.addParamToFixedList("YEAR");
            this.addParamToFixedList("MONTH");
            this.addParamToFixedList("DAY");
            this.addParamToFixedList("DAYWEEK");
            this.addParamToFixedList("SERNUM");
            this.addParamToFixedList("VERPRG");
            this.addParamToFixedList("INFO_STRING");
            this.addParamToFixedList("ASSEMBLY");
            this.addParamToFixedList("WDT_STATE");
            this.UpdateSelectedParamsList(null);
        }
        public void SetModulesImagesData(CModule module)
        {
            List<CImage> images = new List<CImage>();
            //Если есть код продукта, то ищем информацию по нему
            if (module.IsSupportArticule())
            {
                images = CImagesList.GetImagesByCode(module.ProductCode);
            }
            //Код продукте не поддерживает, значит модуль старый либо он ещё не прошит
            if (images.Count == 0)
            {
                //К3 сняли с производства и там не будет новых версий железа
                if (module.ModuleVesrsion == ProductVersion.K3)
                {
                    images = CImagesList.GetCompatibleProductByAdc(module.ADC_Code);
                }
                else
                {
                    images = CImagesList.GetImagesByAdcAndHard(module.ADC_Code, module.HardVersion, module.ModuleVesrsion);
                }
            }
            //Если образы не найдены, то модуль остаётся неизвестным
            if (images.Count != 0)
            {
                 var img = images[0];
                 if (images.Count > 1)
                 {
                     var withSameProduct = images.FirstOrDefault(i =>
                         (i.Product ?? "").ToLower().Trim().Equals(Convert.ToString(module.ModuleVesrsion).ToLower()) &&
                         i.Type == Convert.ToString(module.Type != 0 ? module.Type : module.ADC_Code));
                     if (withSameProduct == null)
                         return;
                     img = withSameProduct;
                 }

                 module.SetImagesData(img);
            }
        }
        public void SetModulesImagesData()
        {
            foreach (CModule module in CGlobal.Config.Modules.GroupsList)
            {
                SetModulesImagesData(module);
            }
        }
        private bool TrySQLFunction(Action<NpgsqlConnection> TryFunc, string error, NpgsqlConnection connection)
        {
            try
            {
                Action<NpgsqlConnection> Function = new Action<NpgsqlConnection>(TryFunc);
                Function.Invoke(connection);
            }
            catch (Exception ex)
            {
                MessageBox.Show(error + ex.Message, "Ошибка при загрузке конфигурации по SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Подгрузка конфигурации по SQL
        /// </summary>
        public bool Load(NpgsqlConnection connection)
        {
            this.Clear();
            //Загрузка струткуры параметров контроллера, с опциями, локализаией и т.д.
            if (!TrySQLFunction(loadControllerStructure, "Ошибка при загрузке струткуры параметров контроллера: ", connection))
                return false;
            //Загрузка описания модулей
            if (!TrySQLFunction(LoadTagsDescription, "Ошибка при загрузке описания модулей: ", connection))
                return false;
            //Подгрузка списка модулей и их параметров
            if (!TrySQLFunction(this.modules.Load, "Ошибка при загрузке данных модулей: ", connection))
                return false;
            //Заполнение тегов
            FillLists();
            LoadImagesCollection(connection);
            //Загрузка коллекции прошивок только после заполнения тегов, чтобы проверить версию
            if (TrySQLFunction(LoadImagesCollection, "Ошибка при загрузке коллекции прошивок: ", connection))
            {
                //Всем модулям присваиваем артикулы и имена в окне прошивок
                SetModulesImagesData();
            }
            return true;
        }
        private void LoadTagsDescription(NpgsqlConnection connection)
        {

            string sql = "select case when exists((select * from information_schema.tables " +
                "where table_name = 'values_descriptions_table')) then 1 else 0 end";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            cmd.CommandTimeout = 1;
            bool exists = (int)cmd.ExecuteScalar() == 1;
            if (!exists)
                return;

            sql = "select * from values_descriptions_table";
            cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    string tag = reader["tag"].ToString();
                    string localization = reader["localization"].ToString();
                    string description = reader["description"].ToString();
                    TagsDescriptions.Add(tag, new string[2] { localization, description });
                }
            }
            catch
            {

            }
            finally
            {
                reader.Close();
            }
        }
        private bool IsTheSameCollection(out CParam colVers)
        {
            colVers = CConfig.ParamByTag("MODULES_COLLECTION_VERSION") as CParam;
            return CGlobal.CurrState.ModulesCollectionVersion == colVers.ValueString;
        }
        private void LoadImagesCollection(NpgsqlConnection connection)
        {
            if (IsTheSameCollection(out CParam colVers))
            {
                return;
            }

            CGlobal.CurrState.ModulesCollectionVersion = colVers.ValueString;
            CImagesList = new CImagesList();
            String sql = "select * from module_update_table order by id";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, CGlobal.Session.Connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                CImage image = new CImage();
                image.LoadFromDB(reader);
                CImagesList.AddProduct(image);
            }
            reader.Close();
        }
        /// <summary>
        /// Функция в соответствии с выбранным языком выставляет локализацию для все групп и параметров
        /// </summary>
        public void ChangeLanguage()
        {
            foreach (CBaseGroup group in this.groupsList)
                group.ChangeLanguage();
        }

        /// <summary>
        /// Очистка конфигурации
        /// </summary>
        public void Clear()
        {
            this.fixedParamsList.Clear();
            this.visibleParamsList.Clear();
            //this.groupsList.Clear();
            CConfig.Localization.Clear();
            CConfig.Options.Clear();
        }

        public ObservableCollection<CBaseGroup> GroupsList
        {
            get
            {
                return this.groupsList;
            }
        }

        public void UpdateSelectedParamsList(CBaseGroup group)
        {
            this.visibleParamsList.Clear();
            if (group == null)
                return;

            List<CBaseParam> list = new List<CBaseParam>();
            group.FillParamsList(list);
            foreach (CBaseParam param in list)
                this.visibleParamsList.Add(param);

        }

        public void UpdateModulesParamsList(CBaseGroup group)
        {
            this.modulesParamsList.Clear();
            if (group == null)
                return;

            List<CBaseParam> list = new List<CBaseParam>();
            group.FillParamsList(list);
            foreach (CBaseParam param in list)
            {
                CModuleParam mParam = param as CModuleParam;
                this.modulesParamsList.Add(param);
            }

        }

        /// <summary>
        /// Функция возвращает список активных тегов
        /// Т.е. это выбранные и те что должны опрашиваться постоянно
        /// </summary>
        /// <returns></returns>
        public List<CParam> GetActiveTags()
        {
            List<CParam> list = new List<CParam>();
            lock (this.VisibleParamsList)
            {
                foreach (CBaseParam param in this.visibleParamsList)
                {
                    if (param is CParam)
                        list.Add(param as CParam);
                }
            }

            //Список дополняется параметрами которые обязательно должны быть в опросе
            foreach (CParam param in fixedParamsList)
            {
                string tag = param.Tagname;
                CParam p = list.Find(x => x.Tagname == tag);
                if (p == null)
                    list.Add(param);
            }

            return list;
        }
        //key - tag, value - tag_localization + description
        public SortedDictionary<string, string[]> TagsDescriptions = new SortedDictionary<string, string[]>();

        public ObservableCollection<CBaseParam> VisibleParamsList
        {
            get
            {
                return this.visibleParamsList;
            }
        }

        /// <summary>
        /// Список параметров для вывода в пользовательский интерфейс
        /// </summary>
        public ObservableCollection<CBaseParam> ModulesParamsList
        {
            get
            {
                return this.modulesParamsList;
            }
        }

        public List<CBaseParam> FixedParamsList
        {
            get
            {
                return this.fixedParamsList;
            }
        }

        public List<String> ScreenParamsList
        {
            get
            {
                return this.screenParamsList;
            }
        }

        public ObservableCollection<CBaseParam> SelectedParamsList
        {
            get => selectedParamsList;
        }

        public CParamsGroup ControllerParams
        {
            get
            {
                return this.controllerParams;
            }
        }

        public CModulesList Modules
        {
            get
            {
                return this.modules;
            }
        }
        public CImagesList CImagesList = null;
        /// <summary>
        /// Флаг разрешающий обновление модулей по CAN
        /// </summary>
        public Boolean EnableModuleUpdate
        {
            get
            {
                return true;
            }
            set
            {
                this.enable_module_update = value;
                this.OnPropertyChanged("EnableModuleUpdate");
            }
        }
        /// <summary>
        /// Флаг одиночного ЦПУ
        /// </summary>
        public Boolean IsCpuSingle
        {
            get
            {
                return this.isCpuSingle;
            }
            set
            {
                this.isCpuSingle = value;
                this.OnPropertyChanged("IsCpuSingle");
            }
        }
        /// <summary>
        /// Состояние сетевого экрана
        /// </summary>
        public bool FirewallState
        {
            get => firewallState;
            set
            {
                this.firewallState = value;
                this.OnPropertyChanged("FirewallState");
            }
        }
        /// <summary>
        /// Версия коллекции прошивки модулей
        /// </summary>
        public string CollectionVersion
        {
            get
            {
                return collectionVersion;
            }
            set
            {
                this.collectionVersion = value;
                this.OnPropertyChanged("CollectionVersion");
            }
        }

        /// <summary>
        /// Версия сборки цпу
        /// </summary>
        public string CPUVersion
        {
            get
            {
                return cpuVersion;
            }
            set
            {
                this.cpuVersion = value;
                this.OnPropertyChanged("CPUVersion");
            }
        }
        /// <summary>
        /// Сохранение конфигурации контроллера в xml файле
        /// </summary>
        /// <param name="saveNode">Узел XML документа в который сохраняется конфигурация</param>
        public void Save(XmlNode saveNode)
        {
            //Сохраняется настройка конфигуграции
            XmlNode configNode = saveNode.OwnerDocument.CreateElement("element", "config", "");
            saveNode.AppendChild(configNode);

            foreach (CBaseGroup group in this.groupsList)
                group.Save(configNode);

            //Сохраняется таблица логализации
            XmlNode localizationNode = saveNode.OwnerDocument.CreateElement("element", "localization", "");
            saveNode.AppendChild(localizationNode);
            CConfig.Localization.Save(localizationNode);
        }

        /// <summary>
        /// Подгрузка настроек из XML файла
        /// </summary>
        /// <param name="loadNode">Узел в котором размещены настройки</param>
        public void Load(XPathNavigator loadNode)
        {
            this.Clear();
            ParamsList.Clear();
            //Вначале осуществляется загрузка таблицы локализации
            //т.к. в последующем даныне из неё понадобятся для формирования имён
            XPathNavigator localizationNode = loadNode.SelectSingleNode("localization");
            CConfig.Localization.Load(localizationNode);

            //Теперь формируется конфигурация
            XPathNavigator configNode = loadNode.SelectSingleNode("config");
            foreach (XPathNavigator groupNode in configNode.Select("group"))
            {
                String groupType = CXML.getAttributeValue(groupNode, "type", "");
                switch (groupType)
                {
                    //Группа параметров вычислителя
                    case CBaseGroup.PARAMS:
                        this.controllerParams.Clear();
                        this.controllerParams.Load(groupNode);
                        break;

                    //Групп с описанием модулей контроллера
                    case CBaseGroup.MODULES:
                        this.modules.Clear();
                        this.modules.Load(groupNode);
                        break;
                }
            }

            //Формируется полный список параметров
            this.controllerParams.FillParamsList(ParamsList);
            this.modules.FillParamsList(ParamsList);
        }
    }
}
