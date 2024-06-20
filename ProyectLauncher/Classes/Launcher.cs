using System;
using System.IO;
using CmlLib.Core;
using System.Threading.Tasks;
using CmlLib.Core.Version;
using CmlLib.Core.Installer.FabricMC;
using System.Collections.Generic;
using CmlLib.Core.VersionMetadata;
using DynamicData.Kernel;
using CmlLib.Core.Installer;
using System.Linq;
using CmlLib.Core.Auth;

namespace ProyectLauncher.Classes
{
    public static class Launcher
    {
        static MinecraftPath path = new();
        public static CMLauncher MCLauncher = new(path);
        public static FabricVersionLoader FabricLoader = new();

        public static MVersionCollection FabricVersions = FabricLoader.GetVersionMetadatas();

        public static event EventHandler<Task> CompleteDownload;
        public static event EventHandler<MLaunchOption> CloseMC;

        public static void LaunchVersion(string version,string UserName = "User")
        {
            MLaunchOption Options = new()
            {
                MaximumRamMb = 2028,
                Session = MSession.CreateOfflineSession(UserName)
            };
            Launch_Process(version,Options);
        }

        private static async void Launch_Process(string version,MLaunchOption Options)
        {
            var versionMetadata = FabricVersions.GetVersionMetadata(version);
            await versionMetadata.SaveAsync(MCLauncher.MinecraftPath);
            var process = await MCLauncher.LaunchAsync(version, Options);
            process.WaitForExit();
            OnMCClose(Options);
        }

        public static async void ReloadVersions()
        {
            await MCLauncher.GetAllVersionsAsync();
        }

        public static async Task DownloadVersion(string sversion)
        {
            var version = MCLauncher.GetVersion(sversion);
            var a =  MCLauncher.CheckAndDownloadAsync(version);
            await a.ContinueWith(OnDownloadComplete);
        }

        public static void DeleteVersion(string version)
        {
            string MCPath = MCLauncher.MinecraftPath.ToString();
            string VersionPath = MCPath + "\\versions\\" + version;
            Directory.Delete(VersionPath, true);
        }

        public static List<MVersionMetadata> FabricToLocalVersions()
        {
            List<MVersionMetadata> result = new();
            var Versions = FabricVersions.AsList();
            var VanillaVersions = MCLauncher.Versions;
            List<MVersionMetadata> LocalVersions ;
            if (VanillaVersions != null)
            {
               LocalVersions = VanillaVersions.AsList().FindAll(v => v.IsLocalVersion);
                if (LocalVersions.Count > 0)
                {
                    foreach (var version in Versions)
                    {
                        foreach (var localVersion in LocalVersions)
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
            CloseMC?.Invoke(null,e);
        }

    }
}
