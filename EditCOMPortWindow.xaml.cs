using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Npgsql;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для EditCOMPortWindow.xaml
    /// </summary>
    public partial class EditCOMPortWindow : Window
    {
        private bool firstLoop = true;
        private Dictionary<string, string> messages = new Dictionary<string, string>
        {
            {"", "Поле не должно быть пустым!"},
            {"0", "Нельзя применять нулевое значение!"},
        };
        private Dictionary<bool, UInt32> validIntervals = new Dictionary<bool, UInt32>
        {
            {false, 4},
            {true, 10}
        };
        private CComPort port;
        private int savedRedirectPort;
        private readonly ObservableCollection<FireWallRule> rules;
        public CComPort Port { get => port; }
        public EditCOMPortWindow(CComPort port, ObservableCollection<FireWallRule> rules, bool isAssembly510)
        {
            this.port = port.Clone() as CComPort;
            savedRedirectPort = this.port.RedirectPort;
            this.rules = rules;
            InitializeComponent();
            if (!isAssembly510)
                RedirectBox.Visibility = RedirectCheckBox.Visibility = Visibility.Collapsed;

            firstLoop = false;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            List<string> range = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            e.Handled = !range.Contains(e.Text);
        }
        private bool IsIntervalCorrect(UInt32 baudRate)
        {
            UInt32 baud = baudRate;
            int interval = Convert.ToInt32(IntervalBox.Text);
            if (validIntervals[baud < 9600] <= interval)
                return true;

            port.Interval = validIntervals[baud < 9600];
            return false;
        }
        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            List<TextBox> textBoxes = SettingsGrid.Children.OfType<TextBox>().ToList();
            foreach (TextBox item in textBoxes)
            {
                if (!messages.ContainsKey(item.Text))
                    continue;

                MessageBox.Show(messages[item.Text], "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool isError = rules.Any(rule => rule.Port == port.RedirectPort.ToString());
            if (isError && port.IsRedirect && savedRedirectPort!= port.RedirectPort && port.RedirectPort != 0)
            {
                MessageBox.Show($"TCP порт {port.RedirectPort} уже занят! Укажите другой порт!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CAuxil.SetTagValue(this.port.SlaveMode.ToString(), $"{this.port.Name}_SLAVE");
            this.DialogResult = true;
        }

        private void RedirectCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            if (checkBox.IsChecked == true && IPTextBox.Text == "0")
            {
                IPTextBox.FontSize = 12;
                IPTextBox.Text = "";
            }
            if (checkBox.IsChecked == false && IPTextBox.Text == "")
            {
                IPTextBox.FontSize = 100;
                IPTextBox.Text = "0";
            }
        }
        private void BaudBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (firstLoop)
                return;

            ComboBox comboBox = (ComboBox)sender;
            IsIntervalCorrect(Convert.ToUInt32(comboBox.SelectedItem));
        }
    }
}
