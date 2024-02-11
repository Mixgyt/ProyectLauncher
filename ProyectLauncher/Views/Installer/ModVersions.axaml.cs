using CmlLib.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProyectLauncher.Classes;
using System.Linq;
using DynamicData.Kernel;
using System;

namespace ProyectLauncher.Views.Installer
{
    public partial class ModVersions : Window
    {
        public string result { get; set; }

        public ModVersions()
        {
            InitializeComponent();
            Loaded += LoadVersions;
            InstallFabricBt.Click += InstallFabricVersion;
        }

        private void LoadVersions(object sender, RoutedEventArgs e)
        {
            LoadFabric();
        }

        private void LoadFabric()
        {
            var versions = Launcher.FabricToLocalVersions();
            if (versions.Count > 0)
            {
                foreach (var version in versions)
                {
                    ListFabric.Items.Add(version.Name);
                }
            }
        }

        public void InstallFabricVersion(object sender, RoutedEventArgs e)
        {
            var version = ListFabric.SelectedItem as string;
            if (!string.IsNullOrEmpty(version))
            {
                var versionMetadata = Launcher.FabricVersions.GetVersionMetadata($"{version}");
                versionMetadata.SaveAsync(Launcher.MCLauncher.MinecraftPath);
                result = $"{version}";
                Close();
            }
        }
    }
}
