using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для SequenceOfEvents.xaml
    /// </summary>
    public partial class SequenceOfEvents : Window
    {
        private Soe.Model model = new Soe.Model();
        public SequenceOfEvents()
        {
            InitializeComponent();

            DataContext = model;
        }

        private void SourceCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cv = sender as ComboBox;
            cv.SelectedIndex = -1;
        }

        private void SourceCombobox_Checked(object sender, RoutedEventArgs e)
        {
            model.FilteredEvents.Refresh();
        }

        private void ModulesCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cv = sender as ComboBox;
            cv.SelectedIndex = -1;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            model.LoadAsync();
        }

        private void EventFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            model.FilteredEvents.Refresh();
        }

        private void CsvButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.AddExtension = true;
            saveDialog.DefaultExt = "csv";
            saveDialog.Filter = "csv|*.csv";
            saveDialog.OverwritePrompt = true;

            if (saveDialog.ShowDialog(this) != true)
                return;

            using (StreamWriter sw = new StreamWriter(saveDialog.FileName))
            {
                sw.WriteLine("unixtimestamp;date;time;source;message");
                foreach (Soe.Event ev in model.FilteredEvents)
                {
                    string row = string.Format("{0};{1};{2};{3};{4}",
                        ev.Timestamp.ToFileTimeUtc(), ev.Timestamp.ToString("dd-MM-yyyy"), ev.Timestamp.ToString("HH:mm:ss.fff"), ev.Source, ev.Message);
                    sw.WriteLine(row);
                }
            }
        }

        private void DateTimePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            model.FilteredEvents.Refresh();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            model.SaveConfiguration();
            MessageBox.Show(this, "Настройки применены!", "Настройки", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
