#if ANDROID
using System;
using Android.Content;
using Android.Runtime; // Register / JniHandleOwnership

namespace IndustrialControlMAUI.Droid
{
    // 建议保留 Register，并使用规范 Java 名称（不能为 null/空/含空格）
    [Register("com.gr.ic.DynamicScanReceiver")]
    public sealed class DynamicScanReceiver : BroadcastReceiver
    {
        public event Action<string, string?>? OnScanned;

        public DynamicScanReceiver() { }

        // ★ 关键：JNI 构造函数，生成 Java stubs 时必备
        protected DynamicScanReceiver(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;

            Android.Util.Log.Info("ScanService", $"[Receiver] onReceive action={intent.Action}");

            // 列出所有 extras（调试）
            var extras = intent.Extras;
            if (extras != null)
            {
                foreach (var k in extras.KeySet())
                {
                    var v = extras.Get(k);
                    Android.Util.Log.Info("ScanService", $"[Receiver] extra {k} = {v}");
                }
            }

            // 优先按你的约定键取值
            string? data = intent.GetStringExtra(IndustrialControlMAUI.Services.ScanService.DataKey);
            string? type = intent.GetStringExtra(IndustrialControlMAUI.Services.ScanService.TypeKey);

            // 常见设备兜底键
            if (string.IsNullOrEmpty(data))
            {
                var altKeys = new[]
                {
                    "com.symbol.datawedge.data_string", // Zebra
                    "barcode_string", "scan_data", "barocode", "scan_result", "value"
                };
                foreach (var k in altKeys)
                {
                    data = intent.GetStringExtra(k);
                    if (!string.IsNullOrEmpty(data))
                    {
                        Android.Util.Log.Info("ScanService", $"[Receiver] 兜底命中 {k} -> {data}");
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(data))
            {
                Android.Util.Log.Info("ScanService", $"[Receiver] 抽取到 data={data}, type={type}");
                OnScanned?.Invoke(data, type);
            }
            else
            {
                Android.Util.Log.Warn("ScanService", "[Receiver] 未从 Intent extras 抽取到条码数据");
            }
        }
    }
}
#endif