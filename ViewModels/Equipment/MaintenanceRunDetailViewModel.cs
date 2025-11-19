using Android.Icu.Text;
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
    public partial class MaintenanceRunDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAuthApi _authApi;
        private readonly IAttachmentApi _attachmentApi;
        private readonly CancellationTokenSource _cts = new();
        // ==== 新增：初始化阶段标记 ====
        private bool _isInitialUpkeepOperatorSetting = false;
        [ObservableProperty] private bool isUpkeepOperatorDropdownOpen;    // 默认关闭

        private const string Folder = "devUpkeepTask";
        private const string LocationFile = "fujian";
        private const string LocationImage = "image";

        // ===== 上传限制 =====
        private const int MaxImageCount = 9;
        private const long MaxImageBytes = 2L * 1024 * 1024;   // 2MB
        private const long MaxFileBytes = 20L * 1024 * 1024;   // 20MB

        public ObservableCollection<OrderMaintenanceAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderMaintenanceAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        public ObservableCollection<WorkflowVmItem> WorkflowSteps { get; } = new();
        public DictMaintenance dicts = new DictMaintenance();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private MaintenanceDetailDto? detail;
        public ObservableCollection<UserInfoDto> UpkeepOperatorSuggestions { get; } = new();
        public List<UserInfoDto> AllUsers { get; private set; } = new();


        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<MaintenanceItem> Items { get; } = new();

        // 检验结果下拉（合格 / 不合格）
        public ObservableCollection<StatusOption> MaintenanceResultOptions { get; } = new();

        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;

        private static bool IsImageExt(string? ext)
            => ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";

        private string? upkeepOperator;
        public string? UpkeepOperator
        {
            get => upkeepOperator;
            set
            {
                if (SetProperty(ref upkeepOperator, value))
                {
                    // ★ 初始化阶段：禁止触发下拉
                    if (_isInitialUpkeepOperatorSetting)
                    {
                        IsUpkeepOperatorDropdownOpen = false;
                        return;
                    }

                    FilterUpkeepOperatorSuggestions(value);
                    IsUpkeepOperatorDropdownOpen = UpkeepOperatorSuggestions.Count > 0;
                }
            }
        }
        // 导航入参
        private string? _id;
        public int Index { get; set; }
        public IReadOnlyList<string> MaintenanceResultTextList { get; } = new[] { "完成", "未完成" };

        public MaintenanceRunDetailViewModel(IEquipmentApi api, IAttachmentApi attachmentApi,IAuthApi authApi)
        {
            _api = api;
            _attachmentApi = attachmentApi;
            _authApi = authApi;
            UpkeepOperatorSuggestions = new ObservableCollection<UserInfoDto>();
            AllUsers = new List<UserInfoDto>();
        }

        private void FilterUpkeepOperatorSuggestions(string? keyword)
        {
            UpkeepOperatorSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(keyword)) return;

            var k = keyword.Trim();

            foreach (var u in AllUsers.Where(u =>
                     (!string.IsNullOrWhiteSpace(u.realname) && u.realname.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.username) && u.username.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.phone) && u.phone.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.email) && u.email.Contains(k, StringComparison.OrdinalIgnoreCase)))
                     .Take(50))
            {
                UpkeepOperatorSuggestions.Add(u);
            }
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
        public async Task LoadUpkeepOperatorsAsync()
        {
            try
            {
                AllUsers = await _authApi.GetAllUsersAsync();

                var current = Preferences.Get("UserName", string.Empty);
                _isInitialUpkeepOperatorSetting = true;

                if (!string.IsNullOrWhiteSpace(Detail?.upkeepOperator))
                {
                    // 保存的是 username，需要映射 realname
                    var hit = AllUsers.FirstOrDefault(u =>
                        string.Equals(u.username, Detail.upkeepOperator, StringComparison.OrdinalIgnoreCase));

                    UpkeepOperator = hit?.realname ?? "";
                }
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    var hit = AllUsers.FirstOrDefault(u =>
                        string.Equals(u.username, current, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.realname, current, StringComparison.OrdinalIgnoreCase));

                    if (hit != null)
                    {
                        Detail!.upkeepOperator = hit.username;  // 保存 username
                        UpkeepOperator = hit.realname;          // UI 显示 realname
                    }
                }

                _isInitialUpkeepOperatorSetting = false;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"加载用户列表失败：{ex.Message}", "OK");
            }
        }


        private async Task LoadDictsAsync()
        {
            try
            {
                // 调你给的接口方法，取出字典
                dicts = await _api.GetMainDictsAsync(_cts.Token);

                // 回到主线程更新下拉选项
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MaintenanceResultOptions.Clear();

                    foreach (var d in dicts.MaintenanceResult ?? new List<DictItem>())
                    {
                        // Text：给用户看的中文，比如 “完成”
                        // Value：真正要保存到 upkeepResult 的 dictItemValue
                        MaintenanceResultOptions.Add(new StatusOption
                        {
                            Text = d.dictItemName,
                            Value = d.dictItemValue
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowTip($"加载保养结果字典失败：{ex.Message}");
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
                IsEditing = !IsCompletedStatus(Detail?.upkeepStatus);

                // ① 先加载字典（里面会顺便填充 MaintenanceResultOptions）
                await LoadDictsAsync();

                // ② 再加载保养人
                await LoadUpkeepOperatorsAsync();

                // ===== 明细 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    int i = 1;
                    foreach (var it in Detail.devUpkeepTaskDetailList ?? new())
                    {
                        it.index = i++; // 1,2,3...

                        // ③ 用字典把原来的 upkeepResult（value）反查到中文名称显示
                        var dictItem = dicts?.MaintenanceResult?
                            .FirstOrDefault(x => x.dictItemValue == it.upkeepResult);

                        it.upkeepResultText = dictItem?.dictItemName;
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


        // ======= 附件：选择/上传/预览/删除 =======
        [RelayCommand] public async Task PickImagesAsync() => await PickAndUploadAsync(isImage: true);
        [RelayCommand] public async Task PickFilesAsync() => await PickAndUploadAsync(isImage: false);

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
                    var localItem = new OrderMaintenanceAttachmentItem
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
        private static bool IsAllowedFile(string? ext) =>
                   IsImageExt(ext) || ext is "pdf" or "doc" or "docx" or "xls" or "xlsx" or "txt" or "rar" or "zip";
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


        [RelayCommand]
        private void PickUpkeepOperator(UserInfoDto? user)
        {
            if (user is null) return;

            // 保存 username
            if (Detail != null) Detail.upkeepOperator = user.username;

            // 页面显示 realname
            UpkeepOperator = user.realname;

            IsUpkeepOperatorDropdownOpen = false;
            UpkeepOperatorSuggestions.Clear();
        }


        [RelayCommand]
        private void ClearUpkeepOperator()
        {
            UpkeepOperator = string.Empty;
            if (Detail != null) Detail.upkeepOperator = string.Empty; // username 清空
            IsUpkeepOperatorDropdownOpen = false;
            UpkeepOperatorSuggestions.Clear();
        }


        // ======= 一键合格 =======
        [RelayCommand]
        private async Task SetAllUpkeep()
        {
            if (Items == null || Items.Count == 0)
            {
                await ShowTip("当前没有可设置为完成的保养项目。");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "提示",
                "确定要一键完成吗？\n将把所有保养项目的结果都设置为【完成】。",
                "确定",
                "取消");

            if (!confirm) return;

            // 从字典中找到“完成”这条（dictItemName 可按实际名称调整）
            var doneDict = dicts?.MaintenanceResult?
                .FirstOrDefault(x =>
                    string.Equals(x.dictItemName, "完成", StringComparison.OrdinalIgnoreCase));

            // 如果找不到，就兜底用原来的字符串
            var value = doneDict?.dictItemValue ?? "finished";
            var text = doneDict?.dictItemName ?? "完成";

            foreach (var item in Items)
            {
                item.upkeepResult = value; // 发送给后端的编码
                item.upkeepResultText = text; // 界面显示的中文
            }
        }






        // ======= 保存/完成 =======
        [RelayCommand]
        private async Task Save()
        {
            if (Detail is null) { await ShowTip("没有可保存的数据。"); return; }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteMainSaveAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已保存。");
                    _ = LoadAsync(); // 保存后刷新
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

        [RelayCommand]
        private async Task Complete()
        {
            if (Detail is null) { await ShowTip("没有可提交的数据。"); return; }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteMainCompleteAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已完成点检。");
                    // 本地立即反映完成态，防止用户回退前误操作
                    Detail.upkeepStatus = "3";
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

        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;

            // ① 先把界面上的中文结果（upkeepResultText）转换成字典值（upkeepResult）
            if (dicts?.MaintenanceResult != null && dicts.MaintenanceResult.Count > 0)
            {
                foreach (var it in Items)
                {
                    if (!string.IsNullOrWhiteSpace(it.upkeepResultText))
                    {
                        var d = dicts.MaintenanceResult
                            .FirstOrDefault(x => x.dictItemName == it.upkeepResultText);

                        if (d != null)
                        {
                            it.upkeepResult = d.dictItemValue;
                        }
                    }
                }
            }

            // ② 组装附件
            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl)
                .Select(g => g.First())
                .ToList();

            Detail.devUpkeepTaskAttachmentList = allAttachments.Select(a => new MaintenanceAttachment
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
            Detail.devUpkeepTaskDetailList = Items?.ToList() ?? new List<MaintenanceItem>();
        }

        private static bool IsCompletedStatus(string? s)
   => string.Equals(s, "3", StringComparison.OrdinalIgnoreCase);
    }

}
