using CmlLib.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading;
using System.Reflection;
using System;

namespace ProyectLauncher.Views;

public partial class MainView : UserControl
{
    MinecraftPath Path = new();
    CMLauncher MCLauncher;

    public MainView()
    {
        MCLauncher = new(Path);

        InitializeComponent();
        Loaded += LoadView;
    }

    public async void LoadView(object sender, RoutedEventArgs e)
    {
        var versions = await MCLauncher.GetAllVersionsAsync();
        foreach (var v in versions)
        {
            VersionsCombo.Items.Add(v.Name);
            if (v.IsLocalVersion)
            {
                VersionsCombo.SelectedItem = v.Name;
            }
        }

        MCLauncher.FileChanged += (e) =>
        {
            LoadText.IsVisible = true;
            LoadText.Text = $"{e.FileKind} | {e.FileName}  => {e.ProgressedFileCount}/{e.TotalFileCount}";
        };

        MCLauncher.ProgressChanged += (s, e) =>
        {
            LoadBar.Value = e.ProgressPercentage;
        };
    }

    public void LaunchClick(object sender, RoutedEventArgs e)
    {
       
        Thread process = new(() => LaunchProcess());
        process.Name = "Launch";
        process.IsBackground = true;
        process.Start();

    }

    private async void LaunchProcess()
    {
        MLaunchOption Options = new()
        {
            MaximumRamMb = 2028,
            JavaPath = "C:\\Program Files\\Java\\jdk-17\\bin\\javaw.exe",
        };

        var process = MCLauncher.CreateProcess($"{VersionsCombo.SelectedItem}", Options);
        process.Start();
    }
}
