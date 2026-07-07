using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WinCleaner.Helpers
{
    public class CircularProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;
            if (value is double dVal)
            {
                percent = dVal / 100.0;
            }
            else if (value is float fVal)
            {
                percent = fVal / 100.0;
            }
            else if (value is int iVal)
            {
                percent = iVal / 100.0;
            }

            double diameter = 100;
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double pDiameter))
            {
                diameter = pDiameter;
            }

            double circumference = Math.PI * diameter;
            if (percent < 0) percent = 0;
            if (percent > 1) percent = 1;

            double dash = percent * circumference;
            double gap = circumference;

            return new DoubleCollection(new double[] { dash, gap });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
