using AbakConfigurator.Secure;
using AbakConfigurator.Toolbox;
using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Drawing2D;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Input;

namespace AbakConfigurator
{
    public class UserCreateDataContext
    {
        public UserInfo User { get; set; } = new UserInfo();
        public ObservableCollection<GroupInfo> Groups { get; set; } = new ObservableCollection<GroupInfo>();
        public GroupInfo SelectedGroup { get; set; } = new GroupInfo();
    }

    public partial class UserCreateWindow : Window
    {
        UserCreateDataContext m_DataContext = new UserCreateDataContext();

        SecureHandler m_Handler = null;
        SecureProvider m_Provider = null;
        SecureAuth m_Auth = null;

        bool m_ChangeMode = false;

        Regex m_RegexNumber = new Regex(@"[0-9]+");
        Regex m_RegexUpperChar = new Regex(@"[A-Z]+");
        Regex m_RegexLowerChar = new Regex(@"[a-z]+");
        Regex m_RegexSymbol = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");
        
        public UserCreateWindow(UserInfo user = null) 
        {
            InitializeComponent();
            DataContext = m_DataContext;

            m_ChangeMode = user != null;
            m_Handler = CGlobal.Handler;
            m_Provider = m_Handler.Provider;
            m_Auth = m_Handler.Auth;

            FillGroups();

            if (m_ChangeMode)
            {
                m_DataContext.User = user;
                Title = CGlobal.GetResourceValue("l_SecureUserChange_Title");
                AccountInput.IsEnabled = false;
                BannedPanel.Visibility = Visibility.Visible;
                CreateButton.Content = CGlobal.GetResourceValue("l_SecureUserChange_Change");
                CreateButton.Click += ChangeButton_Click;
                PasswordLabel.Text = CGlobal.GetResourceValue("l_SecureUserChange_Password");

                foreach (var group in m_DataContext.Groups)
                {
                    if (group.Id == m_DataContext.User.GroupId)
                    {
                        m_DataContext.SelectedGroup = group;
                    }
                }
            }
            else
            {
                BannedPanel.Visibility = Visibility.Collapsed;
                CreateButton.Click += CreateButton_Click;
            }            
        }

        private void Account_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !m_RegexNumber.IsMatch(e.Text) && !m_RegexUpperChar.IsMatch(e.Text) && !m_RegexLowerChar.IsMatch(e.Text);
        }

        private void Password_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !m_RegexNumber.IsMatch(e.Text) && !m_RegexUpperChar.IsMatch(e.Text) && !m_RegexLowerChar.IsMatch(e.Text) && !m_RegexSymbol.IsMatch(e.Text);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAccount(m_DataContext.User.Account))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureUserCreate_AccountRequire"), CGlobal.GetResourceValue("l_SecureUserCreate_AccountError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!ValidatePassword(PasswordInput.Password))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureUserCreate_PasswordRequire"), CGlobal.GetResourceValue("l_SecureUserCreate_PasswordError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!ValidateGroup(GroupInput.SelectedIndex))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureUserCreate_GroupRequire"), CGlobal.GetResourceValue("l_SecureUserCreate_GroupError"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var args = new CreateUserArgs(m_DataContext.User.Account, m_Auth.Account, Utilities.GetSHA256(PasswordInput.Password), m_DataContext.SelectedGroup.Id)
            {
                ExpireDate = m_DataContext.User.ExpireDate,
                DetailName = m_DataContext.User.Name,
                DetailSurname = m_DataContext.User.Surname,
                DetailCompany = m_DataContext.User.Company,
                DetailDepartment = m_DataContext.User.Department,
                DetailPosition = m_DataContext.User.Position,
                DetailEmail = m_DataContext.User.Email,
                DetailPhone = m_DataContext.User.Phone
            };

            string notice_title = CGlobal.GetResourceValue("l_SecureUserCreate_Title");
            string notice_text = CGlobal.GetResourceValue("l_SecureUserCreate_Success");
            
            var result = m_Provider.CreateUser(args);

            if (result != CreateUserResult.Success)
            {
                m_Handler.UserLog(1, string.Format("User {0} create failure ({1})", args.Account, result.ToString()));

                switch (result)
                {
                    case CreateUserResult.AccountAlreadyExist:
                        notice_text = CGlobal.GetResourceValue("l_SecureUserCreate_AccountAlreadyExist");
                        break;

                    case CreateUserResult.GroupNotFound:
                        notice_text = CGlobal.GetResourceValue("l_SecureUserCreate_GroupNotFound");
                        break;
                }

                MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            m_Handler.UserLog(1, string.Format("User {0} create success", args.Account));
            MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            string notice_title = CGlobal.GetResourceValue("l_SecureUserChange_Title");
            string notice_text = CGlobal.GetResourceValue("l_SecureUserChange_Success");

            if (PasswordInput.Password.Length > 0)
            {
                var change_password_args = new ChangeUserPasswordArgs(m_DataContext.User.Account, m_Auth.Account, "", Utilities.GetSHA256(PasswordInput.Password));
                var change_password_result = m_Provider.ChangeUserPassword(change_password_args);

                if (change_password_result != ChangeUserPasswordResult.Success)
                {
                    m_Handler.UserLog(1, string.Format("User {0} change password failure ({1})", change_password_args.Account, change_password_result.ToString()));

                    notice_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error1");

                    switch (change_password_result)
                    {
                        case ChangeUserPasswordResult.CurrentPasswordMismatch:
                            notice_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error2");
                            break;
                        case ChangeUserPasswordResult.NewPasswordSameAsCurrent:
                            notice_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error3");
                            break;
                        case ChangeUserPasswordResult.NewPasswordHasBeenUsedBefore:
                            notice_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error4");
                            break;
                    }

                    MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                m_Handler.UserLog(1, string.Format("User {0} change password success", change_password_args.Account));
            }

            var args = new ChangeUserInfoArgs(m_DataContext.User.Account, m_Auth.Account, m_DataContext.SelectedGroup.Id, m_DataContext.User.ExpireDate, m_DataContext.User.Banned, m_DataContext.User.Name, m_DataContext.User.Surname, m_DataContext.User.Company, m_DataContext.User.Department, m_DataContext.User.Position, m_DataContext.User.Email, m_DataContext.User.Phone);
            var result = m_Provider.ChangeUserInfo(args);

            if (result != ChangeUserInfoResult.Success)
            {
                m_Handler.UserLog(1, string.Format("User '{0}' change failure: ({1})", args.Account, result.ToString()));

                switch (result)
                {
                    case ChangeUserInfoResult.AccountNotFound:
                        notice_text = CGlobal.GetResourceValue("l_SecureUserDelete_AccountNotFound");
                        break;

                    case ChangeUserInfoResult.GroupNotFound:
                        notice_text = CGlobal.GetResourceValue("l_SecureUserCreate_GroupNotFound");
                        break;
                }

                MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            m_Handler.UserLog(1, string.Format("Changed User '{0}'", args.Account));
            MessageBox.Show(notice_text, notice_title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private bool ValidatePassword(string value)
        {
            if (value.Length < 8)
            {
                return false;
            }

            return m_RegexNumber.IsMatch(value) && m_RegexUpperChar.IsMatch(value) && m_RegexLowerChar.IsMatch(value) && m_RegexSymbol.IsMatch(value);
        }

        private bool ValidateAccount(string value)
        {
            if (value.Length < 4)
            {
                return false;
            }

            return true;
        }

        private bool ValidateGroup(int value)
        {
            if (value < 0)
            {
                return false;
            }

            return true;
        }

        private void FillGroups()
        {
            var command = new NpgsqlCommand("SELECT * FROM sec_user_groups ORDER BY id", CGlobal.Handler.DBConnection);
            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            while (reader.Read())
            {
                var group_info = new GroupInfo()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    Type = (GroupTypeEnum)reader.GetInt32(reader.GetOrdinal("type"))
                };

                if (m_Auth.GroupType == GroupTypeEnum.Developer || (group_info.Type != GroupTypeEnum.Developer && group_info.Type != GroupTypeEnum.Administrator))
                {
                    m_DataContext.Groups.Add(group_info);
                }
            }

            reader.Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
