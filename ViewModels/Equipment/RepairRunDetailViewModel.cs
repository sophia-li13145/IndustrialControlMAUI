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
    public partial class RepairRunDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAttachmentApi _attachmentApi;
        private readonly IAuthApi _authapi;
        private readonly CancellationTokenSource _cts = new();
        private bool _isInitializing = false;   // ★ 初始化保护开关

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderRepairAttachmentItem> ErrorAttachments { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderRepairAttachmentItem> Attachments { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderRepairAttachmentItem> ImageAttachments { get; } = new();

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<RepairWorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private RepairDetailDto? detail;

        // 全部用户
        /// <summary>执行 new 逻辑。</summary>
        public List<UserInfoDto> AllUsers { get; private set; } = new();

        // ===== 主/辅维修人：模糊查询下拉 =====

        // 主维修人输入框文本（显示 realname）
        [ObservableProperty] private string? mainRepairUserText;

        // 辅维修人输入框文本（多个 realname，用逗号）
        [ObservableProperty] private string? assitRepairUsersText;

        // 下拉是否展开（默认关闭）
        [ObservableProperty] private bool isMainRepairUserDropdownOpen = false;
        [ObservableProperty] private bool isAssistRepairUserDropdownOpen = false;

        // 下拉候选列表
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<UserInfoDto> MainRepairUserSuggestions { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<UserInfoDto> AssistRepairUserSuggestions { get; } = new();

        // 已选辅维修人标签
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<UserInfoDto> SelectedAssistRepairUsers { get; } = new();


        // 明细与附件集合（用于列表绑定）
        /// <summary>执行 new 逻辑。</summary>
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
        public DictRepair dicts = new DictRepair();
        private const string Folder = "devMaintainExecute";
        private const string LocationFile = "fujian";
        private const string LocationImage = "image";

        // ===== 上传限制 =====
        private const int MaxImageCount = 9;
        private const long MaxImageBytes = 2L * 1024 * 1024;   // 2MB
        private const long MaxFileBytes = 20L * 1024 * 1024;   // 20MB
        /// <summary>执行 RepairRunDetailViewModel 初始化逻辑。</summary>
        public RepairRunDetailViewModel(IEquipmentApi api, IAttachmentApi attachmentApi, IAuthApi authapi)
        {
            _api = api;
            _attachmentApi = attachmentApi;
            _authapi = authapi;
        }

        /// <summary>执行 EnsureDictsLoadedAsync 逻辑。</summary>
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

        /// <summary>执行 GetAllUsers 逻辑。</summary>
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

        /// <summary>执行 LoadAsync 逻辑。</summary>
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
                _isInitializing = true;                    // 先打标记，阻止 TextChanged 时触发筛选
                IsMainRepairUserDropdownOpen = false;
                IsAssistRepairUserDropdownOpen = false;
                MainRepairUserSuggestions.Clear();
                AssistRepairUserSuggestions.Clear();
                SelectedAssistRepairUsers.Clear();
                IsEditing = !IsCompletedStatus(Detail?.auditStatus);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {

                    Detail.UrgentText = urgentMap.TryGetValue(Detail.maintainReportDomain?.urgent ?? "", out var uName)
                        ? uName
                        : Detail.urgent;

                    Detail.MaintainTypeText = typeMap.TryGetValue(Detail.maintainType ?? "", out var sName)
                        ? sName
                        : Detail.maintainType;

                    // === 主维修人 ===
                    var mainUser = AllUsers.FirstOrDefault(x =>
                        string.Equals(x.username, Detail.mainRepairUser, StringComparison.OrdinalIgnoreCase));

                    Detail.MainRepairUserText = mainUser?.realname ?? string.Empty;
                    MainRepairUserText = Detail.MainRepairUserText;

                    // === 辅维修人 ===
                    SelectedAssistRepairUsers.Clear();

                    var assiUsernames = (Detail.assitRepairUsers ?? "")
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();

                    if (assiUsernames.Count > 0)
                    {
                        var hitUsers = AllUsers
                            .Where(x => assiUsernames.Contains(x.username, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var u in hitUsers)
                            SelectedAssistRepairUsers.Add(u);

                        var names = hitUsers.Select(x => x.realname);
                        var text = string.Join(",", names);

                        Detail.AssitRepairUsersText = text;
                        AssitRepairUsersText = text;
                    }
                    else
                    {
                        Detail.AssitRepairUsersText = string.Empty;
                        AssitRepairUsersText = string.Empty;
                    }

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
                // ★ 初始化结束后再允许 TextChanged 去触发模糊查询
                _isInitializing = false;
                // 再保险关一次下拉
                IsMainRepairUserDropdownOpen = false;
                IsAssistRepairUserDropdownOpen = false;
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

        /// <summary>执行 LoadErrorPreviewThumbnailsAsync 逻辑。</summary>
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

        // ======= 附件：选择/上传/预览/删除 =======
        [RelayCommand] public async Task PickImagesAsync() => await PickAndUploadAsync(isImage: true);
        [RelayCommand] public async Task PickFilesAsync() => await PickAndUploadAsync(isImage: false);

        /// <summary>执行 PickAndUploadAsync 逻辑。</summary>
        private async Task PickAndUploadAsync(bool isImage)
        {
            try
            {
                var pick = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var opt = new PickOptions
                    {
                        PickerTitle = isImage ? "选择图片" : "选择附件",
                        FileTypes = isImage
                            ? FilePickerFileType.Images
                            : new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                            {
                                { DevicePlatform.Android,     new[] { "*/*" } },
                                { DevicePlatform.iOS,         new[] { "public.data" } },
                                { DevicePlatform.MacCatalyst, new[] { "public.data" } },
                                { DevicePlatform.WinUI,       new[] { "*" } },
                            })
                    };
                    return await FilePicker.PickMultipleAsync(opt);
                });
                if (pick == null) return;

                foreach (var f in pick)
                {
                    var ext = Path.GetExtension(f.FileName)?.TrimStart('.').ToLowerInvariant();
                    var isImg = isImage;

                    // 1) 白名单
                    if (!isImg && !IsAllowedFile(ext))
                    {
                        await ShowTip($"不支持的文件格式：{f.FileName}\n允许：PDF、Word、Excel、Txt、JPG、PNG、RAR/ZIP");
                        continue;
                    }

                    // 2) 复制到临时计算大小
                    using var s = await f.OpenReadAsync();
                    var (tmpPath, len) = await s.CopyToTempAndLenAsync();

                    // 3) 数量/大小限制
                    if (isImg)
                    {
                        if (ImageAttachments.Count >= MaxImageCount) { await ShowTip($"最多上传 {MaxImageCount} 张图片。"); continue; }
                        if (len > MaxImageBytes) { await ShowTip($"图片过大：{f.FileName}\n单张不超过 2M。"); continue; }
                    }
                    else
                    {
                        if (len > MaxFileBytes) { await ShowTip($"附件过大：{f.FileName}\n单个不超过 20M。"); continue; }
                    }

                    // 4) 本地项（先显示）
                    var localItem = new OrderRepairAttachmentItem
                    {
                        AttachmentName = f.FileName,
                        AttachmentExt = ext,
                        AttachmentSize = len,
                        LocalPath = f.FullPath,
                        CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsImage = isImg
                    };

                    if (isImg) { localItem.AttachmentLocation = LocationImage; ImageAttachments.Insert(0, localItem); }
                    else { localItem.AttachmentLocation = LocationFile; Attachments.Insert(0, localItem); }

                    // 5) 上传
                    var contentType = DetectContentType(ext);
                    await using var fs = File.OpenRead(tmpPath);
                    var resp = await _attachmentApi.UploadAttachmentAsync(
                        attachmentFolder: Folder,
                        attachmentLocation: localItem.AttachmentLocation,
                        fileStream: fs,
                        fileName: f.FileName,
                        contentType: contentType,
                        attachmentName: f.FileName,
                        attachmentExt: ext,
                        attachmentSize: len
                    );

                    if (resp?.success == true && resp.result != null)
                    {
                        localItem.AttachmentUrl = string.IsNullOrWhiteSpace(resp.result.attachmentUrl) ? localItem.AttachmentUrl : resp.result.attachmentUrl;
                        localItem.AttachmentRealName = resp.result.attachmentRealName ?? localItem.AttachmentRealName;
                        localItem.AttachmentFolder = resp.result.attachmentFolder ?? Folder;
                        localItem.AttachmentExt = resp.result.attachmentExt ?? ext;
                        localItem.Name = string.IsNullOrWhiteSpace(localItem.Name) ? localItem.AttachmentName : localItem.Name;
                        localItem.Percent = 100;
                        localItem.Status = "done";

                        if (!string.IsNullOrWhiteSpace(resp.result.attachmentUrl))
                            localItem.LocalPath = null;
                    }
                    else
                    {
                        await ShowTip($"上传失败：{resp?.message ?? "未知错误"}（已仅本地显示）");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"选择/上传异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 预览附件
        /// </summary>
        /// <summary>执行 PreviewAttachment 逻辑。</summary>
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

       

        /// <summary>执行 DownloadAttachment 逻辑。</summary>
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
        /// <summary>执行 ShowTip 逻辑。</summary>
        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

        /// <summary>执行 FilterMainRepairUserSuggestions 逻辑。</summary>
        private void FilterMainRepairUserSuggestions(string? keyword)
        {
            if (_isInitializing) return;  // 新增：阻止初始化时展开

            MainRepairUserSuggestions.Clear();

            if (AllUsers == null || AllUsers.Count == 0)
            {
                IsMainRepairUserDropdownOpen = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                IsMainRepairUserDropdownOpen = false;
                return;
            }

            var k = keyword.Trim();

            var query = AllUsers.Where(u =>
                    (!string.IsNullOrWhiteSpace(u.realname) && u.realname.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.username) && u.username.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.phone) && u.phone.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.email) && u.email.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Take(50);

            foreach (var u in query)
                MainRepairUserSuggestions.Add(u);

            IsMainRepairUserDropdownOpen = MainRepairUserSuggestions.Count > 0;
        }


        /// <summary>执行 FilterAssistRepairUserSuggestions 逻辑。</summary>
        private void FilterAssistRepairUserSuggestions(string? keyword)
        {
            if (_isInitializing) return;

            AssistRepairUserSuggestions.Clear();

            if (AllUsers == null || AllUsers.Count == 0)
            {
                IsAssistRepairUserDropdownOpen = false;
                return;
            }

            // ① 当前已选的辅维修人 username 集合，用来排除
            var selectedUsernames = new HashSet<string>(
                SelectedAssistRepairUsers.Select(u => u.username ?? string.Empty),
                StringComparer.OrdinalIgnoreCase);

            IEnumerable<UserInfoDto> source = AllUsers;

            // ② 有关键字就按姓名 / 工号 / 手机 / 邮箱模糊查
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                source = source.Where(u =>
                    (!string.IsNullOrWhiteSpace(u.realname) && u.realname.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.username) && u.username.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.phone) && u.phone.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(u.email) && u.email.Contains(k, StringComparison.OrdinalIgnoreCase)));
            }

            // ③ 只展示“还没选中”的人
            foreach (var u in source.Where(u => !selectedUsernames.Contains(u.username ?? string.Empty))
                                     .Take(50))
            {
                AssistRepairUserSuggestions.Add(u);
            }

            // ④ 有候选就展开下拉
            IsAssistRepairUserDropdownOpen = AssistRepairUserSuggestions.Count > 0;
        }


        /// <summary>执行 OnMainRepairUserTextChanged 逻辑。</summary>
        partial void OnMainRepairUserTextChanged(string? value)
        {
            if (_isInitializing) return;   // 初始化阶段禁止下拉
            FilterMainRepairUserSuggestions(value);
        }

        /// <summary>执行 OnAssitRepairUsersTextChanged 逻辑。</summary>
        partial void OnAssitRepairUsersTextChanged(string? value)
        {
            if (_isInitializing) return;   // 初始化阶段禁止下拉
            FilterAssistRepairUserSuggestions(value);
        }

        /// <summary>执行 PickMainRepairUser 逻辑。</summary>
        [RelayCommand]
        private void PickMainRepairUser(UserInfoDto? user)
        {
            if (user is null || Detail is null) return;

            // 保存给后端：username
            Detail.mainRepairUser = user.username;

            // 显示在页面：realname
            Detail.MainRepairUserText = user.realname;
            MainRepairUserText = user.realname;

            IsMainRepairUserDropdownOpen = false;
            MainRepairUserSuggestions.Clear();
        }
        /// <summary>执行 ToggleAssistRepairUser 逻辑。</summary>
        [RelayCommand]
        private void ToggleAssistRepairUser(UserInfoDto? user)
        {
            if (user is null || Detail is null) return;

            var code = user.username;
            if (string.IsNullOrWhiteSpace(code)) return;

            // ① 当前后端存的 username 列表
            var list = (Detail.assitRepairUsers ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // ② 已存在 -> 移除；未存在 -> 添加
            if (list.Contains(code, StringComparer.OrdinalIgnoreCase))
            {
                list = list
                    .Where(u => !string.Equals(u, code, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                list.Add(code);
            }

            // ③ 回写到后端字段（username 列表）
            Detail.assitRepairUsers = string.Join(",", list);

            // ④ 重新构造已选列表，用于 Tag 区显示
            SelectedAssistRepairUsers.Clear();
            foreach (var username in list)
            {
                var hit = AllUsers.FirstOrDefault(u =>
                    string.Equals(u.username, username, StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                    SelectedAssistRepairUsers.Add(hit);
            }

            // ⑤ 更新显示文本（realname,realname,...）
            var names = SelectedAssistRepairUsers
                .Select(u => u.realname)
                .Where(n => !string.IsNullOrWhiteSpace(n));
            var text = string.Join(",", names);

            Detail.AssitRepairUsersText = text;
            AssitRepairUsersText = text;

            // ⑥ 重新刷新下拉候选（排除已选）
            FilterAssistRepairUserSuggestions(AssitRepairUsersText);
        }

        /// <summary>执行 ClearAssistRepairUsers 逻辑。</summary>
        [RelayCommand]
        private void ClearAssistRepairUsers()
        {
            if (Detail == null) return;

            Detail.assitRepairUsers = string.Empty;
            Detail.AssitRepairUsersText = string.Empty;
            AssitRepairUsersText = string.Empty;

            SelectedAssistRepairUsers.Clear();
            AssistRepairUserSuggestions.Clear();
            IsAssistRepairUserDropdownOpen = false;
        }
        // ======= 保存/完成 =======
        /// <summary>执行 Save 逻辑。</summary>
        [RelayCommand]
        private async Task Save()
        {
            if (Detail is null) { await ShowTip("没有可保存的数据。"); return; }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteRepairSaveAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已保存。");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await ShowTip($"保存失败：{resp?.message ?? "接口返回失败"}");
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"保存异常：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>执行 Complete 逻辑。</summary>
        [RelayCommand]
        private async Task Complete()
        {
            if (Detail is null) { await ShowTip("没有可提交的数据。"); return; }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteRepairCompleteAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已完成维修。");
                    // 本地立即反映完成态，防止用户回退前误操作
                    Detail.auditStatus = "3";
                    IsEditing = false;

                    // 直接返回，触发搜索页 OnAppearing -> 自动刷新
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                else
                {
                    await ShowTip($"提交失败：{resp?.message ?? "接口返回失败"}");
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"提交异常：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>执行 PreparePayloadFromUi 逻辑。</summary>
        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;

            // ② 组装附件
            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl)
                .Select(g => g.First())
                .ToList();

            Detail.maintainWorkOrderAttachmentDomainList = allAttachments.Select(a => new RepairAttachment
            {
                attachmentExt = a.AttachmentExt,
                attachmentFolder = a.AttachmentFolder,
                attachmentLocation = a.AttachmentLocation,
                attachmentName = a.AttachmentName,
                attachmentRealName = a.AttachmentRealName,
                attachmentSize = a.AttachmentSize,
                attachmentUrl = a.AttachmentUrl,
                createdTime = a.CreatedTime,
                id = a.Id,
                status = string.IsNullOrWhiteSpace(a.Status) ? (a.IsUploaded ? "done" : "uploading") : a.Status,
                uid = string.IsNullOrWhiteSpace(a.Uid) ? Guid.NewGuid().ToString("N") : a.Uid,
                url = string.IsNullOrWhiteSpace(a.Url) ? a.AttachmentUrl : a.Url
            }).ToList();

            // ③ 回写明细列表
            Detail.maintainWorkOrderItemDomainList = Items?.ToList() ?? new List<MaintainWorkOrderItemDomain>();
        }
        private static bool IsCompletedStatus(string? s)
   => string.Equals(s, "3", StringComparison.OrdinalIgnoreCase)|| string.Equals(s, "4", StringComparison.OrdinalIgnoreCase);

        /// <summary>执行 IsAllowedFile 逻辑。</summary>
        private static bool IsAllowedFile(string? ext) =>
                IsImageExt(ext) || ext is "pdf" or "doc" or "docx" or "xls" or "xlsx" or "txt" or "rar" or "zip";

        /// <summary>执行 DetectContentType 逻辑。</summary>
        private static string? DetectContentType(string? ext) => ext?.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "webp" => "image/webp",
            "pdf" => "application/pdf",
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "xls" => "application/vnd.ms-excel",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "txt" => "text/plain",
            "rar" => "application/x-rar-compressed",
            "zip" => "application/zip",
            _ => null
        };
    }

}
