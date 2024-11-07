using AbakConfigurator.Secure.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace AbakConfigurator.Secure
{
    public class PasswordChangeDataContext : INotifyPropertyChanged
    {
        bool m_Force = false;

        int m_PasswordMinLength = 6;
        int m_PasswordMaxLength = 10;
        bool m_PasswordReqLowerCase = false;
        bool m_PasswordReqUpperCase = false;
        bool m_PasswordReqNumeric = false;
        bool m_PasswordReqSpecial = false;

        bool m_RuleMinLengthState = false;
        bool m_RuleMaxLengthState = false;
        bool m_RuleLowerCaseState = false;
        bool m_RuleUpperCaseState = false;
        bool m_RuleNumericState = false;
        bool m_RuleSpecialState = false;
        bool m_RuleEqualState = false;
        bool m_RuleFreshState = false;

        bool m_State = false;
        string m_PasswordMinLengthLabel = "";

        public bool Force
        {
            get => m_Force;
            set
            {
                m_Force = value;
                OnPropertyChanged(nameof(Force));
            }
        }

        public bool State
        {
            get => m_State;
            set
            {
                m_State = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public string PasswordMinLengthLabel
        {
            get => m_PasswordMinLengthLabel;
            set
            {
                m_PasswordMinLengthLabel = value;
                OnPropertyChanged(nameof(PasswordMinLengthLabel));
            }
        }

        public int PasswordMinLength
        {
            get => m_PasswordMinLength;
            set
            {
                m_PasswordMinLength = value;
                OnPropertyChanged(nameof(PasswordMinLength));
            }
        }

        public int PasswordMaxLength
        {
            get => m_PasswordMaxLength;
            set
            {
                m_PasswordMaxLength = value;
                OnPropertyChanged(nameof(PasswordMaxLength));
            }
        }

        public bool PasswordReqLowerCase
        {
            get => m_PasswordReqLowerCase;
            set
            {
                m_PasswordReqLowerCase = value;
                OnPropertyChanged(nameof(PasswordReqLowerCase));
            }
        }

        public bool PasswordReqUpperCase
        {
            get => m_PasswordReqUpperCase;
            set
            {
                m_PasswordReqUpperCase = value;
                OnPropertyChanged(nameof(PasswordReqUpperCase));
            }
        }

        public bool PasswordReqNumeric
        {
            get => m_PasswordReqNumeric;
            set
            {
                m_PasswordReqNumeric = value;
                OnPropertyChanged(nameof(PasswordReqNumeric));
            }
        }

        public bool PasswordReqSpecial
        {
            get => m_PasswordReqSpecial;
            set
            {
                m_PasswordReqSpecial = value;
                OnPropertyChanged(nameof(PasswordReqSpecial));
            }
        }

        public bool RuleMinLengthState
        {
            get => m_RuleMinLengthState;
            set
            {
                m_RuleMinLengthState = value;
                OnPropertyChanged(nameof(RuleMinLengthState));
            }
        }

        public bool RuleMaxLengthState
        {
            get => m_RuleMaxLengthState;
            set
            {
                m_RuleMaxLengthState = value;
                OnPropertyChanged(nameof(RuleMaxLengthState));
            }
        }

        public bool RuleLowerCaseState
        {
            get => m_RuleLowerCaseState;
            set
            {
                m_RuleLowerCaseState = value;
                OnPropertyChanged(nameof(RuleLowerCaseState));
            }
        }

        public bool RuleUpperCaseState
        {
            get => m_RuleUpperCaseState;
            set
            {
                m_RuleUpperCaseState = value;
                OnPropertyChanged(nameof(RuleUpperCaseState));
            }
        }
        public bool RuleNumericState
        {
            get => m_RuleNumericState;
            set
            {
                m_RuleNumericState = value;
                OnPropertyChanged(nameof(RuleNumericState));
            }
        }

        public bool RuleSpecialState
        {
            get => m_RuleSpecialState;
            set
            {
                m_RuleSpecialState = value;
                OnPropertyChanged(nameof(RuleSpecialState));
            }
        }

        public bool RuleEqualState
        {
            get => m_RuleEqualState;
            set
            {
                m_RuleEqualState = value;
                OnPropertyChanged(nameof(RuleEqualState));
            }
        }

        public bool RuleFreshState
        {
            get => m_RuleFreshState;
            set
            {
                m_RuleFreshState = value;
                OnPropertyChanged(nameof(RuleFreshState));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class PasswordChangeWindow : Window
    {
        PasswordChangeDataContext m_DataContext = new PasswordChangeDataContext();

        Regex m_RegexNumber = new Regex("[0-9]+", RegexOptions.Compiled);
        Regex m_RegexLowerChar = new Regex("[a-z]+", RegexOptions.Compiled);
        Regex m_RegexUpperChar = new Regex("[A-Z]+", RegexOptions.Compiled);
        Regex m_RegexSymbol = new Regex("[ !`@#$%^&*()_~+=\\[{\\]};:<>|./?,-]", RegexOptions.Compiled);

        public PasswordChangeWindow(bool force = false)
        {
            InitializeComponent();
            DataContext = m_DataContext;

            var repo = (RuleData)CGlobal.Handler.Repo["Rule"];
            repo.Load();

            m_DataContext.Force = force;

            m_DataContext.PasswordMinLength = repo.GetRuleValueInt("password_min_length");
            m_DataContext.PasswordMaxLength = repo.GetRuleValueInt("password_max_length");
            m_DataContext.PasswordReqLowerCase = repo.GetRuleValueBool("password_req_lowercase");
            m_DataContext.PasswordReqUpperCase = repo.GetRuleValueBool("password_req_uppercase");
            m_DataContext.PasswordReqNumeric = repo.GetRuleValueBool("password_req_numeric");
            m_DataContext.PasswordReqSpecial = repo.GetRuleValueBool("password_req_special");

            m_DataContext.PasswordMinLengthLabel = string.Format("{0} {1} {2}", 
                CGlobal.GetResourceValue("l_SecureChangePassword_RequireMinLength_Begin"), 
                m_DataContext.PasswordMinLength, 
                CGlobal.GetResourceValue("l_SecureChangePassword_RequireMinLength_End"));
        }

        string CurrentPassword = "";
        string NewPassword = "";
        string RepeatNewPassword = "";

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!m_DataContext.State)
            {
                return;
            }

            if (!CGlobal.Handler.ChangeUserPassword(CurrentPassword, NewPassword, m_DataContext.Force))
            {
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            CurrentPassword = CurrentPasswordBox.Password;
            NewPassword = NewPasswordBox.Password;
            RepeatNewPassword = RepeatNewPasswordBox.Password;

            VerifyPasswordRule(CurrentPassword, NewPassword, RepeatNewPassword);
        }
        
        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CurrentPassword = CurrentPasswordTextBox.Text;
            NewPassword = NewPasswordTextBox.Text;
            RepeatNewPassword = RepeatNewPasswordTextBox.Text;

            VerifyPasswordRule(CurrentPassword, NewPassword, RepeatNewPassword);
        }

        private void VerifyPasswordRule(string password, string new_password, string repeat_new_password)
        {
            m_DataContext.RuleMinLengthState = new_password.Length >= m_DataContext.PasswordMinLength;
            m_DataContext.RuleMaxLengthState = new_password.Length <= m_DataContext.PasswordMaxLength;
            m_DataContext.RuleLowerCaseState = !m_DataContext.PasswordReqLowerCase || m_RegexLowerChar.IsMatch(new_password);
            m_DataContext.RuleUpperCaseState = !m_DataContext.PasswordReqUpperCase || m_RegexUpperChar.IsMatch(new_password);
            m_DataContext.RuleNumericState = !m_DataContext.PasswordReqNumeric || m_RegexNumber.IsMatch(new_password);
            m_DataContext.RuleSpecialState = !m_DataContext.PasswordReqSpecial || m_RegexSymbol.IsMatch(new_password);
            m_DataContext.RuleEqualState = (new_password.Length > 0) && (new_password == repeat_new_password);
            m_DataContext.RuleFreshState = (new_password.Length > 0) && (new_password != password);

            m_DataContext.State = password.Length > 0
                && m_DataContext.RuleMinLengthState 
                && m_DataContext.RuleMaxLengthState 
                && m_DataContext.RuleLowerCaseState 
                && m_DataContext.RuleUpperCaseState 
                && m_DataContext.RuleNumericState 
                && m_DataContext.RuleSpecialState 
                && m_DataContext.RuleEqualState
                && m_DataContext.RuleFreshState;
        }

        private void PasswordRevealCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CurrentPasswordTextBox.Text = CurrentPasswordBox.Password;
            CurrentPasswordBox.Visibility = Visibility.Collapsed;
            CurrentPasswordTextBox.Visibility = Visibility.Visible;

            NewPasswordTextBox.Text = NewPasswordBox.Password;
            NewPasswordBox.Visibility = Visibility.Collapsed;
            NewPasswordTextBox.Visibility = Visibility.Visible;

            RepeatNewPasswordTextBox.Text = RepeatNewPasswordBox.Password;
            RepeatNewPasswordBox.Visibility = Visibility.Collapsed;
            RepeatNewPasswordTextBox.Visibility = Visibility.Visible;
        }

        private void PasswordRevealCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CurrentPasswordBox.Password = CurrentPasswordTextBox.Text;
            CurrentPasswordTextBox.Visibility = Visibility.Collapsed;
            CurrentPasswordBox.Visibility = Visibility.Visible;

            NewPasswordBox.Password = NewPasswordTextBox.Text;
            NewPasswordTextBox.Visibility = Visibility.Collapsed;
            NewPasswordBox.Visibility = Visibility.Visible;

            RepeatNewPasswordBox.Password = RepeatNewPasswordTextBox.Text;
            RepeatNewPasswordTextBox.Visibility = Visibility.Collapsed;
            RepeatNewPasswordBox.Visibility = Visibility.Visible;
        }
    }
}
