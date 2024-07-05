using System;
using System.IO;
using CmlLib.Core;
using System.Threading.Tasks;
using CmlLib.Core.Version;
using CmlLib.Core.Installer.FabricMC;
using System.Collections.Generic;
using CmlLib.Core.VersionMetadata;
using DynamicData.Kernel;
using CmlLib.Core.Auth;

namespace ProyectLauncher.Classes
{
    public static class Launcher
    {
        public static MinecraftPath _path = Settings.Instance().MCPath;
        public static CMLauncher McLauncher = new(_path);
        public static MLaunchOption McLauncherOptions = Settings.Instance().Options;
        public static FabricVersionLoader FabricLoader = new();

        public static MVersionCollection FabricVersions = FabricLoader.GetVersionMetadatas();

        public static event EventHandler<Task> CompleteDownload;
        public static event EventHandler<MLaunchOption> CloseMc;

        public static async void LaunchVersion(string version,string userName)
        {
            McLauncherOptions.Session = MSession.CreateOfflineSession(userName);
            if (await CheckVersion(version))
            {
                Launch_Process(version, McLauncherOptions);
            }
            else
            {
                await DownloadVersion(version);
            }
        }

        private static async void Launch_Process(string version,MLaunchOption options)
        {
            var versionMetadata = FabricVersions.GetVersionMetadata(version);
            await versionMetadata.SaveAsync(McLauncher.MinecraftPath);
            var process = await McLauncher.LaunchAsync(version, options);
            process.WaitForExit();
            OnMCClose(options);
        }

        public static async Task<bool> CheckVersion(string version)
        {
            var versions = await McLauncher.GetAllVersionsAsync();
            if (versions != null)
            {
                foreach (var colVersion in versions)
                {
                    if (colVersion.IsLocalVersion && colVersion.Name == version)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static async void ReloadVersions()
        {
            await McLauncher.GetAllVersionsAsync();
        }

        public static async Task DownloadVersion(string sversion)
        {
            var versionMetadata = FabricVersions.GetVersionMetadata(sversion);
            await versionMetadata.SaveAsync(McLauncher.MinecraftPath);
            await McLauncher.GetAllVersionsAsync();
            var version = await McLauncher.GetVersionAsync(sversion);
            
            var a =  McLauncher.CheckAndDownloadAsync(version);
            await a.ContinueWith(OnDownloadComplete);
        }

        public static void DeleteVersion(string version)
        {
            string mcPath = McLauncher.MinecraftPath.ToString();
            string versionPath = mcPath + "\\versions\\" + version;
            Directory.Delete(versionPath, true);
        }

        public static List<MVersionMetadata> FabricToLocalVersions()
        {
            List<MVersionMetadata> result = new();
            var versions = FabricVersions.AsList();
            var vanillaVersions = McLauncher.Versions;
            List<MVersionMetadata> localVersions ;
            if (vanillaVersions != null)
            {
               localVersions = vanillaVersions.AsList().FindAll(v => v.IsLocalVersion);
                if (localVersions.Count > 0)
                {
                    foreach (var version in versions)
                    {
                        foreach (var localVersion in localVersions)
                        {
                            if (version.Name.Contains(localVersion.Name))
                            {
                                result.Add(version);
                            }
                        }
                    }
                }
            }
            return result;
        }

        private static void OnDownloadComplete(Task e)
        {
            CompleteDownload?.Invoke(null, e);
        }

        private static void OnMCClose(MLaunchOption e)
        {
            CloseMc?.Invoke(null,e);
        }

    }
}
