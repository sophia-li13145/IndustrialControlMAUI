using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

/// <summary>Validates actual values before completing a quality inspection.</summary>
internal static class QualityInspectValueValidator
{
    public static bool TryValidate(IEnumerable<QualityItem> items, bool isRequired, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!isRequired)
        {
            return true;
        }

        var position = 0;
        foreach (var item in items)
        {
            position++;
            if (!string.IsNullOrWhiteSpace(item.inspectValue))
            {
                continue;
            }

            var itemNumber = item.index.GetValueOrDefault(position);
            errorMessage = $"第{itemNumber}条质检明细的实际值不能为空。";
            return false;
        }

        return true;
    }
}
