using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Linq;

namespace ValheimExtComponentManager
{
    public class ComponentFileOps
    {
        private readonly ComponentManageContext _context;

        public ComponentFileOps(ComponentManageContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public static void UnzipSubdirectoryToTarget(string archivePath, string subdirectory, string targetDirectory)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith(subdirectory + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = entry.FullName.Substring(subdirectory.Length + 1);
                        string destinationPath = Path.Combine(targetDirectory, relativePath);

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // This is a directory
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        {
                            // This is a file
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
            }
        }

        public async Task DownloadAndStoreComponentArchive(string componentName)
        {
            var tempDir = _context.GetTempDir();
            var archiveFileName = _context.ArchiveSpec.GetComponentArchive(componentName);
            var tempFilePath = Path.Combine(tempDir, archiveFileName);
            var existingArchiveStorePath = _context.GetComponentArchiveStoreCurrentFile(componentName);
            var newArchiveStorePath = _context.GetComponentArchiveStoreFilePerSpec(componentName);
            var downloadUrl = _context.ArchiveSpec.GetComponentArchiveUrl(componentName);

            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                await DownloadUtil.DownloadFileAsync(downloadUrl, tempFilePath);

                if (existingArchiveStorePath != null && File.Exists(newArchiveStorePath))
                {
                    File.Delete(newArchiveStorePath);
                }

                File.Move(tempFilePath, newArchiveStorePath);
            }
            catch (Exception)
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                throw;
            }
        }

        private static bool IsDirectory(ZipArchiveEntry entry)
        {
            return string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith("/");
        }

        public async Task ScanComponentArchive(string componentName, string subdirectory, Action<string, ZipArchiveEntry, bool> callback)
        {
            var archiveStorePath = _context.GetComponentArchiveStoreCurrentFile(componentName);

            if (archiveStorePath == null)
            {
                return; // throw new InvalidOperationException($"No archive found for component {componentName}");
            }

            bool scanSubirectoryOnly = !string.IsNullOrEmpty(subdirectory);
            using (var archive = ZipFile.OpenRead(archiveStorePath))
            {
                foreach (var entry in archive.Entries)
                {
                    var entryPath = entry.FullName;

                    if (scanSubirectoryOnly)
                    {
                        if (entryPath.StartsWith(subdirectory + "/", StringComparison.OrdinalIgnoreCase))
                        {
                            entryPath = entryPath.Substring(subdirectory.Length + 1);
                            // Skip if the entry path is empty (means it's the subdirectory itself)
                            if (string.IsNullOrEmpty(entryPath))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    bool isDirectory = IsDirectory(entry);
                    callback(entryPath, entry, isDirectory);
                }
            }
        }

        public async Task BackupOverlappingFiles(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFilesDir(componentName, true);

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry, isDirectory) =>
            {
                var sourceFilePath = Path.Combine(steamValheimDir, relativeFilePath);
                var backupFilePath = Path.Combine(origValheimFilesDir, relativeFilePath);

                try
                {
                    if (File.Exists(sourceFilePath))
                    {
                        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                        using (var entryStream = entry.Open())
                        {
                            if (!StreamsAreEqual(sourceStream, entryStream))
                            {
                                var backupDir = Path.GetDirectoryName(backupFilePath);
                                if (!Directory.Exists(backupDir))
                                {
                                    Directory.CreateDirectory(backupDir);
                                }

                                File.Copy(sourceFilePath, backupFilePath, true);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                    throw;
                }
            });
        }

        public async Task CopyComponentFilesIntoPlace(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry, isDirectory) =>
            {
                var destinationFilePath = Path.Combine(steamValheimDir, relativeFilePath);

                try
                {
                    if (isDirectory)
                    {
                        if (!Directory.Exists(destinationFilePath))
                        {
                            Directory.CreateDirectory(destinationFilePath);
                        }
                    }
                    else // is File
                    {
                        var destinationDir = Path.GetDirectoryName(destinationFilePath);
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        entry.ExtractToFile(destinationFilePath, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to copy {relativeFilePath}: {ex.Message}");
                    throw;
                }
            });
        }

        // Is this needed anymore?
        public async Task CheckUpdateOverlappingFiles(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFilesDir(componentName, true);

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry, isDirectory) =>
            {
                var sourceFilePath = Path.Combine(steamValheimDir, relativeFilePath);
                var backupFilePath = Path.Combine(origValheimFilesDir, relativeFilePath);

                try
                {
                    if (File.Exists(sourceFilePath))
                    {
                        using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                        using (var entryStream = entry.Open())
                        {
                            if (!StreamsAreEqual(sourceStream, entryStream))
                            {
                                var backupDir = Path.GetDirectoryName(backupFilePath);
                                if (!Directory.Exists(backupDir))
                                {
                                    Directory.CreateDirectory(backupDir);
                                }

                                File.Copy(sourceFilePath, backupFilePath, true);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    if (File.Exists(backupFilePath))
                    {
                        File.Delete(backupFilePath);
                    }
                    throw;
                }
            });
        }

        public async Task CheckUninstallComponentArchive(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFilesDir(componentName, true);

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry, isDirectory) =>
            {
                if (isDirectory)
                {
                    return;
                }

                var backupFilePath = Path.Combine(origValheimFilesDir, relativeFilePath);
                var destinationFilePath = Path.Combine(steamValheimDir, relativeFilePath);

                try
                {
                    bool hasBackup = File.Exists(backupFilePath);
                    bool hasDestination = File.Exists(destinationFilePath);

                    // Skip if no backup exists and no destination file exists
                    if (!hasBackup && !hasDestination)
                    {
                        return;
                    }

                    // Check if destination file matches the zip entry
                    if (hasDestination)
                    {
                        bool destinationMatchesZip = false;
                        using (var destStream = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Read))
                        using (var entryStream = entry.Open())
                        {
                            destinationMatchesZip = StreamsAreEqual(destStream, entryStream);
                        }

                        // If destination exists but doesn't match zip, leave it alone
                        if (!destinationMatchesZip)
                        {
                            return;
                        }
                    }

                    // At this point either:
                    // 1. Destination matches zip and should be replaced/deleted
                    // 2. Destination doesn't exist
                    
                    if (hasBackup)
                    {
                        // Restore from backup
                        var destinationDir = Path.GetDirectoryName(destinationFilePath);
                        Directory.CreateDirectory(destinationDir);

                        try
                        {
                            File.Move(backupFilePath, destinationFilePath);
                        }
                        catch (IOException)
                        {
                            File.Copy(backupFilePath, destinationFilePath, true);
                            File.Delete(backupFilePath);
                        }
                    }
                    else if (hasDestination)
                    {
                        // No backup but destination matches zip - delete it
                        File.Delete(destinationFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to restore {relativeFilePath}: {ex.Message}");
                    throw;
                }
            });
        }

        // TODO: Should we remove this?
        public async Task CheckUninstallAndDeleteComponentArchive(string componentName, string subdirectory)
        {
            try
            {
                await CheckUninstallComponentArchive(componentName, subdirectory);
                var archiveStorePath = _context.GetComponentArchiveStoreCurrentFile(componentName);
                if (archiveStorePath != null && File.Exists(archiveStorePath))
                {
                    File.Delete(archiveStorePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to uninstall component archive for {componentName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Compares two streams for equality. Assumes both streams are positioned at their start (position 0).
        /// </summary>
        /// <param name="stream1">First stream to compare</param>
        /// <param name="stream2">Second stream to compare</param>
        /// <returns>True if the streams contain identical bytes</returns>
        private static bool StreamsAreEqual(Stream stream1, Stream stream2)
        {
            const int bufferSize = 81920;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];
            
            while (true)
            {
                int bytesRead1 = ReadToFill(stream1, buffer1);
                int bytesRead2 = ReadToFill(stream2, buffer2);
                
                // If we get different amounts of data, streams are not equal
                if (bytesRead1 != bytesRead2)
                {
                    return false;
                }
                
                // If both streams are at EOF, and we've matched all bytes so far, streams are equal
                if (bytesRead1 == 0)
                {
                    return true;
                }
                
                // Compare the bytes we read
                for (int i = 0; i < bytesRead1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        return false;
                    }
                }
            }
        }

        private static int ReadToFill(Stream stream, byte[] buffer)
        {
            int totalRead = 0;
            while (totalRead < buffer.Length)
            {
                int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                if (read == 0) break; // EOF
                totalRead += read;
            }
            return totalRead;
        }

        public static bool CompareArchiveToDirectory(string archivePath, string targetDirectory, string archiveSubdirectory)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                // Filter and transform archive entries if subdirectory is specified
                var archiveEntries = archive.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name)) // Filter out directory entries
                    .Where(e => string.IsNullOrEmpty(archiveSubdirectory) || 
                               e.FullName.StartsWith(archiveSubdirectory + "/", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        e => string.IsNullOrEmpty(archiveSubdirectory) 
                            ? e.FullName 
                            : e.FullName.Substring(archiveSubdirectory.Length + 1),
                        e => e
                    );

                var directoryFiles = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories)
                                            .ToDictionary(
                                                f => Path.GetRelativePath(targetDirectory, f).Replace('\\', '/'),
                                                f => f
                                            );

                // Check if all files in the directory match the archive
                foreach (var file in directoryFiles)
                {
                    if (!archiveEntries.TryGetValue(file.Key, out var entry))
                    {
                        return false; // Extra file in the directory
                    }

                    using (var fileStream = new FileStream(file.Value, FileMode.Open, FileAccess.Read))
                    using (var entryStream = entry.Open())
                    {
                        if (!StreamsAreEqual(fileStream, entryStream))
                        {
                            return false; // File data does not match
                        }
                    }
                }

                // Check if there are any extra files in the archive
                foreach (var entry in archiveEntries)
                {
                    if (!directoryFiles.ContainsKey(entry.Key))
                    {
                        return false; // Extra file in the archive
                    }
                }
            }

            return true;
        }

        public void TouchFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }
        }

        public void CreateDirectoryIfNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void CreateFileParentDirectoryIfNotExists(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            CreateDirectoryIfNotExists(directoryPath);
        }

        public void RecreateDirectoryAsEmpty(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
            Directory.CreateDirectory(directoryPath);
        }

        public void RecreateFileParentDirectoryAsEmpty(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            RecreateDirectoryAsEmpty(directoryPath);
        }

    }
}
