using Android.Icu.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialControlMAUI.Models;
using IndustrialControlMAUI.Pages;
using IndustrialControlMAUI.Services;
using System.Collections.ObjectModel;

namespace IndustrialControlMAUI.ViewModels
{
    /// <summary>
    /// 点巡检/质检 执行详情 VM
    /// </summary>
    public partial class InspectionRunDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAuthApi _authApi;
        private readonly IAttachmentApi _attachmentApi;
        private readonly CancellationTokenSource _cts = new();
        // ==== 新增：初始化阶段标记 ====
        private bool _isInitialInspectorSetting = false;

        private const string Folder = "devInspectTask";
        private const string LocationFile = "fujian";
        private const string LocationImage = "image";

        // ===== 上传限制 =====
        private const int MaxImageCount = 9;
        private const long MaxImageBytes = 2L * 1024 * 1024;   // 2MB
        private const long MaxFileBytes = 20L * 1024 * 1024;   // 20MB

        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderInspectionAttachmentItem> Attachments { get; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<OrderInspectionAttachmentItem> ImageAttachments { get; } = new(); // 仅图片
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<InspectionItem> Items { get; } = new();                    // 明细
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<StatusOption> InspectResultOptions { get; } = new();           // 合格/不合格

        [ObservableProperty] private bool isBusy;
        public DictInspection dicts = new DictInspection();

        // ==== 检验员输入 + 下拉 ====
        /// <summary>执行 new 逻辑。</summary>
        [ObservableProperty] private bool isInspectorDropdownOpen;    // 默认关闭
        [ObservableProperty] private double inspectorDropdownOffset = 40; // Entry 高度 + 间距
        public List<UserInfoDto> AllUsers { get; private set; } = new();
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<UserInfoDto> InspectorSuggestions { get; } = new();

        private string? inspectorText;
        public string? InspectorText
        {
            get => inspectorText;
            set
            {
                if (SetProperty(ref inspectorText, value))
                {
                    // ★ 初始化阶段：禁止触发下拉
                    if (_isInitialInspectorSetting)
                    {
                        IsInspectorDropdownOpen = false;
                        return;
                    }

                    FilterInspectorSuggestions(value);
                    IsInspectorDropdownOpen = InspectorSuggestions.Count > 0;
                }
            }
        }


        // ==== 检验结果（主结论） ====
        private StatusOption? _selectedInspectResult;
        public StatusOption? SelectedInspectResult
        {
            get => _selectedInspectResult;
            set
            {
                if (SetProperty(ref _selectedInspectResult, value))
                {
                    if (Detail != null)
                        Detail.inspectResult = value?.Value ?? value?.Text;

                    OnPropertyChanged(nameof(IsInspectionSelected));
                    OnPropertyChanged(nameof(IsUnqualifiedSelected));
                }
            }
        }
        /// <summary>执行 Equals 逻辑。</summary>
        public bool IsInspectionSelected => string.Equals(SelectedInspectResult?.Value, "合格");
        /// <summary>执行 Equals 逻辑。</summary>
        public bool IsUnqualifiedSelected => string.Equals(SelectedInspectResult?.Value, "不合格");

        // 可编辑开关
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        public IReadOnlyList<string> InspectResultTextList { get; } = new[] { "合格", "不合格" };


        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<WorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private InspectDetailDto? detail;


        /// <summary>执行 InspectionRunDetailViewModel 初始化逻辑。</summary>
        public InspectionRunDetailViewModel(IEquipmentApi api, IAuthApi authApi, IAttachmentApi attachmentApi)
        {
            _api = api;
            _authApi = authApi;
            _attachmentApi = attachmentApi;
            InspectorSuggestions = new ObservableCollection<UserInfoDto>();
            AllUsers = new List<UserInfoDto>();
        }

        /// <summary>执行 ApplyQueryAttributes 逻辑。</summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("id", out var v))
            {
                _id = v?.ToString();
                _ = LoadAsync();
            }
        }
        /// <summary>执行 LoadDictsAsync 逻辑。</summary>
        private async Task LoadDictsAsync()
        {
            try
            {
                // 调你给的接口方法，取出字典
                dicts = await _api.GetInspectionDictsAsync(_cts.Token);

                // 回到主线程更新下拉选项
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    InspectResultOptions.Clear();

                    foreach (var d in dicts.InspectResult ?? new List<DictItem>())
                    {
                        InspectResultOptions.Add(new StatusOption
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


        // ==== 数据加载 ====
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

                IsEditing = !IsCompletedStatus(Detail?.inspectStatus);
                await LoadDictsAsync();

                await LoadInspectorsAsync();

                // ===== 明细 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    int i = 1;
                    foreach (var it in Detail.devInspectTaskDetailList ?? new())
                    {
                        it.index = i++; // 1,2,3...
                        var dictItem = dicts?.InspectResult?
                    .FirstOrDefault(x => x.dictItemValue == it.inspectResult);

                        it.inspectResultText = dictItem?.dictItemName;
                        Items.Add(it);
                    }
                });

                // ===== 附件 =====
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Attachments.Clear();

                    foreach (var at in (Detail.devInspectTaskAttachmentList ?? new List<InspectionAttachment>()))
                    {
                        if (string.IsNullOrWhiteSpace(at.attachmentUrl)) continue;

                        var item = new OrderInspectionAttachmentItem
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

        /// <summary>执行 LoadInspectorsAsync 逻辑。</summary>
        [RelayCommand]
        public async Task LoadInspectorsAsync()
        {
            try
            {
                AllUsers = await _authApi.GetAllUsersAsync();

                // 初始化默认检验员=当前用户（若后端已有值优先显示后端）
                var current = Preferences.Get("UserName", string.Empty);
                // 开启初始化模式
                _isInitialInspectorSetting = true;

                if (!string.IsNullOrWhiteSpace(Detail?.inspecter))
                {
                    InspectorText = Detail.inspecter;
                }
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    var hit = AllUsers.FirstOrDefault(u =>
                        string.Equals(u.username, current, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.realname, current, StringComparison.OrdinalIgnoreCase));

                    if (hit != null)
                    {
                        Detail!.inspecter = hit.realname;
                        InspectorText = hit.realname;
                    }
                }

                // 初始化完毕
                _isInitialInspectorSetting = false;

            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"加载用户列表失败：{ex.Message}", "OK");
            }
        }

        /// <summary>执行 FilterInspectorSuggestions 逻辑。</summary>
        private void FilterInspectorSuggestions(string? keyword)
        {
            InspectorSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(keyword)) return;

            var k = keyword.Trim();

            foreach (var u in AllUsers.Where(u =>
                     (!string.IsNullOrWhiteSpace(u.realname) && u.realname.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.username) && u.username.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.phone) && u.phone.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
                     (!string.IsNullOrWhiteSpace(u.email) && u.email.Contains(k, StringComparison.OrdinalIgnoreCase)))
                     .Take(50))
            {
                InspectorSuggestions.Add(u);
            }
        }

        /// <summary>执行 PickInspector 逻辑。</summary>
        [RelayCommand]
        private void PickInspector(UserInfoDto? user)
        {
            if (user is null) return;

            if (Detail != null) Detail.inspecter = user.realname;
            InspectorText = user.realname;

            IsInspectorDropdownOpen = false;
            InspectorSuggestions.Clear();
        }

        /// <summary>执行 ClearInspector 逻辑。</summary>
        [RelayCommand]
        private void ClearInspector()
        {
            InspectorText = string.Empty;
            if (Detail != null) Detail.inspecter = string.Empty;
            IsInspectorDropdownOpen = false;
            InspectorSuggestions.Clear();
        }

        // ======= 一键合格 =======
        /// <summary>执行 SetAllInspection 逻辑。</summary>
        [RelayCommand]
        private async Task SetAllInspection()
        {
            if (Items == null || Items.Count == 0)
            {
                await ShowTip("当前没有可设置为合格的点检项目。");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "提示",
                "确定要一键合格吗？\n将把所有点检项目的结果都设置为【合格】。",
                "确定",
                "取消");

            if (!confirm) return;

            foreach (var item in Items)
            {
                item.inspectResult = "合格";   // 这里现在会触发 OnPropertyChanged
                item.inspectResultText = "合格";
            }

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

                var resp = await _api.ExecuteSaveAsync(Detail);
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

                var resp = await _api.ExecuteCompleteInspectionAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已完成点检。");
                    // 本地立即反映完成态，防止用户回退前误操作
                    Detail.inspectStatus = "3";
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
                    var localItem = new OrderInspectionAttachmentItem
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

        /// <summary>执行 LoadPreviewThumbnailsAsync 逻辑。</summary>
        private async Task LoadPreviewThumbnailsAsync()
        {
            var list = ImageAttachments
                .Where(a => IsImageExt(a.AttachmentExt)
                         && string.IsNullOrWhiteSpace(a.PreviewUrl)
                         && !string.IsNullOrWhiteSpace(a.AttachmentUrl))
                .ToList();
            if (list.Count == 0) return;

            var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = _cts.Token };

            await Task.Run(() =>
                Parallel.ForEach(list, options, item =>
                {
                    try
                    {
                        var resp = _attachmentApi.GetPreviewUrlAsync(item.AttachmentUrl!, 600, options.CancellationToken).GetAwaiter().GetResult();
                        if (resp?.success == true && !string.IsNullOrWhiteSpace(resp.result))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                item.PreviewUrl = resp.result;
                                item.LocalPath = null;
                                item.RefreshDisplay();
                            });
                        }
                    }
                    catch { /* 忽略单条失败 */ }
                })
            );
        }

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

        /// <summary>执行 IsImageExt 逻辑。</summary>
        private static bool IsImageExt(string? ext) =>
            ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";

        /// <summary>执行 IsAllowedFile 逻辑。</summary>
        private static bool IsAllowedFile(string? ext) =>
            IsImageExt(ext) || ext is "pdf" or "doc" or "docx" or "xls" or "xlsx" or "txt" or "rar" or "zip";

        /// <summary>将前端集合回填到 Detail，用于提交</summary>
        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;
            // ① 先把界面上的中文结果（upkeepResultText）转换成字典值（upkeepResult）
            if (dicts?.InspectResult != null && dicts.InspectResult.Count > 0)
            {
                foreach (var it in Items)
                {
                    if (!string.IsNullOrWhiteSpace(it.inspectResultText))
                    {
                        var d = dicts.InspectResult
                            .FirstOrDefault(x => x.dictItemName == it.inspectResultText);

                        if (d != null)
                        {
                            it.inspectResult = d.dictItemValue;
                        }
                    }
                }
            }

            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl)
                .Select(g => g.First())
                .ToList();

            Detail.devInspectTaskAttachmentList = allAttachments.Select(a => new InspectionAttachment
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

            Detail.devInspectTaskDetailList = Items?.ToList() ?? new List<InspectionItem>();

        }

        /// <summary>执行 DownloadAttachment 逻辑。</summary>
        [RelayCommand]
        private async Task DownloadAttachment(OrderInspectionAttachmentItem? item)
        {
            if (item is null) { await ShowTip("无效的附件。"); return; }

            try
            {
                if (!string.IsNullOrWhiteSpace(item.AttachmentUrl))
                {
                    await Launcher.Default.OpenAsync(new Uri(item.AttachmentUrl));
                    return;
                }

                if (!string.IsNullOrWhiteSpace(item.LocalPath) && File.Exists(item.LocalPath))
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(item.LocalPath) });
                    return;
                }

                await ShowTip("该附件没有可用的下载地址。");
            }
            catch (Exception ex)
            {
                await ShowTip($"下载/打开失败：{ex.Message}");
            }
        }

        /// <summary>执行 DeleteAttachmentAsync 逻辑。</summary>
        [RelayCommand]
        private async Task DeleteAttachmentAsync(OrderInspectionAttachmentItem? item)
        {
            if (item is null) return;

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                if (item.AttachmentLocation == LocationFile) Attachments.Remove(item);
                if (item.AttachmentLocation == LocationImage) ImageAttachments.Remove(item);
                await ShowTip("已从本地移除（未上传到服务器）");
                return;
            }

            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "确认删除",
                $"确定删除附件：{item.AttachmentName}？",
                "删除", "取消");
            if (!confirm) return;

            try
            {
                var resp = await _api.DeleteInspectAttachmentAsync(item.Id, _cts?.Token ?? CancellationToken.None);
                if (resp?.success == true && resp.result)
                {
                    Attachments.Remove(item);
                    if (item.IsImage) ImageAttachments.Remove(item);
                    await ShowTip("删除成功");
                }
                else
                {
                    await ShowTip($"删除失败：{resp?.message ?? "未知错误"}");
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"删除异常：{ex.Message}");
            }
        }

        /// <summary>执行 ShowTip 逻辑。</summary>
        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

        private static bool IsCompletedStatus(string? s)
   => string.Equals(s, "3", StringComparison.OrdinalIgnoreCase);
    }
}
