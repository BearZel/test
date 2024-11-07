using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Runtime.InteropServices.ComTypes;
using AbakConfigurator.IEC;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows;

namespace AbakConfigurator.IEC
{
    public enum GroupID
    {
        CS101_INTERROGATED_DEFAULT = 20,
        CS101_INTERROGATED_1 = 21,
        CS101_INTERROGATED_2 = 22,
        CS101_INTERROGATED_3 = 23,
        CS101_INTERROGATED_4 = 24,
        CS101_INTERROGATED_5 = 25,
        CS101_INTERROGATED_6 = 26,
        CS101_INTERROGATED_7 = 27,
        CS101_INTERROGATED_8 = 28,
        CS101_INTERROGATED_9 = 29,
        CS101_INTERROGATED_10 = 30,
        CS101_INTERROGATED_11 = 31,
        CS101_INTERROGATED_12 = 32,
        CS101_INTERROGATED_13 = 33,
        CS101_INTERROGATED_14 = 34,
        CS101_INTERROGATED_15 = 35,
        CS101_INTERROGATED_16 = 36
    }

    public enum DataType
    {
        M_SP_NA_1 = 1,
        M_SP_TA_1 = 2,
        M_DP_NA_1 = 3,
        M_DP_TA_1 = 4,
        M_ST_NA_1 = 5,
        M_ST_TA_1 = 6,
        M_BO_NA_1 = 7,
        M_BO_TA_1 = 8,
        M_ME_NA_1 = 9,
        M_ME_TA_1 = 10,
        M_ME_NB_1 = 11,
        M_ME_TB_1 = 12,
        M_ME_NC_1 = 13,
        M_ME_TC_1 = 14,
        M_IT_NA_1 = 15,
        M_IT_TA_1 = 16,
        //M_EP_TA_1 = 17,
        // В будущем если понадобятся
        //M_EP_TB_1 = 18,
        //M_EP_TC_1 = 19,
        //M_PS_NA_1 = 20,
        M_ME_ND_1 = 21,

        M_SP_TB_1 = 30,
        M_DP_TB_1 = 31,
        M_ST_TB_1 = 32,
        M_BO_TB_1 = 33,
        M_ME_TD_1 = 34,
        M_ME_TE_1 = 35,
        M_ME_TF_1 = 36,
        M_IT_TB_1 = 37,
        // В будущем если понадобятся
        //M_EP_TD_1 = 38,
        //M_EP_TE_1 = 39,
        //M_EP_TF_1 = 40,

        C_SC_NA_1 = 45,
        C_DC_NA_1 = 46,
        C_RC_NA_1 = 47,
        C_SE_NA_1 = 48,
        C_SE_NB_1 = 49,
        C_SE_NC_1 = 50,
        C_BO_NA_1 = 51,
        C_SC_TA_1 = 58,
        C_DC_TA_1 = 59,
        C_RC_TA_1 = 60,
        C_SE_TA_1 = 61,
        C_SE_TB_1 = 62,
        C_SE_TC_1 = 63,
        C_BO_TA_1 = 64,

        M_IT_ND_1 = 230,
        M_IT_TD_1 = 231,
        M_ME_NO_1 = 232,
        M_ME_TO_1 = 233,
        M_ME_NX_1 = 234,
        M_ME_TX_1 = 235,
    }

    public class ServerRow
    {
        public int IOA { get; set; } = 101;
        public DataType DataType { get; set; } = DataType.M_SP_NA_1;
        public int PeriodicCycle { get; set; }
        public GroupID GroupID { get; set; } = GroupID.CS101_INTERROGATED_DEFAULT;
        public float Deadband { get; set; }
        public int ModbusAddress { get; set; }
        public int PollPeriod { get; set; } = 500;
    }

    public class ServerRowModel : ModelViewBase, IDisposable
    {
        public ServerRow Row { get; private set; }
        private UsedAddress _ioaAddress;
        private UsedAddress _modBusAddress;

        private UsedAddressProvider _ioaProvider;
        private IDisposable _ioaDisposable;

        private UsedAddressProvider _modbusAddressProvider;
        private IDisposable _modbusAddressDisposable;

        public ServerRowModel(ServerRow row, UsedAddress ioa, UsedAddress modBus)
        {
            Row = row;
            _ioaAddress = ioa;
            _modBusAddress = modBus;

            _ioaProvider = new UsedAddressProvider(OnIoaChanged);
            _ioaDisposable = _ioaAddress.Subscribe(_ioaProvider);

            _modbusAddressProvider = new UsedAddressProvider(OnModbusAddressChanged);
            _modbusAddressDisposable = _modBusAddress.Subscribe(_modbusAddressProvider);

            _ioaAddress.Add(_ioaProvider, row.IOA, 1);
            _modBusAddress.Add(_modbusAddressProvider, row.ModbusAddress, Constants.DataTypeSizeInModbusRegisters(Row.DataType));
        }

        public void Dispose()
        {
            _ioaAddress.Remove(_ioaProvider);
            _ioaDisposable.Dispose();

            _modBusAddress.Remove(_modbusAddressProvider);
            _modbusAddressDisposable.Dispose();
        }

        public int IOA
        {
            get { return Row.IOA; }
            set
            {
                Row.IOA = value;
                _ioaAddress.Replace(_ioaProvider, Row.IOA, 1);
                OnPropertyChanged(nameof(IOA));
            }
        }
        public DataType DataType
        {
            get { return Row.DataType; }
            set
            {
                Row.DataType = value;

                int registers = Constants.DataTypeSizeInModbusRegisters(Row.DataType);
                _modBusAddress.Replace(_modbusAddressProvider, Row.ModbusAddress, registers);

                OnPropertyChanged(nameof(DataType));
                OnPropertyChanged(nameof(DataTypeToolTip));
                OnPropertyChanged(nameof(IsSupportDeadband));
            }
        }
        public int PeriodicCycle
        {
            get { return Row.PeriodicCycle; }
            set { Row.PeriodicCycle = value; OnPropertyChanged(nameof(PeriodicCycle)); }
        }
        public GroupID GroupID
        {
            get { return Row.GroupID; }
            set { Row.GroupID = value; OnPropertyChanged(nameof(GroupID)); }
        }
        public float Deadband
        {
            get { return Row.Deadband; }
            set { Row.Deadband = value; OnPropertyChanged(nameof(Deadband)); }
        }
        public int ModbusAddress
        {
            get { return Row.ModbusAddress; }
            set
            {
                Row.ModbusAddress = value;

                int registers = Constants.DataTypeSizeInModbusRegisters(Row.DataType);
                _modBusAddress.Replace(_modbusAddressProvider, Row.ModbusAddress, registers);

                OnPropertyChanged(nameof(ModbusAddress));
            }
        }
        public int PollPeriod
        {
            get { return Row.PollPeriod; }
            set { Row.PollPeriod = value; OnPropertyChanged(nameof(PollPeriod)); }
        }

        public string DataTypeToolTip
        {
            get
            {
                return Constants.IECDataTypeTooltip(DataType);
            }
        }

        public bool IsSupportDeadband
        {
            get
            {
                return Row.DataType != DataType.M_SP_NA_1 && Row.DataType != DataType.M_DP_NA_1;
            }
        }

        private SolidColorBrush _ioaColor = Brushes.Transparent;
        public SolidColorBrush IOAColor
        {
            get
            {
                return _ioaColor;
            }
            set
            {
                _ioaColor = value;
                OnPropertyChanged(nameof(IOAColor));
            }
        }

        void OnIoaChanged(UsedAddress used)
        {
            bool has = used.Intersect(this, IOA, 1);
            IOAColor = has ? Brushes.Red : Brushes.Transparent;
        }

        private SolidColorBrush _modbusAddressColor = Brushes.Transparent;
        public SolidColorBrush ModbusAddressColor
        {
            get
            {
                return _modbusAddressColor;
            }
            set
            {
                _modbusAddressColor = value;
                OnPropertyChanged(nameof(ModbusAddressColor));
            }
        }

        void OnModbusAddressChanged(UsedAddress used)
        {
            int registers = Constants.DataTypeSizeInModbusRegisters(Row.DataType);
            bool has = used.Intersect(this, ModbusAddress, registers);
            ModbusAddressColor = has ? Brushes.Red : Brushes.Transparent;
        }

        private class UsedAddressProvider : IObserver<UsedAddress>
        {
            Action<UsedAddress> _action;
            public UsedAddressProvider(Action<UsedAddress> action)
            {
                _action = action;
            }
            public virtual void OnCompleted() { }

            public virtual void OnError(Exception error) { }

            public virtual void OnNext(UsedAddress value)
            {
                _action(value);
            }
        }
    }

    public class ServerModel : ModelViewBase
    {
        public ObservableCollection<ServerRowModel> Rows { get; set; }

        private ObservableCollection<ServerRowModel> _selectedItems;
        public ObservableCollection<ServerRowModel> SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
                OnPropertyChanged(nameof(SelectedItems));
            }
        }

        public static Array GroupIDs { get { return Enum.GetValues(typeof(GroupID)); } }
        public static Array IECDataTypes { get { return Enum.GetValues(typeof(DataType)); } }

        static HashSet<DataType> _commands = new HashSet<DataType>()
        {
            DataType.C_SC_NA_1,
            DataType.C_DC_NA_1,
            DataType.C_RC_NA_1,
            DataType.C_SE_NA_1,
            DataType.C_SE_NB_1,
            DataType.C_SE_NC_1,
            DataType.C_BO_NA_1,
            DataType.C_SC_TA_1,
            DataType.C_DC_TA_1,
            DataType.C_RC_TA_1,
            DataType.C_SE_TA_1,
            DataType.C_SE_TB_1,
            DataType.C_SE_TC_1,
            DataType.C_BO_TA_1,
        };

        public static Array CommandDataTypes
        {
            get
            {
                var commands = Array.FindAll((DataType[])IECDataTypes, IsCommandDataType).ToArray();
                Array.Sort(commands, (l, r) => string.Compare(l.ToString(), r.ToString()));
                return commands;
            }
        }
        public static Array DataTypes
        {
            get
            {
                var data = Array.FindAll((DataType[])IECDataTypes, type => !IsCommandDataType(type)).ToArray();
                Array.Sort(data, (l, r) => string.Compare(l.ToString(), r.ToString()));
                return data;
            }
        }

        private UsedAddress _usedModbusAddress;
        private UsedAddress _ioaAddresses;

        public ServerModel(UsedAddress ioaAddresses, UsedAddress modbusAddress)
        {
            _ioaAddresses = ioaAddresses;
            _usedModbusAddress = modbusAddress;
            Rows = new ObservableCollection<ServerRowModel>();
        }

        public void AddRow(ServerRow row)
        {
            Rows.Add(new ServerRowModel(row, _ioaAddresses, _usedModbusAddress));
        }

        public void UpdateRows(List<ServerRow> rows)
        {
            rows.ForEach(row =>
            {
                var model = new ServerRowModel(row, _ioaAddresses, _usedModbusAddress);
                Rows.Add(model);
            });
        }

        public void SetRows(List<ServerRow> rows)
        {
            Clear();
            UpdateRows(rows);
        }

        public void Clear()
        {
            foreach (var row in Rows)
            {
                row.Dispose();
            }

            Rows.Clear();
        }

        public List<ServerRow> GetRows()
        {
            return Rows.Select(row => row.Row).ToList();
        }

        public UsedAddress GetUsedModBusAddress()
        {
            return _usedModbusAddress;
        }

        public static bool IsCommandDataType(DataType type)
        {
            return _commands.Contains(type);
        }
    }
}
