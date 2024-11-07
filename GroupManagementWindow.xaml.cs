using AbakConfigurator.Secure;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public partial class GroupManagementWindow : Window
    {
        public ObservableCollection<GroupInfo> GroupsList { get; private set; } = new ObservableCollection<GroupInfo>();

        SecureHandler m_Handler = null;
        SecureProvider m_Provider = null;
        SecureAuth m_Auth = null;

        public GroupManagementWindow()
        {
            InitializeComponent();
            DataContext = this;

            m_Handler = CGlobal.Handler;
            m_Provider = m_Handler.Provider;
            m_Auth = m_Handler.Auth;

            FillGroups();
        }

        private void FillGroups()
        {
            GroupsList.Clear();

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
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                    Type = (GroupTypeEnum)reader.GetInt32(reader.GetOrdinal("type")),
                    CreatorId = reader.IsDBNull(reader.GetOrdinal("creator_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("creator_id")),
                    ChangerId = reader.IsDBNull(reader.GetOrdinal("changer_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("changer_id"))
                };

                if (!reader.IsDBNull(reader.GetOrdinal("create_date")))
                {
                    group_info.CreateDate = reader.GetDateTime(reader.GetOrdinal("create_date"));
                }
                if (!reader.IsDBNull(reader.GetOrdinal("change_date")))
                {
                    group_info.ChangeDate = reader.GetDateTime(reader.GetOrdinal("change_date"));
                }

                GroupsList.Add(group_info);
            }

            reader.Close();

            foreach (var group in GroupsList)
            {
                // details of creator

                command = new NpgsqlCommand("SELECT account FROM sec_users WHERE id = @id", CGlobal.Handler.DBConnection);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Integer, group.CreatorId);
                reader = command.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    group.Creator = reader.IsDBNull(reader.GetOrdinal("account")) ? "" : reader.GetString(reader.GetOrdinal("account"));
                }

                reader.Close();

                // details of changer

                command = new NpgsqlCommand("SELECT account FROM sec_users WHERE id = @id", CGlobal.Handler.DBConnection);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Integer, group.ChangerId);
                reader = command.ExecuteReader();

                if (reader.HasRows && reader.Read())
                {
                    group.Changer = reader.IsDBNull(reader.GetOrdinal("account")) ? "" : reader.GetString(reader.GetOrdinal("account"));
                }

                reader.Close();
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            var group_create_window = new GroupCreateWindow() { Owner = Owner };
            if (group_create_window.ShowDialog() == true)
            {
                FillGroups();
            }
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (GroupsListView.SelectedIndex < 0)
            {
                return;
            }

            var group_change_window = new GroupCreateWindow(GroupsList[GroupsListView.SelectedIndex]) { Owner = Owner };
            if (group_change_window.ShowDialog() == true)
            {
                FillGroups();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (GroupsListView.SelectedIndex < 0)
            {
                return;
            }

            if (m_Auth.GroupType != GroupTypeEnum.Developer && (GroupsList[GroupsListView.SelectedIndex].Type == GroupTypeEnum.Developer || GroupsList[GroupsListView.SelectedIndex].Type == GroupTypeEnum.Administrator))
            {
                MessageBox.Show(CGlobal.GetResourceValue("l_SecureGroupDelete_GroupDefault"), CGlobal.GetResourceValue("l_SecureGroupDelete_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string title = CGlobal.GetResourceValue("l_SecureGroupDelete_Title");
            string text = CGlobal.GetResourceValue("l_SecureGroupDelete_Text");

            var confirm_result = MessageBox.Show(text, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm_result != MessageBoxResult.Yes)
            {
                return;
            }

            var args = new DeleteUserGroupArgs(GroupsList[GroupsListView.SelectedIndex].Name);
            var result = m_Provider.DeleteUserGroup(args);

            text = CGlobal.GetResourceValue("l_SecureGroupDelete_Success");

            if (result != DeleteUserGroupResult.Success)
            {
                m_Handler.UserLog(1, string.Format("Group {0} delete failure ({1})", args.Name, result.ToString()));

                switch (result)
                {
                    case DeleteUserGroupResult.GroupNotFound:
                        text = CGlobal.GetResourceValue("l_SecureGroupDelete_GroupNotFound");
                        break;
                }

                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            m_Handler.UserLog(1, string.Format("Group {0} delete success", args.Name));
            MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            FillGroups();
        }

        private void GroupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool changeable = GroupsListView.SelectedIndex >= 0;
            bool deletable = changeable && (m_Auth.GroupType == GroupTypeEnum.Developer || (GroupsList[GroupsListView.SelectedIndex].Type != GroupTypeEnum.Developer && GroupsList[GroupsListView.SelectedIndex].Type != GroupTypeEnum.Administrator));
            ChangeButton.IsEnabled = changeable;
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
