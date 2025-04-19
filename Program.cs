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

                // Check if any non-hidden options are specified
                bool hasNonHiddenOptions = 
                    !string.IsNullOrEmpty(options.ExtComponentManagerCheckUpdate) ||
                    !string.IsNullOrEmpty(options.ValheimVrCheckUpdate) ||
                    !string.IsNullOrEmpty(options.ValheimVrEnabled) ||
                    !string.IsNullOrEmpty(options.VoiceCommanderCheckUpdate) ||
                    !string.IsNullOrEmpty(options.VoiceCommanderManage) ||
                    !string.IsNullOrEmpty(options.ValheimStart);

                if (!hasNonHiddenOptions)
                {
                    Console.WriteLine("No options specified - thus no work to do.");
                    Environment.Exit(1);
                }

                // Validate directory ancestry

                string expectedDirAncestry = Path.Combine("Components", "ValheimExtComponentManager", "Current");

                if (options.ExtComponentManagerUseInstallRoot != null)
                {
                    string useCurrentDirectory = Path.Combine(options.ExtComponentManagerUseInstallRoot, expectedDirAncestry);
                    Directory.SetCurrentDirectory(useCurrentDirectory);
                }

                string currentDir = Directory.GetCurrentDirectory();

                if (!currentDir.EndsWith(expectedDirAncestry))
                {
                    Console.WriteLine("Directory ancestry check failed. Exiting.");
                    Environment.Exit(1);
                }

                string managementInstallDir = Path.GetFullPath(Path.Combine(currentDir, @"..\..\.."));

                // Initialize component manager

                string? steamValheimDir =
                    (options.ExtComponentManagerUseValheimInstallRoot != null)
                        ? options.ExtComponentManagerUseValheimInstallRoot
                        : SteamUtils.LookupAppInstallPath("Valheim");
                
                if (steamValheimDir == null)
                {
                    Console.WriteLine("Valheim path not found.");
                    Environment.Exit(1);
                }
                Console.WriteLine("Valheim path is: " + steamValheimDir);

                string componentSpecUrl = "https://www.dropbox.com/scl/fi/mlwo51czhp6d6jhpe7h2h/manager-config.spec?rlkey=v0yfq9gi0343f725bua5p5ris&st=uwfkav6e&dl=1";

                if (options.ExtComponentManagerUseComponentSpecUrl != null)
                {
                    componentSpecUrl = options.ExtComponentManagerUseComponentSpecUrl;
                }

                ComponentArchiveSpec componentArchiveSpec = await ComponentArchiveSpec.PullSpec(componentSpecUrl);

                ComponentManageContext componentManageContext = new ComponentManageContext(options, managementInstallDir, steamValheimDir, componentArchiveSpec);

                await ExtComponentManagerUpdater.PerformManagementProcessing(componentManageContext, args);

                await PerformVrModsComponentManagementProcessing(componentManageContext);

                Console.WriteLine("Processing completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        public static async Task PerformVrModsComponentManagementProcessing(ComponentManageContext componentManageContext)
        {
            bool checkDownload = (componentManageContext.Options.ValheimVrCheckUpdate == "yes");

            ComponentState targetInstallState;
            switch (componentManageContext.Options.ValheimVrEnabled)
            {
                case "yes":  targetInstallState = ComponentState.Installed;  break;
                case "no":  targetInstallState = ComponentState.Uninstalled;  break;
                case null:  targetInstallState = ComponentState.Maintain;  break;
                default:  throw new InvalidOperationException("Invalid Valheim VR enabled state.");
            }

            BepInExValheimModUpdater bepInExModUpdater = new BepInExValheimModUpdater(componentManageContext);
            VHVRValheimVRValheimModUpdater vhvrValheimVrModUpdater = new VHVRValheimVRValheimModUpdater(componentManageContext);

            ValheimModUpdater[] modUpdaters = [ bepInExModUpdater, vhvrValheimVrModUpdater ];
            if (targetInstallState == ComponentState.Uninstalled)
            {
                Array.Reverse(modUpdaters); // Uninstall in reverse order
            }

            foreach (var modUpdater in modUpdaters)
            {
                Console.WriteLine($"Checking/performing mod updates for module: {modUpdater.GetComponentName()} (per state: {targetInstallState})");
                await modUpdater.CheckImplementInstalledState(checkDownload, targetInstallState);
            }
        }
    }
}