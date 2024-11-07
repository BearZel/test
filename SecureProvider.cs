using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace AbakConfigurator.Secure
{
    public enum CreateUserResult
    {
        Success,
        AccountAlreadyExist,
        GroupNotFound,
    }

    public class CreateUserArgs
    {
        public CreateUserArgs(string account, string creator, string password, int group_id) 
            => (Account, Creator, Password, GroupId) = (account, creator, password, group_id);

        public string Account { get; private set; }
        public string Creator { get; private set; }
        public string Password { get; private set; }
        public int GroupId { get; private set; }
        public DateTime? ExpireDate { get; set; }
        public string DetailName { get; set; }
        public string DetailSurname { get; set; }
        public string DetailCompany { get; set; }
        public string DetailDepartment { get; set; }
        public string DetailPosition { get; set; }
        public string DetailEmail { get; set; }
        public string DetailPhone { get; set; }
    }

    public enum DeleteUserResult
    {
        Success,
        AccountNotFound,
    }

    public class DeleteUserArgs
    {
        public DeleteUserArgs(string account)
            => (Account) = (account);

        public string Account { get; private set; }
    }

    public enum ChangeUserPasswordResult
    {
        Success,
        AccountNotFound,
        CurrentPasswordMismatch,
        NewPasswordSameAsCurrent,
        NewPasswordHasBeenUsedBefore
    }

    public class ChangeUserPasswordArgs
    {
        public ChangeUserPasswordArgs(string account, string changer, string current_password, string new_password)
            => (Account, Changer, CurrentPassword, NewPassword) = (account, changer, current_password, new_password);

        public string Account { get; private set; }
        public string Changer { get; private set; }
        public string CurrentPassword { get; private set; }
        public string NewPassword { get; private set; }
    }

    public enum ChangeUserInfoResult
    {
        Success,
        AccountNotFound,
        GroupNotFound,
    }

    public class ChangeUserInfoArgs
    {
        public ChangeUserInfoArgs(string account, string changer, int group_id, DateTime? expire_date, bool banned, string detail_name, string detail_surname, string detail_company, string detail_department, string detail_position, string detail_email, string detail_phone)
            => (Account, Changer, GroupId, ExpireDate, Banned, DetailName, DetailSurname, DetailCompany, DetailDepartment, DetailPosition, DetailEmail, DetailPhone)
            = (account, changer, group_id, expire_date, banned, detail_name, detail_surname, detail_company, detail_department, detail_position, detail_email, detail_phone);

        public string Account { get; private set; }
        public string Changer { get; private set; }
        public int GroupId { get; private set; }
        public DateTime? ExpireDate { get; private set; }
        public bool Banned { get; private set; }
        public string DetailName { get; private set; }
        public string DetailSurname { get; private set; }
        public string DetailCompany { get; private set; }
        public string DetailDepartment { get; private set; }
        public string DetailPosition { get; private set; }
        public string DetailEmail { get; private set; }
        public string DetailPhone { get; private set; }
    }

    public enum UpdateUserStateResult
    {
        Success,
        AccountNotFound,
    }

    public class UpdateUserStateArgs
    {
        public UpdateUserStateArgs(string account)
            => (Account) = (account);

        public string Account { get; private set; }
    }

    public enum UserLoginResult
    {
        Success,
        AccountNotFound,
        PasswordMismatch,
        AccountAlreadyConnected,
        AccountExpired,
        AccountBanned,
        ForcedPasswordChange,
        GroupNotFound,
    }

    public class UserLoginArgs
    {
        public UserLoginArgs(string account, string password, string ip)
            => (Account, Password, IP) = (account, password, ip);

        public string Account { get; private set; }
        public string Password { get; private set; }
        public string IP { get; private set; }
    }

    public enum UserLogoutResult
    {
        Success,
        AccountNotFound,
        AccountAlreadyDisconnected,
    }

    public class UserLogoutArgs
    {
        public UserLogoutArgs(string account)
            => (Account) = (account);

        public string Account { get; private set; }
    }

    public class UserLogArgs
    {
        public UserLogArgs(string account, string ip, int type, string text)
            => (Account, IP, Type, Text) = (account, ip, type, text);

        public string Account { get; private set; }
        public string IP { get; private set; }
        public int Type { get; private set; }
        public string Text { get; private set; }
    }

    public enum CreateUserGroupResult
    {
        Success,
        GroupAlreadyExist,
    }

    public class CreateUserGroupArgs
    {
        public CreateUserGroupArgs(string name, string creator, string description, int type, short[] policy)
            => (Name, Creator, Description, Type, Policy) = (name, creator, description, type, policy);

        public string Name { get; private set; }
        public string Creator { get; private set; }
        public string Description { get; private set; }
        public int Type { get; private set; }
        public short[] Policy { get; private set; }
    }

    public enum DeleteUserGroupResult
    {
        Success,
        GroupNotFound,
    }

    public class DeleteUserGroupArgs
    {
        public DeleteUserGroupArgs(string name)
            => (Name) = (name);

        public string Name { get; private set; }
    }

    public enum ChangeUserGroupResult
    {
        Success,
        GroupNotFound,
    }

    public class ChangeUserGroupArgs
    {
        public ChangeUserGroupArgs(string name, string changer, string description, int type, short[] policy)
            => (Name, Changer, Description, Type, Policy) = (name, changer, description, type, policy);

        public string Name { get; private set; }
        public string Changer { get; private set; }
        public string Description { get; private set; }
        public int Type { get; private set; }
        public short[] Policy { get; private set; }
    }

    public class GetUserGroupTypeArgs
    {
        public GetUserGroupTypeArgs(string account)
            => (Account) = (account);

        public string Account { get; private set; }
    }

    public class SecureProvider
    {
        public SecureProvider()
        {

        }

        public CreateUserResult CreateUser(CreateUserArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_create_user(@account, @creator, @password, @group_id, @expire_date, @detail_name, @detail_surname, @detail_company, @detail_department, @detail_position, @detail_email, @detail_phone)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);
            command.Parameters.AddWithValue("creator", NpgsqlDbType.Text, args.Creator);
            command.Parameters.AddWithValue("password", NpgsqlDbType.Text, args.Password);
            command.Parameters.AddWithValue("group_id", NpgsqlDbType.Integer, args.GroupId);
            command.Parameters.AddWithValue("expire_date", NpgsqlDbType.Date, args.ExpireDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_name", NpgsqlDbType.Text, args.DetailName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_surname", NpgsqlDbType.Text, args.DetailSurname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_company", NpgsqlDbType.Text, args.DetailCompany ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_department", NpgsqlDbType.Text, args.DetailDepartment ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_position", NpgsqlDbType.Text, args.DetailPosition ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_email", NpgsqlDbType.Text, args.DetailEmail ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_phone", NpgsqlDbType.Text, args.DetailPhone ?? (object)DBNull.Value);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (CreateUserResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public DeleteUserResult DeleteUser(DeleteUserArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_delete_user(@account)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (DeleteUserResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public ChangeUserPasswordResult ChangeUserPassword(ChangeUserPasswordArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_change_user_password(@account, @changer, @current_password, @new_password)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);
            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);
            command.Parameters.AddWithValue("changer", NpgsqlDbType.Text, args.Changer);
            command.Parameters.AddWithValue("current_password", NpgsqlDbType.Text, args.CurrentPassword);
            command.Parameters.AddWithValue("new_password", NpgsqlDbType.Text, args.NewPassword);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            int val = reader.GetInt32(0);
            var result = (ChangeUserPasswordResult)val;
            reader.Close();

            return result;
        }

        public ChangeUserInfoResult ChangeUserInfo(ChangeUserInfoArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_change_user_info(@account, @changer, @group_id, @expire_date, @banned, @detail_name, @detail_surname, @detail_company, @detail_department, @detail_position, @detail_email, @detail_phone)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);
            
            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);
            command.Parameters.AddWithValue("changer", NpgsqlDbType.Text, args.Changer);
            command.Parameters.AddWithValue("group_id", NpgsqlDbType.Integer, args.GroupId);
            command.Parameters.AddWithValue("expire_date", NpgsqlDbType.Date, args.ExpireDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("banned", NpgsqlDbType.Boolean, args.Banned);
            command.Parameters.AddWithValue("detail_name", NpgsqlDbType.Text, args.DetailName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_surname", NpgsqlDbType.Text, args.DetailSurname ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_company", NpgsqlDbType.Text, args.DetailCompany ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_department", NpgsqlDbType.Text, args.DetailDepartment ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_position", NpgsqlDbType.Text, args.DetailPosition ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_email", NpgsqlDbType.Text, args.DetailEmail ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("detail_phone", NpgsqlDbType.Text, args.DetailPhone ?? (object)DBNull.Value);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (ChangeUserInfoResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public UserLoginResult UserLogin(UserLoginArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_user_login(@account, @password, @ip)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);
            command.Parameters.AddWithValue("password", NpgsqlDbType.Text, args.Password);
            command.Parameters.AddWithValue("ip", NpgsqlDbType.Text, args.IP);

            var reader = command.ExecuteReader();
             
            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (UserLoginResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public UserLogoutResult UserLogout(UserLogoutArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed ||
                CGlobal.CurrState.IsConnected == false || CGlobal.Handler.DBConnection.FullState == ConnectionState.Broken)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_user_logout(@account)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (UserLogoutResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public void UserLog(UserLogArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_user_log(@account, @ip, @type, @text)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);
            command.Parameters.AddWithValue("ip", NpgsqlDbType.Text, args.IP);
            command.Parameters.AddWithValue("type", NpgsqlDbType.Integer, args.Type);
            command.Parameters.AddWithValue("text", NpgsqlDbType.Text, args.Text);

            command.ExecuteNonQuery();
        }

        public CreateUserGroupResult CreateUserGroup(CreateUserGroupArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_create_user_group(@name, @creator, @description, @type, @policy)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("name", NpgsqlDbType.Text, args.Name);
            command.Parameters.AddWithValue("creator", NpgsqlDbType.Text, args.Creator);
            command.Parameters.AddWithValue("description", NpgsqlDbType.Text, args.Description);
            command.Parameters.AddWithValue("type", NpgsqlDbType.Integer, args.Type);
            command.Parameters.AddWithValue("policy", NpgsqlDbType.Smallint | NpgsqlDbType.Array, args.Policy);
            
            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (CreateUserGroupResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public DeleteUserGroupResult DeleteUserGroup(DeleteUserGroupArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_delete_user_group(@name)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("name", NpgsqlDbType.Text, args.Name);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (DeleteUserGroupResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public ChangeUserGroupResult ChangeUserGroup(ChangeUserGroupArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_change_user_group(@name, @changer, @description, @type, @policy)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("name", NpgsqlDbType.Text, args.Name);
            command.Parameters.AddWithValue("changer", NpgsqlDbType.Text, args.Changer);
            command.Parameters.AddWithValue("description", NpgsqlDbType.Text, args.Description);
            command.Parameters.AddWithValue("type", NpgsqlDbType.Integer, args.Type);
            command.Parameters.AddWithValue("policy", NpgsqlDbType.Smallint | NpgsqlDbType.Array, args.Policy);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = (ChangeUserGroupResult)reader.GetInt32(0);
            reader.Close();

            return result;
        }

        public GroupTypeEnum GetUserGroupType(GetUserGroupTypeArgs args)
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_get_user_group_type(@account)";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            command.Parameters.AddWithValue("account", NpgsqlDbType.Text, args.Account);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            var result = GroupTypeEnum.None;

            if (reader.Read() && !reader.IsDBNull(0))
            {
                result = (GroupTypeEnum)reader.GetInt32(0);
            }

            reader.Close();

            return result;
        }

        public int GetConnectedUserCount()
        {
            if (CGlobal.Handler.DBConnection == null || CGlobal.Handler.DBConnection.State == System.Data.ConnectionState.Closed)
            {
                throw new SecureProviderException("Invalid connection");
            }

            const string query = "SELECT sec_get_connected_user_count()";
            var command = new NpgsqlCommand(query, CGlobal.Handler.DBConnection);

            var reader = command.ExecuteReader();

            if (!reader.HasRows)
            {
                reader.Close();
                throw new SecureProviderException("Invalid query result");
            }

            reader.Read();
            var result = reader.GetInt32(0);
            reader.Close();

            return result;
        }
    }
}
