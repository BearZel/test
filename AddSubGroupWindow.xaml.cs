using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for AddSubGroupWindow.xaml
    /// </summary>
    public partial class AddSubGroupWindow : Window
    {
        public AddSubGroupWindow()
        {
            InitializeComponent();
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
