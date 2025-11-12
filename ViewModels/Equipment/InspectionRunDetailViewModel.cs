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
        private const long MaxFileBytes = 20L * 1024 * 1024;   // 20MB

        public ObservableCollection<OrderQualityAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderQualityAttachmentItem> ImageAttachments { get; } = new(); // 仅图片
        public ObservableCollection<QualityItem> Items { get; } = new();                           // 明细
        public ObservableCollection<StatusOption> InspectResultOptions { get; } = new();           // 合格/不合格

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private QualityDetailDto? detail;

        // ==== 检验员输入 + 下拉 ====
        [ObservableProperty] private bool isInspectorDropdownOpen;    // 默认关闭
        [ObservableProperty] private double inspectorDropdownOffset = 40; // Entry 高度 + 间距
        public List<UserInfoDto> AllUsers { get; private set; } = new();
        public ObservableCollection<UserInfoDto> InspectorSuggestions { get; } = new();

        private string? inspectorText;
        public string? InspectorText
        {
            get => inspectorText;
            set
            {
                if (SetProperty(ref inspectorText, value))
                {
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

                    OnPropertyChanged(nameof(IsQualifiedSelected));
                    OnPropertyChanged(nameof(IsUnqualifiedSelected));
                }
            }
        }
        public bool IsQualifiedSelected => string.Equals(SelectedInspectResult?.Value, "合格");
        public bool IsUnqualifiedSelected => string.Equals(SelectedInspectResult?.Value, "不合格");

        // 可编辑开关
        [ObservableProperty] private bool isEditing = true;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        public IReadOnlyList<string> InspectResultTextList { get; } = new[] { "合格", "不合格" };

        public InspectionRunDetailViewModel(IQualityApi api, IAuthApi authApi, IAttachmentApi attachmentApi)
        {
            _api = api;
            _authApi = authApi;
            _attachmentApi = attachmentApi;

            // 默认选项（也可以从字典接口加载）
            InspectResultOptions.Add(new StatusOption { Text = "合格", Value = "合格" });
            InspectResultOptions.Add(new StatusOption { Text = "不合格", Value = "不合格" });
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("id", out var v))
            {
                _id = v?.ToString();
                _ = LoadAsync();
            }
        }

        // ==== 数据加载 ====
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
                Detail?.Recalc();

                await LoadInspectorsAsync();

                // 明细
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Items.Clear();
                    int i = 1;
                    foreach (var it in Detail.orderQualityDetailList ?? new())
                    {
                        it.index = i++;
                        Items.Add(it);
                    }
                });

                // 附件
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Attachments.Clear();
                    ImageAttachments.Clear();

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
                            Url = at.url ?? at.attachmentUrl,
                            QualityNo = Detail?.qualityNo
                        };

                        if (item.AttachmentLocation == LocationFile) Attachments.Add(item);
                        if (item.AttachmentLocation == LocationImage) ImageAttachments.Add(item);
                    }
                });

                await LoadPreviewThumbnailsAsync();

                // 设置下拉选中项
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

        [RelayCommand]
        public async Task LoadInspectorsAsync()
        {
            try
            {
                AllUsers = await _authApi.GetAllUsersAsync();

                // 初始化默认检验员=当前用户（若后端已有值优先显示后端）
                var current = Preferences.Get("UserName", string.Empty);
                if (!string.IsNullOrWhiteSpace(Detail?.inspecter))
                    InspectorText = Detail.inspecter;
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    var hit = AllUsers.FirstOrDefault(u => string.Equals(u.username, current, StringComparison.OrdinalIgnoreCase)
                                                        || string.Equals(u.realname, current, StringComparison.OrdinalIgnoreCase));
                    if (hit != null)
                    {
                        Detail!.inspecter = hit.realname;
                        InspectorText = hit.realname;
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("错误", $"加载用户列表失败：{ex.Message}", "OK");
            }
        }

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

        [RelayCommand]
        private void PickInspector(UserInfoDto? user)
        {
            if (user is null) return;

            if (Detail != null) Detail.inspecter = user.realname;
            InspectorText = user.realname;

            IsInspectorDropdownOpen = false;
            InspectorSuggestions.Clear();
        }

        [RelayCommand]
        private void ClearInspector()
        {
            InspectorText = string.Empty;
            if (Detail != null) Detail.inspecter = string.Empty;
            IsInspectorDropdownOpen = false;
            InspectorSuggestions.Clear();
        }

        // ======= 一键合格 =======
        [RelayCommand]
        private void SetAllQualified()
        {
            foreach (var item in Items)
                item.inspectResult = "合格";
        }

        // ======= 缺陷选择弹窗 =======
        [RelayCommand]
        private async Task OpenDefectPicker(QualityItem? row)
        {
            if (row == null) return;

            var preselectedCodes = row.SelectedDefects.Select(x => x.Name).ToList();
            var picked = await DefectPickerPopup.ShowAsync(_api, preselectedCodes);
            if (picked == null) return;

            Color[] palette =
            {
                Color.FromArgb("#DCEBFF"),
                Color.FromArgb("#FFF4B0"),
                Color.FromArgb("#FFDCDC"),
                Color.FromArgb("#E3FFE3"),
                Color.FromArgb("#EDE3FF"),
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

            row.defect = string.Join(",", row.SelectedDefects.Select(x => x.Name));
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

                var resp = await _api.ExecuteSaveAsync(Detail);
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

                var resp = await _api.ExecuteCompleteInspectionAsync(Detail);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已完成点检。");
                    // 可选：返回上一页
                    // await Shell.Current.GoToAsync("..");
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
                    var localItem = new OrderQualityAttachmentItem
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
                        localItem.QualityNo ??= Detail?.qualityNo;

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

        private static bool IsImageExt(string? ext) =>
            ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp";

        private static bool IsAllowedFile(string? ext) =>
            IsImageExt(ext) || ext is "pdf" or "doc" or "docx" or "xls" or "xlsx" or "txt" or "rar" or "zip";

        /// <summary>将前端集合回填到 Detail，用于提交</summary>
        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;

            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl)
                .Select(g => g.First())
                .ToList();

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

            Detail.orderQualityDetailList = Items?.ToList() ?? new List<QualityItem>();

            foreach (var it in Detail.orderQualityDetailList ?? new())
                it.defect = string.Join(",", it.SelectedDefects.Select(d => d.Name));
        }

        [RelayCommand]
        private async Task DownloadAttachment(OrderQualityAttachmentItem? item)
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

        [RelayCommand]
        private async Task DeleteAttachmentAsync(OrderQualityAttachmentItem? item)
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

        private static Task ShowTip(string msg) =>
            Application.Current?.MainPage?.DisplayAlert("提示", msg, "OK") ?? Task.CompletedTask;
    }
}
