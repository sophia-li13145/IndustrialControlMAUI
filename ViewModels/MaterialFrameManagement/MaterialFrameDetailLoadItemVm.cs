using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

public class MaterialFrameDetailLoadItemVm
{
    public MaterialFrameDetailLoadItemVm(MaterialFrameQueryLoadDetail d)
    {
        MaterialName = FirstNotEmpty(d.materialName, "-");
        BatchNo = FirstNotEmpty(d.batchNo, "-");
        QtyDisplay = (d.qty ?? 0m).ToString("0.##");
    }

    public string MaterialName { get; }
    public string BatchNo { get; }
    public string QtyDisplay { get; }

    private static string FirstNotEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v)) return v!;
        }

        return "-";
    }
}
