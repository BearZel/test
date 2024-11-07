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
    /// Логика взаимодействия для AppSettingsWindow.xaml
    /// </summary>
    public partial class AppSettingsWindow : Window
    {
        private CSettings settings;

        public AppSettingsWindow()
        {
            InitializeComponent();

            this.settings = (CSettings)this.FindResource("Settings");
        }

        public CSettings Settings
        {
            get
            {
                return this.settings;
            }
        }

        private void OKButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
