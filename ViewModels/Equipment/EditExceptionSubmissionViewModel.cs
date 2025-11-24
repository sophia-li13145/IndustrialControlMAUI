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
    public partial class EditExceptionSubmissionViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IEquipmentApi _api;
        private readonly IAttachmentApi _attachmentApi;
        private readonly IEnergyApi _energyApi;
        private readonly CancellationTokenSource _cts = new();

        public ObservableCollection<OrderExceptAttachmentItem> ErrorAttachments { get; } = new();
        public ObservableCollection<OrderExceptAttachmentItem> Attachments { get; } = new();
        public ObservableCollection<OrderExceptAttachmentItem> ImageAttachments { get; } = new(); // 仅图片

        public ObservableCollection<ExceptWorkflowVmItem> WorkflowSteps { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private MaintenanceReportDto? detail;
        [ObservableProperty] private bool isNew;       // 新建 = true
        [ObservableProperty] private bool isEditMode;  // 编辑 = true

        // 明细与附件集合（用于列表绑定）
        public ObservableCollection<MaintainWorkOrderItemDomain> Items { get; } = new();


        // 可编辑开关（如需控制 Entry/Picker 的 IsEnabled）
        [ObservableProperty] private bool isEditing = true;
        List<DevItem> devList = new();
        // 设备下拉列表数据源
        public ObservableCollection<DevItem> DevOptions { get; } = new();

        // 当前选中的设备
        [ObservableProperty]
        private DevItem? selectedDev;

        // 导航入参
        private string? _id;
        public int Index { get; set; }
        private bool _dictsLoaded = false;
        [ObservableProperty] private List<DictItem> auditStatusDict = new();
        [ObservableProperty] private List<DictItem> urgentDict = new();
        [ObservableProperty] private List<DictItem> devStatusDict = new();
        // 设备状态下拉选中项
        [ObservableProperty]
        private DictItem? selectedDevStatus;

        // 紧急程度下拉选中项
        [ObservableProperty]
        private DictItem? selectedUrgent;

        public DictExcept dicts = new DictExcept();
        private const string Folder = "devUpkeepTask";
        private const string LocationImage = "image";

        // ===== 上传限制 =====
        private const int MaxImageCount = 9;
        private const long MaxImageBytes = 2L * 1024 * 1024;   // 2MB

        public EditExceptionSubmissionViewModel(IEquipmentApi api, IAttachmentApi attachmentApi, IEnergyApi energyApi)
        {
            _api = api;
            _attachmentApi = attachmentApi;
            _energyApi = energyApi;
        }

        private async Task EnsureDictsLoadedAsync()
        {
            if (_dictsLoaded) return;

            try
            {
                if (DevStatusDict.Count > 0) return; // 已加载则跳过

                var dicts = await _api.GetExceptDictsAsync();
                AuditStatusDict = dicts.AuditStatus;
                UrgentDict = dicts.Urgent;
                DevStatusDict = dicts.DevStatus;
                // ===== 设备列表 =====
                devList = await _energyApi.GetDevListAsync();
                DevOptions.Clear();
                foreach (var d in devList)
                {
                    DevOptions.Add(d);
                }
                _dictsLoaded = true;
            }
            catch (Exception ex)
            {
                _dictsLoaded = true;
            }
        }
        // 选设备
        partial void OnSelectedDevChanged(DevItem? value)
        {
            if (Detail is null || value is null) return;

            Detail.devCode = value.devCode;
            Detail.devName = value.devName;
            Detail.devModel = value.devModel;
            Detail.workShopName = value.workShopName;
        }

        // 选设备状态
        partial void OnSelectedDevStatusChanged(DictItem? value)
        {
            if (Detail is null || value is null) return;

            Detail.devStatus = value.dictItemValue;
            Detail.devStatusText = string.IsNullOrWhiteSpace(value.dictItemName)
                ? value.dictItemValue
                : value.dictItemName;
        }

        // 选紧急程度
        partial void OnSelectedUrgentChanged(DictItem? value)
        {
            if (Detail is null || value is null) return;

            Detail.urgent = value.dictItemValue;
            Detail.urgentText = string.IsNullOrWhiteSpace(value.dictItemName)
                ? value.dictItemValue
                : value.dictItemName;
        }



        /// <summary>
        /// Shell 路由入参，例如：.../RepairDetailPage?id=xxxx
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("id", out var v) && v is not null && !string.IsNullOrWhiteSpace(v.ToString()))
            {
                // 编辑模式
                _id = v.ToString();
                IsNew = false;
                IsEditMode = true;

                _ = LoadAsync();
            }
            else
            {
                // 新增模式
                _id = null;
                IsNew = true;
                IsEditMode = false;

                _ = InitForCreateAsync();
            }
        }
        private async Task InitForCreateAsync()
        {
            await EnsureDictsLoadedAsync();  // 加载下拉字典等

            Detail = new MaintenanceReportDto
            {
                // 根据你后端约定填一些默认值
                auditStatus = "0",   // 新建
                creator = Preferences.Get("UserName", string.Empty),
                createdTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                modifier = Preferences.Get("UserName", string.Empty),
                modifiedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 新建肯定可编辑
            IsEditing = true;
        }



        [RelayCommand]
        private async Task LoadAsync()
        {
            await EnsureDictsLoadedAsync();
            
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
                IsEditing = !IsCompletedStatus(Detail?.auditStatus);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // ===== 根据详情值反选“紧急程度”下拉 =====
                    if (!string.IsNullOrWhiteSpace(Detail.urgent) && UrgentDict != null)
                    {
                        SelectedUrgent = UrgentDict
                            .FirstOrDefault(d => string.Equals(
                                d.dictItemValue, Detail.urgent, StringComparison.OrdinalIgnoreCase));
                    }

                    // ===== 根据详情值反选“设备状态”下拉 =====
                    if (!string.IsNullOrWhiteSpace(Detail.devStatus) && DevStatusDict != null)
                    {
                        SelectedDevStatus = DevStatusDict
                            .FirstOrDefault(d => string.Equals(
                                d.dictItemValue, Detail.devStatus, StringComparison.OrdinalIgnoreCase));
                    }

                    // =============================
                    // 下拉框初始化默认值（关键新增部分）
                    // =============================

                    // 1) 紧急程度：如果接口没给值、或者没匹配到，就取第一个作为默认
                    if (SelectedUrgent == null && UrgentDict != null && UrgentDict.Count > 0)
                    {
                        SelectedUrgent = UrgentDict[0];

                        Detail.urgent = SelectedUrgent.dictItemValue;
                        Detail.urgentText = string.IsNullOrWhiteSpace(SelectedUrgent.dictItemName)
                            ? SelectedUrgent.dictItemValue
                            : SelectedUrgent.dictItemName;
                    }

                    // 2) 设备状态：同理
                    if (SelectedDevStatus == null && DevStatusDict != null && DevStatusDict.Count > 0)
                    {
                        SelectedDevStatus = DevStatusDict[0];

                        Detail.devStatus = SelectedDevStatus.dictItemValue;
                        Detail.devStatusText = string.IsNullOrWhiteSpace(SelectedDevStatus.dictItemName)
                            ? SelectedDevStatus.dictItemValue
                            : SelectedDevStatus.dictItemName;
                    }

                });
                // ===== 根据详情反选设备 =====
                if (Detail != null && DevOptions.Count > 0)
                {
                    var hit = DevOptions.FirstOrDefault(x =>
                        (!string.IsNullOrWhiteSpace(Detail.devCode) &&
                         string.Equals(x.devCode, Detail.devCode, StringComparison.OrdinalIgnoreCase))
                        ||
                        (!string.IsNullOrWhiteSpace(Detail.devName) &&
                         string.Equals(x.devName, Detail.devName, StringComparison.OrdinalIgnoreCase)));

                    if (hit != null)
                    {
                        SelectedDev = hit;
                    }
                    // 如果接口没给设备信息，帮它选一个默认（比如第一个）
                    else
                    {
                        var first = DevOptions.FirstOrDefault();
                        if (first != null)
                        {
                            SelectedDev = first;
                            Detail.devCode = first.devCode;
                            Detail.devName = first.devName;
                            Detail.devModel = first.devModel;
                            Detail.workShopName = first.workShopName;
                        }
                    }
                }

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

        // ======= 附件：选择/上传/预览/删除 =======
        [RelayCommand] public async Task PickImagesAsync() => await PickAndUploadAsync();

        private async Task PickAndUploadAsync()
        {
            try
            {
                var pick = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var opt = new PickOptions
                    {
                        PickerTitle ="选择图片",
                        FileTypes = FilePickerFileType.Images
                            
                    };
                    return await FilePicker.PickMultipleAsync(opt);
                });
                if (pick == null) return;

                foreach (var f in pick)
                {
                    var ext = Path.GetExtension(f.FileName)?.TrimStart('.').ToLowerInvariant();

                    // 2) 复制到临时计算大小
                    using var s = await f.OpenReadAsync();
                    var (tmpPath, len) = await s.CopyToTempAndLenAsync();

                    // 3) 数量/大小限制
                        if (ImageAttachments.Count >= MaxImageCount) { await ShowTip($"最多上传 {MaxImageCount} 张图片。"); continue; }
                        if (len > MaxImageBytes) { await ShowTip($"图片过大：{f.FileName}\n单张不超过 2M。"); continue; }


                    // 4) 本地项（先显示）
                    var localItem = new OrderExceptAttachmentItem
                    {
                        AttachmentName = f.FileName,
                        AttachmentExt = ext,
                        AttachmentSize = len,
                        LocalPath = f.FullPath,
                        CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    };

                    localItem.AttachmentLocation = LocationImage; ImageAttachments.Insert(0, localItem); 

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


        // ======= 保存/完成 =======
        [RelayCommand]
        private async Task Save()
        {
            if (Detail is null) { await ShowTip("没有可保存的数据。"); return; }

            try
            {
                IsBusy = true;
                PreparePayloadFromUi();
                // === 关键：从 Detail 映射出真正的请求体 ===
                var payload = new BuildExceptRequest
                {
                    id = Detail.id,
                    description = Detail.description,
                    devCode = Detail.devCode,
                    devModel = Detail.devModel,
                    devName = Detail.devName,
                    devStatus = Detail.devStatus,
                    expectedRepairDate = Detail.expectedRepairDate,
                    memo = Detail.memo,
                    phenomena = Detail.phenomena,
                    urgent = Detail.urgent,               // 注意这里是 level1/level2/level3
                    workShopName = Detail.workShopName,
                    maintainReportAttachmentDomainList = Detail.maintainReportAttachmentDomainList
                        .Select(a => new BuildExceptAttachment
                        {
                            attachmentExt = a.attachmentExt,
                            attachmentFolder = a.attachmentFolder,
                            attachmentLocation = a.attachmentLocation,
                            attachmentName = a.attachmentName,
                            attachmentRealName = a.attachmentRealName,
                            attachmentSize = a.attachmentSize,
                            attachmentUrl = a.attachmentUrl,
                            id = a.id,
                            memo = a.memo
                        })
                        .ToList()
                };
                var resp = await _api.ExecuteExceptSaveAsync(payload);
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
                // === 关键：从 Detail 映射出真正的请求体 ===
                var payload = new BuildExceptRequest
                {
                    id = Detail.id,
                    description = Detail.description,
                    devCode = Detail.devCode,
                    devModel = Detail.devModel,
                    devName = Detail.devName,
                    devStatus = Detail.devStatus,
                    expectedRepairDate = Detail.expectedRepairDate,
                    memo = Detail.memo,
                    phenomena = Detail.phenomena,
                    urgent = Detail.urgent,               // 注意这里是 level1/level2/level3
                    workShopName = Detail.workShopName,
                    maintainReportAttachmentDomainList = Detail.maintainReportAttachmentDomainList
                        .Select(a => new BuildExceptAttachment
                        {
                            attachmentExt = a.attachmentExt,
                            attachmentFolder = a.attachmentFolder,
                            attachmentLocation = a.attachmentLocation,
                            attachmentName = a.attachmentName,
                            attachmentRealName = a.attachmentRealName,
                            attachmentSize = a.attachmentSize,
                            attachmentUrl = a.attachmentUrl,
                            id = a.id,
                            memo = a.memo
                        })
                        .ToList()
                };
                var resp = await _api.SubmitExceptAsync(payload);
                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("已报修。");
                    // 本地立即反映完成态，防止用户回退前误操作
                    Detail.auditStatus = "1";
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

        // 新增时的“确定”按钮
        [RelayCommand]
        private async Task ConfirmAsync()
        {
            if (Detail is null)
            {
                await ShowTip("没有可提交的数据。");
                return;
            }

            try
            {
                IsBusy = true;

                // 先把附件等从 UI 回填到 Detail
                PreparePayloadFromUi();

                // === 关键：从 Detail 映射出真正的请求体 ===
                var payload = new BuildExceptRequest
                {
                    id = Detail.id,
                    description = Detail.description,
                    devCode = Detail.devCode,
                    devModel = Detail.devModel,
                    devName = Detail.devName,
                    devStatus = Detail.devStatus,
                    expectedRepairDate = Detail.expectedRepairDate??DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    memo = Detail.memo,
                    phenomena = Detail.phenomena,
                    urgent = Detail.urgent,               // 注意这里是 level1/level2/level3
                    workShopName = Detail.workShopName,
                    maintainReportAttachmentDomainList = Detail.maintainReportAttachmentDomainList
                        .Select(a => new BuildExceptAttachment
                        {
                            attachmentExt = a.attachmentExt,
                            attachmentFolder = a.attachmentFolder,
                            attachmentLocation = a.attachmentLocation,
                            attachmentName = a.attachmentName,
                            attachmentRealName = a.attachmentRealName,
                            attachmentSize = a.attachmentSize,
                            attachmentUrl = a.attachmentUrl,
                            id = a.id,
                            memo = a.memo
                        })
                        .ToList()
                };

                var resp = await _api.BuildExceptAsync(payload);

                if (resp?.success == true && resp.result == true)
                {
                    await ShowTip("新增异常已提交。");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await ShowTip($"新增失败：{resp?.message ?? "接口返回失败"}");
                }
            }
            catch (Exception ex)
            {
                await ShowTip($"新增异常：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PreparePayloadFromUi()
        {
            if (Detail is null) return;

            // ② 组装附件
            var allAttachments = Attachments
                .Concat(ImageAttachments)
                .GroupBy(a => a.Id ?? a.AttachmentUrl)
                .Select(g => g.First())
                .ToList();

            Detail.maintainReportAttachmentDomainList = allAttachments.Select(a => new MaintainReportAttachment
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

           
        }
        private static bool IsCompletedStatus(string? s)
    => string.Equals(s, "1", StringComparison.OrdinalIgnoreCase);

    }

}
