using System.Globalization;

namespace IndustrialControlMAUI.Converters;
public class IntConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d ? d.ToString("G29", culture) : value?.ToString() ?? "0";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return Binding.DoNothing;

        if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            return TryParseDecimal(s, culture, out var d) && d >= 0 ? d : 0m;

        return int.TryParse(s, NumberStyles.Integer, culture, out var n) && n >= 0 ? n : 0;
    }

    private static bool TryParseDecimal(string text, CultureInfo culture, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, culture, out value)
            || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
