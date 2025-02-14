#nullable enable

using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace ValheimExtComponentManager
{
    class SteamUtils
    {
        static string? GetSteamPath()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                if (key != null)
                {
                    return key.GetValue("SteamPath") as string;
                }
            }
            return null;
        }

        public static string? LookupAppInstallPath(string appName)
        {
            string? steamPath = GetSteamPath();

            if (steamPath == null)
            {
                Console.WriteLine("Steam path not found in registry.");
                return null;
            }

            string libraryVdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryVdfPath))
            {
                Console.WriteLine($"libraryfolders.vdf not found at {libraryVdfPath}");
                return null;
            }

            Console.WriteLine($"Found libraryfolders.vdf at {libraryVdfPath}");

            string[] lines = File.ReadAllLines(libraryVdfPath);
            foreach (string line in lines)
            {
                if (line.Contains("\"path\""))
                {
                    Console.WriteLine($"Processing path line: {line}");

                    int start = line.IndexOf("\"", line.IndexOf("\"path\"") + 6) + 1;
                    int end = line.IndexOf("\"", start);
                    if (start > 0 && end > start)
                    {
                        string libraryPath = line.Substring(start, end - start).Replace(@"\\", @"\");
                        Console.WriteLine($"Parsed library path: {libraryPath}");

                        string appFolder = Path.Combine(libraryPath, "steamapps", "common", appName);
                        Console.WriteLine($"Checking for app folder: {appFolder}");

                        if (Directory.Exists(appFolder))
                        {
                            Console.WriteLine($"Found app folder: {appFolder}");
                            return appFolder;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to parse path from line.");
                    }
                }
            }

            Console.WriteLine($"App folder for {appName} not found.");
            return null;
        }
    }
}