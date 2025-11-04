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
    public partial class RepairDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAttachmentApi _attachmentApi;
        private readonly IAuthApi _authapi;
        private readonly CancellationTokenSource _cts = new();

        public ObservableCollection<OrderRepairAttachmentItem> ErrorAttachments { get; } = new();
        public ObservableCollection<OrderRepairAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderRepairAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        public ObservableCollection<RepairWorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private RepairDetailDto? detail;
        public List<UserInfoDto> AllUsers { get; private set; }


        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<MaintainWorkOrderItemDomain> Items { get; } = new();



        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        private bool _dictsLoaded = false;
        [ObservableProperty] private List<DictItem> repairStatusDict = new();
        [ObservableProperty] private List<DictItem> urgentDict = new();
        [ObservableProperty] private List<DictItem> repairTypeDict = new();

        public RepairDetailViewModel(IEquipmentApi api, IAttachmentApi attachmentApi, IAuthApi authapi)
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
                if (RepairStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _api.GetRepairDictsAsync();
                RepairStatusDict = dicts.AuditStatus;
                UrgentDict = dicts.Urgent;
                RepairTypeDict = dicts.MaintainType;
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
            var typeMap = RepairTypeDict?
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
                var resp = await _api.GetRepairDetailAsync(_id!);
                if (resp?.result == null)
                {
                    await ShowTip("未获取到详情数据");
                    return;
                }

                Detail = resp.result;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Detail.UrgentText = urgentMap.TryGetValue(Detail.maintainReportDomain?.urgent ?? "", out var uName)
                        ? uName
                        : Detail.urgent;
                    Detail.MaintainTypeText = typeMap.TryGetValue(Detail.maintainType ?? "", out var sName)
                            ? sName
                            : Detail.maintainType;
                    Detail.MainRepairUserText = AllUsers.Where(x => x.username == Detail.mainRepairUser).FirstOrDefault()?.realname;
                    var assiUsers = Detail.assitRepairUsers?.Split(',').ToList();
                    if (assiUsers != null)
                        Detail.AssitRepairUsersText = string.Join(",", AllUsers.Where(x => assiUsers.Contains(x.username)).Select(x => x.realname).ToList());
                    Detail.RepairStartTime = Detail.RepairStartTime?.Split(' ')[0];
                    Detail.RepairEndTime = Detail.RepairEndTime?.Split(' ')[0];
                    Detail.ExpectedRepairDate = Detail.maintainReportDomain?.expectedRepairDate?.Split(' ')[0];
                });
                //异常图片
                ErrorAttachments.Clear();

                foreach (var at in (Detail?.maintainReportDomain?.maintainReportAttachmentDomainList ?? new List<RepairReportAttachment>()))
                {
                    if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                    var error = new OrderRepairAttachmentItem
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

                    // === 关键：只要是图片，才入缩略图集合 ===
                    error.IsImage = IsImageExt(error.AttachmentExt)
                                   || IsImageExt(Path.GetExtension(error.AttachmentUrl));

                    ErrorAttachments.Add(error);
                }
                await LoadErrorPreviewThumbnailsAsync();
                await LoadWorkflowAsync(_id!);

                // ===== 明细 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Items.Clear();
                        int i = 1;
                        foreach (var it in Detail.maintainWorkOrderItemDomainList ?? new())
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

                        foreach (var at in (Detail.maintainWorkOrderAttachmentDomainList ?? new List<RepairAttachment>()))
                        {
                            if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                            var item = new OrderRepairAttachmentItem
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

        private async Task LoadErrorPreviewThumbnailsAsync()
        {
            // 只处理“图片且当前没有 PreviewUrl，但有 AttachmentUrl 的项”
            var list = ErrorAttachments
                .Where(a => (IsImageExt(a.AttachmentExt))
                            && string.IsNullOrWhiteSpace(a.PreviewUrl) && !string.IsNullOrWhiteSpace(a.AttachmentUrl))
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
        private async Task PreviewAttachment(RepairAttachment? att)
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

       

        [RelayCommand]
        private async Task DownloadAttachment(OrderRepairAttachmentItem? item)
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
                // ① 定义 6 个有序节点（按前端展示顺序）
                //   statusValue 请按后端返回值对应好；下方是常见映射示例：
                //   0:报修  1:待派工  2:待维修  3:维修中  4:维修完成  5:已验收
                var baseSteps = new List<RepairWorkflowVmItem>
        {
            new() { StatusValue = "0", Title = "待派工" },
            new() { StatusValue = "1", Title = "待维修" },
            new() { StatusValue = "2", Title = "维修中" },
            new() { StatusValue = "3", Title = "维修完成" },
            new() { StatusValue = "4", Title = "已验收" },
        };

                var resp = await _api.GetRepairWorkflowAsync(id, _cts.Token);
                var nodes = resp?.result ?? new List<RepairWorkflowNode>();

                // ② 回填时间 & 找“当前”（最后一个有时间的索引）
                int currentIndex = -1;
                for (int i = 0; i < baseSteps.Count; i++)
                {
                    var s = baseSteps[i];
                    s.StepNo = i + 1;
                    s.IsLast = (i == baseSteps.Count - 1);
                    s.IsRowEnd = ((i + 1) % 3 == 0);   // 每行 3 列：第 3、6 个为行尾，用于隐藏连线

                    var hit = nodes.FirstOrDefault(x =>
                        string.Equals(x.statusValue, s.StatusValue, StringComparison.OrdinalIgnoreCase));

                    if (hit != null && !string.IsNullOrWhiteSpace(hit.statusTime))
                    {
                        // 仅取日期部分（若格式 "yyyy-MM-dd HH:mm:ss"）
                        var t = hit.statusTime.Trim();
                        var sp = t.Split(' ');
                        s.Time = sp.Length > 0 ? sp[0] : t;

                        currentIndex = i;   // 最后一个有时间的视作“当前”
                    }
                }

                // ③ 标注 Completed / Current / Active（Active = Completed 或 Current）
                for (int i = 0; i < baseSteps.Count; i++)
                {
                    var s = baseSteps[i];
                    s.IsCurrent = (i == currentIndex);
                    s.IsCompleted = (currentIndex >= 0) && (i < currentIndex) && !string.IsNullOrWhiteSpace(s.Time);
                    s.IsActive = s.IsCurrent || s.IsCompleted;
                }

                // ④ 刷新绑定集合
                WorkflowSteps.Clear();
                foreach (var s in baseSteps)
                    WorkflowSteps.Add(s);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadWorkflowAsync error: " + ex.Message);
            }
        }



    }

}
