﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace PopcornTime.Tools.Converters
{
    public class VisibilityConverter : IValueConverter
    {
        public bool Reverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (!(value is bool))
                return Visibility.Collapsed;

            var boolean = (bool) value;

            if (Reverse)
                boolean = !boolean;

            return boolean ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            throw new NotImplementedException();
        }
    }
}