using System.Collections.Generic;
using System.Windows;
using WinCleaner.Models;

namespace WinCleaner.Models
{
    public class DiskNode
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsFolder { get; set; }
        public List<DiskNode> Children { get; set; } = new();
        public DiskNode? Parent { get; set; }

        // Coordenadas calculadas para renderizar en el Canvas (Treemap)
        public Rect Bounds { get; set; }

        // Color para el bloque en la UI
        public string ColorHex { get; set; } = "#3B82F6";

        public string SizeText => CleanableItem.FormatSize(Size);

        public double GetPercentageOf(long totalSize)
        {
            if (totalSize == 0) return 0;
            return (double)Size / totalSize * 100;
        }
    }
}
