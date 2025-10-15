using System.Globalization;

namespace IndustrialControlMAUI.Converters;
public class NullOrEmptyPlaceholderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        var placeholder = parameter?.ToString() ?? "请选择";
        return string.IsNullOrWhiteSpace(s) ? placeholder : s!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value; // 不需要回传
}
