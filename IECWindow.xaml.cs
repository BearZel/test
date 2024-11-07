using AbakConfigurator.Secure;
using AbakConfigurator.Secure.Data;
using AbakConfigurator.Secure.Entry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace AbakConfigurator
{
    internal class IECDataContext : INotifyPropertyChanged
    {
        public IECData IECStore { get; set; } = null;
        bool m_Changed = false;

        public bool Changed
        {
            get => m_Changed;
            set
            {
                m_Changed = value;
                OnPropertyChanged(nameof(Changed));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    
    public partial class IECWindow : Window
    {
        IECDataContext m_DataContext = new IECDataContext();

        public IECWindow()
        {
            InitializeComponent();
            DataContext = m_DataContext;

            m_DataContext.IECStore = (IECData)CGlobal.Handler.Repo["IEC"];
            m_DataContext.IECStore.Load();
        }

        void ConfigList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        void ConfigValueInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_DataContext.Changed = true;
        }

        void ConfigValueInput_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        void ConfigValueToggleButton_Click(object sender, RoutedEventArgs e)
        {
            m_DataContext.Changed = true;
        }

        void ConfigValueToggleButton_GotFocus(object sender, RoutedEventArgs e)
        {

        }    

        void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            m_DataContext.IECStore.Save();
            m_DataContext.Changed = false;
        }

        void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
