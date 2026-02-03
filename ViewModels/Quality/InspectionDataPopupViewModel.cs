using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;
using System;

namespace IndustrialControlMAUI.ViewModels;

public partial class InspectionDataPopupViewModel : ObservableObject
{
    private readonly IQualityApi _api;
    private readonly InspectionDetailQuery _query;

    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<InspectionDataRow> Rows { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int pageNo = 1;
    [ObservableProperty] private int pageSize = 10;
    [ObservableProperty] private long total;

    /// <summary>执行 InspectionDataPopupViewModel 初始化逻辑。</summary>
    public InspectionDataPopupViewModel(IQualityApi api, InspectionDetailQuery query)
    {
        _api = api;
        _query = query;
    }

    public string PageInfo => $"第 {PageNo} 页 / 共 {TotalPages} 页";

    /// <summary>执行 Max 逻辑。</summary>
    private int TotalPages => PageSize <= 0 ? 1 : (int)Math.Max(1, Math.Ceiling(Total * 1.0 / PageSize));

    /// <summary>执行 LoadAsync 逻辑。</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var resp = await _api.GetInspectionDetailPageAsync(
                _query.DeviceCode!,
                _query.ParamCode!,
                _query.CollectTimeBegin,
                _query.CollectTimeEnd,
                PageNo,
                PageSize,
                true);

            if (resp?.success != true || resp.result is null)
            {
                Rows.Clear();
                Total = 0;
                OnPropertyChanged(nameof(PageInfo));
                return;
            }

            Total = resp.result.total;
            Rows.Clear();
            int startIndex = (PageNo - 1) * PageSize;
            int rowNo = 1;
            foreach (var record in resp.result.records)
            {
                Rows.Add(new InspectionDataRow
                {
                    RowNo = startIndex + rowNo++,
                    ParamName = string.IsNullOrWhiteSpace(record.paramName) ? record.paramCode : record.paramName,
                    Value = string.IsNullOrWhiteSpace(record.paramUnit)
                        ? record.collectVal
                        : $"{record.collectVal}{record.paramUnit}",
                    CollectTime = record.collectTime
                });
            }
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    /// <summary>执行 PrevAsync 逻辑。</summary>
    [RelayCommand]
    private async Task PrevAsync()
    {
        if (PageNo <= 1) return;
        PageNo--;
        await LoadAsync();
    }

    /// <summary>执行 NextAsync 逻辑。</summary>
    [RelayCommand]
    private async Task NextAsync()
    {
        if (PageNo >= TotalPages) return;
        PageNo++;
        await LoadAsync();
    }
}

public class InspectionDataRow
{
    public int RowNo { get; set; }
    public string? ParamName { get; set; }
    public string? Value { get; set; }
    public string? CollectTime { get; set; }
}
