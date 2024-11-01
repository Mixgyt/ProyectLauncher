using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Management;
using System.Diagnostics;
using Avalonia.Controls;
using CmlLib.Core;
using CmlLib.Core.Installer.FabricMC;
using ProyectLauncher.Views.Beta;

namespace ProyectLauncher.Classes
{
    public class Settings
    {
        public string UserName { get; set; }
        public bool InstallAllMods { get; set; }
        public bool CloseLauncher { get; set; }
        public bool DeleteOldMods { get; set; }
        public MinecraftPath MCPath { get; set; }
        public string ModsPath { get; set; }
        public MLaunchOption Options { get; set; }
        private static string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData","Roaming","PinoLauncher","settings.json");
        private static string LauncherVersion = "0.7.1";

        public Settings()
        {
            
        }

        public static Settings Instance()
        {
            Settings settings = new();
            if (!File.Exists(SettingsPath))
            {
                Directory.CreateDirectory(Directory.GetParent(SettingsPath).FullName);
                File.Create(SettingsPath).Close();
                settings.UserName = "";
                settings.InstallAllMods = true;
                settings.CloseLauncher = false;
                settings.DeleteOldMods = true;
                settings.MCPath = new MinecraftPath();
                settings.ModsPath = settings.MCPath.BasePath + "/mods";
                settings.Options = new MLaunchOption
                {
                    FullScreen = true,
                    GameLauncherName = "Pino Launcher",
                    GameLauncherVersion = LauncherVersion,
                    VersionType = "Pino Launcher",
                    MaximumRamMb = 2028
                };
                SaveChanges(settings);
                return settings;
            }
            return GetSettings();
        }

        private static Settings GetSettings()
        {
            var json_file = new StreamReader(SettingsPath);
            var json = json_file.ReadToEnd();
            json_file.Close();
            var settings = JsonSerializer.Deserialize<Settings>(json);
            settings.Options.GameLauncherVersion = LauncherVersion;
            return settings;
        }

        public static decimal TotalRam()
        {
            try
            {
                ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                var searcher = new ManagementObjectSearcher(query);
                var results = searcher.Get();
                decimal totalRam = 0;
                foreach (var result in results)
                {
                    ulong totalRamBytes = (ulong)result["TotalPhysicalMemory"];
                    //Conversion a MB
                    totalRam = Math.Round(totalRamBytes / (1024.0m * 1024.0m), 0);
                }
                return totalRam;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 0;
            }
        }

        public void ChangeUserName(string username)
        {
            UserName = username;
        }

        public void ChangeInstallAllMods(bool change)
        {
            InstallAllMods = change;
        }
        public void ChangeOldMods(bool change)
        {
            DeleteOldMods = change;
        }

        public void ChangeModsPath(string path)
        {
            ModsPath = path;
        }

        public void ChangeOptions(MLaunchOption options)
        {
            Options = options;
            Launcher.McLauncherOptions = options;
        }

        public void ChangeMcPath(string path, MainBeta parent)
        {
            MCPath = new MinecraftPath(path);
            Launcher._path = MCPath;
            Launcher.McLauncher = new(Launcher._path);
            Launcher.McLauncherOptions = Options;

            //Funcion que cambia el manejo de los eventos al crear un nuevo McLauncher
            Launcher.McLauncher.ProgressChanged += (sender, e) =>
            {
                parent.pgr_descarga.Value = e.ProgressPercentage;
            };
            Launcher.McLauncher.FileChanged += (sender) =>
            {
                parent.lbl_files.IsEnabled = true;
                parent.lbl_files.Content = $"{sender.ProgressedFileCount}/{sender.TotalFileCount} '{sender.FileName}'";
            };
        }

        public static void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SaveChanges(Settings settings)
        { 
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var serialize = JsonSerializer.Serialize(settings,options);
            File.WriteAllText(SettingsPath,serialize);
        }

    }
}
