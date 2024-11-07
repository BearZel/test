using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace AbakConfigurator.IEC
{
    /// <summary>
    /// Логика взаимодействия для AddRowWindow.xaml
    /// </summary>
    public partial class AddRowWindow : Window
    {
        public ServerModel EditRowModel { get; set; }
        ServerRowModel defaultRowModel;

        private int _numberOfRows = 1;
        public int NumberOfRows { get { return _numberOfRows; } set { _numberOfRows = value; } }

        private UsedAddress _usedIOA;
        private UsedAddress _usedModbusAddresses;
        public AddRowWindow(UsedAddress ioaAddress, UsedAddress modBusAddress, bool isAddCommand)
        {
            InitializeComponent();
            DataContext = this;

            _usedIOA = new UsedAddress(ioaAddress);
            _usedModbusAddresses = new UsedAddress(modBusAddress);
            EditRowModel = new ServerModel(_usedIOA, _usedModbusAddresses);

            DataType dataType = isAddCommand ? DataType.C_SC_NA_1 : DataType.M_SP_NA_1;
            int registers = Constants.DataTypeSizeInModbusRegisters(dataType);
            EditRowModel.AddRow(new ServerRow()
            {
                IOA = _usedIOA.GetFreeAddress(101, 1),
                DataType = dataType,
                ModbusAddress = _usedModbusAddresses.GetFreeAddress(0, registers)
            });
            defaultRowModel = EditRowModel.Rows.First();

            if (isAddCommand)
            {
                GridPollPeriod.Visibility = Visibility.Collapsed;
                GridDeadband.Visibility = Visibility.Collapsed;
                GridPeriodicCycle.Visibility = Visibility.Collapsed;
                GridDataType.ItemsSource = ServerModel.CommandDataTypes;
                Title = "Добавить команду";
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GenerateRows()
        {
            ServerRowModel newRow = EditRowModel.Rows.DefaultIfEmpty(defaultRowModel).First();

            int dataTypeSize = Constants.IECDataTypeSizeInBytes(newRow.DataType);
            int numRegisters = dataTypeSize / Constants.ModbusRegisterSizeInBytes;

            EditRowModel.Clear();
            for (int i = 0; i < NumberOfRows; i++)
            {
                int ioa = _usedIOA.GetFreeAddress(newRow.IOA, 1);
                int modbusAddress = _usedModbusAddresses.GetFreeAddress(newRow.ModbusAddress, numRegisters);
                EditRowModel.AddRow(new ServerRow
                {
                    IOA = ioa,
                    DataType = newRow.DataType,
                    PeriodicCycle = newRow.PeriodicCycle,
                    GroupID = newRow.GroupID,
                    Deadband = newRow.Deadband,
                    ModbusAddress = modbusAddress,
                    PollPeriod = newRow.PollPeriod
                });
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateRows();
        }

        private void NewRowDataGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != Key.Delete || NewRowDataGrid.SelectedItems == null)
            {
                return;
            }

            foreach (var item in NewRowDataGrid.SelectedItems.Cast<ServerRowModel>())
            {
                item.Dispose();
            }
        }
    }
}
