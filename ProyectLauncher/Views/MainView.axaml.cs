using CmlLib.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using System.Threading;
using System.Reflection;
using System;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using CmlLib.Core.Auth;
using ProyectLauncher.Views.Installer;

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
            if (v.IsLocalVersion)
            {
                VersionsCombo.Items.Add(v.Name);
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

    public async void LaunchClick(object sender, RoutedEventArgs e)
    {
        AddVersion n = new AddVersion();
        n.Show();
        /* var result = await MessageBoxManager.GetMessageBoxStandard("Cancelar", "¿Desea cancelar la operacion?",ButtonEnum.YesNo,Icon.Warning).ShowAsPopupAsync(this);
         if(result == ButtonResult.Yes)
         { return; }
         MLaunchOption Options = new()
         {
             MaximumRamMb = 2028,
             Session = MSession.CreateOfflineSession(NameBox.Text.Trim())
         };
         Thread process = new(() => LaunchProcess(Options));
         process.Name = "Launch";
         process.IsBackground = true;
         process.Start();*/

    }

    private void LaunchProcess(MLaunchOption Options)
    {
        var process = MCLauncher.CreateProcess($"{VersionsCombo.SelectedItem}", Options);
        process.Start();
    }

    private void OpenWindow(object sender, RoutedEventArgs e)
    {
        
        
    }
}
