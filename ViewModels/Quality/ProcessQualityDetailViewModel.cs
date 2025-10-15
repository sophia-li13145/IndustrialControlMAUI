// ViewModels/ProcessQualityDetailViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 质检单详情页 VM
    /// </summary>
    public partial class ProcessQualityDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IQualityApi _api;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private QualityDetailDto? detail;

        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<QualityItem> Items { get; } = new();
        public ObservableCollection<QualityAttachment> Attachments { get; } = new();

        // 检验结果下拉（合格 / 不合格）
        public ObservableCollection<StatusOption> InspectResultOptions { get; } = new();

        private StatusOption? _selectedInspectResult;
        public StatusOption? SelectedInspectResult
        {
            get => _selectedInspectResult;
            set
            {
                if (SetProperty(ref _selectedInspectResult, value))
                {
                    // 选中后回写到 Detail.inspectResult
                    if (Detail != null)
                        Detail.inspectResult = value?.Value ?? value?.Text;
                }
            }
        }

        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }

        public ProcessQualityDetailViewModel(IQualityApi api)
        {
            _api = api;

            // 默认选项（也可以从字典接口加载）
            InspectResultOptions.Add(new StatusOption { Text = "合格", Value = "合格" });
            InspectResultOptions.Add(new StatusOption { Text = "不合格", Value = "不合格" });
        }

        /// <summary>
        /// Shell 路由入参，例如：.../ProcessQualityDetailPage?id=xxxx
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("id", out var v))
            {
                _id = v?.ToString();
                _ = LoadAsync();
            }
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(_id)) return;
            IsBusy = true;
            try
            {
                var resp = await _api.GetDetailAsync(_id!);
                if (resp?.result == null)
                {
                    await ShowTip("未获取到详情数据");
                    return;
                }

                Detail = resp.result;

                // 明细
                Items.Clear();
                int i = 1;
                foreach (var it in Detail.orderQualityDetailList ?? new())
                {
                    it.Index = i++;     // ← 生成 1,2,3...
                    Items.Add(it);
                }
                foreach (var it in Detail.orderQualityDetailList ?? new())
                    Items.Add(it);

                // 附件
                Attachments.Clear();
                foreach (var at in Detail.orderQualityAttachmentList ?? new())
                    Attachments.Add(at);

                // 设置“检验结果”选中项
                SelectedInspectResult = InspectResultOptions
                    .FirstOrDefault(o => string.Equals(o.Value, Detail.inspectResult, StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(o.Text, Detail.inspectResult, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                await ShowTip($"加载失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 保存（示例：仅做本地校验与提示；如需调用后端保存接口，按你后端补齐）
        /// </summary>
        [RelayCommand]
        private async Task Save()
        {
            if (Detail is null)
            {
                await ShowTip("没有可保存的数据。");
                return;
            }

            // TODO: 在此处调用你的“保存详情”接口
            await ShowTip("已保存（示例）。");
        }

        /// <summary>
        /// 完成质检（示例：仅做提示；按你的后端接口补齐）
        /// </summary>
        [RelayCommand]
        private async Task Complete()
        {
            if (Detail is null)
            {
                await ShowTip("没有可提交的数据。");
                return;
            }

            // TODO: 在此处调用你的“完成质检/提交”接口
            await ShowTip("已完成质检（示例）。");
        }

        /// <summary>
        /// 预览附件（使用系统浏览器/查看器打开 URL）
        /// </summary>
        [RelayCommand]
        private async Task PreviewAttachment(QualityAttachment? att)
        {
            if (att is null || string.IsNullOrWhiteSpace(att.attachmentUrl))
            {
                await ShowTip("无效的附件。");
                return;
            }

            try
            {
                await Launcher.Default.OpenAsync(new Uri(att.attachmentUrl));
            }
            catch (Exception ex)
            {
                await ShowTip($"无法打开附件：{ex.Message}");
            }
        }

        // --------- 工具方法 ----------

        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;
    }

    /// <summary>
    /// 下拉选项（文本/值）
    /// </summary>
    public class StatusOption
    {
        public string Text { get; set; } = "";
        public string? Value { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(Text) ? Value ?? "" : Text;
    }
}
