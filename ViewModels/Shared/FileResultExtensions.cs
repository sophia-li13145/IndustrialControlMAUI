using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.ViewModels
{
    public static class FileResultExtensions
    {
        /// <summary>执行 CopyToTempAndLenAsync 逻辑。</summary>
        public static async Task<(string tempPath, long len)> CopyToTempAndLenAsync(this Stream s)
        {
            var tmp = Path.Combine(FileSystem.CacheDirectory, $"up_{Guid.NewGuid():N}.bin");
            using (var fs = File.Create(tmp))
            {
                await s.CopyToAsync(fs);
            }
            var fi = new FileInfo(tmp);
            return (tmp, fi.Length);
        }
    }
}
