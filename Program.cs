// Program.cs
using System;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Valheim Ext Component Manager starting...");

                // Parse command line arguments

                var options = ProgramOptions.ParseArguments(args);

                if (options == null)
                {
                    // Parsing failed or --help was specified
                    Environment.Exit(1);
                }

                Console.WriteLine("Options:");
                Console.WriteLine($"\tExt Component Manager Check Update: {options.ExtComponentManagerCheckUpdate}");
                Console.WriteLine($"\tValheim VR Check Update: {options.ValheimVrCheckUpdate}");
                Console.WriteLine($"\tValheim Start: {options.ValheimStart}");
                Console.WriteLine($"\tVoice Commander Check Update: {options.VoiceCommanderCheckUpdate}");
                Console.WriteLine($"\tVoice Commander Manage: {options.VoiceCommanderManage}");

                if (options.ExtComponentManagerCheckUpdate == null)
                {
                    Console.WriteLine("No options specified - thus now work to do.");
                    Environment.Exit(1);
                }

                // Validate directory ancestry

                string expectedDirAncestry = Path.Combine("Components", "ValheimExtComponentManager", "Current");
#if DEBUG
                if (options.ExtComponentManagerUseInstallRoot != null)
                {
                    string useCurrentDirectory = Path.Combine(options.ExtComponentManagerUseInstallRoot, expectedDirAncestry);
                    Directory.SetCurrentDirectory(useCurrentDirectory);
                }
#endif
                string currentDir = Directory.GetCurrentDirectory();

                if (!currentDir.EndsWith(expectedDirAncestry))
                {
                    Console.WriteLine("Directory ancestry check failed. Exiting.");
                    Environment.Exit(1);
                }

                string managementInstallDir = Path.GetFullPath(Path.Combine(currentDir, @"..\..\.."));

                // Initialize component manager

                string? steamValheimDir = SteamUtils.LookupAppInstallPath("Valheim");
                if (steamValheimDir == null)
                {
                    Console.WriteLine("Valheim path not found.");
                    Environment.Exit(1);
                }
                Console.WriteLine("Valheim path is: " + steamValheimDir);

                string componentSpecUrl = "https://www.dropbox.com/scl/fi/mlwo51czhp6d6jhpe7h2h/manager-config.spec?rlkey=v0yfq9gi0343f725bua5p5ris&st=uwfkav6e&dl=1";
#if DEBUG
                if (options.ExtComponentManagerUseComponentSpecUrl != null)
                {
                    componentSpecUrl = options.ExtComponentManagerUseComponentSpecUrl;
                }
#endif
                ComponentArchiveSpec componentArchiveSpec = await ComponentArchiveSpec.PullSpec(componentSpecUrl);

                ComponentManageContext componentManageContext = new ComponentManageContext(options, managementInstallDir, steamValheimDir, componentArchiveSpec);

                if (componentManageContext.Options.ExtComponentManagerCheckUpdate != "No")
                {
                    CheckUpdateExtComponentManager(componentManageContext, args).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private static async Task CheckUpdateExtComponentManager(ComponentManageContext componentManageContext, string[] args)
        {
            string componentName = "ValheimExtComponentManager";

            Console.WriteLine("Checking component " + componentName + " for updates...");

            string componentDir = componentManageContext.GetComponentDir(componentName);
            string currentInstallDir = Path.Combine(componentDir, "Current");
            string installedFlagFile = Path.Combine(componentDir, "Archive-Installed");

            string currentArchivePath = componentManageContext.GetComponentArchiveStoreCurrentFile(componentName);
            string newArchiveTargetPath = componentManageContext.GetComponentArchiveStoreFilePerSpec(componentName);

            if (newArchiveTargetPath == null)
            {
                Console.WriteLine("No archive available in repository.");
            }

            bool newArchiveAvailable = newArchiveTargetPath != null && newArchiveTargetPath != currentArchivePath;

            // Only update if a new archive is available
            if (newArchiveAvailable)
            {
                Console.WriteLine("New component archive available...");

                string newArchiveSourceUrl = componentManageContext.ArchiveSpec.GetComponentArchiveUrl(componentName);
                string newArchiveFilename = componentManageContext.GetComponentArchiveName(componentName);
                string newArchiveTempDownloadPath = componentManageContext.GetTempFilePath(newArchiveFilename);

                Console.WriteLine("Clearing prev archive / install flag / temp download...");

                if (File.Exists(installedFlagFile))
                {
                    File.Delete(installedFlagFile);
                }
                if (currentArchivePath != null && File.Exists(currentArchivePath))
                {
                    File.Delete(currentArchivePath);
                }
                if (File.Exists(newArchiveTempDownloadPath))
                {
                    File.Delete(newArchiveTempDownloadPath);
                }

                // Download and move new zip into archive directory
                Console.WriteLine("Downloading new archive from: " + newArchiveSourceUrl + " to: " + newArchiveTempDownloadPath);
                await DownloadUtil.DownloadFileAsync(newArchiveSourceUrl, newArchiveTempDownloadPath);

                Console.WriteLine("Moving new archive to: " + newArchiveTargetPath);
                File.Move(newArchiveTempDownloadPath, newArchiveTargetPath);
            }
            else // !newArchiveAvailable
            {
                // Checking if last install succeeded
                if (!File.Exists(installedFlagFile) && File.Exists(currentArchivePath))
                {
                    Console.WriteLine("Checing if current install matches current archive...");

                    bool matchesArchive = ComponentFileOps.CompareArchiveToDirectory(currentArchivePath, currentInstallDir);
                    if (matchesArchive)
                    {
                        ComponentFileOps.TouchFile(installedFlagFile);
                    }
                }
            }

            if (!File.Exists(installedFlagFile) && newArchiveTargetPath != null && File.Exists(newArchiveTargetPath))
            {
                Console.WriteLine("New archive available and not installed. Installing...");

                string newInstallDir = Path.Combine(componentDir, "New");
                string newInstallTempDir = Path.Combine(componentDir, "New.Temp"); // Possibly put under the temp dir

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
                ComponentFileOps.UnzipSubdirectoryToTarget(newArchiveTargetPath, "ValheimECM/Components/ValheimExtComponentManager/Current", newInstallTempDir);

                // Rename new-temp to new
                Console.WriteLine("Moving new-temp to new...");
                Directory.Move(newInstallTempDir, newInstallDir);

                // Collect and store params
                var paramsFilePath = Path.Combine(componentDir, "ValheimExtComponentManager.Recall.Params.TEMP");
                var filteredArgs = new List<string>();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--ext-component-manager::check-update")
                    {
                        i++; // Skip the next argument as well
                    }
                    else
                    {
                        filteredArgs.Add(args[i]);
                    }
                }
                filteredArgs.Add("--ext-component-manager::check-update");
                filteredArgs.Add("false");
                var paramsContent = string.Join(" ", filteredArgs);
                File.WriteAllText(paramsFilePath, paramsContent);

                // Exit to allow check-update script to update and restart
                Environment.Exit(0);
            }
        }
    }
}