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
    public partial class OtherQualityDetailViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IQualityApi _api;
        private readonly IAuthApi _authApi;
        private readonly IAttachmentApi _attachmentApi;
        private readonly CancellationTokenSource _cts = new();
        private const string Folder = "quality";
        private const string LocationFile = "table";
        private const string LocationImage = "main";
        // ===== 上传限制 =====
        private const int MaxImageCount = 9;
        private const long MaxImageBytes = 2L * 1024 * 1024;   // 2MB
        private const long MaxFileBytes = 20L * 1024 * 1024;  // 20MB

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

        // 检验结果下拉（合格 / 不合格）
        /// <summary>执行 new 逻辑。</summary>
        public ObservableCollection<StatusOption> InspectResultOptions { get; } = new();

        private StatusOption? _selectedInspectResult;
        public StatusOption? SelectedInspectResult
        {
            get => _selectedInspectResult;
            set
            {
                if (SetProperty(ref _selectedInspectResult, value))
                {
                    // 选中后回写到 Detail.inspectResult（不去改 total*，避免触发连锁）
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
        public IReadOnlyList<string> InspectResultTextList { get; } = new[] { "合格", "不合格" };

        /// <summary>执行 OtherQualityDetailViewModel 初始化逻辑。</summary>
        public OtherQualityDetailViewModel(IQualityApi api, IAuthApi authApi, IAttachmentApi attachmentApi)
        {
            _api = api;
            _authApi = authApi;
            _attachmentApi = attachmentApi;

            // 默认选项（也可以从字典接口加载）
            InspectResultOptions.Add(new StatusOption { Text = "合格", Value = "合格" });
            InspectResultOptions.Add(new StatusOption { Text = "不合格", Value = "不合格" });

            InspectorSuggestions = new ObservableCollection<UserInfoDto>();
            AllUsers = new List<UserInfoDto>();
           
        }

        public List<UserInfoDto> AllUsers { get; private set; }
        public ObservableCollection<UserInfoDto> InspectorSuggestions { get; }

        private string? inspectorText;
        public string? InspectorText
        {
            get => inspectorText;
            set
            {
                if (SetProperty(ref inspectorText, value))
                {
                    // 仅做显示文字，不直接写回 Detail.inspecter（等选中再写回更稳）
                    FilterInspectorSuggestions(value);
                    IsInspectorDropdownOpen =  InspectorSuggestions.Count > 0;
                }
            }
        }


        /// <summary>执行 LoadInspectorsAsync 逻辑。</summary>
        [RelayCommand]
        public async Task LoadInspectorsAsync()
        {
            try
            {
                AllUsers = await _authApi.GetAllUsersAsync();

                if (!string.IsNullOrWhiteSpace(Detail?.inspecter))
                {
                    // 直接显示已有检验员名称
                    InspectorText = Detail.inspecter;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"加载用户列表失败：{ex.Message}", "OK");
            }
        }

        /// <summary>执行 PickInspector 逻辑。</summary>
        [RelayCommand]
        private void PickInspector(UserInfoDto? user)
        {
            if (user is null) return;

            // 存到后端字段
            if (Detail != null) Detail.inspecter = user.realname;
            // 输入框显示
            InspectorText = user.realname;

            IsInspectorDropdownOpen = false;
            InspectorSuggestions.Clear();
        }

        /// <summary>执行 ClearInspector 逻辑。</summary>
        [RelayCommand]
        private void ClearInspector()
        {
            InspectorText = string.Empty;
            IsInspectorDropdownOpen = false;
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

                // —— 只在这里手动触发一次计算，保证初值显示一致 ——
                Detail?.Recalc();
                IsEditing = !IsCompletedStatus(Detail?.inspectStatus);

                await LoadInspectorsAsync();

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

                // ===== 下拉选中项：检验结果 =====
                SelectedInspectResult = InspectResultOptions
                    .FirstOrDefault(o =>
                        string.Equals(o.Value, Detail.inspectResult, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(o.Text, Detail.inspectResult, StringComparison.OrdinalIgnoreCase));
               
                IsInspectorDropdownOpen = false;
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
        /// 保存（示例：仅做本地校验与提示；如需调用后端保存接口，按你后端补齐）
        /// </summary>
        /// <summary>执行 Save 逻辑。</summary>
        [RelayCommand]
        private async Task Save()
        {
            if (Detail is null)
            {
                await ShowTip("没有可保存的数据。");
                return;
            }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteSaveAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已保存。");
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
            if (Detail is null)
            {
                await ShowTip("没有可提交的数据。");
                return;
            }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();

                var resp = await _api.ExecuteCompleteInspectionAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已完成质检。");
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

        /// <summary>执行 PickImagesAsync 逻辑。</summary>
        [RelayCommand]
        public async Task PickImagesAsync() => await PickAndUploadAsync(isImage: true);

        // 新增：附件上传命令（给“上传附件”文字）
        /// <summary>执行 PickFilesAsync 逻辑。</summary>
        [RelayCommand]
        public async Task PickFilesAsync() => await PickAndUploadAsync(isImage: false);

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
                        { DevicePlatform.Android,    new[] { "*/*" } },
                        { DevicePlatform.iOS,        new[] { "public.data" } },
                        { DevicePlatform.MacCatalyst,new[] { "public.data" } },
                        { DevicePlatform.WinUI,      new[] { "*" } },
                            })
                    };
                    return await FilePicker.PickMultipleAsync(opt);
                });
                if (pick == null) return;

                foreach (var f in pick)
                {
                    var ext = Path.GetExtension(f.FileName)?.TrimStart('.').ToLowerInvariant();
                    var isImg = isImage;

                    // === 1) 统一做格式校验（文件入口也要限制到白名单）
                    if (!isImg && !IsAllowedFile(ext))
                    {
                        await ShowTip($"不支持的文件格式：{f.FileName}\n允许：PDF、Word、Excel、Txt、JPG、PNG、RAR");
                        continue;
                    }

                    // === 2) 读尺寸（同时返回临时文件路径以便计算大小）
                    using var s = await f.OpenReadAsync();
                    var (tmpPath, len) = await s.CopyToTempAndLenAsync();

                    // === 3) 数量/大小限制
                    if (isImg)
                    {
                        if (ImageAttachments.Count >= MaxImageCount)
                        {
                            await ShowTip($"最多上传 {MaxImageCount} 张图片。");
                            continue;
                        }
                        if (len > MaxImageBytes)
                        {
                            await ShowTip($"图片过大：{f.FileName}\n单张不超过 2M。");
                            continue;
                        }
                    }
                    else
                    {
                        if (len > MaxFileBytes)
                        {
                            await ShowTip($"附件过大：{f.FileName}\n单个附件不超过 20M。");
                            continue;
                        }
                    }

                    // === 4) 构造本地项（先让 UI 有反馈）
                    var localItem = new OrderQualityAttachmentItem
                    {
                        AttachmentName = f.FileName,
                        AttachmentExt = ext,
                        AttachmentSize = len,
                        LocalPath = f.FullPath,      // 有些设备是 content:// 也没事；缩略图走 LocalPath 时已兼容
                        CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsImage = isImg            // 决定是否进缩略图
                    };

                    if (!isImg)
                    {
                        localItem.AttachmentLocation = LocationFile;
                        Attachments.Insert(0, localItem);
                    }

                    // 仅图片列表
                    if (isImg)
                    {
                        localItem.AttachmentLocation = LocationImage;
                        ImageAttachments.Insert(0, localItem);
                    }

                    // 6) 真正上传文件（关键：multipart/form-data + file）
                    var attachmentLocation = isImg ? LocationImage : LocationFile;
                    var contentType = DetectContentType(ext);     // 见下方辅助函数

                    // 用临时文件重新打开流，避免上面 using 的 src 已被释放
                    await using var fs = File.OpenRead(tmpPath);
                    var resp = await _attachmentApi.UploadAttachmentAsync(
                                attachmentFolder: Folder,
                                attachmentLocation: attachmentLocation,
                                fileStream: fs,
                                fileName: f.FileName,
                                contentType: contentType,
                                attachmentName: f.FileName,
                                attachmentExt: ext,
                                attachmentSize: len
                            );


                    if (resp?.success == true && resp.result != null)
                    {
                        localItem.AttachmentUrl = string.IsNullOrWhiteSpace(resp.result.attachmentUrl)
                                                        ? localItem.AttachmentUrl
                                                        : resp.result.attachmentUrl;
                        localItem.AttachmentRealName = resp.result.attachmentRealName ?? localItem.AttachmentRealName;
                        localItem.AttachmentFolder = resp.result.attachmentFolder ?? Folder;
                        localItem.AttachmentLocation = resp.result.attachmentLocation ?? attachmentLocation;
                        localItem.AttachmentExt = resp.result.attachmentExt ?? ext;
                        localItem.Name = string.IsNullOrWhiteSpace(localItem.Name) ? localItem.AttachmentName : localItem.Name;
                        localItem.Percent = 100;
                        localItem.Status = "done";
                        localItem.QualityNo ??= Detail?.qualityNo;      // 从详情带过来

                        // 如果服务端给了 URL，就让图片改走网络地址展示；本地临时可清掉
                        if (!string.IsNullOrWhiteSpace(resp.result.attachmentUrl))
                            localItem.LocalPath = null;
                    }
                    else
                    {
                        await ShowTip($"上传失败：{resp?.message ?? "未知错误"}\n（已仅本地显示）");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"选择/上传异常：{ex.Message}");
            }
        }

        /// <summary>执行 DetectContentType 逻辑。</summary>
        private static string? DetectContentType(string? ext)
        {
            switch (ext?.ToLowerInvariant())
            {
                case "jpg":
                case "jpeg": return "image/jpeg";
                case "png": return "image/png";
                case "gif": return "image/gif";
                case "bmp": return "image/bmp";
                case "webp": return "image/webp";
                case "pdf": return "application/pdf";
                case "doc": return "application/msword";
                case "docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "xls": return "application/vnd.ms-excel";
                case "xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "txt": return "text/plain";
                case "rar": return "application/x-rar-compressed";
                case "zip": return "application/zip";
                default: return null; // 让 HttpClient 自行处理
            }
        }

        private static bool IsImageExt(string? ext)
            => ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";

        private static bool IsAllowedFile(string? ext)
            => IsImageExt(ext) || ext is "pdf" or "doc" or "docx" or "xls" or "xlsx" or "txt" or "rar" or "zip";


        /// <summary>
        /// 将前端的 Attachments/Items 回填到 Detail，用于提交
        /// </summary>
        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;
            // 合并两个集合并去重（按 Id 或 Url 去重都可以）
            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl) // 防止重复
                .Select(g => g.First())
                .ToList();
            // 1) 附件：把 UI 集合映射回服务器字段
            Detail.orderQualityAttachmentList = allAttachments.Select(a => new QualityAttachment
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
                name = string.IsNullOrWhiteSpace(a.Name) ? a.AttachmentName : a.Name,
                qualityNo = string.IsNullOrWhiteSpace(a.QualityNo) ? Detail.qualityNo : a.QualityNo,
                status = string.IsNullOrWhiteSpace(a.Status) ? (a.IsUploaded ? "done" : "uploading") : a.Status,
                uid = string.IsNullOrWhiteSpace(a.Uid) ? Guid.NewGuid().ToString("N") : a.Uid,
                url = string.IsNullOrWhiteSpace(a.Url) ? a.AttachmentUrl : a.Url
            }).ToList();

            // 2) 明细：Items 已经是服务器的明细模型（你加载时就是 Detail.orderQualityDetailList → Items）
            //    如果后端要求传回列表字段名叫 orderQualityDetailList，则回填：
            Detail.orderQualityDetailList = Items?.ToList() ?? new List<QualityItem>();

            // 3) 选中的检验结果（你在 SelectedInspectResult setter 已经把 Text/Value 写回 Detail.inspectResult）
            //    这里无需额外处理，确保 Detail.inspectResult 已是最终值即可。
            foreach (var it in Detail.orderQualityDetailList ?? new())
            {
                // 让 DTO 里的 defect = “名称1,名称2”
                it.defect = string.Join(",", it.SelectedDefects.Select(d => d.Name ));
                // 如需传编码，再加 it.defectCodeList = it.SelectedDefects.Select(d => d.Code).ToList();
            }

        }

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

        /// <summary>执行 DeleteAttachmentAsync 逻辑。</summary>
        [RelayCommand]
        private async Task DeleteAttachmentAsync(OrderQualityAttachmentItem? item)
        {
            if (item is null) return;

            // 未上传（没有 Id）的本地占位，直接本地删除
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
                var resp = await _api.DeleteAttachmentAsync(item.Id, _cts?.Token ?? CancellationToken.None);
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


        /// <summary>执行 RemoveFromCollections 逻辑。</summary>
        private void RemoveFromCollections(OrderQualityAttachmentItem item)
        {
            Attachments.Remove(item);
            if (item.IsImage && ImageAttachments.Contains(item))
                ImageAttachments.Remove(item);
        }
        // --------- 工具方法 ----------
        /// <summary>执行 ShowTip 逻辑。</summary>
        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;

        private static bool IsCompletedStatus(string? s)
     => string.Equals(s, "3", StringComparison.OrdinalIgnoreCase);
    }
    
}
