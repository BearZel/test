using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.Secure
{
    public interface ISecureData
    {
        void Init();

        void Load();

        void Save();

        bool Loaded();

        bool Changed();
    }
}
