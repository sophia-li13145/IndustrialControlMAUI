#if ANDROID
using System;
using Android.App;
using Android.Content;
using Android.Runtime; // Register/JNI
using Microsoft.Maui.ApplicationModel; // MainThread
using IndustrialControlMAUI.Services;

namespace GR
{
    [Register("com.gr.HoneywellScanReceiver")]            // 合法 Java 名称
    public sealed class HoneywellScanReceiver : BroadcastReceiver
    {
        public HoneywellScanReceiver() { }
        protected HoneywellScanReceiver(IntPtr h, JniHandleOwnership t) : base(h, t) { }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null) return;
            var data = intent.GetStringExtra("data")
                   ?? intent.GetStringExtra("barcode_data")
                   ?? intent.GetStringExtra("dataString");
            if (string.IsNullOrEmpty(data)) return;

            // Receiver 可能不在 UI 线程，切回主线程调用服务更保险
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var scanSvc = MauiApplication.Current.Services.GetService<ScanService>();
                scanSvc?.Publish(data!);
            });
        }
    }
}
#endif
