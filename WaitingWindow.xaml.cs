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
    /// <summary>
    /// Interaction logic for WaitingWindow.xaml
    /// </summary>
    public partial class WaitingWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private string message = "";
        public bool EnableClosing { get; set; } = false;
        private void SizeSettings(double height = 0, double width = 0)
        {

        }

        public WaitingWindow()
        {
            InitializeComponent();
            Message.Content = "Пожалуйста, подождите...";
        }
        
        public WaitingWindow(string message)
        {
            InitializeComponent();
            Message.Content = message;
        }
        public WaitingWindow(string message, double height, double width)
        {
            this.message = message;
            InitializeComponent();
            Message.Content = message;
            this.Height = height;
            this.Width = width;
        }

        public void HardClose()
        {
            EnableClosing = true;
            this.Close();
        }
        ////Функция для отмены закрытия конфигуратора во время обновления
        //private void ClosingOff(object sender, CancelEventArgs e)
        //{
        //    EnableClosing = true;
        //}
    }
}
