using System;

namespace ValheimExtComponentManager
{
    public class ComponentManageContext
    {
        public ProgramOptions Options { get; set; }
        public string ManagementInstallDir { get; set; }
        public string SteamValheimDir { get; set; }
        public ComponentArchiveSpec ArchiveSpec { get; set; }
        public ComponentFileOps ComponentFileOps { get; set; }

        public ComponentManageContext(ProgramOptions options, string managementInstallDir, string steamValheimDir, ComponentArchiveSpec archiveSpec)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            ManagementInstallDir = managementInstallDir ?? throw new ArgumentNullException(nameof(managementInstallDir));
            SteamValheimDir = steamValheimDir ?? throw new ArgumentNullException(nameof(steamValheimDir));
            ArchiveSpec = archiveSpec ?? throw new ArgumentNullException(nameof(archiveSpec));
            ComponentFileOps = new ComponentFileOps(this);
        }

        public string GetComponentDir(string componentName)
        {
            var componentDir = System.IO.Path.Combine(ManagementInstallDir, "Components", componentName);
            System.IO.Directory.CreateDirectory(componentDir);
            return componentDir;
        }

        public string GetComponentArchiveStoreDir(string componentName)
        {
            var componentDir = GetComponentDir(componentName);
            var archiveDir = System.IO.Path.Combine(componentDir, "Archive");
            System.IO.Directory.CreateDirectory(archiveDir);
            return archiveDir;
        }

        public string GetComponentArchiveStoreCurrentFile(string componentName)
        {
            var archiveDir = GetComponentArchiveStoreDir(componentName);
            var files = System.IO.Directory.GetFiles(archiveDir);

            if (files.Length == 0)
            {
                return null;
            }
            else if (files.Length > 1)
            {
                throw new InvalidOperationException("More than one file found in the archive directory.");
            }

            return files[0];
        }

        public string GetComponentArchiveStoreFilePerSpec(string componentName)
        {
            var archiveDir = GetComponentArchiveStoreDir(componentName);
            var archiveFile = System.IO.Path.Combine(archiveDir, GetComponentArchiveName(componentName));
            return archiveFile;
        }

        public string GetComponentArchiveName(string componentName)
        {
            return ArchiveSpec.GetComponentArchive(componentName);
        }

        public string GetOrigValheimFilesDir(string componentName, bool createIfNotExists)
        {
            var componentDir = GetComponentDir(componentName);
            var origValheimFilesDir = System.IO.Path.Combine(componentDir, "OrigValheimFiles");
            if (createIfNotExists)
            {
                System.IO.Directory.CreateDirectory(origValheimFilesDir);
            }
            return origValheimFilesDir;
        }

        public string GetTempDir()
        {
            var tempDir = System.IO.Path.Combine(ManagementInstallDir, "Temp");
            System.IO.Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        public string GetTempFilePath(string filename)
        {
            var tempDir = GetTempDir();
            var timeFilePath = System.IO.Path.Combine(tempDir, filename);
            return timeFilePath;
        }
    }
}
