// Converters/InverseBoolConverter.cs
using System.Globalization;
namespace IndustrialControlMAUI.Converters
{
    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b ? !b : value;
        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is bool b ? !b : value;
    }
}
