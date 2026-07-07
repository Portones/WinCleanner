using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace WinCleaner.Models
{
    public class PhotoItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsSelected { get; set; }
        public ImageSource? Thumbnail { get; set; }

        public string SizeText
        {
            get
            {
                if (Size >= 1024 * 1024)
                    return $"{(double)Size / (1024 * 1024):F2} MB";
                if (Size >= 1024)
                    return $"{(double)Size / 1024:F2} KB";
                return $"{Size} B";
            }
        }
    }

    public class DuplicatePhotoGroup
    {
        public string Hash { get; set; } = string.Empty;
        public List<PhotoItem> Photos { get; set; } = new();
    }
}
