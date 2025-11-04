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
    public partial class MaintenanceDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAttachmentApi _attachmentApi;
        private readonly CancellationTokenSource _cts = new();

        public ObservableCollection<OrderMaintenanceAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderMaintenanceAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        public ObservableCollection<WorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private MaintenanceDetailDto? detail;


        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<MaintenanceItem> Items { get; } = new();

        // 检验结果下拉（合格 / 不合格）
        public ObservableCollection<StatusOption> MaintenanceResultOptions { get; } = new();

        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        private static bool IsImageExt(string? ext)
            => ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";


        // 导航入参
        private string? _id;
        public int Index { get; set; }
        public IReadOnlyList<string> MaintenanceResultTextList { get; } = new[] { "合格", "不合格" };

        public MaintenanceDetailViewModel(IEquipmentApi api, IAttachmentApi attachmentApi)
        {
            _api = api;

            // 默认选项（也可以从字典接口加载）
            MaintenanceResultOptions.Add(new StatusOption { Text = "合格", Value = "合格" });
            MaintenanceResultOptions.Add(new StatusOption { Text = "不合格", Value = "不合格" });

            _attachmentApi = attachmentApi;
        }


        /// <summary>
        /// Shell 路由入参，例如：.../MaintenanceDetailPage?id=xxxx
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
                var resp = await _api.GetMainDetailAsync(_id!);
                if (resp?.result == null)
                {
                    await ShowTip("未获取到详情数据");
                    return;
                }

                Detail = resp.result;
                await LoadWorkflowAsync(_id!);

                // ===== 明细 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    int i = 1;
                    foreach (var it in Detail.devUpkeepTaskDetailList ?? new())
                    {
                        it.index = i++; // 1,2,3...
                        Items.Add(it);
                    }
                });

                // ===== 附件 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Attachments.Clear();
                    ImageAttachments.Clear();

                    foreach (var at in (Detail.devUpkeepTaskAttachmentList ?? new List<MaintenanceAttachment>()))
                    {
                        if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                        var item = new OrderMaintenanceAttachmentItem
                        {
                            AttachmentExt = at.attachmentExt ?? "",
                            AttachmentFolder = at.attachmentFolder ?? "",
                            AttachmentLocation = at.attachmentLocation ?? "",
                            AttachmentName = at.attachmentName ?? "",
                            AttachmentRealName = at.attachmentRealName ?? "",
                            AttachmentSize = (long)at.attachmentSize,
                            AttachmentUrl = at.attachmentUrl ?? "",
                            Id = at.id ?? "",
                            CreatedTime = at.createdTime ?? "",
                            LocalPath = null,
                            IsUploaded = true
                        };

                        if (item.AttachmentLocation == "fujian") Attachments.Add(item);
                        if (item.AttachmentLocation == "image") ImageAttachments.Add(item);
                    }

                    
                });
                await LoadPreviewThumbnailsAsync();
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
        private async Task LoadPreviewThumbnailsAsync()
        {
            // 只处理“图片且当前没有 PreviewUrl，但有 AttachmentUrl 的项”
            var list = ImageAttachments
                .Where(a => (IsImageExt(a.AttachmentExt))
                            && string.IsNullOrWhiteSpace(a.PreviewUrl)                            && !string.IsNullOrWhiteSpace(a.AttachmentUrl))
                .ToList();
            if (list.Count == 0) return;

            // 并发控制：最多 4 条并发
            var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = _cts.Token };

            await Task.Run(() =>
                Parallel.ForEach(list, options, item =>
                {
                    try
                    {
                        // 预签名有效期：例如 10 分钟
                        var resp = _attachmentApi.GetPreviewUrlAsync(item.AttachmentUrl!, 600, options.CancellationToken).GetAwaiter().GetResult();
                        if (resp?.success == true && !string.IsNullOrWhiteSpace(resp.result))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                item.PreviewUrl = resp.result;
                                item.LocalPath = null;  // 有了直连地址就不再用本地
                                item.RefreshDisplay();
                            });
                        }
                    }
                    catch
                    {
                        // 忽略单条失败，必要时写日志
                    }
                })
            );
        }
       


        /// <summary>
        /// 预览附件
        /// </summary>
        [RelayCommand]
        private async Task PreviewAttachment(MaintenanceAttachment? att)
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


        [RelayCommand]
        private async Task DownloadAttachment(OrderMaintenanceAttachmentItem? item)
        {
            if (item is null)
            {
                await ShowTip("无效的附件。");
                return;
            }

            try
            {
                // 1) 有线上链接：交给系统处理（浏览器/下载管理器）
                if (!string.IsNullOrWhiteSpace(item.AttachmentUrl))
                {
                    // 如果你的 URL 需要带 token，可以在这里拼接
                    await Launcher.Default.OpenAsync(new Uri(item.AttachmentUrl));
                    return;
                }

                // 2) 没有 URL，但本地有路径（刚选完未上传）→ 直接打开
                if (!string.IsNullOrWhiteSpace(item.LocalPath) && File.Exists(item.LocalPath))
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(item.LocalPath)
                    });
                    return;
                }

                await ShowTip("该附件没有可用的下载地址。");
            }
            catch (Exception ex)
            {
                await ShowTip($"下载/打开失败：{ex.Message}");
            }
        }

        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

        private async Task LoadWorkflowAsync(string id)
        {
            try
            {
                var baseSteps = new List<WorkflowVmItem>
        {
            new() { StatusValue = "0", Title = "新建" },
            new() { StatusValue = "1", Title = "待保养" },
            new() { StatusValue = "2", Title = "保养中" },
            new() { StatusValue = "3", Title = "已完成" }
        };

                var resp = await _api.GetMainWorkflowAsync(id, _cts.Token);
                var list = resp?.result ?? new List<MaintenanceWorkflowNode>();

                // 回填时间 & 找“当前”
                int currentIndex = -1;
                for (int i = 0; i < baseSteps.Count; i++)
                {
                    var s = baseSteps[i];
                    s.StepNo = i + 1;
                    var hit = list.FirstOrDefault(x => string.Equals(x.statusValue, s.StatusValue, StringComparison.OrdinalIgnoreCase));
                    if (hit != null && !string.IsNullOrWhiteSpace(hit.statusTime))
                    {
                        s.Time = hit.statusTime.Split(' ')[0];
                        currentIndex = i; // 最后一个有时间的就是“当前”
                    }
                }

                // 标注 Completed/Current
                for (int i = 0; i < baseSteps.Count; i++)
                {
                    baseSteps[i].IsCurrent = (i == currentIndex);
                    baseSteps[i].IsCompleted = (i < currentIndex) && !string.IsNullOrWhiteSpace(baseSteps[i].Time);
                    baseSteps[i].IsLast = (i == baseSteps.Count - 1);
                }

                WorkflowSteps.Clear();
                foreach (var s in baseSteps) WorkflowSteps.Add(s);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadWorkflowAsync error: " + ex.Message);
            }
        }


    }

}
