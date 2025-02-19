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

        public async Task ScanComponentArchive(string componentName, string subdirectory, Action<string, ZipArchiveEntry> callback)
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
                        }
                        else
                        {
                            continue;
                        }
                    }

                    callback(entryPath, entry);
                }
            }
        }

        public async Task BackupOverlappingFiles(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFiles();

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry) =>
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

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry) =>
            {
                var destinationFilePath = Path.Combine(steamValheimDir, relativeFilePath);

                try
                {
                    var destinationDir = Path.GetDirectoryName(destinationFilePath);
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    entry.ExtractToFile(destinationFilePath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to copy {relativeFilePath}: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task CheckUpdateOverlappingFiles(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFiles();

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry) =>
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

        public async Task RestoreOverlappingFiles(string componentName, string subdirectory)
        {
            var steamValheimDir = _context.SteamValheimDir;
            var origValheimFilesDir = _context.GetOrigValheimFiles();

            await ScanComponentArchive(componentName, subdirectory, (relativeFilePath, entry) =>
            {
                var backupFilePath = Path.Combine(origValheimFilesDir, relativeFilePath);
                var destinationFilePath = Path.Combine(steamValheimDir, relativeFilePath);

                try
                {
                    if (File.Exists(backupFilePath) && File.Exists(destinationFilePath))
                    {
                        using (var sourceStream = new FileStream(destinationFilePath, FileMode.Open, FileAccess.Read))
                        using (var entryStream = entry.Open())
                        {
                            if (StreamsAreEqual(sourceStream, entryStream))
                            {
                                var destinationDir = Path.GetDirectoryName(destinationFilePath);
                                if (!Directory.Exists(destinationDir))
                                {
                                    Directory.CreateDirectory(destinationDir);
                                }

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to restore {relativeFilePath}: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task CheckUninstallComponentArchive(string componentName, string subdirectory)
        {
            try
            {
                await RestoreOverlappingFiles(componentName, subdirectory);
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

        private static bool StreamsAreEqual(Stream stream1, Stream stream2)
        {
            const int bufferSize = 2048;
            var buffer1 = new byte[bufferSize];
            var buffer2 = new byte[bufferSize];

            while (true)
            {
                int count1 = stream1.Read(buffer1, 0, bufferSize);
                int count2 = stream2.Read(buffer2, 0, bufferSize);

                if (count1 != count2)
                {
                    return false;
                }

                if (count1 == 0)
                {
                    return true;
                }

                for (int i = 0; i < count1; i++)
                {
                    if (buffer1[i] != buffer2[i])
                    {
                        return false;
                    }
                }
            }
        }

        public static bool CompareArchiveToDirectory(string archivePath, string targetDirectory)
        {
            using (var archive = ZipFile.OpenRead(archivePath))
            {
                var archiveEntries = archive.Entries.ToDictionary(e => e.FullName, e => e);
                var directoryFiles = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories)
                                              .ToDictionary(f => f.Substring(targetDirectory.Length + 1).Replace("\\", "/"), f => f);

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

        public static void TouchFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }
        }
    }
}
