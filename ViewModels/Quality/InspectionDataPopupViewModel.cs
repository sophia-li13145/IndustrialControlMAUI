using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Globalization;

namespace IndustrialControlMAUI.ViewModels;

public partial class InspectionDataPopupViewModel : ObservableObject
{
    private readonly IQualityApi _api;
    private readonly InspectionDetailQuery _query;

    public ObservableCollection<InspectionDataRow> Rows { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private int pageNo = 1;
    [ObservableProperty] private int pageSize = 10;
    [ObservableProperty] private long total;

    [ObservableProperty] private ISeries[]? series;
    [ObservableProperty] private Axis[]? xAxes;
    [ObservableProperty] private Axis[]? yAxes;
    [ObservableProperty] private bool hasChartData;

    public InspectionDataPopupViewModel(IQualityApi api, InspectionDetailQuery query)
    {
        _api = api;
        _query = query;
    }

    public string PageInfo => $"第 {PageNo} 页 / 共 {TotalPages} 页";
    public bool NoChartDataVisible => !HasChartData;

    private int TotalPages => PageSize <= 0
        ? 1
        : (int)Math.Max(1, Math.Ceiling(Total * 1.0 / PageSize));

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
                Series = null;
                XAxes = null;
                YAxes = null;
                HasChartData = false;
                OnPropertyChanged(nameof(PageInfo));
                OnPropertyChanged(nameof(NoChartDataVisible));
                return;
            }

            Total = resp.result.total;
            Rows.Clear();

            int startIndex = (PageNo - 1) * PageSize;
            int rowNo = 1;

            foreach (var record in resp.result.records)
            {
                var unit = record.paramUnit;
                var valueText = string.IsNullOrWhiteSpace(unit) || unit == "无"
                    ? record.collectVal
                    : $"{record.collectVal}{unit}";

                Rows.Add(new InspectionDataRow
                {
                    RowNo = startIndex + rowNo++,
                    ParamName = string.IsNullOrWhiteSpace(record.paramName)
                        ? record.paramCode
                        : record.paramName,
                    RawValue = record.collectVal,
                    Unit = unit,
                    Value = valueText,
                    CollectTime = record.collectTime
                });
            }

            BuildChart();
        }
        catch
        {
            Rows.Clear();
            Total = 0;
            Series = null;
            XAxes = null;
            YAxes = null;
            HasChartData = false;
            OnPropertyChanged(nameof(NoChartDataVisible));
            throw;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PageInfo));
        }
    }

    private void BuildChart()
    {
        if (Rows.Count == 0)
        {
            Series = null;
            XAxes = null;
            YAxes = null;
            HasChartData = false;
            OnPropertyChanged(nameof(NoChartDataVisible));
            return;
        }

        var values = new List<double>();
        var labels = new List<string>();

        foreach (var row in Rows)
        {
            if (!TryParseDouble(row.RawValue, out var value))
                continue;

            values.Add(value);
            labels.Add(FormatChartLabel(row.CollectTime));
        }

        if (values.Count == 0)
        {
            Series = null;
            XAxes = null;
            YAxes = null;
            HasChartData = false;
            OnPropertyChanged(nameof(NoChartDataVisible));
            return;
        }

        Series = new ISeries[]
        {
        new LineSeries<double>
        {
            Values = values,
            GeometrySize = 10,
            Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
            Fill = null
        }
        };

        XAxes = new Axis[]
        {
        new Axis
        {
            Labels = labels,
            LabelsRotation = 0,
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220), 1),
            Name = "时间",
            NameTextSize = 12
        }
        };

        YAxes = new Axis[]
        {
        new Axis
        {
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(new SKColor(220, 220, 220), 1),
            Name = "数值",
            NameTextSize = 12
        }
        };

        HasChartData = true;
        OnPropertyChanged(nameof(NoChartDataVisible));
    }

    private static bool TryParseDouble(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
               || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
    }

    private static string FormatChartLabel(string? collectTime)
    {
        if (DateTime.TryParse(collectTime, out var dt))
            return dt.ToString("yyyy-MM-dd HH:mm:ss");

        return string.Empty;
    }

    [RelayCommand]
    private async Task PrevAsync()
    {
        if (PageNo <= 1) return;
        PageNo--;
        await LoadAsync();
    }

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
    public string? RawValue { get; set; }
    public string? Unit { get; set; }
    public string? Value { get; set; }
    public string? CollectTime { get; set; }
}