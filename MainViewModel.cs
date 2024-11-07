using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator.IEC
{
    public class MainViewModel : ModelViewBase
    {
        private ServerModel _serverModel;
        private ServerModel _commandsModel;
        private SettingsModel _settingsModel;

        public UsedAddress UsedIOA { get; private set; }

        public MainViewModel()
        {
            UsedIOA = new UsedAddress(101);

            ServerModel = new ServerModel(UsedIOA, new UsedAddress());
            CommandsModel = new ServerModel(UsedIOA, new UsedAddress());
            SettingsModel = new SettingsModel();
        }
        
        public ServerModel ServerModel
        {
            get { return _serverModel; }
            set { _serverModel = value; OnPropertyChanged(nameof(ServerModel)); }
        }
        public ServerModel CommandsModel
        {
            get { return _commandsModel; }
            set { _commandsModel = value; OnPropertyChanged(nameof(CommandsModel)); }
        }
        public SettingsModel SettingsModel
        {
            get { return _settingsModel; }
            set { _settingsModel = value; OnPropertyChanged(nameof(SettingsModel)); }
        }

        public void SetRows(List<ServerRow> rows)
        {
            CommandsModel.Clear();
            ServerModel.Clear();

            foreach (var r in rows)
            {
                if (ServerModel.IsCommandDataType(r.DataType))
                {
                    CommandsModel.AddRow(r);
                }
                else
                {
                    ServerModel.AddRow(r);
                }
            }
        }

        public void UpdateRows(List<ServerRow> rows)
        {
            ServerModel.UpdateRows(rows.FindAll(row => !ServerModel.IsCommandDataType(row.DataType)));
            CommandsModel.UpdateRows(rows.FindAll(row => ServerModel.IsCommandDataType(row.DataType)));
        }

        public void Clear()
        {
            CommandsModel.Clear();
            ServerModel.Clear();
            SettingsModel = new SettingsModel();
        }
    }
}
