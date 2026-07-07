using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IDiskAnalyzerService
    {
        Task<DiskNode> AnalyzeDirectoryAsync(string path, IProgress<double> progress, CancellationToken cancellationToken);
        void CalculateTreemapLayout(DiskNode rootNode, Rect bounds);
    }
}
