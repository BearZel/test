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
    /// Окно с опциями востановления контроллера
    /// </summary>
    public partial class RestoreOptionsWindow : Window
    {
        public RestoreOptionsWindow()
        {
            InitializeComponent();
        }

        private void YesClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
