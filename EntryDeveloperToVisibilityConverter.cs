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
    public class EntryDeveloperToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolean_value = (bool)value;

            if (CGlobal.Handler == null || !CGlobal.Handler.Auth.Authorized)
            {
                return Visibility.Collapsed;
            }

            return (boolean_value && CGlobal.Handler.Auth.GroupType != GroupTypeEnum.Developer) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
