using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels;

public partial class DefectPickerViewModel : ObservableObject
{
    private readonly IQualityApi _api;
    private readonly HashSet<string> _preselected; // 传入的已选编码
    private int _pageNo = 1;
    private int _pageSize = 10; // 与截图一致
    private long _total = 0;

    /// <summary>执行 new 逻辑。</summary>
    public ObservableCollection<Row> Rows { get; } = new();

    [ObservableProperty] private string? defectName;
    [ObservableProperty] private string? defectCode;

    /// <summary>执行 SetProperty 逻辑。</summary>
    public int PageNo { get => _pageNo; set => SetProperty(ref _pageNo, value); }
    /// <summary>执行 SetProperty 逻辑。</summary>
    public int PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }
    /// <summary>执行 SetProperty 逻辑。</summary>
    public long Total { get => _total; set => SetProperty(ref _total, value); }

    /// <summary>执行 DefectPickerViewModel 初始化逻辑。</summary>
    public DefectPickerViewModel(IQualityApi api, IEnumerable<string>? preselectedCodes)
    {
        _api = api;
        _preselected = new HashSet<string>(preselectedCodes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>执行 LoadAsync 逻辑。</summary>
    public async Task LoadAsync()
    {
        var resp = await _api.GetDefectPageAsync(PageNo, PageSize,
            defectCode: DefectCode, defectName: DefectName, searchCount: true);

        Rows.Clear();
        if (resp?.success == true && resp.result != null)
        {
            Total = resp.result.Total;
            int i = 1 + (PageNo - 1) * PageSize;
            foreach (var d in resp.result.Records)
            {
                Rows.Add(new Row
                {
                    RowNo = i++,
                    Id = d.Id,
                    Name = d.DefectName,
                    Code = d.DefectCode,
                    Status = d.Status,
                    Level = !string.IsNullOrWhiteSpace(d.LevelName) ? d.LevelName : d.LevelCode,
                    Description = d.DefectDescription,
                    Standard = d.EvaluationStandard,
                    Creator = d.Creator,
                    CreatedAt = d.CreatedTime,
                    UpdatedAt = d.ModifiedTime,
                    IsChecked = !string.IsNullOrWhiteSpace(d.DefectCode) && _preselected.Contains(d.DefectCode)
                });
            }
        }
        else
        {
            Total = 0;
        }
    }

    [RelayCommand] private async Task Search() { PageNo = 1; await LoadAsync(); }
    [RelayCommand] private async Task Prev() { if (PageNo > 1) { PageNo--; await LoadAsync(); } }
    [RelayCommand] private async Task Next() { var max = Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize)); if (PageNo < max) { PageNo++; await LoadAsync(); } }

    public partial class Row : ObservableObject
    {
        public int RowNo { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Status { get; set; }
        public string? Level { get; set; }
        public string? Description { get; set; }
        public string? Standard { get; set; }
        public string? Creator { get; set; }
        public string? CreatedAt { get; set; }
        public string? UpdatedAt { get; set; }
        public string?  LevelCode { get; set; }

        [ObservableProperty] private bool isChecked;
    }
}
