using CmlLib.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProyectLauncher.Classes;
using CmlLib.Core.Auth;
using ProyectLauncher.Views.Installer;
using System.Linq;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace ProyectLauncher.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += LoadView;
        NameBox.TextChanged += SaveUserName;
    }

    public async void LoadView(object sender, RoutedEventArgs e)
    {
        DisableControls();
        await Launcher.McLauncher.GetAllVersionsAsync();
        EnableControls();

        LoadVersions();
        Launcher.McLauncher.FileChanged += (e) =>
        {
            LoadText.IsVisible = true;
            LoadText.Text = $"{e.FileKind} | {e.FileName}  => {e.ProgressedFileCount}/{e.TotalFileCount}";
        };

        Launcher.McLauncher.ProgressChanged += (s, e) =>
        {
            LoadBar.Value = e.ProgressPercentage;
        };
    }

    public void LoadVersions()
    {
        Launcher.ReloadVersions();
        VersionsCombo.Items.Clear();
        var versions = Launcher.McLauncher.Versions;
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

        string userName;
        if(NameBox.Text != null)
        {
            userName = NameBox.Text.Trim();
        }
        else
        {
            userName = "username";
        }

        DisableControls();

        MLaunchOption options = new()
        {
            MaximumRamMb = 2028,
            Session = MSession.CreateOfflineSession(userName)
        };

        LaunchProcess(options);
        
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

    private async void LaunchProcess(MLaunchOption options)
    {
        if(VersionsCombo.SelectedValue == null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "No se selecciono ninguna version", ButtonEnum.Ok, Icon.Error,WindowStartupLocation.CenterOwner).ShowAsPopupAsync(this);
            EnableControls();
            return;
        }
        var process = await Launcher.McLauncher.LaunchAsync($"{VersionsCombo.SelectedItem}", options);
        process.WaitForExit();
        EnableControls();
    }

    private void SaveUserName(object? sender, RoutedEventArgs e)
    {

    }
}
