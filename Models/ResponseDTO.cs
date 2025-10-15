using IndustrialControlMAUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialControlMAUI.Models;
    public class PageResponeResult<T>
    {
        public int code { get; set; }
        public string? message { get; set; }
        public bool success { get; set; }
        public OrderPageData<T>? result { get; set; }
        public long costTime { get; set; }
    }

    public class OrderPageData<T>
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public long total { get; set; }
        public List<T> records { get; set; } = new();
    }

