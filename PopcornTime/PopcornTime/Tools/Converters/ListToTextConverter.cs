using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml.Data;

namespace PopcornTime.Tools.Converters
{
    public class ListToTextConverter : IValueConverter
    {
        public string Seperator { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var list = value as IEnumerable<object>;

            object o_s;
            try
            {
                o_s = string.Join(Seperator, list.Select(p => p.ToString()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] List2Text Convert Exception : " + ex.Message);

                o_s = "error";
            }

            return o_s;

            //return string.Join(Seperator, list.Select(p => p.ToString()));

        }//Convert

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}