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
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        public PasswordWindow()
        {
            InitializeComponent();
        }

        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void previewTextInput(object sender, TextCompositionEventArgs e)
        {
            //Если не цифра то запрещаем
            e.Handled = !CAuxil.CheckStringForInt(e.Text, true);
        }

        private void dataObjectPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String text = e.DataObject.GetData(typeof(String)) as String;
                if (!CAuxil.CheckStringForInt(text, true))
                    e.CancelCommand(); //Если не цифра то запрещаем
            }
            else
                e.CancelCommand();
        }
    }
}
