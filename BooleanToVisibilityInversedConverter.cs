﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AbakConfigurator
{
    public class BooleanToVisibilityInversedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolean_value = (bool)value;
            return boolean_value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
