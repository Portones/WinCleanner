namespace WinCleaner.Models
{
    public class ActiveProcessItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long MemoryBytes { get; set; }
        public string MemoryText => CleanableItem.FormatSize(MemoryBytes);
        public double CpuPercentage { get; set; }
        public string CpuText => $"{CpuPercentage:F1}%";
    }
}
