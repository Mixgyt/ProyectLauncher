using CmlLib.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProyectLauncher.Classes;
using CmlLib.Core.Auth;
using ProyectLauncher.Views.Installer;
using System.Linq;

namespace ProyectLauncher.Views;

public partial class MainView : UserControl
{
    public MainView()
    {

        InitializeComponent();
        Loaded += LoadView;
    }

    public async void LoadView(object sender, RoutedEventArgs e)
    {
        DisableControls();
        await Launcher.MCLauncher.GetAllVersionsAsync();
        EnableControls();

        LoadVersions();
        Launcher.MCLauncher.FileChanged += (e) =>
        {
            LoadText.IsVisible = true;
            LoadText.Text = $"{e.FileKind} | {e.FileName}  => {e.ProgressedFileCount}/{e.TotalFileCount}";
        };

        Launcher.MCLauncher.ProgressChanged += (s, e) =>
        {
            LoadBar.Value = e.ProgressPercentage;
        };
    }

    public void LoadVersions()
    {
        Launcher.ReloadVersions();
        VersionsCombo.Items.Clear();
        var versions = Launcher.MCLauncher.Versions;
        if (versions != null && versions.Any())
        {
            foreach (var v in versions)
            {
                if (v.IsLocalVersion)
                {
                    VersionsCombo.Items.Add(v.Name);
                }
            }
        }
    }

    public void LaunchClick(object sender, RoutedEventArgs e)
    {
        /*var result = await MessageBoxManager.GetMessageBoxStandard("Cancelar", "¿Desea cancelar la operacion?",ButtonEnum.YesNo,Icon.Warning).ShowAsPopupAsync(this);
        if(result == ButtonResult.Yes)
        { return; }*/

        string UserName;
        if(NameBox.Text != null)
        {
            UserName = NameBox.Text.Trim();
        }
        else
        {
            UserName = "username";
        }

        DisableControls();

        MLaunchOption Options = new()
        {
            MaximumRamMb = 2028,
            Session = MSession.CreateOfflineSession(UserName)
        };

        LaunchProcess(Options);
        
    }

    public void DisableControls()
    {
        InstallBt.IsEnabled = false;
        LaunchBt.IsEnabled = false;
        LoadersBt.IsEnabled = false;
        VersionsCombo.IsEnabled = false;
        NameBox.IsEnabled = false;
        DeleteBt.IsEnabled = false;
    }

    public void EnableControls()
    {
        InstallBt.IsEnabled = true;
        LaunchBt.IsEnabled = true;
        LoadersBt.IsEnabled = true;
        VersionsCombo.IsEnabled = true;
        NameBox.IsEnabled = true;
        DeleteBt.IsEnabled = true;
    }

    private async void LaunchProcess(MLaunchOption Options)
    {
        if(VersionsCombo.SelectedValue != null)
        {
            var process = await Launcher.MCLauncher.LaunchAsync($"{VersionsCombo.SelectedItem}", Options);
            process.WaitForExit();
        }
        EnableControls();
    }
}
