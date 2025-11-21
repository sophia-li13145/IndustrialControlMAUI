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
    public partial class ExceptionSubmissionViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAttachmentApi _attachmentApi;
        private readonly IAuthApi _authapi;
        private readonly CancellationTokenSource _cts = new();

        public ObservableCollection<OrderExceptAttachmentItem> ErrorAttachments { get; } = new();
        public ObservableCollection<OrderExceptAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderExceptAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        public ObservableCollection<ExceptWorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private MaintenanceReportDto? detail;
        public List<UserInfoDto> AllUsers { get; private set; }


        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<MaintainWorkOrderItemDomain> Items { get; } = new();



        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        private bool _dictsLoaded = false;
        [ObservableProperty] private List<DictItem> auditStatusDict = new();
        [ObservableProperty] private List<DictItem> urgentDict = new();
        [ObservableProperty] private List<DictItem> devStatusDict = new();

        public ExceptionSubmissionViewModel(IEquipmentApi api, IAttachmentApi attachmentApi, IAuthApi authapi)
        {
            _api = api;
            _attachmentApi = attachmentApi;
            _authapi = authapi;
        }

        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                if (AuditStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _api.GetExceptDictsAsync();
                AuditStatusDict = dicts.AuditStatus;
                UrgentDict = dicts.Urgent;
                DevStatusDict = dicts.DevStatus;
                 _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                _dictsLoaded = true;
            }
        }

        public async Task GetAllUsers()
        {
            try
            {
                AllUsers = await _authapi.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"加载用户列表失败：{ex.Message}", "OK");
            }
        }
        /// <summary>
        /// Shell 路由入参，例如：.../RepairDetailPage?id=xxxx
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
            await EnsureDictsLoadedAsync();
            if (AllUsers is null || AllUsers.Count == 0)
                await GetAllUsers();
            var urgentMap = UrgentDict?
           .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
           .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
           .Select(g => g.First())
           .ToDictionary(
           k => k.dictItemValue!,
           v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
           StringComparer.OrdinalIgnoreCase
       ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var typeMap = DevStatusDict?
           .Where(d => !string.IsNullOrWhiteSpace(d.dictItemValue))
           .GroupBy(d => d.dictItemValue!, StringComparer.OrdinalIgnoreCase)
           .Select(g => g.First())
           .ToDictionary(
           k => k.dictItemValue!,
           v => string.IsNullOrWhiteSpace(v.dictItemName) ? v.dictItemValue! : v.dictItemName!,
           StringComparer.OrdinalIgnoreCase
       ) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (IsBusy || string.IsNullOrWhiteSpace(_id)) return;
            IsBusy = true;
            try
            {
                var resp = await _api.GetExceptDetailAsync(_id!);
                if (resp?.result == null)
                {
                    await ShowTip("未获取到详情数据");
                    return;
                }

                Detail = resp.result;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Detail.urgentText = urgentMap.TryGetValue(Detail?.urgent ?? "", out var uName)
                        ? uName
                        : Detail.urgent;
                    Detail.devStatusText = typeMap.TryGetValue(Detail.devStatus ?? "", out var sName)
                            ? sName
                            : Detail.devStatus;
                });

                await LoadWorkflowAsync(_id!);
                    // ===== 附件 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Attachments.Clear();
                        ImageAttachments.Clear();

                        foreach (var at in (Detail.maintainReportAttachmentDomainList ?? new List<MaintainReportAttachment>()))
                        {
                            if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                            var item = new OrderExceptAttachmentItem
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
            var list = ImageAttachments.Where(a => (IsImageExt(a.AttachmentExt))
                            && string.IsNullOrWhiteSpace(a.PreviewUrl) && !string.IsNullOrWhiteSpace(a.AttachmentUrl)).ToList();
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

        
        private static bool IsImageExt(string? ext)
            => ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";

       

        [RelayCommand]
        private async Task DownloadAttachment(OrderExceptAttachmentItem? item)
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



        // --------- 工具方法 ----------
        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

        private async Task LoadWorkflowAsync(string id)
        {
            try
            {
                var baseSteps = new List<ExceptWorkflowVmItem>();
                //{
                //    new() { StatusValue = "0", Title = "新建" },
                //    new() { StatusValue = "1", Title = "已报修" },
                //};
                var dicts = await _api.GetExceptDictsAsync();
                foreach (var d in AuditStatusDict)
                    baseSteps.Add(new ExceptWorkflowVmItem { Title = d.dictItemName ?? "", StatusValue = d.dictItemValue ?? "" });
                baseSteps = baseSteps.OrderBy(x => x.StatusValue).ToList();
                var resp = await _api.GetExceptWorkflowAsync(id, _cts.Token);
                var nodes = resp?.result ?? new List<ExceptWorkflowNode>();

                // 回填时间 & 找“当前”
                int currentIndex = -1;
                for (int i = 0; i < baseSteps.Count; i++)
                {
                    var s = baseSteps[i];
                    s.StepNo = i + 1;
                    var hit = nodes.FirstOrDefault(x => string.Equals(x.statusValue, s.StatusValue, StringComparison.OrdinalIgnoreCase));
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
