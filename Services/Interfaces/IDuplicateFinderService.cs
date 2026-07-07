using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IDuplicateFinderService
    {
        Task<List<DuplicateGroup>> FindDuplicatesAsync(List<string> paths, IProgress<double> progress, CancellationToken cancellationToken);
        Task<int> CleanDuplicatesAsync(List<DuplicateFile> filesToClean, bool permanent, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
