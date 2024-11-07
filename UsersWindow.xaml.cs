using Npgsql;
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

    /// <summary>
    /// Класс описывающий роль пользователя
    /// </summary>
    public class CUserRole : INotifyPropertyChanged
    {
        //Цифровой код роли
        private UInt32 code;
        //Ключ роли в таблице локализации, за это берётся название параметра в английской локали
        private String nameKey;

        public uint Code { get => code; set => code = value; }

        public string Name
        {
            get
            {
                return CConfig.Localization.GetLocaleValue(this.nameKey);
            }
        }

        public string NameKey { get => nameKey; set => nameKey = value; }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }

    /// <summary>
    /// Класс описывающий пользователя
    /// </summary>
    public class CUserInfo : INotifyPropertyChanged
    {
        //Имя пользователя
        private String userName;
        //Пароль пользователя
        private String password;
        //Роль пользователя
        private UInt32 roleCode;
        //ключ названия роли
        private String roleNameKey;

        public string UserName
        {
            get
            {
                return this.userName;
            }
            set
            {
                if (this.userName == value)
                    return;

                this.userName = value;
                this.OnPropertyChanged("UserName");
            }
        }

        public uint RoleCode
        {
            get
            {
                return this.roleCode;
            }
            set
            {
                if (this.roleCode == value)
                    return;

                this.roleCode = value;
                this.OnPropertyChanged("RoleCode");
            }
        }

        public String RoleName
        {
            get
            {
                return CConfig.Localization.GetLocaleValue(this.roleNameKey); ;
            }
        }

        public String RoleNameKey
        {
            get
            {
                return this.roleNameKey;
            }
            set
            {
                this.roleNameKey = value;
            }
        }

        public String Password
        {
            get
            {
                return this.password;
            }
            set
            {
                if (this.password == value)
                    return;

                this.password = value;
                this.OnPropertyChanged("Password");
            }
        }

        public void Assign(CUserInfo userInfo)
        {
            this.UserName = userInfo.UserName;
            this.Password = userInfo.Password;
            this.RoleNameKey = userInfo.RoleNameKey;
            this.RoleCode = userInfo.RoleCode;
        }

        public void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }

    /// <summary>
    /// Логика взаимодействия для UsersWindow.xaml
    /// </summary>
    public partial class UsersWindow : Window
    {
        //Указатель на экземпляр класса соединения с базой
        private NpgsqlConnection connection;
        //Список ролей 
        private ObservableCollection<CUserRole> rolesList = new ObservableCollection<CUserRole>();
        //Список пользователей
        private ObservableCollection<CUserInfo> usersList = new ObservableCollection<CUserInfo>();

        public UsersWindow(NpgsqlConnection connection)
        {
            InitializeComponent();

            this.connection = connection;
        }


        private bool editUserSettings(CUserInfo userInfo, String header, Boolean addUser)
        {
            while (true)
            {
                EditUserWindow editUserWindow = new EditUserWindow(this.rolesList);
                editUserWindow.Owner = this;

                editUserWindow.UserName = userInfo.UserName;
                editUserWindow.Role = userInfo.RoleCode;
                editUserWindow.Password = userInfo.Password;
                editUserWindow.Password2 = userInfo.Password;
                editUserWindow.EnableAllControls = addUser;

                if (editUserWindow.ShowDialog() == true)
                {
                    if (editUserWindow.Password != editUserWindow.Password2)
                    {
                        MessageBox.Show(CGlobal.GetResourceValue("l_userWindowPasswordsNotEqual"), header, MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    if (editUserWindow.Role == 0)
                    {
                        MessageBox.Show(CGlobal.GetResourceValue("l_userWindowPasswordsNotEqual"), header, MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }

                    userInfo.UserName = editUserWindow.UserName;
                    userInfo.Password = editUserWindow.Password;
                    userInfo.RoleCode = editUserWindow.Role;
                    return true;
                }

                break;
            }

            return false;
        }

        private void AddButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            //Добавление нового пользователя
            CUserInfo userInfo = new CUserInfo();
            if (!this.editUserSettings(userInfo, CGlobal.GetResourceValue("l_userWindowAddUser"), true))
                return;

            try
            {
                String sql = String.Format("select create_user('{0}', '{1}', {2})", userInfo.UserName, userInfo.Password, userInfo.RoleCode);
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();

                this.loadUsers();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_userWindowAddUser"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //Изменение пароля
            if (this.usersGrid.SelectedItem == null)
                return;

            CUserInfo userInfo = this.usersGrid.SelectedItem as CUserInfo;
            if (!this.editUserSettings(userInfo, CGlobal.GetResourceValue("l_userWindowEditUser"), false))
                return;

            try
            {
                String sql = String.Format("select change_user_password('{0}', '{1}')", userInfo.Password, userInfo.UserName);
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();

                this.loadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_userWindowEditUser"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveButtonClick_Handler(object sender, RoutedEventArgs e)
        {
            //Удаление пользователя
            if (this.usersGrid.SelectedItem == null)
                return;

            CUserInfo userInfo = this.usersGrid.SelectedItem as CUserInfo;
            String question = String.Format("{0} '{1}?'", CGlobal.GetResourceValue("l_deleteUserQuestion"), userInfo.UserName);
            if (MessageBox.Show(question, CGlobal.GetResourceValue("l_userWindowDeleteUser"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            
            try
            {
                String sql = String.Format("select delete_user('{0}')", userInfo.UserName);
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                cmd.ExecuteNonQuery();

                this.loadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, CGlobal.GetResourceValue("l_userWindowDeleteUser"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void windowLoaded(object sender, RoutedEventArgs e)
        {
            this.loadData();
        }

        /// <summary>
        /// Подгружает данные о пользователях и ролях
        /// </summary>
        private void loadData()
        {
            this.loadRoles();
            this.loadUsers();
        }

        /// <summary>
        /// Подгрузка списка ролей
        /// </summary>
        private void loadRoles()
        {
            this.rolesList.Clear();

            String sql = CConfig.PreinitSQL();
            sql += "r.code, r.locale from user_roles r inner join localization l on r.locale=l.id";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                while(reader.Read())
                {
                    CUserRole userRole = new CUserRole();
                    userRole.Code = Convert.ToUInt32(reader["code"]);
                    userRole.NameKey = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);

                    this.rolesList.Add(userRole);
                }
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Подгрузка списка пользователей
        /// </summary>
        private void loadUsers()
        {
            this.usersList.Clear();

            String sql = CConfig.PreinitSQL();
            sql += "u.name, u.password, u.role from users u inner join user_roles r on r.code=u.role inner join localization l on l.id=r.locale order by u.name";
            NpgsqlCommand cmd = new NpgsqlCommand(sql, this.connection);
            NpgsqlDataReader reader = cmd.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    CUserInfo userInfo = new CUserInfo();
                    userInfo.UserName = Convert.ToString(reader["name"]);
                    userInfo.Password = Convert.ToString(reader["password"]);
                    userInfo.RoleCode = Convert.ToUInt32(reader["role"]);
                    userInfo.RoleNameKey = CConfig.Localization.GetLocalesFromDataBaseRecord(reader);

                    this.usersList.Add(userInfo);
                }
            }
            finally
            {
                reader.Close();
            }
        }

        //Список ролей 
        public ObservableCollection<CUserRole> RolesList
        {
            get
            {
                return this.rolesList;
            }
        }

        //Список пользователей
        public ObservableCollection<CUserInfo> UsersList
        {
            get
            {
                return this.usersList;
            }
        }

        private void windowClosed(object sender, EventArgs e)
        {

        }
    }
}
