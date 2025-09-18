// File: Services/ScanService.cs
using System;
using Microsoft.Maui.Controls;

#if ANDROID
using Android.Content;
using Android.Util;
using IndustrialControlMAUI.Droid; // 引用 DynamicScanReceiver
#endif

namespace IndustrialControlMAUI.Services
{
    /// <summary>
    /// 统一的扫码服务：支持软键盘回车、手动发布、以及 Android 广播动态接收
    /// </summary>
    public class ScanService
    {
        public event Action<string, string?>? Scanned;

        /// <summary>可选：前缀过滤（匹配到则裁剪）</summary>
        public string? Prefix { get; set; }

        /// <summary>可选：后缀过滤（匹配到则裁剪）</summary>
        public string? Suffix { get; set; }

        /// <summary>去抖间隔（毫秒）：相同码在该间隔内只触发一次</summary>
        public int DebounceMs { get; set; } = 250;

        private string? _lastData;
        private DateTime _lastAt = DateTime.MinValue;

        // 约定的广播/键名（与你的设备或发送方对齐）
        public const string BroadcastAction = "lc";
        public const string DataKey = "data";
        public const string TypeKey = "SCAN_BARCODE_TYPE_NAME";

        /// <summary>
        /// 绑定一个 Entry：回车或换行时触发扫码
        /// </summary>
        public void Attach(Entry entry)
        {
            // 回车（Completed）
            entry.Completed += (s, e) =>
            {
                var data = entry.Text?.Trim();
                if (!string.IsNullOrEmpty(data))
                {
#if ANDROID
                    Log.Info("ScanService", $"[Attach] Entry.Completed -> {data}");
#endif
                    FilterAndRaise(data, "kbd");
                    entry.Text = string.Empty;
                }
            };

            // 文本变化：遇到 \n/\r 也触发
            entry.TextChanged += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.NewTextValue)) return;

                if (e.NewTextValue.EndsWith("\n") || e.NewTextValue.EndsWith("\r"))
                {
                    var data = e.NewTextValue.Trim();
#if ANDROID
                    Log.Info("ScanService", $"[Attach] Entry.TextChanged -> {data}");
#endif
                    FilterAndRaise(data, "kbd");
                    entry.Text = string.Empty;
                }
            };
        }

        /// <summary>
        /// 代码侧模拟一次扫码（用于调试/联调）
        /// </summary>
        public void Publish(string code, string? type = null)
        {
#if ANDROID
            Log.Info("ScanService", $"[Publish] 模拟扫码 -> {code}, type={type}");
#endif
            FilterAndRaise(code, type ?? string.Empty);
        }

        /// <summary>
        /// 开始监听 Android 广播（动态注册）
        /// </summary>
        public void StartListening()
        {
#if ANDROID
            Log.Info("ScanService", "[StartListening] ENTER");
            if (_receiver != null) return;

            _receiver = new DynamicScanReceiver();
            _receiver.OnScanned += OnScannedFromPlatform;

            _filter = new IntentFilter(BroadcastAction);
            Android.App.Application.Context.RegisterReceiver(_receiver, _filter);

            Log.Info("ScanService", $"[StartListening] 已注册广播 Action={BroadcastAction}");
#endif
        }

        /// <summary>
        /// 停止监听 Android 广播（反注册）
        /// </summary>
        public void StopListening()
        {
#if ANDROID
            if (_receiver == null) return;

            try
            {
                Android.App.Application.Context.UnregisterReceiver(_receiver);
                Log.Info("ScanService", "[StopListening] 已注销广播");
            }
            catch (Exception ex)
            {
                Log.Warn("ScanService", $"[StopListening] 注销异常: {ex.Message}");
            }

            _receiver.OnScanned -= OnScannedFromPlatform;
            _receiver = null;
            _filter = null;
#endif
        }

        /// <summary>
        /// 统一过滤 + 去抖 + 触发事件
        /// </summary>
        private bool FilterAndRaise(string data, string? type)
        {
            if (!string.IsNullOrEmpty(Prefix) && data.StartsWith(Prefix, StringComparison.Ordinal))
                data = data.Substring(Prefix.Length);

            if (!string.IsNullOrEmpty(Suffix) && data.EndsWith(Suffix, StringComparison.Ordinal))
                data = data.Substring(0, data.Length - Suffix.Length);

            var now = DateTime.UtcNow;
            if (_lastData == data && (now - _lastAt).TotalMilliseconds < DebounceMs)
            {
#if ANDROID
                Log.Info("ScanService", $"[FilterAndRaise] 去抖丢弃: {data}");
#endif
                return false;
            }

            _lastData = data;
            _lastAt = now;

#if ANDROID
            Log.Info("ScanService", $"[FilterAndRaise] 触发 -> {data}, type={type}");
#endif
            Scanned?.Invoke(data, type);
            return true;
        }

#if ANDROID
        private DynamicScanReceiver? _receiver;
        private IntentFilter? _filter;

        private void OnScannedFromPlatform(string data, string? type)
        {
            Log.Info("ScanService", $"[OnScannedFromPlatform] 原始 -> {data}, type={type}");
            FilterAndRaise(data, type);
        }
#endif
    }
}