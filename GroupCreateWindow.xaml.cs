using AbakConfigurator.Secure;
using AbakConfigurator.Toolbox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public class GroupCreateDataContext
    {
        public GroupInfo Group { get; set; } = new GroupInfo();
        public ObservableCollection<GroupTypeInfo> Types { get; set; } = new ObservableCollection<GroupTypeInfo>();
        public GroupTypeInfo SelectedType { get; set; } = new GroupTypeInfo();
    }

    public partial class GroupCreateWindow : Window
    {
        GroupCreateDataContext m_DataContext = new GroupCreateDataContext();

        SecureHandler m_Handler = null;
        SecureProvider m_Provider = null;
        SecureAuth m_Auth = null;

        Regex m_RegexNumber = new Regex(@"[0-9]+");
        Regex m_RegexUpperChar = new Regex(@"[A-Z]+");
        Regex m_RegexLowerChar = new Regex(@"[a-z]+");
        Regex m_RegexSymbol = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

        bool m_ChangeMode = false;

        public GroupCreateWindow(GroupInfo group = null)
        {
            InitializeComponent();
            DataContext = m_DataContext;

            m_ChangeMode = group != null;
            m_Handler = CGlobal.Handler;
            m_Provider = m_Handler.Provider;
            m_Auth = m_Handler.Auth;

            if (m_ChangeMode)
            {
                Title = CGlobal.GetResourceValue("l_SecureGroupChange_Title");
                m_DataContext.Group = group;

                CreateButton.Content = CGlobal.GetResourceValue("l_SecureGroupChange_Change");
                CreateButton.Click += ChangeButton_Click;

                NameInput.IsEnabled = false;

                if (m_Auth.GroupType == GroupTypeEnum.Developer)
                {
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeDeveloper"), Type = GroupTypeEnum.Developer });
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeAdministrator"), Type = GroupTypeEnum.Administrator });
                }
                else if (m_DataContext.Group.Type == GroupTypeEnum.Developer || m_DataContext.Group.Type == GroupTypeEnum.Administrator)
                {
                    TypeInput.IsEnabled = false;
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeDeveloper"), Type = GroupTypeEnum.Developer });
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeAdministrator"), Type = GroupTypeEnum.Administrator });
                }

                m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeModerator"), Type = GroupTypeEnum.Moderator });
                m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeSpectator"), Type = GroupTypeEnum.Spectator });

                foreach (var type in m_DataContext.Types)
                {
                    if (type.Type == m_DataContext.Group.Type)
                    {
                        m_DataContext.SelectedType = type;
                    }
                }
            }
            else
            {
                CreateButton.Content = CGlobal.GetResourceValue("l_SecureGroupCreate_Create");
                CreateButton.Click += CreateButton_Click;

                if (m_Auth.GroupType == GroupTypeEnum.Developer)
                {
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeDeveloper"), Type = GroupTypeEnum.Developer });
                    m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeAdministrator"), Type = GroupTypeEnum.Administrator });
                }

                m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeModerator"), Type = GroupTypeEnum.Moderator });
                m_DataContext.Types.Add(new GroupTypeInfo() { Name = CGlobal.GetResourceValue("l_SecureGroupCreate_TypeSpectator"), Type = GroupTypeEnum.Spectator });
            }
        }

        private bool ValidateType(int value)
        {
            return value >= 0;
        }

        private bool ValidateName(string value)
        {
            if (value.Length < 4)
            {
                return false;
            }

            return true;
        }

        private void Name_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !m_RegexNumber.IsMatch(e.Text) && !m_RegexUpperChar.IsMatch(e.Text) && !m_RegexLowerChar.IsMatch(e.Text);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateName(m_DataContext.Group.Name))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureGroupCreate_NameRequire"), CGlobal.GetResourceValue("l_SecureGroupCreate_NameError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!ValidateType(TypeInput.SelectedIndex))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureGroupCreate_TypeRequire"), CGlobal.GetResourceValue("l_SecureGroupCreate_TypeError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var args = new CreateUserGroupArgs(m_DataContext.Group.Name, m_Auth.Account, m_DataContext.Group.Description, (int)m_DataContext.SelectedType.Type, new short[1] { 0 });

            string notice_title = CGlobal.GetResourceValue("l_SecureGroupCreate_Title");
            string notice_text = CGlobal.GetResourceValue("l_SecureGroupCreate_Success");

            var result = m_Provider.CreateUserGroup(args);

            if (result != CreateUserGroupResult.Success)
            {
                m_Handler.UserLog(1, string.Format("Group {0} create failure ({1})", args.Name, result.ToString()));

                switch (result)
                {
                    case CreateUserGroupResult.GroupAlreadyExist:
                        notice_text = CGlobal.GetResourceValue("l_SecureGroupCreate_GroupAlreadyExist");
                        break;
                }

                MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            m_Handler.UserLog(1, string.Format("Group {0} create success", args.Name));
            MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            string notice_title = CGlobal.GetResourceValue("l_SecureGroupChange_Title");
            string notice_text = CGlobal.GetResourceValue("l_SecureGroupChange_Success");

            var args = new ChangeUserGroupArgs(m_DataContext.Group.Name, m_Auth.Account, m_DataContext.Group.Description, (int)m_DataContext.SelectedType.Type, new short[1] { 0 });
            var result = m_Provider.ChangeUserGroup(args);

            if (result != ChangeUserGroupResult.Success)
            {
                m_Handler.UserLog(1, string.Format("Group {0} change failure ({1})", args.Name, result.ToString()));

                switch (result)
                {
                    case ChangeUserGroupResult.GroupNotFound:
                        notice_text = CGlobal.GetResourceValue("l_SecureGroupDelete_GroupNotFound");
                        break;
                }

                MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            m_Handler.UserLog(1, string.Format("Group {0} change success", args.Name));
            MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}
