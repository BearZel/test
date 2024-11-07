using Npgsql;
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
using System.Threading;
using System.ComponentModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Окно выводит список доступных для модуля обновлений в контроллере
    /// </summary>
    public partial class ModuleUpdatesWindow : Window
    {
        private void OKClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }       
    }
       
}

