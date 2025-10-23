using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Converters
{
    public class DefectLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = value?.ToString()?.Trim();
            // 你可以按实际等级调整
            return level switch
            {
                "一级" => Color.FromArgb("#FFD1D1"), // 淡红
                "二级" => Color.FromArgb("#FFF4B0"), // 淡黄
                "三级" => Color.FromArgb("#BEE8FF"), // 淡蓝
                _ => Color.FromArgb("#E5E7EB")  // 灰底
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null!;
    }

}
