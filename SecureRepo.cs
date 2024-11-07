using AbakConfigurator.Secure.Data;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.Secure
{
    public class SecureRepo
    {
        private Dictionary<string, ISecureData> Repo = null;

        public SecureRepo()
        {
            Repo = new Dictionary<string, ISecureData>()
            {
                { "Rule", new RuleData() },
                { "IEC", new IECData() }
            };
        }

        public void Init()
        {
            foreach (ISecureData data in Repo.Values)
            {
                data.Init();
            }
        }

        public async void InitAsync() => await Task.Run(() => Init());

        public void Load()
        {
            foreach (ISecureData data in Repo.Values)
            {
                data.Load();
            }
        }

        public async void LoadAsync() => await Task.Run(() => Load());

        public void Save()
        {
            foreach (ISecureData data in Repo.Values)
            {
                if (data.Changed())
                {
                    data.Save();
                }
            }
        }

        public async void SaveAsync() => await Task.Run(() => Save());

        public ISecureData this[string key]
        {
            get {
                if (!Repo.ContainsKey(key))
                {
                    throw new Exception(string.Format("Data '{}' not found", key));
                }

                return Repo[key];
            }
            private set
            {

            }
        }
    }
}
