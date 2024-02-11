using Avalonia.Controls;
using Avalonia.Interactivity;
using CmlLib.Core;
using ProyectLauncher.Classes;
using System.Linq;

namespace ProyectLauncher.Views.Installer
{
    public partial class DeleteVersion : Window
    {
        public DeleteVersion()
        {
            InitializeComponent();
            Loaded += LoadVersions; 
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            string version = ListVersions.SelectedItem != null ? ListVersions.SelectedItem.ToString() : "";
            
            if(version != string.Empty && version != null)
            {
                Launcher.DeleteVersion(version);
                Close();
            }
        }

        private void LoadVersions(object sender, RoutedEventArgs e)
        {
            var versions = Launcher.MCLauncher.Versions;
            if (versions != null && versions.Any())
            {
                foreach (var v in versions)
                {
                    if (v.IsLocalVersion)
                    {
                        ListVersions.Items.Add(v.Name);
                    }
                }
            }
        }
    }
}
