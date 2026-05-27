using CommunityToolkit.Mvvm.ComponentModel;
using IndustrialControlMAUI.Models;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class MaterialFrameDetailViewModel : ObservableObject
{
    public ObservableCollection<MaterialFrameDetailLoadItemVm> LoadDetails { get; } = new();

    [ObservableProperty] private string frameNoDisplay = "-";
    [ObservableProperty] private string currentLocationDisplay = "未分配位置";
    [ObservableProperty] private string useStatusText = "空闲";
    [ObservableProperty] private string useStatusColor = "#22C55E";

    public int DetailCount => LoadDetails.Count;

    public void Apply(MaterialFrameQueryRecord? record)
    {
        LoadDetails.Clear();
        if (record == null)
        {
            OnPropertyChanged(nameof(DetailCount));
            return;
        }

        FrameNoDisplay = string.IsNullOrWhiteSpace(record.frameNo) ? "-" : record.frameNo!;
        CurrentLocationDisplay = string.IsNullOrWhiteSpace(record.currentLocation) ? "未分配位置" : record.currentLocation!;
        var use = record.frameInfo?.useStatus ?? 0;
        UseStatusText = use == 1 ? "占用" : "空闲";
        UseStatusColor = use == 1 ? "#EF4444" : "#22C55E";

        foreach (var detail in record.loadDetailList ?? new List<MaterialFrameQueryLoadDetail>())
            LoadDetails.Add(new MaterialFrameDetailLoadItemVm(detail));

        OnPropertyChanged(nameof(DetailCount));
    }
}
