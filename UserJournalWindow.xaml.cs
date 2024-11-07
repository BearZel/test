using AbakConfigurator.Secure;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace AbakConfigurator
{
    public class UserJournalRow : INotifyPropertyChanged
    {
        int m_Id = 0;
        string m_Account = "";
        string m_IP = "";
        DateTime? m_Date = null;
        int m_Type = 0;
        string m_Text = "";

        public int Id
        {
            get => m_Id;
            set
            {
                m_Id = value;
                OnPropertyChanged("Id");
            }
        }

        public string Account
        {
            get => m_Account;
            set
            {
                m_Account = value;
                OnPropertyChanged("Account");
            }
        }

        public string IP
        {
            get => m_IP;
            set
            {
                m_IP = value;
                OnPropertyChanged("IP");
            }
        }

        public DateTime? Date
        {
            get => m_Date;
            set
            {
                m_Date = value;
                OnPropertyChanged("Date");
            }
        }

        public int Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                OnPropertyChanged("Type");
            }
        }

        public string Text
        {
            get => m_Text;
            set
            {
                m_Text = value;
                OnPropertyChanged("Text");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class UserJournalDataContext
    {
        public ObservableCollection<UserJournalRow> LogsList { get; set; } = new ObservableCollection<UserJournalRow>();
    }

    public partial class UserJournalWindow : Window
    {
        UserJournalDataContext m_DataContext = new UserJournalDataContext();

        bool m_PersonalMode = false;

        public UserJournalWindow(bool personal_mode = false)
        {
            InitializeComponent();
            DataContext = m_DataContext;

            m_PersonalMode = personal_mode;
            if(!m_PersonalMode)
            {
                Title = "Журнал аудита";
            }
            else
            {
                Title = "Журнал действий пользователя";
            }

            FillLogs();
        }

        void FillLogs()
        {
            m_DataContext.LogsList.Clear();

            string query = "SELECT * FROM sec_user_journal ORDER BY id DESC";
            if (m_PersonalMode)
            {
                query = string.Format("SELECT * FROM sec_user_journal WHERE account = '{0}' ORDER BY id DESC", CGlobal.Handler.Auth.Account);
            }

            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);
            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            while (reader.Read())
            {
                var journal_row = new UserJournalRow()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Account = reader.GetString(reader.GetOrdinal("account")),
                    IP = reader.GetString(reader.GetOrdinal("ip")),
                    Type = reader.GetInt32(reader.GetOrdinal("type")),
                    Text = reader.GetString(reader.GetOrdinal("text"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("date")))
                {
                    journal_row.Date = reader.GetDateTime(reader.GetOrdinal("date"));
                }

                m_DataContext.LogsList.Add(journal_row);
            }

            reader.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.SaveFileDialog()
            {
                Filter = "Text (*.txt)|*.txt|All (*.*)|*"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (var stream = new StreamWriter(dialog.FileName))
                {
                    foreach (var log in m_DataContext.LogsList)
                    {
                        stream.WriteLine("{0} [{1}][{2}] ({3}) {4}", log.Date.ToString(), log.Account, log.IP, log.Type, log.Text);
                    }
                }

                MessageBox.Show(CGlobal.GetResourceValue("l_SecureUserJournal_SaveSuccess"), CGlobal.GetResourceValue("l_SecureUserJournal_Title"), MessageBoxButton.OK, MessageBoxImage.Information);

                if (m_PersonalMode)
                {
                    CGlobal.Handler.UserLog(2, string.Format("Download Personal User Journal"));
                }
                else
                {
                    CGlobal.Handler.UserLog(2, string.Format("Download User Journal"));
                }
            }
        }

        private void LogsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
