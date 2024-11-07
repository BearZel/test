using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.Secure
{
    public enum GroupTypeEnum
    {
        Developer = 0,
        Administrator,
        Moderator,
        Spectator,

        None = 100,
    }

    public class SecureAuth
    {
        SecureHandler m_Handler = null;
        SecureProvider m_Provider = null;

        public string Account { get; private set; }
        public GroupTypeEnum GroupType { get; private set; }
        public string IP { get; private set; }
        public bool Authorized { get; private set; }

        public void Reset(string account = "", string ip = "", bool authorized = false)
        {
            Account = account;
            IP = ip;
            Authorized = authorized;

            if (Authorized)
            {
                m_Handler = CGlobal.Handler;
                m_Provider = m_Handler.Provider;

                GroupType = m_Provider.GetUserGroupType(new GetUserGroupTypeArgs(Account));
            }
        }
    }
}
