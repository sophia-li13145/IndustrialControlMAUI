using System.Globalization;


namespace IndustrialControlMAUI.Converters
{
    public class IntGreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is int n) && n > 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 0;
    }
}
