using System;
using System.Globalization;
using System.Windows.Data;

namespace AbakConfigurator
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string string_value = (string)value;

            if (string_value == "0")
            {
                string_value = "false";
            }
            else if (string_value == "1")
            {
                string_value = "true";
            }

            if (string_value == "")
            {
                return false;
            }

            bool result = false;
            bool.TryParse(string_value, out result);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bool_value = (bool)value;
            return bool_value.ToString();
        }
    }
}
