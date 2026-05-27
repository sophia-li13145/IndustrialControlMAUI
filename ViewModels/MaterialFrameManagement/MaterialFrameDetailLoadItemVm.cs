using IndustrialControlMAUI.Models;

namespace IndustrialControlMAUI.ViewModels;

public class MaterialFrameDetailLoadItemVm
{
    public MaterialFrameDetailLoadItemVm(MaterialFrameQueryLoadDetail d)
    {
        MaterialName = FirstNotEmpty(d.materialName, d.productName, d.itemName, "-");
        BatchNo = FirstNotEmpty(d.batchNo, d.lotNo, "-");
        QtyDisplay = (d.currentQty ?? d.currentQuantity ?? d.quantity ?? 0m).ToString("0.##");
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
