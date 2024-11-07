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

namespace AbakConfigurator
{
    /// <summary>
    /// Interaction logic for CurrencyWindow.xaml
    /// </summary>
    public partial class CurrencyWindow : Window
    {
        Dictionary<string, int> Currency = new Dictionary<string, int>();
        public int GetCurrency = 0;
        public CurrencyWindow()
        {
            InitializeComponent();
            Currency.Add("Стандартная", 10000);
            Currency.Add("1 сек", 1000);
            Currency.Add("500 мс", 500);
            Currency.Add("100 мс", 100);
            CurrencyComboBox.ItemsSource = Currency.Keys;
            //CurrencyComboBox.SelectedValue = "Стандартная";
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(CurrencyComboBox.SelectedValue.ToString());
            int get_currency = Currency[CurrencyComboBox.SelectedValue.ToString()];
            if(get_currency == 10000)
                this.DialogResult = true;

            bool yes = MessageBox.Show("Внимание!", "При использовании нестандарных настроек, значительно увеличивается нагрузка на ПЛК." +
                " Выбранную частоту необходимо использовать только во время наладки!",
                 MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        }
    }
}
