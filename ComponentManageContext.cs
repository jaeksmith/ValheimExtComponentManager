using System;

namespace ValheimExtComponentManager
{
    public class ComponentManageContext
    {
        public ProgramOptions Options { get; set; }
        public string ManagementInstallDir { get; set; }
        public string SteamValheimDir { get; set; }
        public ComponentArchiveSpec ArchiveSpec { get; set; }

        public ComponentManageContext(ProgramOptions options, string managementInstallDir, string steamValheimDir, ComponentArchiveSpec archiveSpec)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            ManagementInstallDir = managementInstallDir ?? throw new ArgumentNullException(nameof(managementInstallDir));
            SteamValheimDir = steamValheimDir ?? throw new ArgumentNullException(nameof(steamValheimDir));
            ArchiveSpec = archiveSpec ?? throw new ArgumentNullException(nameof(archiveSpec));
        }

        public string GetComponentDir(string componentName)
        {
            var componentDir = System.IO.Path.Combine(ManagementInstallDir, "Components", componentName);
            System.IO.Directory.CreateDirectory(componentDir);
            return componentDir;
        }

        public string GetComponentArchiveStorePath(string componentName)
        {
            var componentDir = GetComponentDir(componentName);
            var archiveDir = System.IO.Path.Combine(componentDir, "Archive");
            System.IO.Directory.CreateDirectory(archiveDir);
            return System.IO.Path.Combine(archiveDir, ArchiveSpec.GetComponentArchive(componentName));
        }

        public string GetTempDir()
        {
            var tempDir = System.IO.Path.Combine(ManagementInstallDir, "Temp");
            System.IO.Directory.CreateDirectory(tempDir);
            return tempDir;
        }

        public string GetOrigValheimFiles()
        {
            var origValheimFilesDir = System.IO.Path.Combine(ManagementInstallDir, "OrigValheimFiles");
            System.IO.Directory.CreateDirectory(origValheimFilesDir);
            return origValheimFilesDir;
        }
    }
}