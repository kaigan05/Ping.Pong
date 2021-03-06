﻿using System;
using System.Globalization;
using System.Windows.Data;
using Caliburn.Micro;

namespace PingPong.Converters
{
    public class SourceToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string link = value != null ? value.ToString() : string.Empty;
                if (link == "web")
                    return new Uri("http://www.twitter.com");

                if (link.StartsWith("<a href"))
                {
                    var parts = link.Split('"');
                    Uri uri;
                    if (Uri.TryCreate(parts[1], UriKind.Absolute, out uri))
                        return uri;
                }
            }
            catch (Exception e)
            {
                LogManager.GetLog(GetType()).Error(e);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}