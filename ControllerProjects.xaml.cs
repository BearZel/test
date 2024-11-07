using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace AbakConfigurator.IEC
{
    /// <summary>
    /// Логика взаимодействия для ControllerProjects.xaml
    /// </summary>
    /// 

    public struct Project
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Size { get; set; }
    }

    public partial class ControllerProjects : Window
    {
        public ObservableCollection<Project> Projects { get; set; } = new ObservableCollection<Project>();
        public string FileData { get; private set; }
        public Project SelectedProject { get; set; }

        CSSHClient _shh = CGlobal.Session.SSHClient;

        const string _projectsPath = "/opt/abak/iecprojects";
        const string _browseProjectsCommand = "ls -l /opt/abak/iecprojects | grep .iec";
        const string _parseProjectsRegex = ".*root *(\\d{1,}) *(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) *(\\d{1,}) *(\\d{1,}:\\d{1,}|\\d{1,}) *(\\w{1,}\\.iec)";
        public ControllerProjects()
        {
            InitializeComponent();
            DataContext = this;

            Load();
        }

        public Project? GetProjectByName(string name)
        {
            var projects = Projects.Where(p => p.Name == name);
            if (projects.Count() == 0)
            {
                return null;
            }
            return projects.First();
        }

        public void Load()
        {
            try
            {
                string result = _shh.ExecuteCommand(_browseProjectsCommand);
                Regex regex = new Regex(_parseProjectsRegex);

                foreach (Match match in regex.Matches(result))
                {
                    if (match.Groups.Count == 6)
                    {
                        Project project = new Project();
                        project.Size = match.Groups[1].Value;

                        string date_str = string.Format("{0} {1} {2}", match.Groups[2].Value, match.Groups[3], match.Groups[4]);
                        DateTime date = DateTime.Now;
                        if (!DateTime.TryParseExact(date_str, "MMM d H:m", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                        {
                            DateTime.TryParseExact(date_str, "MMM d yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                        }
                        project.Date = date;
                        project.Name = match.Groups[5].Value;

                        Projects.Add(project);
                    }
                }
            }
            catch { }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            string result = _shh.ExecuteCommand(string.Format("rm {0}/{1}", _projectsPath, SelectedProject.Name));
            if (result.Length == 0)
            {
                Projects.Remove(SelectedProject);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string project_path = string.Format("{0}/{1}", _projectsPath, SelectedProject.Name);
            var stream = _shh.ReadFile(project_path);
            if (stream == null)
            {
                MessageBox.Show(this, "Не удалось загрузить проект! " + project_path, "Загрузка проекта", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var reader = new StreamReader(stream);
            FileData = reader.ReadToEnd();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
