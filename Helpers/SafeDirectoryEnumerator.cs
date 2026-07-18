using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinCleaner.Helpers
{
    public static class SafeDirectoryEnumerator
    {
        public static IEnumerable<FileInfo> EnumerateFilesSafe(string rootPath, List<string>? excludedDirs = null)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                yield break;

            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                string currentDir = stack.Pop();

                if (excludedDirs != null && excludedDirs.Count > 0)
                {
                    if (excludedDirs.Any(ex => currentDir.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                        continue;
                }

                DirectoryInfo? dirInfo = null;
                try
                {
                    dirInfo = new DirectoryInfo(currentDir);
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;
                }
                catch { }

                if (dirInfo != null)
                {
                    try
                    {
                        foreach (var subDir in dirInfo.EnumerateDirectories())
                        {
                            try
                            {
                                if (!subDir.Attributes.HasFlag(FileAttributes.ReparsePoint) &&
                                    !subDir.Name.StartsWith("$") &&
                                    !subDir.Name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase))
                                {
                                    stack.Push(subDir.FullName);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }

                    IEnumerable<FileInfo>? files = null;
                    try
                    {
                        files = dirInfo.EnumerateFiles();
                    }
                    catch { }

                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            yield return file;
                        }
                    }
                }
            }
        }
    }
}
