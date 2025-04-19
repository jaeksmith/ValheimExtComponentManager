using System;
using System.IO;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public class ValheimModUpdater
    {
        protected readonly string _componentName;
        protected readonly string _subDirectoryInArchiveToInstall;

        protected readonly string _componentDir;
        protected readonly string _installedFlagFile;
        protected readonly string _currentArchivePath;
        protected readonly string _newArchiveTargetPath;
        protected readonly string _newArchiveTempDirPath;
        protected readonly string _newArchiveTempFilePath;
        protected readonly string _origValheimFilesDir;

        protected readonly ComponentManageContext _componentManageContext;

        public ValheimModUpdater(ComponentManageContext componentManageContext, string componentName, string subDirectoryInArchiveToInstall)
        {
            _componentName = componentName;
            _subDirectoryInArchiveToInstall = subDirectoryInArchiveToInstall;
            
            _componentManageContext = componentManageContext;
            _componentDir = _componentManageContext.GetComponentDir(_componentName);
            _installedFlagFile = Path.Combine(_componentDir, "Component-Installed");

            _currentArchivePath = _componentManageContext.GetComponentArchiveStoreCurrentFile(_componentName);
            _newArchiveTargetPath = _componentManageContext.GetComponentArchiveStoreFilePerSpec(_componentName);

            string newArchiveFilename = _componentManageContext.GetComponentArchiveName(_componentName);
            _newArchiveTempDirPath = Path.Combine(_componentDir, "Archive-New");
            _newArchiveTempFilePath = Path.Combine(_newArchiveTempDirPath, newArchiveFilename);

            _origValheimFilesDir = _componentManageContext.GetOrigValheimFilesDir(_componentName, false);
        }

        public string GetComponentName()
        {
            return _componentName;
        }

        protected void DeleteOrigFilesDirIfExists()
        {
            if (Directory.Exists(_origValheimFilesDir))
            {
                Directory.Delete(_origValheimFilesDir, recursive: true);
            }
        }

        protected void PreparatoryClean()
        {
            if (Directory.Exists(_newArchiveTempDirPath))
            {
                Console.WriteLine("Removing old/abandoned new-archive-temp dir...");
                Directory.Delete(_newArchiveTempDirPath, recursive: true);
            }
        }

        protected async Task<bool> CheckDownloadUpdate()
        {
            Console.WriteLine("Checking component " + _componentName + " for updates...");

            if (_newArchiveTargetPath == null)
            {
                Console.WriteLine("No archive available in repository.");
                return false;
            }

            bool newArchiveAvailable = _newArchiveTargetPath != null && _newArchiveTargetPath != _currentArchivePath;

            if (newArchiveAvailable)
            {
                await DownloadNewArchive();
                return true;
            }

            return false;
        }

        protected async Task DownloadNewArchive()
        {
            Console.WriteLine("New component archive available...");

            string newArchiveSourceUrl = _componentManageContext.ArchiveSpec.GetComponentArchiveUrl(_componentName);
            _componentManageContext.ComponentFileOps.RecreateDirectoryAsEmpty(_newArchiveTempDirPath);

            Console.WriteLine("Downloading new archive from: " + newArchiveSourceUrl + " to: " + _newArchiveTempDirPath);
            await DownloadUtil.DownloadFileAsync(newArchiveSourceUrl, _newArchiveTempFilePath);
        }

        protected async Task UninstallArchive()
        {
            await _componentManageContext.ComponentFileOps.CheckUninstallComponentArchive(_componentName, _subDirectoryInArchiveToInstall);

            if (File.Exists(_installedFlagFile))
            {
                File.Delete(_installedFlagFile);
            }

            DeleteOrigFilesDirIfExists();
        }

        protected void MoveNewArchiveIntoPlace()
        {
            if (File.Exists(_currentArchivePath))
            {
                File.Delete(_currentArchivePath);
            }

            _componentManageContext.ComponentFileOps.CreateFileParentDirectoryIfNotExists(_newArchiveTargetPath);

            File.Move(_newArchiveTempFilePath, _newArchiveTargetPath);

            Directory.Delete(_newArchiveTempDirPath);
        }

        protected async Task InstallArchive()
        {
            DeleteOrigFilesDirIfExists();

            await _componentManageContext.ComponentFileOps.BackupOverlappingFiles(_componentName, _subDirectoryInArchiveToInstall);

            await _componentManageContext.ComponentFileOps.CopyComponentFilesIntoPlace(_componentName, _subDirectoryInArchiveToInstall);

            _componentManageContext.ComponentFileOps.TouchFile(_installedFlagFile);
        }

        public async Task CheckImplementInstalledState(bool checkDownload, ComponentState requestedTargetInstallState)
        {
            bool noOp = (!checkDownload && requestedTargetInstallState == ComponentState.Maintain);
            if (noOp)
            {
                return;
            }

            // Remove incomplete states, if present
            PreparatoryClean();

            bool newArchiveDownloaded = false;
            if (checkDownload)
            {
                newArchiveDownloaded = await CheckDownloadUpdate();
            }

            ComponentState currentState = (_currentArchivePath != null && File.Exists(_installedFlagFile)) ? ComponentState.Installed : ComponentState.Uninstalled;
            ComponentState targetState = (requestedTargetInstallState == ComponentState.Maintain) ? currentState : requestedTargetInstallState;
            bool reinstall = newArchiveDownloaded && (currentState == ComponentState.Installed && targetState == ComponentState.Installed);
            bool uninstall = reinstall || (targetState == ComponentState.Uninstalled && currentState == ComponentState.Installed);
            bool install = reinstall || (targetState == ComponentState.Installed && currentState == ComponentState.Uninstalled);

            if (uninstall)
            {
                await UninstallArchive();
            }

            if (newArchiveDownloaded)
            {
                MoveNewArchiveIntoPlace();
            }

            if (install)
            {
                await InstallArchive();
            }
        }
    }
}
