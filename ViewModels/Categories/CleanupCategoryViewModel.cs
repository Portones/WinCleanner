using System;

namespace WinCleaner.ViewModels.Categories
{
    public class CleanupCategoryViewModel : ViewModelBase
    {
        public CleanupViewModel CleanupViewModel { get; }
        public DuplicateFilesViewModel DuplicateFilesViewModel { get; }
        public PhotosCleanupViewModel PhotosCleanupViewModel { get; }
        public BrowserCleanupViewModel BrowserCleanupViewModel { get; }
        public CleanupHistoryViewModel CleanupHistoryViewModel { get; }

        public CleanupCategoryViewModel(
            CleanupViewModel cleanupViewModel,
            DuplicateFilesViewModel duplicateFilesViewModel,
            PhotosCleanupViewModel photosCleanupViewModel,
            BrowserCleanupViewModel browserCleanupViewModel,
            CleanupHistoryViewModel cleanupHistoryViewModel)
        {
            CleanupViewModel = cleanupViewModel ?? throw new ArgumentNullException(nameof(cleanupViewModel));
            DuplicateFilesViewModel = duplicateFilesViewModel ?? throw new ArgumentNullException(nameof(duplicateFilesViewModel));
            PhotosCleanupViewModel = photosCleanupViewModel ?? throw new ArgumentNullException(nameof(photosCleanupViewModel));
            BrowserCleanupViewModel = browserCleanupViewModel ?? throw new ArgumentNullException(nameof(browserCleanupViewModel));
            CleanupHistoryViewModel = cleanupHistoryViewModel ?? throw new ArgumentNullException(nameof(cleanupHistoryViewModel));
        }
    }
}
