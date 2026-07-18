using System;
using System.Globalization;
using System.Windows.Data;
using WinCleaner.Models;

namespace WinCleaner.Helpers
{
    /// <summary>
    /// Convierte un valor de bytes (long/double) a texto legible (KB, MB, GB, …).
    /// </summary>
    public class BytesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long bytes = value switch
            {
                long l   => l,
                int  i   => i,
                double d => (long)d,
                _        => 0L
            };

            return CleanableItem.FormatSize(bytes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
