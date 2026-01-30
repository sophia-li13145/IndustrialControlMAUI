using System.Globalization;

namespace IndustrialControlMAUI.Services
{
//    本地文本日志：

//每天一个文件，按日期命名；

//只记录时间（时分秒）+ 文本，不区分等级（Info/Error 等），也没有结构化字段。

//后台轮询 + 事件推送：

//800ms 轮询当前日志文件，检测变化；

//有变化就把全量日志文本通过 LogTextUpdated 推给 UI；

//写日志时会额外推送增量单条日志给订阅者。

//页面级启停：

//只在日志查看页面打开时启动轮询，离开时停止，避免长期占资源。

//系统级 Debug 日志：

//DEBUG 模式下，用 Microsoft.Extensions.Logging 把日志输出到 VS 调试器。
    public class LogService : IDisposable
    {
        private readonly string _logsDir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(800);
        private CancellationTokenSource? _cts;

        public event Action<string>? LogTextUpdated;

        public string TodayLogPath => Path.Combine(_logsDir, $"gr-{DateTime.Now:yyyy-MM-dd}.txt");

        // 日志保留天数（包含今天）
        private const int LogRetainDays = 7;


        public LogService()
        {
            // 确保日志目录存在
            Directory.CreateDirectory(_logsDir);
        }

        public void Start()
        {
            Stop();
            // 启动时清理旧日志（只保留最近 7 天）
            DeleteOldLogs();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => LoopAsync(_cts.Token));
        }

        private void DeleteOldLogs()
        {
            try
            {
                var dir = new DirectoryInfo(_logsDir);
                if (!dir.Exists) return;

                // 今天往前推 (LogRetainDays - 1) 天，形成一个“最早保留日期”
                // 比如 LogRetainDays=7，今天 2025-11-24，则 threshold = 2025-11-18
                var threshold = DateTime.Today.AddDays(-(LogRetainDays - 1));

                foreach (var file in dir.GetFiles("gr-*.txt"))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.Name); // gr-2025-11-24

                    // 防御：先简单形状判断一下，避免奇怪文件名导致异常
                    if (string.IsNullOrWhiteSpace(fileNameWithoutExt) || fileNameWithoutExt.Length < 11)
                        continue;

                    // 取出日期部分：yyyy-MM-dd
                    // 前缀固定是 "gr-"
                    var datePart = fileNameWithoutExt.Substring(3); // 从下标 3 开始

                    if (DateTime.TryParseExact(
                            datePart,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var fileDate))
                    {
                        // 小于阈值就删除（也就是早于“最近 7 天”之外的文件）
                        if (fileDate < threshold)
                        {
                            file.Delete();
                        }
                    }
                    else
                    {
                        // 文件名不是标准格式时可以选择忽略或删除，这里选择忽略
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // 删除失败不影响主流程，在日志窗口里简单提示
                LogTextUpdated?.Invoke($"删除旧日志失败: {ex.Message}");
            }
        }


        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

                            var text = await sr.ReadToEndAsync().ConfigureAwait(false);
                        await SafeDelay(_interval, token).ConfigureAwait(false);
        {
                await Task.Delay(step).ConfigureAwait(false);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (File.Exists(TodayLogPath))
                        {
                            // 用 FileStream + 共享读，避免写入方占用时报错
                            using var fs = new FileStream(
                                TodayLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            using var sr = new StreamReader(fs);
                            var text = await sr.ReadToEndAsync();

                            if (!string.Equals(text, last, StringComparison.Ordinal))
                            {
                                last = text;
                                // 事件尽量在主线程触发（视你的订阅者而定）
                                MainThread.BeginInvokeOnMainThread(() => LogTextUpdated?.Invoke(text));
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // token 取消时会跑到这里，直接退出大循环
                        break;
                    }
                    catch
                    {
                        // 其他 IO 等异常忽略一轮
                    }

                    try
                    {
                        await SafeDelay(_interval, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // 被 Stop() 取消，正常退出
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 兜底（通常到不了这里）
            }
        }

        private async Task SafeDelay(TimeSpan interval, CancellationToken token)
        {
            var ms = (int)interval.TotalMilliseconds;
            var step = 50; // 50ms 一次
            var loops = ms / step;

            for (int i = 0; i < loops; i++)
            {
                if (token.IsCancellationRequested) return;
                await Task.Delay(step);
            }
        }



        // 写日志
        public void WriteLog(string message)
        {
            try
            {
                var logMessage = $"{DateTime.Now:HH:mm:ss} - {message}";
                // 将日志内容追加到文件末尾
                File.AppendAllText(TodayLogPath, logMessage + Environment.NewLine);
                // 触发更新事件
                LogTextUpdated?.Invoke(logMessage);
            }
            catch (Exception ex)
            {
                // 如果写入失败，可以在这里处理错误
                LogTextUpdated?.Invoke($"错误: {ex.Message}");
            }
        }

        public void Dispose() => Stop();
    }
}
