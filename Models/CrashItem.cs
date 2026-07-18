using System;

namespace WinCleaner.Models
{
    public class CrashItem
    {
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RawMessage { get; set; } = string.Empty;

        public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        public bool IsCritical => Level.Equals("Critical", StringComparison.OrdinalIgnoreCase) || 
                                  Level.Equals("Crítico", StringComparison.OrdinalIgnoreCase) ||
                                  EventId == 41;
    }
}
