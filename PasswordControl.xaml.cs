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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AbakConfigurator
{
    /// <summary>
    /// Логика взаимодействия для PasswordControl.xaml
    /// </summary>
    public partial class PasswordControl : UserControl
    {
        private Boolean textBoxChanging = false;
        private Boolean passwordBoxChanging = false;

        public static readonly DependencyProperty passwordProperty = DependencyProperty.Register("Password", 
                                                  typeof(String), typeof(PasswordControl), new FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                      PasswordChanged, CoerseChanged, false, UpdateSourceTrigger.PropertyChanged));

        public PasswordControl()
        {
            InitializeComponent();
        }

        private static object CoerseChanged(DependencyObject sender, object ob)
        {
            return ob;
        }

        private static void PasswordChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PasswordControl passwordControl = sender as PasswordControl;
            if (passwordControl == null)
                return;

            if (!passwordControl.textBoxChanging)
            {
                passwordControl.textBoxChanging = true;
                try
                {
                    passwordControl.textBox.Text = passwordControl.Password;
                }
                finally
                {
                    passwordControl.textBoxChanging = false;
                }
            }

            if (!passwordControl.passwordBoxChanging)
            {
                passwordControl.passwordBoxChanging = true;
                try
                {
                    passwordControl.passwordBox.Password = passwordControl.Password;
                }
                finally
                {
                    passwordControl.passwordBoxChanging = false;
                }
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
            }
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.passwordBoxChanging)
                return;

            this.textBoxChanging = true;
            try
            {
                SetValue(passwordProperty, this.textBox.Text);
                this.passwordBox.Password = (String)GetValue(passwordProperty); ;
            }
            finally
            {
                this.textBoxChanging = false;
            }
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.textBoxChanging)
                return;

            this.passwordBoxChanging = true;
            try
            {
                SetValue(passwordProperty, this.passwordBox.Password);
                this.textBox.Text = (String)GetValue(passwordProperty); ;
            }
            finally
            {
                this.passwordBoxChanging = false;
            }
        }
    }
}
