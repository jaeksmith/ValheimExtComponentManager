using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public class BepInExValheimModUpdater
    {
        private readonly string _componentName = "BepInExPack_Valheim";
        private readonly string _SubDirectoryInArchiveToInstall = "BepInExPack_Valheim";

        public const int STATE_MAINTAIN = 0;
        public const int STATE_INSTALLED = 1;
        public const int STATE_UNINSTALLED = 2;

        private readonly string _componentDir;
        private readonly string _installedFlagFile;
        private readonly string _currentArchivePath;
        private readonly string _newArchiveTargetPath;
        private readonly string _newArchiveTempDirPath;
        private readonly string _newArchiveTempFilePath;
        private readonly string _origValheimFilesDir;

        private readonly ComponentManageContext _componentManageContext;

        private BepInExValheimModUpdater(ComponentManageContext componentManageContext)
        {
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

        private void DeleteOrigFilesDirIfExists()
        {
            if (Directory.Exists(_origValheimFilesDir))
            {
                Directory.Delete(_origValheimFilesDir, recursive: true);
            }
        }

        private void PreparatoryClean()
        {
            if (Directory.Exists(_newArchiveTempDirPath))
            {
                Console.WriteLine("Removing old/abandoned new-archive-temp dir...");
                Directory.Delete(_newArchiveTempDirPath, recursive: true);
            }
        }

        private async Task<bool> CheckDownloadUpdate()
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

        private async Task DownloadNewArchive()
        {
            Console.WriteLine("New component archive available...");

            string newArchiveSourceUrl = _componentManageContext.ArchiveSpec.GetComponentArchiveUrl(_componentName);
//            string newArchiveFilename = _componentManageContext.GetComponentArchiveName(_componentName);

            // if (_currentArchivePath != null && File.Exists(_currentArchivePath))
            // {
            //     File.Delete(_currentArchivePath);
            // }
            // if (File.Exists(_newArchiveTempDirPath))
            // {
            //     File.Delete(_newArchiveTempDirPath);
            // }

            _componentManageContext.ComponentFileOps.RecreateDirectoryAsEmpty(_newArchiveTempDirPath);

            // // Download and move new zip into archive directory
            Console.WriteLine("Downloading new archive from: " + newArchiveSourceUrl + " to: " + _newArchiveTempDirPath);
            await DownloadUtil.DownloadFileAsync(newArchiveSourceUrl, _newArchiveTempFilePath);

            // Console.WriteLine("Moving new archive to: " + _newArchiveTargetPath);
            // File.Move(newArchiveTempDownloadPath, _newArchiveTargetPath);
        }

        private async Task UninstallArchive()
        {
            await _componentManageContext.ComponentFileOps.CheckUninstallComponentArchive(_componentName, _SubDirectoryInArchiveToInstall);

            if (File.Exists(_installedFlagFile))
            {
                File.Delete(_installedFlagFile);
            }

            DeleteOrigFilesDirIfExists();
        }

        private void MoveNewArchiveIntoPlace()
        {
            if (File.Exists(_currentArchivePath))
            {
                File.Delete(_currentArchivePath);
            }

            _componentManageContext.ComponentFileOps.CreateFileParentDirectoryIfNotExists(_newArchiveTargetPath);

            File.Move(_newArchiveTempFilePath, _newArchiveTargetPath);

            Directory.Delete(_newArchiveTempDirPath);
        }

        private async Task InstallArchive()
        {
            DeleteOrigFilesDirIfExists();

            await _componentManageContext.ComponentFileOps.BackupOverlappingFiles(_componentName, _SubDirectoryInArchiveToInstall);

            await _componentManageContext.ComponentFileOps.CopyComponentFilesIntoPlace(_componentName, _SubDirectoryInArchiveToInstall);

            _componentManageContext.ComponentFileOps.TouchFile(_installedFlagFile);
        }

        public async Task CheckImplementInstalledState(bool checkDownload, int requestedTargetInstallState)
        {
            bool noOp = (!checkDownload && requestedTargetInstallState == STATE_MAINTAIN);
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

            int currentState = (_currentArchivePath != null && File.Exists(_installedFlagFile)) ? STATE_INSTALLED : STATE_UNINSTALLED;
            int targetState = (requestedTargetInstallState == STATE_MAINTAIN) ? currentState : requestedTargetInstallState;
            bool reinstall = newArchiveDownloaded && (currentState == STATE_INSTALLED && targetState == STATE_INSTALLED);
            bool uninstall = reinstall || (targetState == STATE_UNINSTALLED && currentState == STATE_INSTALLED);
            bool install = reinstall || (targetState == STATE_INSTALLED && currentState == STATE_UNINSTALLED);

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

        public static async Task PerformManagementProcessing(ComponentManageContext componentManageContext)
        {
            bool checkDownload = (componentManageContext.Options.ValheimVrCheckUpdate == "yes");

            int targetInsallState;
            switch (componentManageContext.Options.ValheimVrEnabled)
            {
                case "yes":  targetInsallState = STATE_INSTALLED;  break;
                case "no":  targetInsallState = STATE_UNINSTALLED;  break;
                case null:  targetInsallState = STATE_MAINTAIN;  break;
                default:  throw new InvalidOperationException("Invalid Valheim VR enabled state.");
            }

            // // BepInExValheimModUpdater updater = new BepInExValheimModUpdater(componentManageContext);

            // // bool checkInstall;
            // // bool checkUninstall;
            // // if (setEnabled != null)
            // // {
            // //     checkInstall = setEnabled.Value;
            // // }
            // // else
            // // {
            // //     checkInstall = IsMarkedInstalled();
            // //     checkInstall = false;
            // //     checkUninstall = false;
            // // }
            // // checkUninstall = !checkInstall;

            // // bool markedInstalled = ?;
            // // bool checkInstall = (setEnabled != null && setEnabled.Value);
            // // bool checkUninstall = !checkInstall && (setEnabled != null && !setEnabled.Value);

            // // if (!checkInstall && !checkUninstall)

            // // if (checkUpdate == false && setEnabled == null)
            // // {
            // //     return;
            // // }

            // BepInExValheimModUpdater updater = new BepInExValheimModUpdater(componentManageContext);

            // updater.PreparatoryClean();

            // bool updateAvailable = false;
            // if (checkDownload)
            // {
            //     updateAvailable = await updater.CheckDownloadUpdate();
            // }

            // // if (setEnabled == null)
            // // {
            // //     return;
            // // }
            // //
            // // bool checkInstall = setEnabled.Value;
            // // if (checkInstall)
            // // {
            // //     await updater.CheckInstall();
            // // }
            // // else
            // // {
            // //     await updater.CheckUninstall();
            // // }

            BepInExValheimModUpdater updater = new BepInExValheimModUpdater(componentManageContext);
            await updater.CheckImplementInstalledState(checkDownload, targetInsallState);
        }
    }
}