using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using ProyectLauncher.Classes;
using ProyectLauncher.Views.Installer;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using CmlLib.Core;

namespace ProyectLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += LoadWindow;
        Control.btn_launch.Click += Launch_btn;

        Launcher.CloseMc += Reinvoke_Window;
        /*
        Control.InstallBt.Click += OpenInstallWindow;
        Control.DeleteBt.Click += OpenDeleteWindow;
        Control.LoadersBt.Click += OpenLoadersWindow;
        Launcher.CompleteDownload += CheckDownloads;*/
    }
    /*
    private void CheckDownloads(object sender, Task e)
    {
        Dispatcher.UIThread.Invoke(new Action(async () =>
        {
            Control.EnableControls();
            if (!e.IsCompletedSuccessfully)
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", "Error al descargar los archivos de la version seleccionada",MsBox.Avalonia.Enums.ButtonEnum.Ok,MsBox.Avalonia.Enums.Icon.Error).ShowAsPopupAsync(this);
            }
        }));
    }*/

    private void Reinvoke_Window(object sender, MLaunchOption e)
    {
        Show();
    }

    private async void Launch_btn(object? sender, RoutedEventArgs e)
    {
        
        if (await Launcher.CheckVersion("fabric-loader-0.15.11-1.19.2") && Settings.Instance().CloseLauncher)
        {
            Hide();
        }
    }

    private void LoadWindow(object sender, RoutedEventArgs e)
    {
        Title = "Pino Launcher: ";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Title += "Windows ";
            var assemblyData = Process.GetCurrentProcess().MainModule.FileName;
            Title += FileVersionInfo.GetVersionInfo(assemblyData).FileVersion.Remove(5);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Title += "MacOS ";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Title += "Linux ";
        }
    }

    /*
    private async void OpenInstallWindow(object sender, RoutedEventArgs e) 
    { 
        AddVersion addVersion = new AddVersion();
        await addVersion.ShowDialog(this);
        var result = addVersion.Result;
        
        if(result != string.Empty)
        {
            Control.DisableControls();
            Launcher.ReloadVersions();
            Control.LoadVersions();
            Control.VersionsCombo.SelectedValue = result;
        }
    }

    private async void OpenLoadersWindow(object sender, RoutedEventArgs e)
    {
        ModVersions modVersions = new ModVersions();
        await modVersions.ShowDialog(this);
        var result = modVersions.result;

        if(!string.IsNullOrEmpty(result))
        {
            Launcher.ReloadVersions();
            Control.LoadVersions();
            Control.VersionsCombo.SelectedValue = result;
        }
    }
    
    private async void OpenDeleteWindow(object sender, RoutedEventArgs e)
    {
        DeleteVersion deleteVersion = new DeleteVersion();
        await deleteVersion.ShowDialog(this);
        Launcher.ReloadVersions();
        Control.LoadVersions();
    }*/
}
