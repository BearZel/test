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
using System.ComponentModel;

namespace AbakConfigurator
{
    public partial class CustomMsgBox : Window
    {
        public List<Func<string>> CheckingsForFirstButton = new List<Func<string>>();
        public List<Func<string>> CheckingsForSecondButton = new List<Func<string>>();
        public string Message
        { 
            get;
        }
        public string WindowTitle
        {
            get;
        }
        public string FirstButtonName 
        {
            get;
        }
        public string SecondButtonName
        {
            get;
        }
        public string FirstButtonTip
        {
            get;
        }
        public string SecondButtonTip 
        {
            get;
        }
        public int ButtonEvent = 0;
        public CustomMsgBox()
        {
            InitializeComponent();
        }
        public void SetFuncsForFirstButton()
        {

        }
        public CustomMsgBox(string title, string message, string firstButtonName, string secondButtonName, string firstButtonTip, string secondButtonTip)
        {
            WindowTitle = title;
            Message = message;
            FirstButtonName = firstButtonName;
            SecondButtonName = secondButtonName;
            FirstButtonTip = firstButtonTip;
            SecondButtonTip = secondButtonTip;
            InitializeComponent();
        }
        private void AutomaticButtonClick_Handler(object sender, EventArgs e)
        {
            foreach (var action in CheckingsForFirstButton)
            {
                string message = (string)action.DynamicInvoke();
                if (message != "")
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            ButtonEvent = 1;
            this.DialogResult = true;
        }
        private void ManualButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            foreach (var action in CheckingsForSecondButton)
            {
                string message = (string)action.DynamicInvoke();
                if (message != "")
                {
                    MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            ButtonEvent = 2;
            this.DialogResult = true;
        }

        private void NoButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
