using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для TermsOfUseDialog.xaml
    /// </summary>
    public partial class TermsOfUseDialog : Window, INotifyPropertyChanged
    {
        public TermsOfUseDialog()
        {
            InitializeComponent();
            DataContext = this;
            TermsTextBlock.Text = Properties.Resources.TermsOfUse;
        }

        private bool isTermsReaded = false;
        public bool IsTermsReaded
        {
            get { return isTermsReaded; }
            set
            {
                isTermsReaded = value;
                OnPropertyChanged(nameof(IsTermsReaded));
            }
        }

        void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        bool IsScrolledToEnd(ScrollViewer v)
        {
            if (v.ExtentHeight <= v.ActualHeight)
            {
                return true;
            }
            return false;
        }

        void TermsScroolViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var v = (ScrollViewer)sender;
            IsTermsReaded = IsScrolledToEnd(v);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
