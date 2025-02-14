// Program.cs
using System;
using System.IO;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var options = ProgramOptions.ParseArguments(args);

                if (options == null)
                {
                    // Parsing failed or --help was specified
                    Environment.Exit(1);
                }

                Console.WriteLine($"Ext Component Manager Check Update: {options.ExtComponentManagerCheckUpdate}");
                Console.WriteLine($"Valheim VR Check Update: {options.ValheimVrCheckUpdate}");
                Console.WriteLine($"Valheim Start: {options.ValheimStart}");
                Console.WriteLine($"Voice Commander Check Update: {options.VoiceCommanderCheckUpdate}");
                Console.WriteLine($"Voice Commander Manage: {options.VoiceCommanderManage}");

                Console.WriteLine("Welcome to Valheim Ext Component Manager!");
                string? path = SteamUtils.LookupAppInstallPath("Valheim");
                if (path != null)
                {
                    Console.WriteLine("Valheim path is: " + path);
                }
                else
                {
                    Console.WriteLine("Valheim path not found.");
                }

                // Download and print the text from the specified URL
                ComponentArchiveSpec componentArchiveSpec = await ComponentArchiveSpec.PullSpec();
                Console.WriteLine("Downloaded spec check--");
                Console.WriteLine("BepInExPack_Valheim File: " + componentArchiveSpec.GetComponentArchive("BepInExPack_Valheim"));
                Console.WriteLine("BepInExPack_Valheim URL: " + componentArchiveSpec.GetComponentArchiveUrl("BepInExPack_Valheim"));
                Console.WriteLine("VHVR-Valheim_VR File: " + componentArchiveSpec.GetComponentArchive("VHVR-Valheim_VR"));
                Console.WriteLine("VHVR-Valheim_VR URL: " + componentArchiveSpec.GetComponentArchiveUrl("VHVR-Valheim_VR"));

                string name = componentArchiveSpec.GetComponentArchive("VHVR-Valheim_VR");
                string url = componentArchiveSpec.GetComponentArchiveUrl("VHVR-Valheim_VR");

                // Use DownloadUtil to save the file
                string targetDirectory = @"D:\Temp\ToDelete";
                string targetPath = Path.Combine(targetDirectory, name);
                await DownloadUtil.DownloadFileAsync(url, targetPath);
                Console.WriteLine($"File '{name}' has been downloaded to '{targetPath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }
    }
}