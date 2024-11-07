using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    /// Логика взаимодействия для EditUserWindow.xaml
    /// </summary>
    public partial class EditUserWindow : Window, INotifyPropertyChanged
    {
        private String userName = "";
        private UInt32 role = 0;
        //Флаг разрешающий изменение всех параметров
        private Boolean enableAllControls = true;

        private ObservableCollection<CUserRole> rolesList = new ObservableCollection<CUserRole>();

        public static readonly DependencyProperty passwordProperty = DependencyProperty.Register("Password",
                                                  typeof(String), typeof(PasswordControl), new FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                      PasswordChanged, CoerseChanged, false, UpdateSourceTrigger.PropertyChanged));

        public static readonly DependencyProperty password2Property = DependencyProperty.Register("Password2",
                                                  typeof(String), typeof(PasswordControl), new FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                      PasswordChanged, CoerseChanged, false, UpdateSourceTrigger.PropertyChanged));

        private static object CoerseChanged(DependencyObject sender, object ob)
        {
            return ob;
        }

        private static void PasswordChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }


        public EditUserWindow(ObservableCollection<CUserRole> rolesList)
        {
            InitializeComponent();

            foreach (CUserRole role in rolesList)
                this.rolesList.Add(role);

        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            

            this.DialogResult = true;
        }

        public ObservableCollection<CUserRole> RolesList
        {
            get
            {
                return this.rolesList;
            }
        }

        public String UserName
        {
            get
            {
                return this.userName;
            }
            set
            {
                this.userName = value;
                this.OnPropertyChanged("UserName");
            }
        }

        public String Password
        {
            get
            {
                return (String)GetValue(passwordProperty);
            }
            set
            {
                SetValue(passwordProperty, value);
                this.OnPropertyChanged("Password");
            }
        }

        public String Password2
        {
            get
            {
                return (String)GetValue(password2Property);
            }
            set
            {
                SetValue(password2Property, value);
                this.OnPropertyChanged("Password2");
            }
        }

        public UInt32 Role
        {
            get
            {
                return this.role;
            }
            set
            {
                this.role = value;
                this.OnPropertyChanged("Role");
            }
        }

        public Boolean EnableAllControls
        {
            get
            {
                return this.enableAllControls;
            }
            set
            {
                this.enableAllControls = value;
                this.OnPropertyChanged("EnableAllControls");
            }
        }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Password1_Changed(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetValue(passwordProperty, passwordBox.Password);
        }

        private void Password2_Changed(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetValue(password2Property, passwordBox.Password);

        }
    }
}
