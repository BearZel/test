
using System.Linq;
using System.Windows;
using System.Xml;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Windows.Threading;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Npgsql;
using Renci.SshNet;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

namespace AbakConfigurator.IEC
{
    /// <summary>
    /// Логика взаимодействия для IEC.xaml
    /// </summary>
    public partial class IECWindow : Window
    {
        public MainViewModel MainViewModel { get; private set; }
        const string cpuProjectsPath = "/opt/abak/iecprojects/";
        private string _projectPath = "";
        private string _restartServiceTag = "IEC_SERVER_RESTART";

        public IECWindow()
        {
            InitializeComponent();

            MainViewModel = new MainViewModel();
            DataContext = MainViewModel;
        }

        private void AddDataButton_Click(object sender, RoutedEventArgs e)
        {
            AddRowWindow addRowWindow = new AddRowWindow(MainViewModel.UsedIOA, MainViewModel.ServerModel.GetUsedModBusAddress(), false);
            addRowWindow.Owner = this;
            if (addRowWindow.ShowDialog() == true)
            {
                MainViewModel.UpdateRows(addRowWindow.EditRowModel.GetRows());
            }
        }

        private void AddCommandButton_Click(object sender, RoutedEventArgs e)
        {
            AddRowWindow addRowWindow = new AddRowWindow(MainViewModel.UsedIOA, MainViewModel.CommandsModel.GetUsedModBusAddress(), true);
            addRowWindow.Owner = this;
            if (addRowWindow.ShowDialog() == true)
            {
                MainViewModel.UpdateRows(addRowWindow.EditRowModel.GetRows());
            }
        }

        private void MenuNewButton_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.Clear();
        }

        private void MenuOpenLocal_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog saveDialog = new OpenFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "iec";
            saveDialog.Filter = "iec|*.iec";

            if (saveDialog.ShowDialog(this) != true)
                return;

            _projectPath = saveDialog.FileName;
            XDocument xdoc = XDocument.Load(_projectPath);

            ProjectLoader ps = new ProjectLoader(xdoc);
            Settings s = null;
            ps.Load(out s);

            List<ServerRow> rows;
            ps.Load(out rows);

            MainViewModel.SettingsModel.Settings = s;
            MainViewModel.SetRows(rows);
        }

        private void MenuOpenFromController_Click(object sender, RoutedEventArgs e)
        {
            ControllerProjects cp = new ControllerProjects();
            cp.Owner = this;
            if (cp.ShowDialog() == true)
            {
                _projectPath = "";
                XDocument xDoc = XDocument.Parse(cp.FileData);
                ProjectLoader ps = new ProjectLoader(xDoc);
                Settings s = null;
                ps.Load(out s);

                List<ServerRow> rows = null;
                ps.Load(out rows);

                MainViewModel.SettingsModel.Settings = s;
                MainViewModel.SetRows(rows);
            }
        }

        private void MenuOpenENode_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog saveDialog = new OpenFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "xml";
            saveDialog.Filter = "xml|*.xml";

            if (saveDialog.ShowDialog(this) != true)
                return;

            XDocument xDoc = XDocument.Load(saveDialog.FileName);
            ENodeLoader loader = new ENodeLoader(xDoc);

            Settings s = null;
            loader.Load(out s);

            List<ServerRow> rows;
            loader.Load(out rows);

            MainViewModel.SettingsModel.Settings = s;
            MainViewModel.SetRows(rows);
        }
        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            if (_projectPath.Length == 0)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.AddExtension = true;
                saveDialog.DefaultExt = "iec";
                saveDialog.Filter = "iec|*.iec";

                if (saveDialog.ShowDialog(this) != true)
                    return;
                _projectPath = saveDialog.FileName;
            }

            XDocument xDoc = new XDocument();
            ProjectSaver ps = new ProjectSaver(xDoc);
            ps.Save(MainViewModel.SettingsModel.Settings);
            ps.Save(MainViewModel.ServerModel.GetRows());
            ps.Save(MainViewModel.CommandsModel.GetRows());

            xDoc.Save(_projectPath);
        }

        private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "iec";
            saveDialog.Filter = "iec|*.iec";

            if (saveDialog.ShowDialog(this) != true)
                return;

            _projectPath = saveDialog.FileName;

            XDocument xDoc = new XDocument();
            ProjectSaver ps = new ProjectSaver(xDoc);
            ps.Save(MainViewModel.SettingsModel.Settings);
            ps.Save(MainViewModel.ServerModel.GetRows());
            ps.Save(MainViewModel.CommandsModel.GetRows());

            xDoc.Save(_projectPath);
        }

        private void MenuUploadToController_Click(object sender, RoutedEventArgs e)
        {
            ControllerProjects cp = new ControllerProjects();
            Project? defProject = cp.GetProjectByName("default.iec");

            DateTime dateTime = defProject != null ? defProject.Value.Date : DateTime.Now;

            var ssh = CGlobal.Session.SSHClient;

            var cmd = string.Format($"mv {cpuProjectsPath}default.iec {cpuProjectsPath}project_{0:MM_dd_yy_H_mm_ss}.iec", dateTime);
            string rename_last_project = ssh.ExecuteCommand(cmd);
            if (rename_last_project.Length != 0)
            {
                MessageBox.Show(this, "Не удалось загрузить проект! " + rename_last_project, "Загрузка проекта", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var xDoc = new XDocument();
            ProjectSaver ps = new ProjectSaver(xDoc);
            ps.Save(MainViewModel.SettingsModel.Settings);
            ps.Save(MainViewModel.ServerModel.GetRows());
            ps.Save(MainViewModel.CommandsModel.GetRows());

            var stream = CAuxil.StringToStream(xDoc.ToString());
            ssh.ExecuteCommand($"mkdir -p {cpuProjectsPath}");
            bool success_upload = ssh.WriteFile($"{cpuProjectsPath}default.iec", stream);
            if (!success_upload)
            {
                MessageBox.Show(this, "Не удалось загрузить проект!", "Загрузка проекта", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(this, "Проект успешно загружен!", "Загрузка проекта", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuRestartIEC_Click(object sender, RoutedEventArgs e)
        {
            var param = CConfig.ParamByTag(_restartServiceTag) as CParam;
            if (param == null)
            {
                MessageBox.Show(this, $"Не удалось найти тэг '{_restartServiceTag}'!", "Ошибка при перезапуске МЭК.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CAuxil.SetTagValue("true", _restartServiceTag);
            MessageBox.Show(this, "Перезапуск прошел успешно!", "Перезапуск МЭК.", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DataDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete || DataDataGrid.SelectedItems == null)
            {
                return;
            }

            foreach (var item in DataDataGrid.SelectedItems.Cast<ServerRowModel>())
            {
                item.Dispose();
            }
        }

        private void CommandDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete || CommandDataGrid.SelectedItems == null)
            {
                return;
            }

            foreach (var item in CommandDataGrid.SelectedItems.Cast<ServerRowModel>())
            {
                item.Dispose();
            }
        }
    }
}
