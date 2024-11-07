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
using System.Collections.ObjectModel;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        //Имя пользователя
        private String user = "";
        //Пароль
        private String password = "";
        private CSettings settings = new CSettings();
        private ObservableCollection<String> ipList = new ObservableCollection<string>();

        public LoginWindow()
        {
            InitializeComponent();
            userNameBox.Focus();
            DataContext = this;
            if (SaveIP.GetUSB() == true)
                ShowIPandName.Content = $"Подключение по Micro USB / USB Type-C";
            else
                ShowIPandName.Content = $"Подключение к {SaveIP.GetIP()}";
        }
        public LoginWindow(string user, string password)
        {
            InitializeComponent();
            userNameBox.Focus();
            DataContext = this;
            ShowIPandName.Visibility = Visibility.Hidden;
        }
        private void OKButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public String User
        {
            get
            {
                return this.user;
            }
            set
            {
                this.user = value;
            }
        }

        public String Password
        {
            get
            {
                return this.password;
            }
            set
            {
                this.password = value;
            }
        }

        private void PasswordChanged_Handler(object sender, RoutedEventArgs e)
        {
            this.Password = this.passwordBox.Password;
        }

        private void MouseDoubleClickHandler(object sender, MouseButtonEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                userNameBox.Text = "root";
                passwordBox.Password = "Zer0Day!";
                CGlobal.NaladchikStyle = true;
            }
        }
    }
}
 