using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AbakConfigurator
{
    /// <summary>
    /// Единичная секция со значениями
    /// </summary>
    public class CSection
    {
        /// <summary>
        /// Список параметров
        /// </summary>
        public Dictionary<String, String> Values = new Dictionary<string, string>();

        /// <summary>
        /// Название секции
        /// </summary>
        public String Name = "";
    }

    /// <summary>
    /// Класс работающий с файлами тип ini
    /// </summary>
    public class CIniFile
    {
        /// <summary>
        /// Список секций ini файла
        /// </summary>
        private Dictionary<String, CSection> sections = new Dictionary<string, CSection>();
        /// <summary>
        /// Флаг записи в файл без пробелов
        /// </summary>
        private bool extraSpace = true;
        /// <summary>
        /// Содержимое файла
        /// </summary>
        private StreamReader reader;

        /// <summary>
        /// Символ завершения строки
        /// </summary>
        private String newLine = "\n\r";

        /// <summary>
        /// Разбор полученного файла
        /// </summary>
        private void parseIniFile()
        {
            this.sections.Clear();

            String line;
            CSection section = null;
            while ((line = this.reader.ReadLine()) != null)
            {
                if (line == "")
                    continue;

                if ((line.IndexOf('[') == 0) && (line.IndexOf(']') == line.Length - 1))
                {
                    //Это секция
                    section = new CSection();
                    section.Name = line.Substring(1, line.Length - 2);

                    try
                    {
                        this.sections.Add(section.Name, section);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    //Это значение
                    if (section == null)
                        continue;

                    //Если строка не содержит = она игнорируется
                    if (line.IndexOf('=') == -1)
                        continue;

                    String[] lines = line.Split('=');
                    try
                    {
                        section.Values.Add(lines[0].Trim(), lines[1].Trim());
                    }
                    catch
                    {
                    }
                }
            }
            reader.Dispose();
        }

        public CIniFile(String filename)
        {
            this.reader = new StreamReader(filename);
            this.parseIniFile();
        }

        public CIniFile(Stream stream)
        {
            this.reader = new StreamReader(stream);
            this.parseIniFile();
        }

        public Boolean IsSectionExists(String sectionName)
        {
            return this.sections.ContainsKey(sectionName);
        }

        public bool WriteValue(String sectionName, String paramName, String value)
        {
            CSection section;
            if (!this.sections.TryGetValue(sectionName, out section))
            {
                //Создается новая секция
                section = new CSection();
                section.Name = sectionName;
                this.sections.Add(section.Name, section);
            }

            if (!section.Values.ContainsKey(paramName))
                section.Values.Add(paramName, value);
            else
                section.Values[paramName] = value;
                
            return true;
        }
        public CSection GetSection(string sectionName)
        {
            if (sections.ContainsKey(sectionName))
                return sections[sectionName];

            return null;
        }
        public String ReadValue(String sectionName, String paramName, string defValue = "")
        {
            CSection section;
            if (!this.sections.TryGetValue(sectionName, out section))
            {
                if(!this.sections.TryGetValue(sectionName.ToLower(), out section))
                    return defValue;
            }
            if (section.Values.ContainsKey(paramName))
                return section.Values[paramName];

            if (section.Values.ContainsKey(paramName.ToLower()))
                return section.Values[paramName.ToLower()];

            return defValue;
        }

        public void RemoveKey(string sectionName, string key)
        {
            CSection section;
            if (!this.sections.TryGetValue(sectionName, out section))
                return;

            if (section.Values.Keys.Contains(key))
                section.Values.Remove(key);
        }

        public void RemoveSection(String sectionName)
        {
            this.sections.Remove(sectionName);
        }

        public void Save(Stream stream)
        {
            stream.Position = 0;
            StreamWriter writer = new StreamWriter(stream);
            writer.NewLine = this.newLine;
            foreach (KeyValuePair<String, CSection> section in this.sections)
            {
                //Название секции
                String s = String.Format("[{0}]", section.Key);
                writer.WriteLine(s);

                //Список параметров
                foreach (KeyValuePair<String, String> value in section.Value.Values)
                {

                    if(extraSpace)
                        s = String.Format("{0} = {1}", value.Key, value.Value);
                    else
                        s = String.Format("{0}={1}", value.Key, value.Value);
                    writer.WriteLine(s);
                }

                //Пустая строка для разделения секций
                writer.WriteLine("");
            }
            writer.Flush();
            stream.Position = 0;
        }

        public void Save(String path)
        {
            FileStream stream = new FileStream(path, FileMode.Create);
            this.Save(stream);
        }
        /// <summary>
        /// Поиск ключа в секции ini файла
        /// </summary>
        public bool IsKeyExists(String sectionName, String key)
        {
            CSection section;
            if (!this.sections.TryGetValue(sectionName, out section))
                return false;

            if (!section.Values.Keys.Contains(key))
                return false;

            section.Values.Remove(key);
            return true;
        }

        /// <summary>
        /// Добавления секции ini файла
        /// </summary>
        public void AddSection(String sectionName)
        {
            if (sections.ContainsKey(sectionName))
                return;

            CSection section = new CSection();
            this.sections.Add(sectionName, section);
        }
        /// <summary>
        /// Добавления ключа к секции ini файла
        /// </summary>
        public void AddKey(String sectionName, String key, string value)
        {
            CSection section = this.sections[sectionName];
            section.Values.Add(key, value);
            this.sections[sectionName] = section;
        }
        /// <summary>
        /// Очитска секции ini файла
        /// </summary>
        public bool ClearSection(String sectionName)
        {
            if (!this.sections.ContainsKey(sectionName))
                return false;

            CSection section = this.sections[sectionName];
            section.Values.Clear();
            this.sections[sectionName] = section;
            return true;
        }
        /// <summary>
        /// Формирует поток с данными
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            MemoryStream stream = new MemoryStream();

            this.Save(stream);

            return stream;
        }


        /// <summary>
        /// Символ перехода на новую строку
        /// </summary>
        public String NewLine
        {
            get
            {
                return this.newLine;
            }
            set
            {
                this.newLine = value;
            }
        }
        /// <summary>
        /// Флаг записи в файл без пробелов
        /// </summary>
        public bool ExtraSpace
        {
            get
            {
                return this.extraSpace;
            }
            set
            {
                this.extraSpace = value;
            }
        }
        /// <summary>
        /// Список секций ini файла
        /// </summary>
        public Dictionary<String, CSection> Sections
        {
            get
            {
                return this.sections;
            }
        }
    }
}
