using System.Globalization;

namespace IndustrialControlMAUI.Converters;

public sealed class InventoryStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value?.ToString()?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s)) return "–";
        return s switch
        {
            "instock" => "已存货",
            "unstocked" => "未存货",
            _ => s   // 兜底：原样显示
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
