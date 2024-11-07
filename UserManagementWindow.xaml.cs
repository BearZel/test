using AbakConfigurator.Secure;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AbakConfigurator
{
    public class GroupTypeInfo : INotifyPropertyChanged
    {
        string m_Name = "";
        GroupTypeEnum m_Type = GroupTypeEnum.Developer;

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
            }
        }

        public GroupTypeEnum Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                OnPropertyChanged("Type");
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

    public class GroupInfo : INotifyPropertyChanged
    {
        int m_Id = 0;
        string m_Name = "";
        string m_Description = "";
        GroupTypeEnum m_Type = GroupTypeEnum.Developer;
        int m_CreatorId = 0;
        string m_Creator = null;
        DateTime? m_CreateDate = null;
        int m_ChangerId = 0;
        string m_Changer = null;
        DateTime? m_ChangeDate = null;

        public int Id
        {
            get => m_Id;
            set
            {
                m_Id = value;
                OnPropertyChanged("Id");
            }
        }

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
            }
        }

        public string Description
        {
            get => m_Description;
            set
            {
                m_Description = value;
                OnPropertyChanged("Description");
            }
        }

        public GroupTypeEnum Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                OnPropertyChanged("Type");
            }
        }

        public int CreatorId
        {
            get => m_CreatorId;
            set
            {
                m_CreatorId = value;
                OnPropertyChanged("CreatorId");
            }
        }

        public string Creator
        {
            get => m_Creator;
            set
            {
                m_Creator = value;
                OnPropertyChanged("Creator");
            }
        }

        public DateTime? CreateDate
        {
            get => m_CreateDate;
            set
            {
                m_CreateDate = value;
                OnPropertyChanged("CreateDate");
            }
        }

        public int ChangerId
        {
            get => m_ChangerId;
            set
            {
                m_ChangerId = value;
                OnPropertyChanged("ChangerId");
            }
        }

        public string Changer
        {
            get => m_Changer;
            set
            {
                m_Changer = value;
                OnPropertyChanged("Changer");
            }
        }

        public DateTime? ChangeDate
        {
            get => m_ChangeDate;
            set
            {
                m_ChangeDate = value;
                OnPropertyChanged("ChangeDate");
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

    public class UserInfo : INotifyPropertyChanged
    {
        int m_Id = 0;
        string m_Account = "";
        string m_Password = "";
        int m_GroupId = 0;
        string m_Group = "";
        GroupTypeEnum m_GroupType = GroupTypeEnum.Developer;
        int m_CreatorId = 0;
        string m_Creator = null;
        DateTime? m_CreateDate = null;
        int m_ChangerId = 0;
        string m_Changer = null;
        DateTime? m_ChangeDate = null;
        bool m_Connected = false;
        DateTime? m_ConnectDate = null;
        string m_ConnectIP = null;
        DateTime? m_ExpireDate = null;
        bool m_Banned = false;
        string m_Name = null;
        string m_Surname = null;
        string m_Company = null;
        string m_Department = null;
        string m_Position = null;
        string m_Email = null;
        string m_Phone = null;

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

        public string Password
        {
            get => m_Password;
            set
            {
                m_Password = value;
                OnPropertyChanged("Password");
            }
        }

        public int GroupId
        {
            get => m_GroupId;
            set
            {
                m_GroupId = value;
                OnPropertyChanged("GroupId");
            }
        }

        public string Group
        {
            get => m_Group;
            set
            {
                m_Group = value;
                OnPropertyChanged("Group");
            }
        }

        public GroupTypeEnum GroupType
        {
            get => m_GroupType;
            set
            {
                m_GroupType = value;
                OnPropertyChanged("GroupType");
            }
        }

        public int CreatorId
        {
            get => m_CreatorId;
            set
            {
                m_CreatorId = value;
                OnPropertyChanged("CreatorId");
            }
        }

        public string Creator
        {
            get => m_Creator;
            set
            {
                m_Creator = value;
                OnPropertyChanged("Creator");
            }
        }

        public DateTime? CreateDate
        {
            get => m_CreateDate;
            set
            {
                m_CreateDate = value;
                OnPropertyChanged("CreateDate");
            }
        }

        public int ChangerId
        {
            get => m_ChangerId;
            set
            {
                m_ChangerId = value;
                OnPropertyChanged("ChangerId");
            }
        }

        public string Changer
        {
            get => m_Changer;
            set
            {
                m_Changer = value;
                OnPropertyChanged("Changer");
            }
        }

        public DateTime? ChangeDate
        {
            get => m_ChangeDate;
            set
            {
                m_ChangeDate = value;
                OnPropertyChanged("ChangeDate");
            }
        }

        public bool Connected
        {
            get => m_Connected;
            set
            {
                m_Connected = value;
                OnPropertyChanged("Connected");
            }
        }

        public DateTime? ConnectDate
        {
            get => m_ConnectDate;
            set
            {
                m_ConnectDate = value;
                OnPropertyChanged("ConnectDate");
            }
        }

        public string ConnectIP
        {
            get => m_ConnectIP;
            set
            {
                m_ConnectIP = value;
                OnPropertyChanged("ConnectIP");
            }
        }

        public DateTime? ExpireDate
        {
            get => m_ExpireDate;
            set
            {
                m_ExpireDate = value;
                OnPropertyChanged("ExpireDate");
            }
        }

        public bool Banned
        {
            get => m_Banned;
            set
            {
                m_Banned = value;
                OnPropertyChanged("Banned");
            }
        }

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                OnPropertyChanged("Name");
            }
        }

        public string Surname
        {
            get => m_Surname;
            set
            {
                m_Surname = value;
                OnPropertyChanged("Surname");
            }
        }

        public string Company
        {
            get => m_Company;
            set
            {
                m_Company = value;
                OnPropertyChanged("Company");
            }
        }

        public string Department
        {
            get => m_Department;
            set
            {
                m_Department = value;
                OnPropertyChanged("Department");
            }
        }

        public string Position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                OnPropertyChanged("Position");
            }
        }

        public string Email
        {
            get => m_Email;
            set
            {
                m_Email = value;
                OnPropertyChanged("Email");
            }
        }

        public string Phone
        {
            get => m_Phone;
            set
            {
                m_Phone = value;
                OnPropertyChanged("Phone");
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

    public partial class UserManagementWindow : Window
    {
        public ObservableCollection<UserInfo> UsersList { get; private set; } = new ObservableCollection<UserInfo>();

        public UserManagementWindow()
        {
            InitializeComponent();
            DataContext = this;

            FillUsers();
        }

        private void FillUsers()
        {
            UsersList.Clear();

            var command = new NpgsqlCommand("SELECT * FROM sec_users ORDER BY id", CGlobal.Handler.DBConnection);
            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            while (reader.Read())
            {
                var user_info = new UserInfo()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Account = reader.GetString(reader.GetOrdinal("account")),
                    GroupId = reader.GetInt32(reader.GetOrdinal("group_id")),
                    CreatorId = reader.IsDBNull(reader.GetOrdinal("creator_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("creator_id")),
                    ChangerId = reader.IsDBNull(reader.GetOrdinal("changer_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("changer_id")),
                    Connected = reader.IsDBNull(reader.GetOrdinal("connected")) ? false : reader.GetBoolean(reader.GetOrdinal("connected")),
                    ConnectIP = reader.IsDBNull(reader.GetOrdinal("connect_ip")) ? "" : reader.GetString(reader.GetOrdinal("connect_ip")),
                    Banned = reader.IsDBNull(reader.GetOrdinal("banned")) ? false : reader.GetBoolean(reader.GetOrdinal("banned")),
                    Name = reader.IsDBNull(reader.GetOrdinal("detail_name")) ? "" : reader.GetString(reader.GetOrdinal("detail_name")),
                    Surname = reader.IsDBNull(reader.GetOrdinal("detail_surname")) ? "" : reader.GetString(reader.GetOrdinal("detail_surname")),
                    Company = reader.IsDBNull(reader.GetOrdinal("detail_company")) ? "" : reader.GetString(reader.GetOrdinal("detail_company")),
                    Department = reader.IsDBNull(reader.GetOrdinal("detail_department")) ? "" : reader.GetString(reader.GetOrdinal("detail_department")),
                    Position = reader.IsDBNull(reader.GetOrdinal("detail_position")) ? "" : reader.GetString(reader.GetOrdinal("detail_position")),
                    Email = reader.IsDBNull(reader.GetOrdinal("detail_email")) ? "" : reader.GetString(reader.GetOrdinal("detail_email")),
                    Phone = reader.IsDBNull(reader.GetOrdinal("detail_phone")) ? "" : reader.GetString(reader.GetOrdinal("detail_phone"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("create_date")))
                {
                    user_info.CreateDate = reader.GetDateTime(reader.GetOrdinal("create_date"));
                }
                if (!reader.IsDBNull(reader.GetOrdinal("change_date")))
                {
                    user_info.ChangeDate = reader.GetDateTime(reader.GetOrdinal("change_date"));
                }
                if (!reader.IsDBNull(reader.GetOrdinal("connect_date")))
                {
                    user_info.ConnectDate = reader.GetDateTime(reader.GetOrdinal("connect_date"));
                }
                if (!reader.IsDBNull(reader.GetOrdinal("expire_date")))
                {
                    user_info.ExpireDate = reader.GetDateTime(reader.GetOrdinal("expire_date"));
                }

                UsersList.Add(user_info);
            }

            reader.Close();

            foreach (var user in UsersList)
            {
                // details of creator

                command = new NpgsqlCommand("SELECT account FROM sec_users WHERE id = @id", CGlobal.Handler.DBConnection);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Integer, user.CreatorId);
                reader = command.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    user.Creator = reader.IsDBNull(reader.GetOrdinal("account")) ? "" : reader.GetString(reader.GetOrdinal("account"));
                }

                reader.Close();

                // details of changer

                command = new NpgsqlCommand("SELECT account FROM sec_users WHERE id = @id", CGlobal.Handler.DBConnection);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Integer, user.ChangerId);
                reader = command.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    user.Changer = reader.IsDBNull(reader.GetOrdinal("account")) ? "" : reader.GetString(reader.GetOrdinal("account"));
                }

                reader.Close();

                // details of group

                command = new NpgsqlCommand("SELECT name, type FROM sec_user_groups WHERE id = @id", CGlobal.Handler.DBConnection);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Integer, user.GroupId);
                reader = command.ExecuteReader();

                if (reader.HasRows && reader.Read() && !reader.IsDBNull(reader.GetOrdinal("name")))
                {
                    user.Group = reader.GetString(reader.GetOrdinal("name"));
                    user.GroupType = (GroupTypeEnum)reader.GetInt32(reader.GetOrdinal("type"));
                }
                else
                {
                    user.Group = "";
                    user.GroupType = GroupTypeEnum.None;
                }

                reader.Close();
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var user_create_window = new UserCreateWindow() { Owner = Owner };
            if (user_create_window.ShowDialog() == true)
            {
                FillUsers();
            }
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListView.SelectedIndex < 0)
            {
                return;
            }

            var user_change_window = new UserCreateWindow(UsersList[UsersListView.SelectedIndex]) { Owner = Owner };
            if (user_change_window.ShowDialog() == true)
            {
                FillUsers();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListView.SelectedIndex < 0)
            {
                return;
            }

            string title = CGlobal.GetResourceValue("l_SecureUserDelete_Title");
            string text = CGlobal.GetResourceValue("l_SecureUserDelete_Text");

            var confirm_result = MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm_result != MessageBoxResult.Yes)
            {
                return;
            }

            var args = new DeleteUserArgs(UsersList[UsersListView.SelectedIndex].Account);
            var result = CGlobal.Handler.Provider.DeleteUser(args);

            text = CGlobal.GetResourceValue("l_SecureUserDelete_Success");

            if (result != DeleteUserResult.Success)
            {
                CGlobal.Handler.UserLog(1, string.Format("User {0} delete failure ({1})", args.Account, result.ToString()));

                switch (result)
                {
                    case DeleteUserResult.AccountNotFound:
                        text = CGlobal.GetResourceValue("l_SecureUserDelete_AccountNotFound");
                        break;
                }

                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CGlobal.Handler.UserLog(1, string.Format("User {0} delete success", args.Account));
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            FillUsers();
        }

        private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changeable = UsersListView.SelectedIndex >= 0;
            bool deletable = changeable && (CGlobal.Handler.Auth.GroupType == GroupTypeEnum.Developer || (UsersList[UsersListView.SelectedIndex].GroupType != GroupTypeEnum.Developer && UsersList[UsersListView.SelectedIndex].GroupType != GroupTypeEnum.Administrator));
            ChangeButton.IsEnabled = deletable;
            DeleteButton.IsEnabled = deletable;
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
