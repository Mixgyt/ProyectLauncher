using Avalonia.Controls;
using Avalonia.Interactivity;
using ProyectLauncher.Classes;
using System.Linq;

namespace ProyectLauncher.Views.Installer
{
    public partial class AddVersion : Window
    {
        public string Result { get; set; }

        public AddVersion()
        {
            InitializeComponent();
            this.Loaded += LoadData;
            SnapshotsCheck.IsCheckedChanged += LoadData;
            Result = string.Empty;
        }

        private void LoadData(object sender, RoutedEventArgs e)
        {
            var versions = Launcher.MCLauncher.Versions;
            bool snapshots = SnapshotsCheck.IsChecked.Value;
            ListVersion.Items.Clear();
            if (versions != null && versions.Count() > 0)
            {
                foreach (var version in versions)
                {
                    if (version.MType == CmlLib.Core.Version.MVersionType.Release && !snapshots)
                    {
                        ListVersion.Items.Add(version.Name);
                    }
                    else if (snapshots && (version.MType == CmlLib.Core.Version.MVersionType.Release || version.MType == CmlLib.Core.Version.MVersionType.Snapshot))
                    {
                        ListVersion.Items.Add(version.Name);
                    }
                }
            }
        }

        private void InstallVersion(object sender, RoutedEventArgs e)
        {
            string selectVersion = ListVersion.SelectedItem != null ? ListVersion.SelectedItem.ToString() : "";
            
            if (selectVersion != string.Empty)
            {
                _ = Launcher.DownloadVersion(selectVersion);  
                Result = selectVersion;
                this.Close();
            }
        }
    }
}
