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
    /// Interaction logic for CanSpeedWarning.xaml
    /// </summary>
    public partial class CanSpeedWarning : Window
    {
        public int ButtonEvent = 0;

        public CanSpeedWarning()
        {
            InitializeComponent();
        }

        private void AutomaticButtonClick_Handler(object sender, EventArgs e)
        {
            ButtonEvent = 1;
            this.DialogResult = true;
        }
        private void ManualButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            ButtonEvent = 2;
            this.DialogResult = true;
        }

        private void NoButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

