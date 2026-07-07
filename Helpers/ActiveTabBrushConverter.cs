using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinCleaner.Helpers
{
    public class ActiveTabBrushConverter : IMultiValueConverter
    {
        private static readonly System.Windows.Media.Brush ActiveBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#312E81"));
        private static readonly System.Windows.Media.Brush InactiveBackground = System.Windows.Media.Brushes.Transparent;

        private static readonly System.Windows.Media.Brush ActiveForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
        private static readonly System.Windows.Media.Brush InactiveForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] is ActivePage (string)
            // values[1] is ButtonDestination (string)
            if (values.Length >= 2 && values[0] != null && values[1] != null)
            {
                bool isActive = string.Equals(values[0]?.ToString(), values[1]?.ToString(), StringComparison.OrdinalIgnoreCase);

                string type = parameter?.ToString() ?? "background";
                if (type.Equals("foreground", StringComparison.OrdinalIgnoreCase))
                {
                    return isActive ? ActiveForeground : InactiveForeground;
                }
                return isActive ? ActiveBackground : InactiveBackground;
            }

            return parameter?.ToString() == "foreground" ? InactiveForeground : InactiveBackground;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
