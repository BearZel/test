using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace AbakConfigurator.Soe
{
    public class EventsLoader
    {
        static string logDir = "/var/log/";

        public SortedDictionary<DateTime, List<Event>> Events = new SortedDictionary<DateTime, List<Event>>();

        public List<string> Sources = new List<string>();

        struct SourceLog
        {
            public string full_path;
            public string file_name;
        };

        SourceLog AddSource(string path)
        {
            SourceLog sourceLog = new SourceLog();
            sourceLog.full_path = path;
            sourceLog.file_name = Path.GetFileName(path);
            Sources.Add(sourceLog.file_name);

            return sourceLog;
        }

        public void LoadFiles(IProgress<int> progress)
        {
            Events.Clear();
            Sources.Clear();

            progress.Report(0);
            var files = CGlobal.Session.SSHClient.ListDirectory(logDir + "/abak");
            files = files.Where(f => f.EndsWith(".log")).ToList();

            SourceLog? src = null;

            int percent = 0;

            // Расчет процентов прогресса
            // 70% суммарно отдаем на файлы из /var/log/abak
            // Эти 70% делим поровну между всеми файлами
            // Эту долю на файл прибавляем к прогрессу
            int percent_add = 70 / files.Count;
            foreach (var file in files)
            {
                src = AddSource(logDir + "abak/" + file);
                LoadFile(src?.full_path, src?.file_name, AbakEventParser); progress.Report(percent+= percent_add);
            }

            CodesysConfig cds_cfg = new CodesysConfig(CGlobal.Session.SSHClient);
            src = AddSource(cds_cfg.Log.Name);
            LoadFile(src?.full_path, src?.file_name, CodesysEventParser); progress.Report(75);

            src = AddSource(logDir + "syslog");
            LoadFile(src?.full_path, src?.file_name, SyslogEventParser); progress.Report(90);

            CanLoggerConfig can_logger_cfg = new CanLoggerConfig(CGlobal.Session.SSHClient);
            src = AddSource(can_logger_cfg.Props.LogName);
            LoadCanLog(src?.full_path, src?.file_name); progress.Report(100);
        }

        void LoadFile(string filename, string source, Func<string, DateTime, string, Event> eventParser)
        {
            var stream = CGlobal.Session.SSHClient.ReadFile(filename);
            if (stream == null)
            {
                return;
            }

            using (StreamReader sr = new StreamReader(stream))
            {
                string s;
                DateTime timestamp = new DateTime();
                while ((s = sr.ReadLine()) != null)
                {
                    
                    Event e = eventParser(s, timestamp, source);
                    if (e == null)
                    {
                        continue;
                    }
                    timestamp = e.Timestamp;
                    if (Events.ContainsKey(timestamp))
                    {
                        Events[timestamp].Add(e);
                    }
                    else
                    {
                        Events.Add(timestamp, new List<Event> { e });
                    }
                }
            }
        }

        Event AbakEventParser(string logLine, DateTime timestamp, string source)
        {
            int delimiter_position = logLine.IndexOf(" --");
            string dateTime = delimiter_position > 0 ? logLine.Substring(0, delimiter_position) : "";
            string message = delimiter_position > 0 ? logLine.Substring(delimiter_position + 3).Trim() : logLine;
            if (message.Length ==0)
            {
                return null;
            }

            DateTime eventDate;
            bool successParse = DateTime.TryParseExact(dateTime, "dd.MM.yyyy HH:mm:ss.fff",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out eventDate);

            eventDate = successParse ? eventDate : timestamp;
            return new Event(eventDate, source, message);
        }

        Event CodesysEventParser(string logLine, DateTime timestamp, string source)
        {
            if (logLine.Length == 0 || logLine[0] == ';')
            {
                return null;
            }

            int delimiter_position = logLine.IndexOf(", ");
            string dateTime = delimiter_position > 0 ? logLine.Substring(0, delimiter_position) : "";
            string message = delimiter_position > 0 ? logLine.Substring(delimiter_position + 2) : logLine;

            DateTime eventDate;
            bool successParse = DateTime.TryParseExact(dateTime, "yyyy-MM-ddTHH:mm:ssZ",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out eventDate);

            eventDate = successParse ? eventDate : timestamp;
            return new Event(eventDate.ToLocalTime(), source, message.Trim());
        }

        Event SyslogEventParser(string logLine, DateTime timestamp, string source)
        {
            int delimiter_position = logLine.IndexOf(" ABAK");
            string dateTime = delimiter_position > 0 ? logLine.Substring(0, delimiter_position) : "";
            string message = delimiter_position > 0 ? logLine.Substring(delimiter_position + 1) : logLine;

            DateTime eventDate;
            dateTime = Regex.Replace(dateTime, @"\s+", " ");
            bool successParse = DateTime.TryParseExact(dateTime, "MMM d HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out eventDate);

            eventDate = successParse ? eventDate : timestamp;
            return new Event(eventDate, source, message.Trim());
        }

        void LoadCanLog(string full_path, string filename)
        {
            var stream = CGlobal.Session.SSHClient.ReadFile(full_path);
            if (stream == null)
            {
                return;
            }

            Func<byte[], string> to_hex = (byte[] bytes) =>
            {
                StringBuilder hex = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    hex.AppendFormat("{0:x2}", b);
                return hex.ToString();
            };

            using (BinaryReader sr = new BinaryReader(stream, Encoding.ASCII))
            {
                const int block_size = 33;
                char[] buffer = new char[block_size];
                while (sr.BaseStream.Position != sr.BaseStream.Length)
                {
                    var timestamp = sr.ReadInt64();
                    var can_name = Encoding.ASCII.GetString(sr.ReadBytes(12));
                    var module = sr.ReadByte();
                    var pdo_type = sr.ReadUInt32();
                    var pdo_value = sr.ReadBytes(8);

                    string message = string.Format("name:{0} module:{1} type:{2} value:{3}", can_name, module, pdo_type, to_hex(pdo_value));

                    DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp);
                    DateTime local_time = offset.LocalDateTime;

                    var e = new Event(local_time, filename, message);
                    if (Events.ContainsKey(local_time))
                    {
                        Events[local_time].Add(e);
                    }
                    else
                    {
                        Events.Add(local_time, new List<Event> { e });
                    }
                }
            }
        }
    }
}
