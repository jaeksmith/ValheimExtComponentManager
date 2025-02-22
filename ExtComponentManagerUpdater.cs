using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public class ExtComponentManagerUpdater
    {
        private readonly string _componentName = "ValheimExtComponentManager";
        private const string ARCHIVE_CURRENT_SUBDIRECTORY = "ValheimECM/Components/ValheimExtComponentManager/Current";

        private readonly ComponentManageContext _componentManageContext;
        private readonly string[] _args;

        private readonly string _componentDir;
        private readonly string _currentInstallDir;
        private readonly string _installedFlagFile;
        private readonly string _currentArchivePath;
        private readonly string _newArchiveTargetPath;

        private ExtComponentManagerUpdater(ComponentManageContext componentManageContext, string[] args)
        {
            _componentManageContext = componentManageContext;
            _args = args;
            _componentDir = _componentManageContext.GetComponentDir(_componentName);
            _currentInstallDir = Path.Combine(_componentDir, "Current");
            _installedFlagFile = Path.Combine(_componentDir, "Archive-Installed");
            _currentArchivePath = _componentManageContext.GetComponentArchiveStoreCurrentFile(_componentName);
            _newArchiveTargetPath = _componentManageContext.GetComponentArchiveStoreFilePerSpec(_componentName);
        }

        private async Task CheckUpdate()
        {
            Console.WriteLine("Checking component " + _componentName + " for updates...");

            if (_newArchiveTargetPath == null)
            {
                Console.WriteLine("No archive available in repository.");
                return;
            }

            bool newArchiveAvailable = _newArchiveTargetPath != null && _newArchiveTargetPath != _currentArchivePath;

            if (newArchiveAvailable)
            {
                await DownloadNewArchive();
            }
            else
            {
                // If the install flag is not present, try verifying the current install (and setting the flag if it matches)
                CheckVerifyCurrentInstall();
            }

            if (!File.Exists(_installedFlagFile) && _newArchiveTargetPath != null && File.Exists(_newArchiveTargetPath))
            {
                await InstallNewArchive();
            }
        }

        private async Task DownloadNewArchive()
        {
            Console.WriteLine("New component archive available...");

            string newArchiveSourceUrl = _componentManageContext.ArchiveSpec.GetComponentArchiveUrl(_componentName);
            string newArchiveFilename = _componentManageContext.GetComponentArchiveName(_componentName);
            string newArchiveTempDownloadPath = _componentManageContext.GetTempFilePath(newArchiveFilename);

            Console.WriteLine("Clearing prev archive / install flag / temp download...");

            if (File.Exists(_installedFlagFile))
            {
                File.Delete(_installedFlagFile);
            }
            if (_currentArchivePath != null && File.Exists(_currentArchivePath))
            {
                File.Delete(_currentArchivePath);
            }
            if (File.Exists(newArchiveTempDownloadPath))
            {
                File.Delete(newArchiveTempDownloadPath);
            }

            // Download and move new zip into archive directory
            Console.WriteLine("Downloading new archive from: " + newArchiveSourceUrl + " to: " + newArchiveTempDownloadPath);
            await DownloadUtil.DownloadFileAsync(newArchiveSourceUrl, newArchiveTempDownloadPath);

            Console.WriteLine("Moving new archive to: " + _newArchiveTargetPath);
            File.Move(newArchiveTempDownloadPath, _newArchiveTargetPath);
        }

        private bool CheckVerifyCurrentInstall()
        {
            // Checking if last install succeeded
            if (File.Exists(_installedFlagFile))
            {
                return true;
            }

            if (!File.Exists(_currentArchivePath))
            {
                Console.WriteLine("Install not verified due to no current archive.");
                return false;
            }

            Console.WriteLine("Checking if current install matches current archive...");

            bool matchesArchive = ComponentFileOps.CompareArchiveToDirectory(_currentArchivePath, _currentInstallDir, ARCHIVE_CURRENT_SUBDIRECTORY);

            if (!matchesArchive)
            {
                Console.WriteLine("Current archive does not match current install.");
                return false;
            }

            ComponentFileOps.TouchFile(_installedFlagFile);
            return true;
        }

        private void CheckWarnIfCurrentInstallIsNotVerified()
        {
            Console.WriteLine("Verifying current install...");

            if (!CheckVerifyCurrentInstall())
            {
                Console.WriteLine("WARNING: Current archive not present or not installed.  Suggest updating when possible!");
                return;
            }

            Console.WriteLine("Current archive installed.");

            CheckRemovePrevInstallDir();
        }

        private async Task InstallNewArchive()
        {
            Console.WriteLine("New archive available and not installed. Installing...");

            string newInstallDir = Path.Combine(_componentDir, "New");
            string newInstallTempDir = Path.Combine(_componentDir, "New.Temp"); // Possibly put under the temp dir

            if (Directory.Exists(newInstallDir))
            {
                Console.WriteLine("Removing old/abandoned new dir...");
                Directory.Delete(newInstallDir, recursive: true);
            }

            if (Directory.Exists(newInstallTempDir))
            {
                Console.WriteLine("Removing old/abandoned new-temp dir...");
                Directory.Delete(newInstallTempDir, recursive: true);
            }

            // Unzip archive to new-temp directory
            Console.WriteLine("Extracting new archive to: " + newInstallTempDir);
            ComponentFileOps.UnzipSubdirectoryToTarget(_newArchiveTargetPath, ARCHIVE_CURRENT_SUBDIRECTORY, newInstallTempDir);

            // Rename new-temp to new
            Console.WriteLine("Moving new-temp to new...");
            Directory.Move(newInstallTempDir, newInstallDir);

            // Be sure no old install dir is present to conflict with swapping out current version
            CheckRemovePrevInstallDir(); 

            // Collect and store params
            var paramsFilePath = Path.Combine(_componentDir, "ValheimExtComponentManager.Recall.Params.TEMP");
            var filteredArgs = new List<string>();
            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i] == "--ext-component-manager::check-update")
                {
                    i++; // Skip the next argument as well
                }
                else
                {
                    filteredArgs.Add(_args[i]);
                }
            }
            filteredArgs.Add("--ext-component-manager::check-update");
            filteredArgs.Add("false");
            var paramsContent = string.Join(" ", filteredArgs);
            File.WriteAllText(paramsFilePath, paramsContent);

            // Exit to allow check-update script to update and restart
            Environment.Exit(0);
        }

        private void CheckRemovePrevInstallDir()
        {
            string oldInstallDir = Path.Combine(_componentDir, "Old");
            if (Directory.Exists(oldInstallDir))
            {
                Console.WriteLine("Removing prev install...");
                Directory.Delete(oldInstallDir, recursive: true);
            }
        }

        public static async Task PerformManagementProcessing(ComponentManageContext componentManageContext, string[] args)
        {
            if (componentManageContext.Options.ExtComponentManagerCheckUpdate == "no")
            {
                return;
            }

            var updater = new ExtComponentManagerUpdater(componentManageContext, args);

            if (componentManageContext.Options.ExtComponentManagerCheckUpdate == "yes")
            {
                await updater.CheckUpdate();
            }
            else if (componentManageContext.Options.ExtComponentManagerCheckUpdate == "check-install-only")
            {
                updater.CheckWarnIfCurrentInstallIsNotVerified();
            }
            else
            {
                throw new ArgumentException("Invalid check-update option: " + componentManageContext.Options.ExtComponentManagerCheckUpdate);
            }
        }
    }
}