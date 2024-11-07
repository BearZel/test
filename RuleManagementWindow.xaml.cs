using AbakConfigurator.Secure;
using AbakConfigurator.Secure.Data;
using Npgsql;
using NpgsqlTypes;
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
    internal class RuleManagementDataContext : INotifyPropertyChanged
    {
        public RuleData RuleStore { get; set; } = null;
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

    public partial class RuleManagementWindow : Window
    {
        private RuleManagementDataContext m_DataContext = new RuleManagementDataContext();
        private SecureHandler m_Handler = null;
        private SecureProvider m_Provider = null;
        private SecureAuth m_Auth = null;

        public RuleManagementWindow()
        {
            InitializeComponent();

            m_Handler = CGlobal.Handler;
            m_Provider = m_Handler.Provider;
            m_Auth = m_Handler.Auth;

            m_DataContext.RuleStore = (RuleData)m_Handler.Repo["Rule"];
            DataContext = m_DataContext;

            m_DataContext.RuleStore.Init();
            m_DataContext.RuleStore.Load();

            m_DataContext.RuleStore.SelectedGroup = m_DataContext.RuleStore.Groups.ElementAt(0);

            GroupList.SelectedIndex = 0;
            RuleList.SelectedIndex = 0;
        }

        private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupList.SelectedIndex == -1)
            {
                return;
            }

            m_DataContext.RuleStore.SelectedGroup = m_DataContext.RuleStore.Groups.ElementAt(GroupList.SelectedIndex);
            RuleList.SelectedIndex = RuleList.Items.IndexOf(m_DataContext.RuleStore.SelectedGroup.SelectedRule);
            RuleList.ScrollIntoView(m_DataContext.RuleStore.SelectedGroup.SelectedRule);
        }

        private void RuleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RuleList.SelectedIndex == -1)
            {
                return;
            }

            m_DataContext.RuleStore.SelectedGroup.SelectedRule = m_DataContext.RuleStore.SelectedGroup.Rules.ElementAt(RuleList.SelectedIndex);
        }

        private void RuleValueInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            m_DataContext.RuleStore.SelectedGroup.Changed = true;
            m_DataContext.RuleStore.SelectedGroup.SelectedRule.Changed = true;
            m_DataContext.Changed = true;
        }

        private void RuleValueInput_GotFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            RuleEntry entry = (RuleEntry)element.DataContext;

            RuleList.SelectedIndex = RuleList.Items.IndexOf(entry);
        }

        private void RuleValueToggleButton_Click(object sender, RoutedEventArgs e)
        {
            m_DataContext.RuleStore.SelectedGroup.Changed = true;
            m_DataContext.RuleStore.SelectedGroup.SelectedRule.Changed = true;
            m_DataContext.Changed = true;
        }

        private void RuleValueToggleButton_GotFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            RuleEntry entry = (RuleEntry)element.DataContext;

            RuleList.SelectedIndex = RuleList.Items.IndexOf(entry);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            m_DataContext.RuleStore.Save();

            foreach (RuleGroup group in m_DataContext.RuleStore.Groups)
            {
                if (group.Changed)
                {
                    group.Changed = false;

                    foreach (RuleEntry rule in group.Rules)
                    {
                        if (rule.Changed)
                        {
                            rule.Changed = false;
                        }
                    }
                }
            }

            m_DataContext.Changed = false;
            CGlobal.CurrState.Menu.Refresh();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
