using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;
using Npgsql;
using System.Drawing;

namespace AbakConfigurator
{
    /// <summary>
    /// Стоп биты
    /// </summary>
    public enum STOPBITS_TYPE
    {
        [Description("1")]
        STOPBITS_ONE = 0,
        [Description("2")]
        STOPBITS_TWO
    }

    /// <summary>
    /// Конвертер из типа STOPBITS_TYPE в человеческий и обратно
    /// </summary>
    public class StopBitsValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            STOPBITS_TYPE stopBits = (STOPBITS_TYPE)value;

            switch (stopBits)
            {
                case STOPBITS_TYPE.STOPBITS_ONE:
                    return "1";

                case STOPBITS_TYPE.STOPBITS_TWO:
                    return "2";
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String stopBits = value as String;

            if (stopBits == "1")
                return STOPBITS_TYPE.STOPBITS_ONE;
            else
                return STOPBITS_TYPE.STOPBITS_TWO;
        }
    }

    /// <summary>
    /// Бит паритета
    /// </summary>
    public enum PARITYBITS_TYPE
    {
        [Description("l_parityNone_COM")]
        PARITY_NONE = 0,
        [Description("l_parityOdd_COM")]
        PARITY_ODD,
        [Description("l_parityEven_COM")]
        PARITY_EVEN
    }

    public class ParityEnumToItemsSource : MarkupExtension
    {
        private readonly Type _type;

        public ParityEnumToItemsSource(Type type)
        {
            _type = type;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            List<String> list = _type.GetMembers().SelectMany(member => member.GetCustomAttributes(typeof(DescriptionAttribute), true).Cast<DescriptionAttribute>()).Select(x => x.Description).ToList();
            List<String> newList = new List<String>();
            foreach (String s in list)
                newList.Add(CGlobal.GetResourceValue(s));
            return newList;
        }
    }


    /// <summary>
    /// Конвертер из типа PARITYBITS_TYPE в человеческий и обратно
    /// </summary>
    public class ParityBitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PARITYBITS_TYPE parity = (PARITYBITS_TYPE)value;

            switch (parity)
            {
                case PARITYBITS_TYPE.PARITY_NONE:
                    return CGlobal.GetResourceValue("l_parityNone_COM");

                case PARITYBITS_TYPE.PARITY_ODD:
                    return CGlobal.GetResourceValue("l_parityOdd_COM");

                case PARITYBITS_TYPE.PARITY_EVEN:
                    return CGlobal.GetResourceValue("l_parityEven_COM");
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((String)value == CGlobal.GetResourceValue("l_parityOdd_COM"))
                return PARITYBITS_TYPE.PARITY_ODD;
            else if ((String)value == CGlobal.GetResourceValue("l_parityEven_COM"))
                return PARITYBITS_TYPE.PARITY_EVEN;
            else
                return PARITYBITS_TYPE.PARITY_NONE;
        }
    }

    /// <summary>
    /// Класс с настройками COM порта
    /// </summary>
    public class CComPort : INotifyPropertyChanged, ICloneable
    {
        //Идентификатор в базе данных
        private UInt32 id = 0;
        //Номер порта
        private UInt16 number = 0;
        //Modbus адрес
        private Byte modbusAddr = 1;
        //Скорость работы
        private UInt32 baudRate = 9600;
        //Стоп биты
        private STOPBITS_TYPE stopBits = STOPBITS_TYPE.STOPBITS_ONE;
        //Биты паритета
        private PARITYBITS_TYPE parityBit = PARITYBITS_TYPE.PARITY_NONE;
        //Межбайтовый интервал
        private UInt32 interval = 2;
        //Режим работы
        private Boolean slaveMode = true;
        //Флаг изменения конфигурации
        private Boolean changed = false;
        //Флаг показывает что это встроенный в контроллер или принадлежащий IM модулю инfтерфейс
        private Boolean internal_tty = false;
        //Флаг физического наличия последовательного интерфейса
        private Boolean active;
        //Символьная ссылка на последовательный интерфейс
        private String tty_link;
        //Редирект
        private bool isRedirect = false;
        //Порт для редиректа
        private int redirectPort = 0;
        public CComPort()
        {

        }

        public CComPort(UInt16 number)
        {
            this.number = number;
        }

        /// <summary>
        /// Запоняет поля класса данными из SQL
        /// </summary>
        /// <param name="reader"></param>
        public void SetValuesFromSQL(NpgsqlDataReader reader)
        {
            this.id = Convert.ToUInt32(reader["id"]);
            this.baudRate = Convert.ToUInt32(reader["baud"]);
            this.parityBit = (PARITYBITS_TYPE)Convert.ToUInt32(reader["parity"]);
            this.stopBits = (STOPBITS_TYPE)Convert.ToUInt32(reader["stopbits"]);
            this.interval = Convert.ToUInt32(reader["interval"]);
            this.internal_tty = Convert.ToBoolean(reader["internal"]);
            this.number = Convert.ToUInt16(reader["num"]);
            int numb = CGlobal.CurrState.AssemblyLo;
            numb = CGlobal.CurrState.AssemblyHi * 1000
                + CGlobal.CurrState.AssemblyMid * 100 + CGlobal.CurrState.AssemblyLo;           
            //ASSEMBLY 5.0.3 => 5000 + 0 + 3 = 5003
            if (numb < 5003)
                this.slaveMode = Convert.ToBoolean(reader["slave"]);
            else
            {
                CParam param = CGlobal.Config.ControllerParams.AllParams.Find(x => x.Tagname == $"COM{this.id}_SLAVE") as CParam;
                this.slaveMode = Convert.ToBoolean(param.Value);
            }
            this.active = Convert.ToBoolean(reader["active"]);
            this.tty_link = reader["path"].ToString();
            this.modbusAddr = Convert.ToByte(reader["maddr"]);
            this.FileName = reader["device"].ToString();
            numb = CGlobal.CurrState.AssemblyHi * 1000
               + CGlobal.CurrState.AssemblyMid * 100 + CGlobal.CurrState.AssemblyLo;
            //ASSEMBLY 5.1.0 => 5000 + 100 + 0 = 5100
            if (numb < 5100)
                return;
            this.isRedirect = Convert.ToBoolean(reader["redirect"]);
            this.redirectPort = Convert.ToInt32(reader["redirect_port"]);
        }

        public void Assign(CComPort port)
        {
            this.id = port.id;
            this.ModbusAddr = port.ModbusAddr;
            this.BaudRate = port.BaudRate;
            this.SlaveMode = port.SlaveMode;
            this.StopBits = port.StopBits;
            this.ParityBit = port.ParityBit;
            this.FileName = port.FileName;
            this.Interval = port.Interval;
            this.number = port.number;
            this.internal_tty = port.internal_tty;
            this.active = port.active;
            this.tty_link = port.tty_link;
            this.RedirectPort = port.RedirectPort;
            this.IsRedirect = port.IsRedirect;
            this.Changed = false;
        }

        public UInt32 ID { get => id; }

        public Boolean Active { get => active; }

        public String Name
        {
            get
            {
                return String.Format("COM{0}", this.number);
            }
        }

        //Имя файла в Linux
        public String FileName
        {
            get;
            set;
        }

        //Modbus адрес
        public Byte ModbusAddr
        {
            get
            {
                return this.modbusAddr;
            }
            set
            {
                if (this.modbusAddr == value)
                    return;

                this.modbusAddr = value;
                this.Changed = true;
                this.OnPropertyChanged("ModbusAddr");
            }
        }

        //Скорость работы
        public UInt32 BaudRate
        {
            get
            {
                return this.baudRate;
            }
            set
            {
                if (this.baudRate == value)
                    return;

                this.baudRate = value;
                this.Changed = true;
                this.OnPropertyChanged("BaudRate");
            }
        }

        //Стоп биты
        public STOPBITS_TYPE StopBits
        {
            get
            {
                return this.stopBits;
            }
            set
            {
                if (this.stopBits == value)
                    return;

                this.stopBits = value;
                this.Changed = true;
                this.OnPropertyChanged("StopBits");
            }
        }

        //Биты паритета
        public PARITYBITS_TYPE ParityBit
        {
            get
            {
                return this.parityBit;
            }
            set
            {
                if (this.parityBit == value)
                    return;

                this.parityBit = value;
                this.Changed = true;
                this.OnPropertyChanged("ParityBit");
            }
        }

        //Межбайтовый интервал
        public UInt32 Interval
        {
            get
            {
                return this.interval;
            }
            set
            {
                if (this.interval == value)
                    return;

                this.interval = value;
                this.Changed = true;
                this.OnPropertyChanged("Interval");
            }
        }

        //Режим работы
        public Boolean SlaveMode
        {
            get
            {
                return this.slaveMode;
            }
            set
            {
                if (this.slaveMode == value)
                    return;

                this.slaveMode = value;
                this.Changed = true;
                this.OnPropertyChanged("SlaveMode");
            }
        }

        //Флаг изменения
        public Boolean Changed
        {
            get
            {
                return this.changed;
            }
            set
            {
                if (this.changed == value)
                    return;

                this.changed = value;
                this.OnPropertyChanged("Changed");
            }
        }

        public Boolean IsRedirect
        {
            get
            {
                return this.isRedirect;
            }
            set
            {
                isRedirect = value;
                this.changed = value;
                this.OnPropertyChanged("IsRedirect");
            }
        }

        public int RedirectPort
        {
            get
            {
                return this.redirectPort;
            }
            set
            {
                this.redirectPort = value;
                this.Changed = true;
                this.OnPropertyChanged("RedirectPort");
            }
        }

        protected void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public object Clone()
        {
            CComPort port = new CComPort();
            port.Assign(this);

            return port;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Список COM портов
    /// </summary>
    public class CComPortsList
    {
        //Путь к файлу с настройками интерфейсов
        private const String configFile = "/opt/abak/A:/assembly/serial.config";
        //SSH клиент
        private CSSHClient sshClient = null;
        //Список доступных интерфесов контроллера
        private List<CComPort> portsList = new List<CComPort>();

        /// <summary>
        /// Список интерфейсов контроллера
        /// </summary>
        public List<CComPort> PortsList
        {
            get
            {
                return this.portsList;
            }
        }

        public CComPortsList(CSSHClient sshClient)
        {
            this.sshClient = sshClient;
        }

        /// <summary>
        /// Подгрузка настроек из контроллера
        /// </summary>
        /// <returns></returns>
        private CIniFile loadPortsSettings()
        {
            Stream stream = this.sshClient.ReadFile(configFile);
            if (stream == null)
                stream = new MemoryStream();
            CIniFile ini = new CIniFile(stream);
            ini.NewLine = "\n";

            return ini;
        }

        public void LoadSettings()
        {
            if (this.sshClient == null)
                return;

            this.portsList.Clear();
            CIniFile ini = this.loadPortsSettings();

            int i = 1;
            //Формирование списка портов в системе
            foreach(KeyValuePair<string, CSection> kvp in ini.Sections)
            {
                CSection section = kvp.Value;

                //Инциализация экземпляра COM порта
                CComPort comPort = new CComPort(Convert.ToUInt16(i));

                comPort.FileName = section.Name;
                comPort.ModbusAddr = Convert.ToByte(section.Values["addr"]);
                comPort.BaudRate = Convert.ToUInt32(section.Values["baud"]);
                comPort.StopBits = (STOPBITS_TYPE)Convert.ToUInt32(section.Values["stopbits"]);
                comPort.ParityBit = (PARITYBITS_TYPE)Convert.ToUInt32(section.Values["parity"]);
                comPort.Interval = Convert.ToUInt32(section.Values["interval"]);
                String s = section.Values["slave"];
                if (s == "0")
                    comPort.SlaveMode = false;
                else
                    comPort.SlaveMode = true;
                comPort.Changed = false;

                this.portsList.Add(comPort);
                i++;
            }
        }

        public void ChangeCommPortSettings(CComPort commPort)
        {
            if (this.sshClient == null)
                return;

            CIniFile ini = this.loadPortsSettings();

            String section_name = commPort.FileName;

            ini.WriteValue(section_name, "addr", commPort.ModbusAddr.ToString());
            ini.WriteValue(section_name, "baud", commPort.BaudRate.ToString());
            ini.WriteValue(section_name, "stopbits", ((UInt32)commPort.StopBits).ToString());
            ini.WriteValue(section_name, "parity", ((UInt32)commPort.ParityBit).ToString());
            ini.WriteValue(section_name, "interval", commPort.Interval.ToString());
            if (commPort.SlaveMode)
                ini.WriteValue(section_name, "slave", "1");
            else
                ini.WriteValue(section_name, "slave", "0");


            //Сохранение настроек в контроллер
            this.sshClient.WriteFile(configFile, ini.GetStream());
        }

    }
}
