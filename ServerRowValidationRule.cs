using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace AbakConfigurator.IEC
{
    public class ServerRowValidationRule : ValidationRule
    {
        static List<DataType> supportFloatDeadband = new List<DataType>()
        {
            DataType.M_ME_TC_1,
            DataType.M_ME_TF_1,
            DataType.M_IT_ND_1,
            DataType.M_IT_TD_1,
            DataType.M_ME_NA_1,
            DataType.M_ME_TA_1,
            DataType.M_ME_TD_1,
            DataType.M_ME_ND_1
        };

        private bool IsSupportFloatDeadband(DataType dataType)
        {
            return supportFloatDeadband.Contains(dataType);
        }

        HashSet<int> _usedIoa = new HashSet<int>();
        public override ValidationResult Validate(object value,
            System.Globalization.CultureInfo cultureInfo)
        {
            ServerRowModel serverRow = (value as BindingGroup).Items[0] as ServerRowModel;
            if (serverRow.IOA < 101)
            {
                return new ValidationResult(false,
                    "IOA должен иметь значение: 101 - 65535, и не совпадать другими строками.");
            }
            if (serverRow.PeriodicCycle != 0 && (serverRow.PeriodicCycle < 100 || serverRow.PeriodicCycle > 10000))
            {
                return new ValidationResult(false,
                    "Periodic Cycle должен иметь значение: 0 - выключено, 100 - 10000 период в миллисекундах.");
            }

            if (!IsSupportFloatDeadband(serverRow.DataType))
            {
                double diff = Math.Ceiling(serverRow.Deadband) - Math.Floor(serverRow.Deadband);
                if (diff > 0.0)
                {
                    return new ValidationResult(false,
                        "Deadband должен иметь целочисленное значение.");
                }
            }

            if (serverRow.Deadband < 0 || serverRow.Deadband > 10000)
            {
                return new ValidationResult(false,
                    "Deadband должен иметь значение: 0 - выключено, 0.0 - 10000.0 значение мертвой зоны.");
            }
            if (serverRow.ModbusAddress < 0 || serverRow.ModbusAddress > 65535)
            {
                return new ValidationResult(false,
                    "Modbus Address должен иметь значение: 0 - 65535.");
            }
            if (serverRow.PollPeriod < 100 || serverRow.PollPeriod > 10000)
            {
                return new ValidationResult(false,
                    "Poll Period должен иметь значение: 100 - 10000 период в миллисекундах.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
