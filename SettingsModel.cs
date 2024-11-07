using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.IEC
{
    public class SettingsModel : ModelViewBase
    {
        private Settings _settings;

        public SettingsModel()
        {
            Settings = new Settings();
        }

        public Settings Settings
        {
            get { return _settings; }
            set { _settings = value; OnPropertyChanged(nameof(Settings)); }
        }
    }
}
