using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using System.Collections.ObjectModel;
using System.Windows;
using System.Xml.XPath;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Globalization;
using System.Collections;
using AbakConfigurator.Secure;
using System.Text.RegularExpressions;

namespace AbakConfigurator
{
    /// <summary>
    /// Коды состояний модулей
    /// </summary>
    public enum ModuleState
    {
        Initialising	= 0,
	    Disconnected	= 1,
	    Connecting		= 2,
	    Preparing		= 3,
	    Stopped			= 4,
	    Operational		= 5, 
	    PreOperational	= 127,
        /*
         * Состояния для внутреннего использования
         */
        Unknown         = 0xF0, //Статус модуля неизвестен
        Failed          = 0xFF	//Во время работы произошла ошибка
    }
    public enum ProductVersion
    {
        K2 = 0,
        K3 = 1,
        K31 = 2
    }
    public class CModuleParam : CBaseParam
    {
        /// <summary>
        /// Класс с границами масштабирования
        /// </summary>
        private class CScale : ICloneable
        {
            //Настройки масштабирования
            private Double raw_hi = 0;
            private Double raw_lo = 0;
            private Double eu_hi = 0;
            private Double eu_lo = 0;
            //Значение внешнего параметра при котром отрабатывает этот класс
            private String value = "";

            public double RawHi { get => raw_hi; set => raw_hi = value; }
            public double RawLo { get => raw_lo; set => raw_lo = value; }
            public double EuHi { get => eu_hi; set => eu_hi = value; }
            public double EuLo { get => eu_lo; set => eu_lo = value; }
            public string Value { get => value; set => this.value = value; }

            public Double CalculateEu(Double raw_value)
            {
                return CAuxil.Scale(this.eu_hi, this.eu_lo, this.raw_hi, this.raw_lo, raw_value);
            }

            public Double CalculateRaw(Double eu_value)
            {
                return CAuxil.Scale(this.raw_hi, this.raw_lo, this.eu_hi, this.eu_lo, eu_value);
            }

            public object Clone()
            {
                CScale clone = new CScale();

                clone.raw_hi = this.raw_hi;
                clone.raw_lo = this.raw_lo;
                clone.eu_hi = this.eu_hi;
                clone.eu_lo = this.eu_lo;

                return clone;
            }
        }

        //Ссылка на тег, значение которого определяет "подстветку" значения тега
        private bool backlight_var = false;
        //Тег, определяющий подсвечивание тега (True)
        private bool backlight_on = false;
        //Ссылка на тег значение которого опеределяет переключение масштабирующих границ
        private String scale_ref = "";
        //Список границ масштабирования
        private Dictionary<String, CScale> scaleList = new Dictionary<String, CScale>();
        //Список границ лимитов
        public Dictionary<String, double []> LimitsList = new Dictionary<String, double[]>();
        public CModuleParam()
        {

        }
        public double[] GetScaleLimits()
        {
            if (this.scale_ref == "")
            {
                if (LimitsList.Count() != 0)
                    return LimitsList[""];
                else
                    return new double[0];
            }
            //Есть указатель на значение тега, который переключает шкалу масштабирования
            CBaseParam ref_param;
            if (!CConfig.ParamsList.TryGetValue(this.scale_ref, out ref_param))
                return new double[0];

            //Нашёлся параметр
            double [] limits;
            if (!this.LimitsList.TryGetValue(ref_param.Value.ToString().ToLower(), out limits))
                return new double[0];

            return limits;
        }
        protected override CBaseParam createParam()
        {
            return new CModuleParam();
        }

        public override void Assign(CBaseParam param)
        {
            base.Assign(param);

            CModuleParam p = param as CModuleParam;

            //Ссылка на тег значение которого опеределяет переключение масштабирующих границ
            this.scale_ref = p.scale_ref;
            //Копирование списка масштабирования
            foreach(KeyValuePair<String, CScale> pair in p.scaleList)
            {
                CScale bound = pair.Value.Clone() as CScale;
                this.scaleList.Add(pair.Key, bound);
            }
        }


        public string NumToRound(double val, int point)
        {
            if (point == -1)
            {
                return Math.Round(val, 4).ToString();
            }
            return Math.Round(val, point).ToString($"F{point}", CultureInfo.CurrentCulture);
        }

        private object RoundValue(object value)
        {
            object valueForConvertation = value;
            double doubleValue = 0;
            try
            {
                doubleValue = Convert.ToDouble(valueForConvertation.ToString().Replace(",", "."));
            }
            catch
            {
                return value;
            }
            
            return Math.Round(doubleValue, 6);
        }

        protected override object getScaleValue()
        {
            if(this.value == null || this.value.ToString() == "")
                return "";

            switch (this.type)
            {
                case ParamType.FLOAT:
                case ParamType.INT:
                case ParamType.UINT:

                    if (this.scaleList.Count == 0)
                    {
                        string var = base.getScaleValue().ToString();
                        if (this.hex)
                            return var;

                        var = CAuxil.AdaptFloat(var);
                        return NumToRound(Convert.ToDouble(var), PrecisionDecimalPoint);
                    }
                    //Есть что масштабировать
                    //Указатель на параметр значение которого используется для переключения границ масштабирования
                    if (this.scale_ref != "")
                    {
                        //Есть указатель на значение тега, который переключает шкалу масштабирования
                        CBaseParam ref_param;
                        if (CConfig.ParamsList.TryGetValue(this.scale_ref, out ref_param))
                        {
                            //Нашёлся параметр
                            CScale bounds;
                            if (this.scaleList.TryGetValue(ref_param.Value.ToString().ToLower(), out bounds))
                            {
                                string var = CAuxil.AdaptFloat(this.value.ToString());
                                Double raw_value = Convert.ToDouble(var);
                                return NumToRound(Convert.ToDouble(bounds.CalculateEu(raw_value)), PrecisionDecimalPoint);
                            }
                        }
                    }
                    else
                    {
                        //Нет указателя на тег который переключает шкалу, значит берётся первое значение в списке
                        CScale bounds;
                        if (this.scaleList.TryGetValue("", out bounds))
                        {
                            string var = CAuxil.AdaptFloat(this.value.ToString());
                            Double raw_value = Convert.ToDouble(var);
                            return NumToRound(Convert.ToDouble(bounds.CalculateEu(raw_value)), PrecisionDecimalPoint);
                            //return bounds.CalculateEu(raw_value);
                        }
                    }

                    //Нет такого параметра, странно
                    return base.getScaleValue();

                default:
                    return base.getScaleValue();

                case ParamType.BOOLEAN:
                    //Если есть параметр подстветки и канал DI включен то подсвечиваем его
                    bool val = Convert.ToBoolean(this.value);
                    if (Backlight_var && val)
                        Backlight_on = true;
                    else
                        Backlight_on = false;

                    return val;
            }
        }

        protected override void setScaleValue(object value)
        {
            switch (this.type)
            {
                case ParamType.FLOAT:
                case ParamType.INT:
                case ParamType.UINT:
                    try
                    {
                        if (this.scaleList.Count == 0)
                        {
                            //Список масштабирования пустой, значит возвращается значение по умолчанию
                            base.setScaleValue(value);
                            break;
                        }

                        CBaseParam ref_param;
                        if (CConfig.ParamsList.TryGetValue(this.scale_ref, out ref_param))
                        {
                            if (ref_param.Value == null)
                            {
                                base.setScaleValue(value);
                                break;
                            }
                            //Нашёлся параметр
                            CScale bounds;

                            if (this.scaleList.TryGetValue(ref_param.Value.ToString().ToLower(), out bounds))
                            {
                                Double eu_value = Convert.ToDouble(CAuxil.AdaptFloat(value.ToString()));
                                Double raw_value = bounds.CalculateRaw(eu_value);
                                base.setScaleValue(Math.Round(raw_value));
                                break;
                            }
                        }
                        else
                        {
                            CScale bounds;
                            if (this.scaleList.TryGetValue("", out bounds))
                            {
                                Double eu_value = Convert.ToDouble(CAuxil.AdaptFloat(value.ToString()));
                                Double raw_value = bounds.CalculateRaw(eu_value);

                                base.setScaleValue(raw_value);
                                break;
                            }
                        }

                        //Нет такого параметра, странно
                        base.setScaleValue(value);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format(CGlobal.GetResourceValue("l_moduleFailedToWriteValue"), ex.Message), Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;

                default:
                    base.setScaleValue(value);
                    break;
            }
        }

        public override XmlNode Save(XmlNode saveNode)
        {
            XmlNode paramNode = base.Save(saveNode);

            //Сохранение настроек масштабирования

            //Список шкал
            if (this.scaleList.Count > 0)
            {
                XmlNode scalesNode = paramNode.OwnerDocument.CreateElement("element", "scales", "");
                paramNode.AppendChild(scalesNode);

                //Ссылка на параметр от которого зависит переключение шкалы
                XmlAttribute attr = scalesNode.OwnerDocument.CreateAttribute("scaleref");
                attr.Value = this.scale_ref;
                scalesNode.Attributes.Append(attr);

                foreach (KeyValuePair<String, CScale> pair in this.scaleList)
                {
                    CScale scale = pair.Value;

                    XmlNode boundsNode = saveNode.OwnerDocument.CreateElement("element", "bounds", "");
                    scalesNode.AppendChild(boundsNode);

                    //Настройки масштабирования
                    attr = boundsNode.OwnerDocument.CreateAttribute("rawhi");
                    attr.Value = scale.RawHi.ToString();
                    boundsNode.Attributes.Append(attr);
                    attr = boundsNode.OwnerDocument.CreateAttribute("rawlo");
                    attr.Value = scale.RawLo.ToString();
                    boundsNode.Attributes.Append(attr);
                    attr = boundsNode.OwnerDocument.CreateAttribute("euhi");
                    attr.Value = scale.EuHi.ToString();
                    boundsNode.Attributes.Append(attr);
                    attr = boundsNode.OwnerDocument.CreateAttribute("eulo");
                    attr.Value = scale.EuLo.ToString();
                    boundsNode.Attributes.Append(attr);
                    //Значение параметра переключающего на шкалу
                    attr = boundsNode.OwnerDocument.CreateAttribute("boundvalue");
                    attr.Value = scale.Value;
                    boundsNode.Attributes.Append(attr);
                }
            }

            return paramNode;
        }

        public override void Load(XPathNavigator loadNode)
        {
            base.Load(loadNode);
            //Поиск тега указывающий на необходимость подсветки
            this.backlight_var = Convert.ToBoolean(CXML.getAttributeValue(loadNode, "backlight", "false"));
            //Поиск узла с настройками масштабирования
            XPathNavigator scalesNode = loadNode.SelectSingleNode("scales");
            if (scalesNode == null)
                return;

            //Ссылка на параметр от которого зависит переключение шкалы
            this.scale_ref = CXML.getAttributeValue(scalesNode, "scaleref", "");
            //Настройки масштабирования
            foreach(XPathNavigator boundsNode in scalesNode.Select("bounds"))
            {
                CScale bounds = new CScale();

                //Настройки масштабирования
                bounds.RawHi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "rawhi", "0")));
                bounds.RawLo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "rawlo", "0")));
                bounds.EuHi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "euhi", "0")));
                bounds.EuLo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "eulo", "0")));
                //Значение параметра перключающего шкалу
                bounds.Value = CXML.getAttributeValue(boundsNode, "boundvalue", "");

                this.scaleList.Add(bounds.Value, bounds);
            }
        }

        public bool Backlight_var { get => backlight_var; set => backlight_var = value; }

        public bool Backlight_on { get => backlight_on; set => backlight_on = value; }
        public void LoadStructure(XPathNavigator node, String tagname)
        {
            this.index = Convert.ToInt32(CXML.getAttributeValue(node, "index", "0x0"), 16);
            //Флаг показывающий что параметр конфигурируемый
            this.config = Convert.ToBoolean(CXML.getAttributeValue(node, "config", "false"));
            //Флаг только чтение
            if (CGlobal.Handler != null && CGlobal.Handler.Auth.GroupType >= GroupTypeEnum.Spectator)
            {
                readOnly = true;
            }
            else
            {
                this.readOnly = Convert.ToBoolean(CXML.getAttributeValue(node, "readonly", "false"));
            }
            this.hex = Convert.ToBoolean(CXML.getAttributeValue(node, "hex", "false"));
            this.isversion = Convert.ToBoolean(CXML.getAttributeValue(node, "version", "false"));
            //Название параметра
            this.nameKey = CConfig.Localization.GetLocalesFromXmlNode(node);
            this.name = CConfig.Localization.GetLocaleValue(this.nameKey);
            //Указатель на настройки масштабирования
            String scale_name = CXML.getAttributeValue(node, "scale", "");
            SetLimits(node, scale_name, tagname);
            //Тэг указывающий количество знаков после запятой
            PrecisionDecimalPoint = Convert.ToInt32(CXML.getAttributeValue(node, "point", "-1"));
            //Тег параметра
            this.tagname = String.Format("{0}.{1}", tagname, CXML.getAttributeValue(node, "name", "")).ToLower();
            //Тег параметра, который отвечает за подсветку значения
            this.backlight_var = Convert.ToBoolean(CXML.getAttributeValue(node, "backlight", "false"));
            if (scale_name != "")
            {
                //Есть настройки масштабирования
                //Поиск узла с настройками
                XPathNavigator scalesNode = node.SelectSingleNode(String.Format("//scale[@name='{0}']", scale_name));
                if (scalesNode == null)
                    return;
                Double raw_lo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(scalesNode, "lo", "0")));
                Double raw_hi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(scalesNode, "hi", "0")));
                String switch_param = CXML.getAttributeValue(scalesNode, "switch", "");
                if (switch_param != "")
                {
                    this.scale_ref = String.Format("{0}.{1}", tagname, switch_param).ToLower();
                    foreach (XPathNavigator boundsNode in scalesNode.Select("bounds"))
                    {
                        Double eu_lo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "lo", "0")));
                        Double eu_hi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "hi", "0")));
                        String val = CXML.getAttributeValue(boundsNode, "value", "").ToLower();
                        CScale scale = new CScale();
                        scale.RawHi = raw_hi;
                        scale.RawLo = raw_lo;
                        scale.EuHi = eu_hi;
                        scale.EuLo = eu_lo;
                        scale.Value = val;

                        this.scaleList.Add(scale.Value, scale);
                    }
                }
                else
                {
                    XPathNavigator boundsNode = scalesNode.SelectSingleNode("bounds");
                    Double eu_lo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "lo", "0")));
                    Double eu_hi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "hi", "0")));

                    CScale scale = new CScale();
                    scale.RawHi = raw_hi;
                    scale.RawLo = raw_lo;
                    scale.EuHi = eu_hi;
                    scale.EuLo = eu_lo;
                    scale.Value = "";
                    
                    this.scaleList.Add(scale.Value, scale);
                }

            }
            //Тип параметра
            String typeString = CXML.getAttributeValue(node, "type", "uint16");
            switch(typeString)
            {
                case "uint8":
                case "uint16":
                case "uint32":
                case "uint64":
                    this.type = ParamType.UINT;
                    break;

                case "int8":
                case "int16":
                case "int32":
                case "int64":
                    this.type = ParamType.INT;
                    break;

                case "real32":
                case "real64":
                    this.type = ParamType.FLOAT;
                    break;

                case "boolean":
                    this.type = ParamType.BOOLEAN;
                    break;

                case "string":
                    this.type = ParamType.STRING;
                    break;
            }

            //Подсказка
            hint = CXML.getAttributeValue(node, "hint", "");

            //Поиск узла с опциями
            String options = CXML.getAttributeValue(node, "options", "");
            if (options == "")
                return;

            //Получение информации о модуле для которого собирается тег
            XPathNavigator moduleNode = node.SelectSingleNode("//module");
            String moduleName = CXML.getAttributeValue(moduleNode, "name", "");
            String moduleType = CXML.getAttributeValue(moduleNode, "type", "");
            String moduleRev = CXML.getAttributeValue(moduleNode, "revision", "");

            //Получение указатель на набор опций
            String optionSetName = String.Format("{0}_{1}_{2}_{3}", moduleName, moduleType, moduleRev, options);
            COptionSet optionSet;
            if (!CConfig.Options.OptionSets.TryGetValue(optionSetName, out optionSet))
            {
                optionSet = new COptionSet();
                optionSet.Name = optionSetName;
                CConfig.Options.OptionSets.Add(optionSet.Name, optionSet);

                //Перебор опциональных значений
                XPathNavigator optionsNode = node.SelectSingleNode(String.Format("//option[@name='{0}']", options));
                if (optionsNode == null)
                    return;

                foreach (XPathNavigator pairNode in optionsNode.Select("pair"))
                {
                    COption option = new COption();
                    option.Value = CXML.getAttributeValue(pairNode, "value", "").ToLower();
                    option.Locale = CConfig.Localization.GetLocalesFromXmlNode(pairNode);

                    optionSet.Options.Add(option.Value, option);
                }
            }
            this.options = optionSet;
        }
        private void SetLimits(XPathNavigator node, String scale_name, String tagname)
        {
            string[] limits = new string[2];
            limits[0] = CXML.getAttributeValue(node, "lo", "");
            limits[1] = CXML.getAttributeValue(node, "hi", "");
            this.limit = limits[0] != "" && limits[1] != "";
            if (!limit)
                return;

            double lo = Convert.ToDouble(limits[0]);
            double hi = Convert.ToDouble(limits[1]);
            if (scale_name == "")
            {
                this.LimitsList.Add("", new double[2] { lo, hi });
                return;
            }
            //Есть настройки масштабирования
            //Поиск узла с настройками
            XPathNavigator scalesNode = node.SelectSingleNode(String.Format("//scale[@name='{0}']", scale_name));
            if (scalesNode == null)
                return;

            double rawLo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(scalesNode, "lo", "0")));
            double rawHi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(scalesNode, "hi", "0")));
            String switch_param = CXML.getAttributeValue(scalesNode, "switch", "");
            if (switch_param != "")
            {
                this.scale_ref = String.Format("{0}.{1}", tagname, switch_param).ToLower();
                foreach (XPathNavigator boundsNode in scalesNode.Select("bounds"))
                {
                    //Пример:
                    //50000 - код ацп, 20 - hi => 50000/20 = 2500; 
                    //2500 - 1 физ. величина
                    double[] range = new double[2] { 0, 0 };
                    Double eu_lo = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "lo", "0")));
                    if (eu_lo != 0 && lo != 0 && rawLo != 0)
                        range[0] = lo / (rawLo / eu_lo);

                    Double eu_hi = Convert.ToDouble(CAuxil.AdaptFloat(CXML.getAttributeValue(boundsNode, "hi", "0")));
                    if (eu_hi != 0 && hi != 0 && rawHi != 0)
                        range[1] = hi / (rawHi / eu_hi);

                    string val = CXML.getAttributeValue(boundsNode, "value", "").ToLower();
                    this.LimitsList.Add(val, range);
                }

            }
        }
        /// <summary>
        /// Возвращает указатель на модуль которому принадлежит параметр
        /// </summary>
        public CModule Module
        {
            get
            {
                CModuleGroup moduleGroup = this.group as CModuleGroup;
                return moduleGroup.Module;
            }
        }
    }

    /// <summary>
    /// Класс описывает канал модуля
    /// </summary>
    public class CModuleGroup : CBaseGroup
    {
        private UInt16 number = 0;

        public CModuleGroup()
        {

        }

        public CModuleGroup(CBaseGroup parent)
            : base(parent)
        {

        }

        protected override String getName()
        {
            if (this.number == 0)
                return base.getName();
            else
                return String.Format("{0} №{1}", this.name, this.number);
        }

        protected override CBaseParam createParam()
        {
            return new CModuleParam();
        }

        protected override CBaseGroup createGroup()
        {
            return new CModuleGroup(this);
        }

        public override XmlNode Save(XmlNode saveNode)
        {
            XmlNode groupNode = base.Save(saveNode);

            //Номер группы
            if (this.number != 0)
            {
                XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute("number");
                attr.Value = this.number.ToString();
                groupNode.Attributes.Append(attr);
            }

            return groupNode;
        }

        public override void Load(XPathNavigator loadNode)
        {
            //Подгрузка номера группы
            this.number = Convert.ToUInt16(CXML.getAttributeValue(loadNode, "number", "0"));

            base.Load(loadNode);
        }

        public UInt16 Number
        {
            get
            {
                return this.number;
            }
            set
            {
                if (this.number == value)
                    return;

                this.number = value;
                this.OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// Возвращает указатель на модуль которому принадлежит группа
        /// </summary>
        public CModule Module
        {
            get
            {
                if (this is CModule)
                    return this as CModule;

                if (this.ParentGroup == null)
                    return null;

                if (this.ParentGroup is CModule)
                    return this.ParentGroup as CModule;

                CModuleGroup parent = this.ParentGroup as CModuleGroup;
                return parent.Module;
            }
        }
    }

    /// <summary>
    /// Класс описывает модуль контроллера
    /// </summary>
    public class CModule : CModuleGroup
    {
        //CAN node_id
        protected UInt16 node_id;
        //Текущее состояние модуля
        protected ModuleState state;
        //Флаг активности, т.е. наличия модуля на шине
        protected Boolean active = false;
        //Флаг того что конфигурация модуля сохранена в базе данных
        protected Boolean persistent = false;
        //Строка описывающая тип модуля
        protected String typeString;
        //Строка описывающая тип модуля
        protected String module_type = "Неизвестное устройство";
        //Строка описывающая имя модуля
        protected String module_name = "Неизвестное устройство";
        protected String productCode = "0";
        //Артикул
        protected string orderCode = "Отсутствует";
        //Тип модуля, после прошивки
        protected string realType;
        //Код типа модуля (локальный)
        protected UInt32 type;
        //Номер ревизии модуля
        protected UInt16 revision;
        //Версия программного обеспечения
        protected String soft_version;
        //Версия платы
        protected String hard_version;
        //Состояние процедуры обновления 
        private UInt16 update_state;
        //Прогрес выполнения обновления
        private UInt16 progress;
        public UInt16 stopprogress = 0;
        //Рсашифровка текущего состояния
        private String update_text = "";
        //Флаг наличия модуля в листе
        private Boolean isthismoduleexists = false;
        private Boolean ischecked = false;
        //Код АЦП
        private UInt32 adc_code = 0;
        private Double greencolor = 0;
        private Double redcolor = 0;
        //Подтип модуля (для новых верский К3)
        private int subType = 0;
        //Список прошивок для модуля
        private List<CImage> images = new List<CImage>();
        public CModule(CBaseGroup parent)
            : base(parent)
        {
        }

        protected override CBaseGroup createGroup()
        {
            return new CModuleGroup(this);
        }
        /// <summary>
        /// Функция задаёт список прошивок и их информацию
        /// </summary>
        /// <param name="image"></param>
        public void SetImagesData(CImage image)
        {
            if(!string.IsNullOrEmpty(image.OrderCode))
            {
                //Нам подойдёт любая прошивка
                orderCode = image.OrderCode;
            }

            //Нам подойдёт любая прошивка
            module_type = image.HardAdc;
            module_name = image.Name;
        }
        /// <summary>
        /// В зависимости от состояния модуля выставляется его изображение
        /// </summary>

        public bool IsThisModuleExists
        {
            get
            {
                return this.isthismoduleexists;
            }
            set
            {
                if (value != this.isthismoduleexists)
                    this.isthismoduleexists = value;
            }
        }

        private void setImage()
        {
            if (this.persistent)
            {
                if (this.active)
                    this.ImageSource = "/AbakConfigurator;component/icons/save480.png";
                else
                    this.ImageSource = "/AbakConfigurator;component/icons/save480gray.png";

            }
            else
                this.ImageSource = null;
        }
        
        private static ModuleState StateFromString(String stateString)
        {
            switch (stateString)
            {
                case "Initialising":
                    return ModuleState.Initialising;

                case "Disconnected":
                    return ModuleState.Disconnected;

                case "Connecting":
                    return ModuleState.Connecting;

                case "Preparing":
                    return ModuleState.Preparing;

                case "Stopped":
                    return ModuleState.Stopped;

                case "Operational":
                    return ModuleState.Operational;

                case "PreOperational":
                    return ModuleState.PreOperational;
            }

            return ModuleState.Initialising;
        }

        public String Module_Type
        {
            get
            {
                return this.module_type;
            }
            set
            {
                this.module_type = value;
                this.OnPropertyChanged("Module_Type");
            }
        }
        public ProductVersion ModuleVesrsion
        {
            get
            {
                Regex rgx = new Regex("[^0-9]");
                string str = rgx.Replace(hard_version, "");
                if (str.Length == 8)
                {
                    return ProductVersion.K31;
                }
                else
                {
                    return nameKey.ToLower().Contains("k2") ? ProductVersion.K2 : ProductVersion.K3;
                }
            }
        }
        public String Module_Name
        {
            get
            {
                return this.module_name;
            }
            set
            {
                this.module_name = value;
                this.OnPropertyChanged("Module_Name");
            }
        }
        /// <summary>
        /// Список прошивок, поддерживаемые модулем
        /// </summary>
        public List<CImage> Images => images;
        public string TypeString 
        { get => this.typeString; }
        public String ProductCode { get => productCode; set => productCode = value; }
        public string OrderCode
        {
            get
            {
                return this.orderCode;
            }
            set
            {
                this.orderCode = value;
                this.OnPropertyChanged("OrderCode");
            }
        }
        protected override String getName()
        {
            return String.Format("{0} {1} ({2})", this.name, this.node_id, this.typeString);
        }
        /// <summary>
        /// Функция вернёт false если модуль не поддерживает артикулы
        /// </summary>
        /// <returns></returns>
        public bool IsSupportArticule()
        {
            return this.productCode != "0";
        }
        /// <summary>
        /// Функция вернёт false если в модуле нет ПО
        /// </summary>
        /// <returns></returns>
        public bool IsModuleFlashed()
        {
            var str = soft_version.Replace(".", string.Empty).ToList();
            bool test = str.Any(c => c == '0' && c != ' ');
            return soft_version.Replace(".", string.Empty).Any(c => c != '0' && c != ' ');
        }
        private void loadGroupStructure(CModuleGroup moduleGroup, XPathNavigator groupNode, String tagname)
        {
            String templateName = CXML.getAttributeValue(groupNode, "template", "");
            
            XPathNavigator paramsNode;
            if (templateName == "")
            {
                //Шаблона нет, значит надо перебирать параметры переданной группы
                paramsNode = groupNode;
            }
            else
            {
                //Шаблон есть, значит надо найти узел с его описанием
                paramsNode = groupNode.SelectSingleNode(String.Format("//template[@name='{0}']", templateName));
            }

            //Создание экземпляра группы
            CModuleGroup group = new CModuleGroup(moduleGroup);
            moduleGroup.GroupsList.Add(group);
            //Формируется запись в таблице локализации
            group.NameKey = CConfig.Localization.GetLocalesFromXmlNode(paramsNode);
            //Имя параметра из таблицы локализации
            group.Name = CConfig.Localization.GetLocaleValue(group.NameKey);

            tagname = String.Format("{0}.{1}", tagname, CXML.getAttributeValue(paramsNode, "type", ""));
            String groupNumberString = CXML.getAttributeValue(groupNode, "num", "");
            if (groupNumberString != "")
            {
                tagname = String.Format("{0}.{1}", tagname, groupNumberString);
                group.Number = Convert.ToUInt16(groupNumberString);
            }
            foreach (XPathNavigator paramNode in paramsNode.Select("param"))
            {
                //Формирование параметров
                CModuleParam param = new CModuleParam();
                param.LoadStructure(paramNode, tagname);
                param.GroupName = group.Name;
                param.Group = group;
                group.ParamsList.Add(param);
            }
            foreach (XPathNavigator node in groupNode.Select("group"))
            {
                this.loadGroupStructure(group, node, tagname);
            }
        }

        public void GenerateConfigStructureFromTemplate(XPathDocument doc)
        {
            XPathNavigator nav = doc.CreateNavigator();

            //Получение указателья на группу Module
            XPathNavigator moduleNode = nav.SelectSingleNode("//module");
            //Имя модуля
            String key = String.Format("{0}.{1}", this.typeString, this.node_id);
            this.nameKey = CConfig.Localization.GetLocalesFromXmlNode(moduleNode, key);
            this.name = CConfig.Localization.GetLocaleValue(this.nameKey);

            //Проход по группам
            XPathNavigator groupsNode = moduleNode.SelectSingleNode("groups");
            //Заготовка полного имени тега параметра
            String tagname = String.Format("{0}.{1}", this.typeString, this.node_id);
            foreach (XPathNavigator groupNode in groupsNode.Select("group"))
                this.loadGroupStructure(this, groupNode, tagname);
        }

        public void GenerateConfigStructureFromTemplate(CModulesList.ModuleTemplate template)
        {
            //Строка описывающая тип модуля
            this.typeString = template.Name;
            this.GenerateConfigStructureFromTemplate(template.Doc);
        }

        protected virtual void loadState(NpgsqlDataReader reader)
        {
            //Текущее состояние модуля
            this.state = (ModuleState)Convert.ToUInt32(reader["state"]);

            this.OnPropertyChanged("StateString");
        }

        /// <summary>
        /// Функция читает из базы конфигурацию модуля и его шаблон и строит основываясь на этом дерево групп и параметров
        /// </summary>
        /// <param name="reader"></param>
        public virtual void LoadConfig(NpgsqlDataReader reader)
        {
            //node_id узла
            this.node_id = Convert.ToUInt16(reader["node_id"]);
            //Текущее состояние модуля
            this.loadState(reader);
            //Флаг активности
            this.Active = Convert.ToBoolean(reader["active"]);
            //Флаг постоянности
            this.Persistent = Convert.ToBoolean(reader["persistent"]);
            //Код типа модуля
            this.type = Convert.ToUInt32(reader["type"]);
            //Номер ревизии модуля
            this.revision = Convert.ToUInt16(reader["revision"]);
            //Номер версии ПО
            this.soft_version = Convert.ToString(reader["swver"]);
            //Номер версии платы
            this.hard_version = Convert.ToString(reader["hwver"]);
            //Состояние процедуры обновления 
            this.update_state = Convert.ToUInt16(reader["update_state"]);
            //Прогрес выполнения обновления
            this.progress = Convert.ToUInt16(reader["update_progress"]);
            //Получаем код АЦП
            this.adc_code = Convert.ToUInt32(reader["adc"]);
            //Код продукта
            if (reader.GetColumnSchema().FirstOrDefault(x => x.ColumnName == "product_code") != null)
            {
                this.productCode = reader["product_code"].ToString();
            }
        }

        public override XmlNode Save(XmlNode saveNode)
        {
            XmlNode node = base.Save(saveNode);

            //node_id узла
            XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute("nodeid");
            attr.Value = this.node_id.ToString();
            node.Attributes.Append(attr);
            //Текущее состояние модуля
            attr = saveNode.OwnerDocument.CreateAttribute("state");
            attr.Value = this.state.ToString();
            node.Attributes.Append(attr);
            //Флаг активности
            attr = saveNode.OwnerDocument.CreateAttribute("active");
            attr.Value = this.active.ToString();
            node.Attributes.Append(attr);
            //Флаг постоянности
            attr = saveNode.OwnerDocument.CreateAttribute("persistent");
            attr.Value = this.persistent.ToString();
            node.Attributes.Append(attr);
            //Код типа модуля
            attr = saveNode.OwnerDocument.CreateAttribute("type");
            attr.Value = this.type.ToString();
            node.Attributes.Append(attr);
            //Название типа модуля
            attr = saveNode.OwnerDocument.CreateAttribute("typename");
            attr.Value = this.typeString;
            node.Attributes.Append(attr);
            //Номер ревизии модуля
            attr = saveNode.OwnerDocument.CreateAttribute("revision");
            attr.Value = this.revision.ToString();
            node.Attributes.Append(attr);

            return node;
        }

        public override void Load(XPathNavigator loadNode)
        {
            base.Load(loadNode);

            //node_id узла
            this.node_id = Convert.ToUInt16(CXML.getAttributeValue(loadNode, "nodeid", "0"));
            //Текущее состояние модуля
            this.state = CModule.StateFromString(CXML.getAttributeValue(loadNode, "state", ""));
            //Флаг активности
            this.Active = Convert.ToBoolean(CXML.getAttributeValue(loadNode, "active", "false"));
            //Флаг постоянности
            this.Persistent = Convert.ToBoolean(CXML.getAttributeValue(loadNode, "persistent", "false"));
            //Код типа модуля
            this.type = Convert.ToUInt16(CXML.getAttributeValue(loadNode, "type", "0"));
            //Название типа модуля
            this.typeString = CXML.getAttributeValue(loadNode, "typename", "");
            //Номер ревизии модуля
            this.revision = Convert.ToUInt16(CXML.getAttributeValue(loadNode, "revision", "0"));
        }

        public virtual void ReadState(NpgsqlConnection connection)
        {
            String sql = String.Format("select m.state from fast_modules m where m.node_id={0}", this.node_id);
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            try
            {
                if (!reader.HasRows)
                    return;

                try
                {
                    //Текущее состояние модуля
                    if (!reader.Read())
                        return;
                    this.loadState(reader);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Module Read State");
                }
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Чтение текущих значений параметров модуля
        /// </summary>
        /// <param name="connection"></param>
        /// Теги моду
        public void ReadCurrentValues(NpgsqlConnection connection)
        {
            String sql = String.Format("select * from fast_can_params where node_id={0}", this.node_id);
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            cmd.CommandTimeout = 1;
            NpgsqlDataReader reader = cmd.ExecuteReader();
            //string values = "";
            List<CModuleParam> list = new List<CModuleParam>();
            while (reader.Read())
            {
                CBaseParam base_param;
                if (!CConfig.ParamsList.TryGetValue(reader["tag"].ToString(), out base_param))
                    continue;
                CModuleParam param = base_param as CModuleParam;
                try
                {
                    if (param.HexCode)
                    {
                        int hex = Convert.ToInt32(reader["value"]);
                        if (param.IsVersion)
                            param.Value = "HEXConvert" + " " + hex.ToString("X");
                        else
                            param.Value = "HEX" + " " + hex.ToString("X");
                    }
                    else
                    {
                        param.Value = reader["value"].ToString();
                    }

                    if (CGlobal.Config.TagsDescriptions.ContainsKey(param.Tagname))
                    {
                        param.Description = CGlobal.Config.TagsDescriptions[param.Tagname][1];
                    }
                    else
                    {
                        param.Description = "";
                    }
                }
                catch
                {
                    param.Value = reader["value"].ToString();
                }

                list.Add(param);

                if (!param.IsLimit)
                    continue;

                try
                {
                    param.Exceed = param.LowLimit > Convert.ToInt32(param.Value) ||
                    Convert.ToInt32(param.Value) < Convert.ToInt32(param.Value);
                }
                catch
                {

                }
            }
            reader.Close();
            foreach (CModuleParam param in list)
                param.RefreshGUIValue();
        }

        

        /// <summary>
        /// Генерирует конфигурацию модуля в виде JSON строки для сохранения в базе
        /// </summary>
        /// <returns></returns>
        public String GenerateConfig()
        {
            List<CBaseParam> list = this.AllParams;
            list.RemoveAll(param => (param.ReadOnly == true || param.Config == false || param.ValueString == ""));

            return JsonConvert.SerializeObject(list);
        }

        public UInt16 NodeID { get => node_id; set => node_id = value; }

        public String ShortDescription
        {
            get
            {
                return String.Format("{0} {1}: №{2}", CGlobal.GetResourceValue("l_module"), this.typeString, this.node_id);
            }
        }

        public bool Active
        {
            get
            {
                return this.active;
            }
            set
            {
                this.active = value;
                this.setImage();
            }
        }

        protected String stateStringFromState(ModuleState state)
        {
            switch (state)
            {
                case ModuleState.Initialising:
                    return "Initialising";
                case ModuleState.Disconnected:
                    return "Disconnected";
                case ModuleState.Connecting:
                    return "Connecting";
                case ModuleState.Preparing:
                    return "Preparing";
                case ModuleState.Stopped:
                    return "Stopped";
                case ModuleState.Operational:
                    return "Operational";
                case ModuleState.PreOperational:
                    return "PreOperational";
                case ModuleState.Unknown:
                    return "Unknown";
                case ModuleState.Failed:
                    return "Failed";
                default:
                    return "";
            }
        }

        protected virtual String getStateString()
        {
            return this.stateStringFromState(this.state);
        }

        public String StateString { get => this.getStateString(); }

        public UInt32 Type { get => type; }
        public ushort Revision { get => revision; }
        public String SoftVersion
        {
            get
            {
                return this.soft_version;
            }
            set
            {
                this.soft_version = value;
                this.OnPropertyChanged("SoftVersion");
            }
        }

        public Boolean IsChecked
        {
            get
            {
                return this.ischecked;
            }
            set
            {
                if (this.ischecked == value)
                    return;
                
                this.ischecked = value;
                this.OnPropertyChanged("IsChecked");
            }
        }
        //Для выбора COMM-платы
        private int GetSubType()
        {
            //У старых К3 код ацп не больше 16 бит, а у К2 его вообще нет в таком виде
            if (adc_code <= 4095)
                return 0;

            string bits = "";
            for (int bit = 10; bit >= 6; bit--)
                bits += Convert.ToUInt32((adc_code & (1 << bit - 1)) != 0).ToString();

            return Convert.ToInt32(bits, 2);
        }
        public int SubType { get => GetSubType(); set => this.subType = value; }
        public UInt32 ADC_Code 
        {
            get
            {
                //Так себя ведёт старый К2 в boot режиме
                if(adc_code == 0 && type != 0)
                {
                    return type;
                }
                return this.adc_code;
            }
            set => this.adc_code = value; 
        }
        public String HardVersion { get => this.hard_version; }
        public String TemplateCode { get => String.Format("{0}_{1}", this.type, this.revision); }
        public static String flagstate="";

        public UInt16 UpdateState
        { 
            get
            {
                return this.update_state;
            }
            set
            {
                this.update_state = value;
                this.OnPropertyChanged("UpdateState");
                this.OnPropertyChanged("UpdateStateString");
            }
        }

        public Double GreenColor
        {
            get
            {
                return this.greencolor;
            }
            set
            {
                this.greencolor = value;
                this.OnPropertyChanged("GreenColor");
            }
        }

        public Double RedColor
        {
            get
            {
                return this.redcolor;
            }
            set
            {
                this.redcolor = value;
                this.OnPropertyChanged("RedColor");
            }
        }

        

        public String UpdateStateString
        {
            get
            {
                COptionSet set;
                if (CConfig.Options.OptionSets.TryGetValue("UPDATE_STATE", out set))
                {
                    COption option;
                    if (set.Options.TryGetValue(this.update_state.ToString(), out option))
                        return option.Locale;
                }
                return this.update_state.ToString();
            }
        }


        public UInt16 Progress
        {
            get
            {   
                return this.progress;
            }
            set
            {
                this.progress = value;
                this.OnPropertyChanged("Progress");
            }
        }

        public String UpdateText
        {
            get
            {
                return this.update_text;
            }
            set
            {
                this.update_text = value;
                this.OnPropertyChanged("UpdateText");
            }
        }

        public string FlagState
        {
            get
            {
                return flagstate;
            }
            set
            {
                flagstate = value;
                this.OnPropertyChanged("FlagState");
            }
        }

        public bool Persistent
        {
            get
            {
                return this.persistent;
            }
            set
            {
                this.persistent = value;
                this.setImage();
            }
        }
    }

    /// <summary>
    /// Модуль работающий с двумя CAN шинами
    /// </summary>
    public class CModule2CAN : CModule
    {
        //Состояние модуля по каждой шине
        private ModuleState state0;
        private ModuleState state1;

        public CModule2CAN(CBaseGroup parent)
            : base(parent)
        {
        }

        protected override String getStateString()
        {
            return String.Format("{0}/{1}", this.stateStringFromState(this.state0), this.stateStringFromState(this.state1));  
        }

        /// <summary>
        /// Функция читает из базы конфигурацию модуля и его шаблон и строит основываясь на этом дерево групп и параметров
        /// </summary>
        /// <param name="reader"></param>
        public override void LoadConfig(NpgsqlDataReader reader)
        {
            base.LoadConfig(reader);
        }

        protected override void loadState(NpgsqlDataReader reader)
        {
            //Текущее состояние модуля
            this.state0 = (ModuleState)Convert.ToUInt32(reader["state0"]);
            this.state1 = (ModuleState)Convert.ToUInt32(reader["state1"]);

            this.OnPropertyChanged("StateString");
        }

        public override void ReadState(NpgsqlConnection connection)
        {
            String sql = String.Format("select m.state0, m.state1 from fast_modules m where m.node_id={0}", this.node_id);
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();

            try
            {
                if (!reader.HasRows)
                    return;

                try
                {
                    //Текущее состояние модуля
                    if (!reader.Read())
                        return;

                    this.loadState(reader);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Module Read State");
                }
            }
            finally
            {
                reader.Close();
            }
        }
    }

    /// <summary>
    /// Список модулей контроллера
    /// </summary>
    public class CModulesList : CModuleGroup
    {
        public class ModuleTemplate
        {
            //Тип модуля
            private UInt32 type;
            //Ревизия модуля
            private UInt16 revision;
            //Название модуля
            private String name;
            //Документ с шаблоном модуля
            private XPathDocument doc = null;

            public ModuleTemplate(UInt32 module_type, UInt16 module_rev)
            {
                this.type = module_type;
                this.revision = module_rev;
            }
            public void Load(NpgsqlConnection connection)
            {
                String sql = String.Format("select t.name, t.template from modules_templates t where (t.type={0} and t.revision={1}) limit 1", this.type, this.revision);
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    //Если не получилось группы прочитать, то и делать тут нечего
                    if ((!reader.HasRows) || (!reader.Read()))
                        return;

                    //Название модуля
                    this.name = reader["name"] as String;
                    //Шаблон модуля
                    this.doc = new XPathDocument(new StringReader(reader["template"] as String));
                }
                finally
                {
                    reader.Close();
                }
            }

            public UInt32 Type {get => this.type;}
            public UInt16 Revision { get => this.revision; }
            public String Name { get => name; }
            public XPathDocument Doc { get => this.doc; }
        }

        /*
         * Настройки синхронизации модулей
         */
        //COB ID синхронизации
        private UInt32 cobID = 128;
        //Communication cycle period
        private UInt32 comm_cycle_period = 10000;
    	//Synchronous window length
	    private UInt32 synch_window_length = 9000;
	    //Consumer heartbeat node id
	    private Byte consumer_node_id = 120; 
	    //Consumer heartbeat period
	    private UInt32 consumer_period = 1500; 
	    //Producer heartbeat time
	    private UInt16 producer_period = 1000;
        //Флаг разрешающий отправку heartbeat сообщений контроллером
        private Boolean enableHeartBeat = false;
        //Количество CAN шин контроллера
        private Byte can_count;
        //Список шаблонов контроллера
        private Dictionary<String, ModuleTemplate> templates = new Dictionary<string, ModuleTemplate>();
        public CModulesList()
        {

        }

        protected override CBaseGroup createGroup()
        {
            return this.can_count == 2 ? new CModule2CAN(this) : new CModule(this);
        }

        protected override String getName()
        {
            return String.Format("{0} ({1})", this.name, this.groupsList.Count);
        }

        public uint CobID
        {
            get
            {
                return cobID;
            }
            set
            {
                if (this.cobID == value)
                    return;
                this.cobID = value;
                this.OnPropertyChanged("CobID");
            }
        }

        public uint CommCyclePeriod
        {
            get
            {
                return this.comm_cycle_period;
            }
            set
            {
                if (this.comm_cycle_period == value)
                    return;
                this.comm_cycle_period = value;
                this.OnPropertyChanged("CommCyclePeriod");
            }
        }

        public uint SynchWindowLength
        {
            get
            {
                return this.synch_window_length;
            }
            set
            {
                if (this.synch_window_length == value)
                    return;
                this.synch_window_length = value;
                this.OnPropertyChanged("SynchWindowLength");
            }
        }

        public byte ConsumerNodeID
        {
            get
            {
                return this.consumer_node_id;
            }
            set
            {
                if (this.consumer_node_id == value)
                    return;
                this.consumer_node_id = value;
                this.OnPropertyChanged("ConsumerNodeID");
            }
        }

        public uint ConsumerPeriod
        {
            get
            {
                return this.consumer_period;
            }
            set
            {
                if (this.consumer_period == value)
                    return;
                this.consumer_period = value;
                this.OnPropertyChanged("ConsumerPeriod");
            }
        }

        public ushort ProducerPeriod
        {
            get
            {
                return this.producer_period;
            }
            set
            {
                if (this.producer_period == value)
                    return;
                this.producer_period = value;
                this.OnPropertyChanged("ProducerPeriod");
            }
        }


        public Boolean EnableHeartBeat
        {
            get
            {
                return this.enableHeartBeat;
            }
            set
            {
                if (this.enableHeartBeat == value)
                    return;

                this.enableHeartBeat = value;
                this.OnPropertyChanged("EnableHeartBeat");
            }
        }

        public CModule FindModule(UInt32 node_id)
        {
            foreach(CBaseGroup group in this.groupsList)
            {
                CModule module = group as CModule;
                if (module.NodeID == node_id)
                    return module;
            }

            return null;
        }

        public void RemoveModule(UInt32 node_id)
        {
            CModule module = this.FindModule(node_id);
            if (module == null)
                return;

            module.RemoveParamsFromParamsList(CConfig.ParamsList);
            this.groupsList.Remove(module);

            //Это что бы обновилось количество в дереве главного окна
            this.OnPropertyChanged("Name");
        }

        private void loadCANCount(NpgsqlConnection connection)
        {
            this.can_count = 1;
            //Выборка
            NpgsqlCommand cmd = new NpgsqlCommand("select get_can_count()", connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    //Если не получилось группы прочитать, то и делать тут нечего
                    if (!reader.HasRows)
                        return;

                    //Перебор групп
                    if (!reader.Read())
                        return;

                    this.can_count = Convert.ToByte(reader["get_can_count"]);
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

        public override void Load(NpgsqlConnection connection)
        {
            //Подгрузка количества CAN шин контроллера
            this.loadCANCount(connection);
            //Подгрузка конфигурации
            this.Load(connection, 0);
        }

        public override XmlNode Save(XmlNode saveNode)
        {
            XmlNode node = base.Save(saveNode);

            //COB ID синхронизации
            XmlAttribute attr = saveNode.OwnerDocument.CreateAttribute("cobid");
            attr.Value = this.cobID.ToString();
            node.Attributes.Append(attr);
            //Communication cycle period
            attr = saveNode.OwnerDocument.CreateAttribute("commcycle");
            attr.Value = this.comm_cycle_period.ToString();
            node.Attributes.Append(attr);
            //Synchronous window length
            attr = saveNode.OwnerDocument.CreateAttribute("synchwindow");
            attr.Value = this.synch_window_length.ToString();
            node.Attributes.Append(attr);
            //Consumer heartbeat node id
            attr = saveNode.OwnerDocument.CreateAttribute("consumernode");
            attr.Value = this.consumer_node_id.ToString();
            node.Attributes.Append(attr);
            //Consumer heartbeat period
            attr = saveNode.OwnerDocument.CreateAttribute("consumerperiod");
            attr.Value = this.consumer_period.ToString();
            node.Attributes.Append(attr);
            //Producer heartbeat time
            attr = saveNode.OwnerDocument.CreateAttribute("producerperiod");
            attr.Value = this.producer_period.ToString();
            node.Attributes.Append(attr);
            //Флаг разрешающий контроллеру записывать настройки синхронизации в модули
            attr = saveNode.OwnerDocument.CreateAttribute("enableheartbeat");
            attr.Value = this.enableHeartBeat.ToString();
            node.Attributes.Append(attr);

            return node;
        }

        public override void Load(XPathNavigator loadNode)
        {
            base.Load(loadNode);

            //COB ID синхронизации
            this.cobID = Convert.ToUInt32(CXML.getAttributeValue(loadNode, "cobid", "0"));
            //Communication cycle period
            this.comm_cycle_period = Convert.ToUInt32(CXML.getAttributeValue(loadNode, "commcycle", "0"));
            //Synchronous window length
            this.synch_window_length = Convert.ToUInt32(CXML.getAttributeValue(loadNode, "synchwindow", "0")); 
            //Consumer heartbeat node id
            this.consumer_node_id = Convert.ToByte(CXML.getAttributeValue(loadNode, "consumernode", "0"));
            //Consumer heartbeat period
            this.consumer_period = Convert.ToUInt32(CXML.getAttributeValue(loadNode, "consumerperiod", "0"));
            //Producer heartbeat time
            this.producer_period = Convert.ToUInt16(CXML.getAttributeValue(loadNode, "producerperiod", "0"));
            //Флаг разрешающий контроллеру записывать настройки синхронизации в модули
            this.enableHeartBeat = Convert.ToBoolean(CXML.getAttributeValue(loadNode, "enableheartbeat", "false"));
        }

        public void LoadSynchronizationSettings(NpgsqlConnection connection)
        {
            String sql = "select value from settings where name='modules_synch' limit 1";

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            try
            {
                NpgsqlDataReader reader = cmd.ExecuteReader();
                try
                {
                    if (!reader.HasRows)
                        return;

                    if (!reader.Read())
                        return;

                    String json = reader["value"].ToString();
                    JObject jobject = JObject.Parse(json);

                    ////COB ID синхронизации
                    //this.CobID = Convert.ToUInt32(jobject["synch_cobid"]);
                    ////Communication cycle period
                    //this.CommCyclePeriod = Convert.ToUInt32(jobject["comm_cycle_period"]);
                    ////Synchronous window length
                    //this.SynchWindowLength = Convert.ToUInt32(jobject["synch_window_length"]);
                    //Consumer heartbeat node id
                    this.ConsumerNodeID = Convert.ToByte(jobject["consumer_node_id"]);
                    //Consumer heartbeat period
                    this.ConsumerPeriod = Convert.ToUInt32(jobject["consumer_period"]);
                    //Producer heartbeat time
                    this.ProducerPeriod = Convert.ToUInt16(jobject["producer_period"]);
                    //Флаг инициализации
                    this.EnableHeartBeat = Convert.ToBoolean(jobject["enable_heartbeat"]);
                }
                finally
                {
                    reader.Close();
                }
            }
            catch
            {
                //Что то не так
            }
        }

        public void Load(NpgsqlConnection connection, UInt16 node_id)
        {
            //Временный список с модулями
            ObservableCollection<CModule> list = new ObservableCollection<CModule>();
            String sql;

            if (node_id == 0)
            {
                //Загрузка настроек синхронизации модулей
                this.LoadSynchronizationSettings(connection);

                sql = "select f.* from fast_modules f order by f.node_id";
                this.groupsList.Clear();
            }
            else
            {
                //т.к. будет чтение настроек только одного модуля, то во временный список копируем все существующие
                this.RemoveModule(node_id);
                foreach (CBaseGroup group in this.groupsList)
                    list.Add(group as CModule);
                sql = String.Format("select f.* from fast_modules f where node_id={0} limit 1", node_id);
            }

            //Выборка
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            CModule module = null;
            try
            {
                //Если не получилось группы прочитать, то и делать тут нечего
                if (!reader.HasRows)
                    return;

                //Перебор групп
                while (reader.Read())
                {
                    module = this.createGroup() as CModule;
                    try
                    {
                        module.LoadConfig(reader);
                        list.Add(module);
                    }
                    catch(Exception)
                    {
                        //Неудача, модуль в список не добавляется
                        // TODO Здесь надо будет сделать так что бы модуль показался, 
                        //но при этом был бы помечен в интерфейсе как некорректный
                        module = null;
                    }
                }
            }
            finally
            {
                reader.Close();
            }

            //Список модулей получили, теперь надо обработать по шаблонам
            foreach (CModule md in list)
            {
                try
                {
                    ModuleTemplate template;
                    if (!this.templates.TryGetValue(md.TemplateCode, out template))
                    {
                        template = new ModuleTemplate(md.Type, md.Revision);
                        template.Load(connection);
                        this.templates.Add(md.TemplateCode, template);
                    }
                   // Чтобы заново не создавать шаблон
                    if (!md.IsThisModuleExists)
                    {
                        md.IsThisModuleExists = true;
                        md.GenerateConfigStructureFromTemplate(template);
                    }
                }
                catch
                {
                    ////Обработка прошла неудачно
                    ////Считаем что это неизвестный тип модуля
                }
            }

            if (node_id != 0)
            {
                if (module == null)
                    return;
                //Сортировка по node_id
                List<CModule> sorted = new List<CModule>();
                sorted.AddRange(list.OrderBy(m => m.NodeID));
                //Артикул
                //Теперь необходимо этот модуль вставить в существующий список
                module.Load(connection);
                int index = sorted.IndexOf(module);
                this.groupsList.Insert(index, module);
            }
            else
            {
                //т.к. читался весь список, то просто копируем
                foreach (CModule md in list)
                {
                    //Артикулы
                    md.Load(connection);
                    this.groupsList.Add(md);
                }
            }

            //Это что бы обновилось количество модулей в дереве главного окна
            this.OnPropertyChanged("Name");
        }

        /// <summary>
        /// Генерация настроек синхронизации в виде json файла
        /// </summary>
        public JObject GenerateJson()
        {
            JObject settings = new JObject(
                    ////COB ID синхронизации
                    new JProperty("synch_cobid", this.cobID),
                    ////Communication cycle period
                    new JProperty("comm_cycle_period", this.comm_cycle_period),
                    ////Synchronous window length
                    new JProperty("synch_window_length", this.synch_window_length),
                    //Consumer heartbeat node id
                    new JProperty("consumer_node_id", this.consumer_node_id),
                    //Consumer heartbeat period
                    new JProperty("consumer_period", this.consumer_period),
                    //Consumer heartbeat period
                    new JProperty("producer_period", this.producer_period),
                    //Инициализация модулей
                    new JProperty("enable_heartbeat", this.enableHeartBeat)
                );

            return settings;
        }
    }
}
