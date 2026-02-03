using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 质检单详情页 VM
    /// </summary>
    public partial class QualityDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IQualityApi _api;
        private readonly IAuthApi _authApi;
        private readonly IAttachmentApi _attachmentApi;
        private readonly CancellationTokenSource _cts = new();
        private const string Folder = "quality";
        private const string LocationFile = "table";
        private const string LocationImage = "main";
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<WorkflowVmItem> WorkflowSteps { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderQualityAttachmentItem> Attachments { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderQualityAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private QualityDetailDto? detail;
        [ObservableProperty]
        private bool isInspectorDropdownOpen = false; // 检验员下拉列表框默认关闭
        [ObservableProperty]
        private double inspectorDropdownOffset = 40; // Entry 高度 + 间距

        // 明细与附件集合（用于列表绑定）
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<QualityItem> Items { get; } = new();

        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        public IReadOnlyList<string> InspectResultTextList { get; } = new[] { "合格", "不合格" };

        /// <summary>执行 QualityDetailViewModel 初始化逻辑。</summary>
        public QualityDetailViewModel(IQualityApi api, IAuthApi authApi, IAttachmentApi attachmentApi)
        {
            _api = api;
            _authApi = authApi;
            _attachmentApi = attachmentApi;

            InspectorSuggestions = new ObservableCollection<UserInfoDto>();
            AllUsers = new List<UserInfoDto>();
           
        }

        public List<UserInfoDto> AllUsers { get; private set; }
        public ObservableCollection<UserInfoDto> InspectorSuggestions { get; }


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

        /// <summary>执行 LoadAsync 逻辑。</summary>
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
                await LoadWorkflowAsync(Detail.id);
                // —— 只在这里手动触发一次计算，保证初值显示一致 ——
                Detail?.Recalc();

                // ===== 明细 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    int i = 1;
                    foreach (var it in Detail.orderQualityDetailList ?? new())
                    {
                        it.index = i++; // 1,2,3...
                        Items.Add(it);
                    }
                });
                Color[] palette =
            {
        Color.FromArgb("#DCEBFF"), // 淡蓝
        Color.FromArgb("#FFF4B0"), // 淡黄
        Color.FromArgb("#FFDCDC"), // 淡红
        Color.FromArgb("#E3FFE3"), // 淡绿
        Color.FromArgb("#EDE3FF"), // 淡紫
    };
                // ===== 附件 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Attachments.Clear();

                    foreach (var at in (Detail.orderQualityAttachmentList ?? new List<QualityAttachment>()))
                    {
                        if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                        var item = new OrderQualityAttachmentItem
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
                            IsUploaded = true,
                            Name = at.name ?? at.attachmentName ?? at.attachmentRealName ?? "",
                            Percent = at.percent ?? 100,
                            Status = string.IsNullOrWhiteSpace(at.status) ? "done" : at.status!,
                            Uid = at.uid,
                            Url = at.url ?? at.attachmentUrl,          // 如果返回了绝对地址，用 url；否则用相对 attachmentUrl
                            QualityNo = Detail?.qualityNo
                        };

                        if (item.AttachmentFolder == LocationFile) Attachments.Add(item);
                        if (item.AttachmentFolder == LocationImage) ImageAttachments.Add(item);
                    }

                    foreach (var it in Detail.orderQualityDetailList ?? new())
                    {
                        var names = (it.defect ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        it.SelectedDefects.Clear();
                        int i = 0;
                        foreach (var d in names)
                        {
                            it.SelectedDefects.Add(new DefectChip
                            {
                                Name = d ,
                                ColorHex = palette[i++ % palette.Length]
                            });
                        }
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
        /// <summary>执行 LoadPreviewThumbnailsAsync 逻辑。</summary>
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
        /// <summary>执行 PreviewAttachment 逻辑。</summary>
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

        private static bool IsImageExt(string? ext)
            => ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";


        /// <summary>执行 DownloadAttachment 逻辑。</summary>
        [RelayCommand]
        private async Task DownloadAttachment(OrderQualityAttachmentItem? item)
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
        /// <summary>执行 LoadWorkflowAsync 逻辑。</summary>
        private async Task LoadWorkflowAsync(string id)
        {
            try
            {
                var baseSteps = new List<WorkflowVmItem>();

                var dicts = await _api.GetQualityDictsAsync();
                var inspectStatusDict = dicts.InspectStatus;
                foreach (var d in inspectStatusDict)
                    baseSteps.Add(new WorkflowVmItem { Title = d.dictItemName ?? "", StatusValue = d.dictItemValue ?? "" });
                baseSteps = baseSteps.OrderBy(x => x.StatusValue).ToList();

                var resp = await _api.GetWorkflowAsync(id, _cts.Token);
                var list = resp?.result ?? new List<InspectWorkflowNode>();

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

        /// <summary>执行 OpenDefectPicker 逻辑。</summary>
        [RelayCommand]
        private async Task OpenDefectPicker(QualityItem? row)
        {
            if (row == null) return;

            var preselectedCodes = row.SelectedDefects.Select(x => x.Name).ToList(); // 你若保存的是 code，就改成 Code
            var picked = await DefectPickerPopup.ShowAsync(_api, preselectedCodes);
            if (picked == null) return;

            // 固定调色板，依次循环
            Color[] palette =
            {
        Color.FromArgb("#DCEBFF"), // 淡蓝
        Color.FromArgb("#FFF4B0"), // 淡黄
        Color.FromArgb("#FFDCDC"), // 淡红
        Color.FromArgb("#E3FFE3"), // 淡绿
        Color.FromArgb("#EDE3FF"), // 淡紫
    };

            row.SelectedDefects.Clear();
            int i = 0;
            foreach (var d in picked)
            {
                row.SelectedDefects.Add(new DefectChip
                {
                    Name = d.DefectName ?? d.DefectCode ?? "",
                    ColorHex = palette[i++ % palette.Length]
                });
            }

            // 如果后端需要回填文本字段：
            row.defect = string.Join(",", row.SelectedDefects.Select(x => x.Name));
        }

        // --------- 工具方法 ----------
        /// <summary>执行 ShowTip 逻辑。</summary>
        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

    }
    
}
