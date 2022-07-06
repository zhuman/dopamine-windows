using Digimezzo.Foundation.Core.Utils;
using Digimezzo.Foundation.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;

namespace Dopamine.Core.IO
{
    public sealed class FileOperations
    {
        public static List<FolderPathInfo> GetValidFolderPaths(long folderId, string directory, string[] validExtensions)
        {
            var folderPaths = new List<FolderPathInfo>();

            try
            {
                // Use the ultra-optimized EnumerateFiles API
                foreach (var file in new DirectoryInfo(directory).EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true }))
                {
                    try
                    {
                        // Only add the file if they have a valid extension
                        if (validExtensions.Contains(Path.GetExtension(file.Name.ToLower())))
                        {
                            folderPaths.Add(new FolderPathInfo(folderId, file.FullName, file.LastWriteTime.Ticks));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("Error occurred while getting folder path for file '{0}'. Exception: {1}", file, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Unexpected error occurred while getting folder paths. Exception: {0}", ex.Message);
            }

            return folderPaths;
        }

        public static bool IsDirectoryContentAccessible(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return false;
            }

            try
            {
                var watcher = new FileSystemWatcher(directoryPath) { EnableRaisingEvents = true, IncludeSubdirectories = true };
                watcher.Dispose();
                watcher = null;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
