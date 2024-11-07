using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AbakConfigurator
{
    /// <summary>
    /// Класс для работы с командной строкой
    /// 
    /// Вес параметры передаются в командной строке в виде param[:value:[value:[value и т.д.]]]
    /// param - имя передаваемого параметра
    /// value - значение передаваемого параметра, для некоторых параметров (булевых) может отсутствовать, в этом случае передается просто param
    /// 
    /// type - тип контроллера type:type, тип может быть ivk или plc, если ни то ни се то указывается появится окно с выбором типа контроллера
    /// config - путь к файлу с конфигурацией, config:filepath, пример - config:c:\1.aprj
    /// run - команда запуска опроса, значение параметра не требуется
    /// write - команда записывающая конфигурацию в файл
    /// user - имя пользователя, user:username1[:username2[username3]], пример - user:oper, user:nal, user:pov, user:oper:pov
    /// password - пароль пользователя, password:password[:password2[:password3]]
    /// serial - настройки последовательного порта serial:com:baud, пример serial:com1:9600
    /// tcp - настройки соединения по ethernet, tcp:ip:port[:rtu][:ftp]
    ///     tcp:192.168.3.100:502:ftp - соединение с устройством по IP адресу 192.168.3.100, порт 502, протокол Modbus TCP, для запроса xparams использовать FTP
    ///     tcp:192.168.3.100:502:rtu - соединение с устройством по IP адресу 192.168.3.100, порт 502, протокол Modbus RTU over IP, для запроса xparams не использовать FTP
    /// id - modbus адрес устройство, id:id
    /// timeout - время ожиданния ответа от устройства на запрос программы, timeout:timeout
    /// top - флаг того что приложение должно быть поверх всех
    /// </summary>
    class CommandLineClass
    {
        /// <summary>
        /// Список доступных параметров
        /// </summary>
        private const String TYPE = "type";
        private const String CONFIGURATION = "config";
        private const String RUN = "run";
        private const String WRITE = "write";
        private const String USER = "user";
        private const String PASSWORD = "password";
        private const String TCP = "tcp";
        private const String TIMEOUT = "timeout";
        private const String TOP = "top";
        public const String IVK = "ivk";
        public const String PLC = "plc";

        //Тип контроллера
        public static String Type = "";
        //Путь к файлу с конфигурацией
        public static String Configuration = "";
        //Флаг автозапуска опроса
        public static Boolean Run = false;
        //Имя пользователя 1
        public static String User1 = "";
        //Имя пользователя 1
        public static String User2 = "";
        //Имя пользователя 1
        public static String User3 = "";
        //Пароль 1
        public static String Password1 = "";
        //Пароль 2
        public static String Password2 = "";
        //Пароль 3
        public static String Password3 = "";
        //Поверх всех
        public static Boolean Top = false;
        //Флаг записи конфигурации в контроллер
        public static Boolean WriteConfig = false;

        /// <summary>
        /// Функция разбора командной строки
        /// </summary>
        /// <param name="args"></param>
        public static void ProcessCommandLine(string[] args)
        {
            foreach (string s in args)
            {
                String cmd = "";
                String val = "";
                String str;

                try
                {
                    if (s.Contains(":"))
                    {
                        //Параметр со значением
                        int index = s.IndexOf(":");
                        cmd = s.Substring(0, index).ToLower();
                        val = s.Substring(index + 1, s.Length - index - 1).ToLower();
                    }
                    else
                    {
                        //Просто параметр
                        cmd = s;
                    }

                    switch (cmd)
                    {
                        case CommandLineClass.TYPE:
                            switch (val)
                            {
                                case CommandLineClass.IVK:
                                    CommandLineClass.Type = CommandLineClass.IVK;
                                    break;

                                case CommandLineClass.PLC:
                                    CommandLineClass.Type = CommandLineClass.PLC;
                                    break;
                            }

                            break;

                        case CommandLineClass.CONFIGURATION:
                            CommandLineClass.Configuration = val;
                            break;

                        case CommandLineClass.RUN:
                            CommandLineClass.Run = true;
                            break;

                        case CommandLineClass.WRITE:
                            CommandLineClass.WriteConfig = true;
                            //Для записи конфигурации необходимы все пользователи
                            CommandLineClass.User1 = "oper";
                            CommandLineClass.User2 = "nal";
                            CommandLineClass.User3 = "pov";
                            break;

                        case CommandLineClass.USER:
                            CommandLineClass.User1 = val;
                            break;

                        case CommandLineClass.PASSWORD:
                            //Получение списка паролей
                            int index;
                            int passNum = 0;
                            while (true)
                            {
                                index = val.IndexOf(":");
                                if (index == -1)
                                    str = val;
                                else
                                    str = val.Substring(0, index);
                                switch (passNum)
                                {
                                    //Пароль оператора
                                    case 0:
                                        CommandLineClass.Password1 = str;
                                        break;

                                    //Пароль наладчика
                                    case 1:
                                        CommandLineClass.Password2 = str;
                                        break;

                                    //Пароль поверителя
                                    case 2:
                                        CommandLineClass.Password3 = str;
                                        break;
                                }
                                if (index != -1)
                                {
                                    val = val.Remove(0, index + 1);
                                    passNum++;
                                    continue;
                                }
                                break;
                            }
                            break;

                        case CommandLineClass.TCP:
                            //Разбор строки с параметрами ehternet tcp:ip
                            //IP адрес
                            str = val.Substring(0, val.IndexOf(":"));
                            String IP = str;
                            val = val.Remove(0, val.IndexOf(":") + 1);

                            CGlobal.Settings.IP = IP;
                            break;

                        case CommandLineClass.TOP:
                            CommandLineClass.Top = true;
                            break;

                    }
                }
                catch
                {
                    CommandLineClass.User1 = "";
                    CommandLineClass.User2 = "";
                    CommandLineClass.User3 = "";

                    CommandLineClass.Run = false;
                    CommandLineClass.WriteConfig = false;
                }
            }
        }
    }
}
