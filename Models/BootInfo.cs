using System;
using System.Collections.Generic;

namespace WinCleaner.Models
{
    public class BootInfo
    {
        public double LastBootDurationSeconds { get; set; }
        public DateTime LastBootDateTime { get; set; }
        public string UptimeText { get; set; } = string.Empty;
        public List<BootHistoryItem> BootHistory { get; set; } = new();
    }

    public class BootHistoryItem
    {
        public DateTime Date { get; set; }
        public double DurationSeconds { get; set; }
        public string DateText => Date.ToString("dd/MM/yyyy HH:mm");
        public string DurationText => $"{DurationSeconds:F1}s";
    }
}
