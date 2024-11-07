using AbakConfigurator.Secure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using AbakConfigurator.Secure.Entry;

namespace AbakConfigurator
{
    public class EntryTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EntryType input_type = (EntryType)value;

            switch ((string)parameter)
            {
                case "TextBox":
                    return (input_type != EntryType.Boolean) ? Visibility.Visible : Visibility.Collapsed;

                case "ToggleButton":
                    return (input_type == EntryType.Boolean) ? Visibility.Visible : Visibility.Collapsed;

                default:
                    throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
