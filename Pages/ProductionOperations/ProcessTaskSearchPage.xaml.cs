using CommunityToolkit.Maui.Views;
using IndustrialControlMAUI.Services;
using IndustrialControlMAUI.ViewModels;
using System.Text.Json;

namespace IndustrialControlMAUI.Pages;

public partial class ProcessTaskSearchPage : ContentPage, IQueryAttributable
{
    private readonly ProcessTaskSearchViewModel _vm;
    private string? _entryMode;
    private bool _isStatusPopupOpening;
    private bool _isWorkstationPopupOpening;
    private bool _leavingForScan;

    public ProcessTaskSearchPage(ProcessTaskSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _leavingForScan = false;   // 扫码返回后复位
        _vm.SetEntryMode(_entryMode);
        OrderEntry.Focus();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _entryMode = null;
        if (query.TryGetValue("entryMode", out var mode))
        {
            _entryMode = mode?.ToString();
        }

        _vm.SetEntryMode(_entryMode);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // 仅跳转到扫码页（会触发本页 Disappearing）时不重置筛选，避免影响扫码匹配
        if (_leavingForScan)
            return;

        // 真正退出页面：将工序筛选变回默认（“全部”）
        if (BindingContext is ProcessTaskSearchViewModel vm)
            vm.ResetProcessToDefault();
    }

    private async void OnScanClicked(object sender, EventArgs e)
    {
        _leavingForScan = true;   // 标记：即将跳转扫码页，OnDisappearing 时不要重置筛选
        var tcs = new TaskCompletionSource<string>();
        await Navigation.PushAsync(new QrScanPage(tcs));

        var result = await tcs.Task;
        if (string.IsNullOrWhiteSpace(result))
            return;

        var raw = result.Trim();

        if (BindingContext is not ProcessTaskSearchViewModel vm)
            return;

        // 1) 尝试解析为工序 JSON（含 processCode / processName）
        var (processCode, processName, isJsonButInvalid) = TryParseScanProcess(raw);
        if (processCode is not null || processName is not null)
        {
            var ok = await vm.TrySelectProcessByScanAsync(processCode, processName);
            if (!ok) return;                       // 未匹配到工序，已提示，结束

            // 清掉工单号输入，确保查询按所选工序 + 时间范围筛选（而非按工单号）
            OrderEntry.Text = string.Empty;
            vm.Keyword = string.Empty;

            if (vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);
            return;
        }

        // 2) 以 “{” 开头但解析失败/缺字段 → 工序二维码识别失败
        if (isJsonButInvalid)
        {
            await DisplayAlert("提示", "工序二维码识别失败，请重新扫码。", "确定");
            return;
        }

        // 3) 其余按工单号处理：需通过基本格式校验，避免乱码被误当工单号
        if (!LooksLikeWorkOrderNo(raw))
        {
            await DisplayAlert("提示", "扫码内容无法识别（可能是工序二维码识别失败），请重试。", "确定");
            return;
        }

        // 工单号：清除之前已筛选的工序数据（变回默认“全部”）并查询
        OrderEntry.Text = raw;
        OrderEntry?.Focus();
        vm.ResetProcessToDefault();
        vm.Keyword = raw;

        if (vm.SearchCommand.CanExecute(null))
            vm.SearchCommand.Execute(null);
    }

    /// <summary>
    /// 解析扫码结果：若为标准 JSON 且含 processCode/processName，则返回对应值；
    /// isJsonButInvalid 表示内容以 “{” 开头但解析失败或缺少工序字段（即工序二维码识别失败）。
    /// </summary>
    private static (string? processCode, string? processName, bool isJsonButInvalid) TryParseScanProcess(string raw)
    {
        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            return (null, null, false);

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var hasCode = doc.RootElement.TryGetProperty("processCode", out var codeEl);
            var hasName = doc.RootElement.TryGetProperty("processName", out var nameEl);
            if (!hasCode && !hasName)
                return (null, null, true);   // 是 JSON 但无工序字段 → 识别失败

            var code = hasCode && codeEl.ValueKind == JsonValueKind.String ? codeEl.GetString() : null;
            var name = hasName && nameEl.ValueKind == JsonValueKind.String ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name))
                return (null, null, true);

            return (string.IsNullOrWhiteSpace(code) ? null : code,
                    string.IsNullOrWhiteSpace(name) ? null : name,
                    false);
        }
        catch (JsonException)
        {
            return (null, null, true);   // 以 “{” 开头却解析失败 → 工序码残缺，识别失败
        }
    }

    /// <summary>
    /// 判断扫码内容是否像工单号：仅允许字母与数字、长度 >= 4、且至少包含一个字母。
    /// 用于过滤明显乱码（如 “$3”、纯数字等），避免被误当作工单号查询。
    /// </summary>
    private static bool LooksLikeWorkOrderNo(string raw)
    {
        if (raw.Length < 4)
            return false;
        if (raw.Any(ch => !char.IsLetterOrDigit(ch)))
            return false;
        return raw.Any(char.IsLetter);
    }

    private async void OnStatusFilterClicked(object sender, EventArgs e)
    {
        if (_isStatusPopupOpening)
            return;
        System.Diagnostics.Debug.WriteLine("A1: 点击状态按钮");

        try
        {
            _isStatusPopupOpening = true;

            if (BindingContext is ProcessTaskSearchViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"A2: StatusOptions.Count={vm.StatusOptions?.Count}");
                await this.ShowPopupAsync(new StatusMultiSelectPopup(vm.StatusOptions));
                System.Diagnostics.Debug.WriteLine("A3: Popup 已关闭");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"A_ERR: {ex}");
            await DisplayAlert("错误", ex.ToString(), "确定");
        }
        finally
        {
            _isStatusPopupOpening = false;
        }
    }

    private async void OnWorkstationFilterClicked(object sender, TappedEventArgs e)
    {
        if (_isWorkstationPopupOpening) return;
        try
        {
            _isWorkstationPopupOpening = true;
            var result = await this.ShowPopupAsync(new WorkstationSelectPopup(_vm.WorkOrderApi, _vm.SelectedWorkstations));
            if (result is IReadOnlyCollection<IndustrialControlMAUI.Models.WorkstationInfo> selected)
                _vm.SetSelectedWorkstations(selected);
        }
        finally { _isWorkstationPopupOpening = false; }
    }
}
