using Newtonsoft.Json.Linq;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Principal;
using AbakConfigurator.Toolbox;
using static System.Net.Mime.MediaTypeNames;
using NpgsqlTypes;
using System.Data.Common;
using System.Collections.ObjectModel;
using AbakConfigurator.Secure.Data;
using System.Windows.Threading;
using Renci.SshNet;

namespace AbakConfigurator.Secure
{
    public class SecureHandler
    {
        Window m_Owner = null;
        NpgsqlConnection m_DBConnection = null;
        NpgsqlConnection m_DBStateConnection = null;
        CSSHClient m_SSHClient = null;
        DispatcherTimer m_UserStateTimer = new DispatcherTimer();

        public SecureProvider Provider { get; private set; } = null;
        public SecureAuth Auth { get; private set; } = new SecureAuth();
        public SecureRepo Repo { get; private set; } = null;

        public SecureHandler(Window owner)
        {
            m_Owner = owner;
            Provider = new SecureProvider();
            Repo = new SecureRepo();
            m_UserStateTimer.IsEnabled = false;
            m_UserStateTimer.Interval = new TimeSpan(0, 0, 0, 25, 0);
            m_UserStateTimer.Tick += new EventHandler(UserStateTimer_Tick);
        }

        void UserStateTimer_Tick(object sender, EventArgs e)
        {
            if (!Auth.Authorized)
            {
                return;
            }
            try
            {
                UpdateUserState(DBStateConnection);
            }
            catch
            {

            }
        }

        public bool UserLogin()
        {
            if (Auth.Authorized)
            {
                return false;
            }

            var login_window = new LoginWindow() { Owner = m_Owner };
            if (login_window.ShowDialog() != true)
            {
                return false;
            }

            var rule_repo = (RuleData)Repo["Rule"];

            string account = login_window.User;
            string password = login_window.Password;
            string ip = Utilities.GetLocalIP();

            if (account == "" || password == "")
            {
                return false;
            }

            Auth.Reset(account, ip);
            if (Auth.Account != "root")
            {
                if (rule_repo.GetRuleValueInt("connection_max_count") != 0 && Provider.GetConnectedUserCount() >= rule_repo.GetRuleValueInt("connection_max_count"))
                {
                    UserLog(0, string.Format("Login failure ({0})", CGlobal.GetResourceValue("l_SecureLoginErrorText6")));
                    MessageBox.Show(CGlobal.GetResourceValue("l_SecureLoginErrorText6"), CGlobal.GetResourceValue("l_SecureLoginErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    Auth.Reset();
                    return false;
                }
            }
            var group = Provider.GetUserGroupType(new GetUserGroupTypeArgs(account));

            if (group == GroupTypeEnum.None)
            {
                UserLog(0, string.Format("Login failure ({0})", CGlobal.GetResourceValue("l_SecureGroupDelete_GroupNotFound")));
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureGroupDelete_GroupNotFound"), CGlobal.GetResourceValue("l_SecureLoginErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Auth.Reset();
                return false;
            }

            var result = Provider.UserLogin(new UserLoginArgs(account, Utilities.GetSHA256(password), Auth.IP));

            if (result == UserLoginResult.ForcedPasswordChange)
            {
                var password_window = new PasswordChangeWindow(true) { Owner = m_Owner };
                if (password_window.ShowDialog() != true)
                {
                    UserLog(0, string.Format("Login failure ({0})", CGlobal.GetResourceValue("l_SecureLoginErrorText5")));
                    MessageBox.Show(CGlobal.GetResourceValue("l_SecureLoginErrorText5"), CGlobal.GetResourceValue("l_SecureLoginErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    Auth.Reset();
                    return false;
                }
            }
            else if (result != UserLoginResult.Success /*&& Auth.Account != "root"*/)
            {
                UserLog(0, string.Format("Login failure ({0})", result.ToString()));

                string error_text = CGlobal.GetResourceValue("l_SecureLoginErrorText1");
                string error_title = CGlobal.GetResourceValue("l_SecureLoginErrorTitle");

                switch (result)
                {
                    case UserLoginResult.AccountAlreadyConnected:
                        error_text = CGlobal.GetResourceValue("l_SecureLoginErrorText2");
                        break;
                    case UserLoginResult.AccountExpired:
                        error_text = CGlobal.GetResourceValue("l_SecureLoginErrorText3");
                        break;
                    case UserLoginResult.AccountBanned:
                        error_text = CGlobal.GetResourceValue("l_SecureLoginErrorText4");
                        break;
                }

                MessageBox.Show(error_text, error_title, MessageBoxButton.OK, MessageBoxImage.Warning);
                Auth.Reset();

                return false;
            }

            var terms = new TermsOfUseDialog { Owner = m_Owner };
            if (terms.ShowDialog() == false)
            {
                return false;
            }

            UserLog(0, string.Format("Login success"));

            Auth.Reset(account, ip, true);
            m_UserStateTimer.IsEnabled = true;

            return true;
        }

        public bool UserLogout(bool manual_action = false)
        {
            if (!Auth.Authorized)
            {
                return false;
            }

            if (manual_action)
            {
                string title = CGlobal.GetResourceValue("l_SecureLogoutTitle");
                string text = CGlobal.GetResourceValue("l_SecureLogoutText") + Environment.NewLine + CGlobal.GetResourceValue("l_SecureLogoutNotice");
                var confirm_result = MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm_result != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            try
            {
                var result = Provider.UserLogout(new UserLogoutArgs(Auth.Account));

                if (result != UserLogoutResult.Success)
                {
                    UserLog(0, string.Format("Logout failure ({0})", result.ToString()));
                    return false;
                }

                UserLog(0, string.Format("Logout success"));
            }
            catch (SecureProviderException)
            {

            }

            Auth.Reset();
            m_UserStateTimer.IsEnabled = false;
            return true;
        }

        public bool ChangeUserPassword(string current_password, string new_password, bool force = false)
        {
            if (!Auth.Authorized && !force)
            {
                return false;
            }

            string error_title = CGlobal.GetResourceValue("l_SecureChangePassword_Title");
            string error_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error1");
            
            var result = Provider.ChangeUserPassword(new ChangeUserPasswordArgs(Auth.Account, Auth.Account, Utilities.GetSHA256(current_password), Utilities.GetSHA256(new_password)));

            if (result != ChangeUserPasswordResult.Success)
            {
                UserLog(1, string.Format("Change password failure ({0})", result.ToString()));

                switch (result)
                {
                    case ChangeUserPasswordResult.CurrentPasswordMismatch:
                        error_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error2");
                        break;
                    case ChangeUserPasswordResult.NewPasswordSameAsCurrent:
                        error_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error3");
                        break;
                    case ChangeUserPasswordResult.NewPasswordHasBeenUsedBefore:
                        error_text = CGlobal.GetResourceValue("l_SecureChangePassword_Error4");
                        break;
                }

                MessageBox.Show(error_text, error_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            UserLog(1, string.Format("Change password success"));
            MessageBox.Show(CGlobal.GetResourceValue("l_SecureChangePassword_Success"), error_title, MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        public void UpdateUserState(NpgsqlConnection connection)
        {
            if (!Auth.Authorized)
            {
                return;
            }

            var command = new NpgsqlCommand("SELECT sec_update_user_state(@account)", connection);
            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, Auth.Account);
            command.ExecuteNonQuery();
        }

        public void UserLog(int type, string text)
        {
            if (Auth.Account == "")
            {
                return;
            }

            Provider.UserLog(new UserLogArgs(Auth.Account, Auth.IP, type, text));
        }

        public NpgsqlConnection DBConnection
        {
            get
            {
                if (m_DBConnection == null)
                {
                    m_DBConnection = CGlobal.Session.CreateSQLConnection(CGlobal.DBUser, CGlobal.DBPassword, true, false, 5);
                }

                return m_DBConnection;
            }
        }

        public NpgsqlConnection DBStateConnection
        {
            get
            {
                if (m_DBStateConnection == null)
                {
                    m_DBStateConnection = CGlobal.Session.CreateSQLConnection(CGlobal.DBUser, CGlobal.DBPassword, true, false, 5);
                }

                return m_DBStateConnection;
            }
        }

        public CSSHClient SSHClient
        {
            get
            {
                if (m_SSHClient == null)
                {
                    m_SSHClient = CGlobal.Session.OpenSSHConnection(DBConnection);
                }

                return m_SSHClient;
            }
        }
    }
}
