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
    /// Логика взаимодействия для UserSession.xaml
    /// </summary>
    /// 

    ///<summary>
    ///Класс для передачи в datagrid 
    ///<summary>
    public class UserLogin
    {
        public string login { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
    }
    
 
    public partial class SessionWindows : Window
    {
        public SessionWindows()
        {
            InitializeComponent();

        }
        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            UserLogin userLogin = new UserLogin();
            List<UserLogin> loginList = new List<UserLogin>();
            List<Autentification> userList = CGlobal.Session.GetSessions;
            foreach (Autentification l in userList)
            {
                if (String.Equals(l.ip, CGlobal.Settings.ConnectIP))
                {
                    loginList.Add(new UserLogin { login = l.user, ip = l.ip, port = l.usb });
                }
            }
            usersGrid.ItemsSource = loginList;
        }
      
        //Получаем логин и IP с datagrid, ищем по ним еще и пароль в списке и логинимся в контроллере
        private void usersGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (usersGrid.SelectedItem != null)
            {
                UserLogin path = usersGrid.SelectedItem as UserLogin;
                List<Autentification> userList = CGlobal.Session.GetSessions;
                foreach (Autentification item in userList)
                {
                    if (String.Equals(path.ip, item.ip) & String.Equals(path.login, item.user))
                    {
                        CGlobal.Settings.IP = item.ip;
                        CGlobal.Session.setConnectParams(item.user, item.password, item.ip);

                        //Флаги авторизации с окна сессий
                        CGlobal.Settings.flagSessionWindow = true;
                        CGlobal.CurrState.flagSessionWindows = true;
                        this.DialogResult = true;
                    }
                }
            }
        }
    }
}
