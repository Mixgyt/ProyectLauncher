using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Styling;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PinoLauncher.ViewModels;

public partial class ThemeManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ThemeOption> _availableThemes = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentThemeStatus))]
    private ThemeOption? _selectedTheme;

    public string CurrentThemeStatus => SelectedTheme != null 
        ? $"Tema actual: {SelectedTheme.DisplayName}"
        : "Tema no configurado";

    public ThemeManagerViewModel()
    {
        InitializeThemes();
    }

    private void InitializeThemes()
    {
        AvailableThemes.Clear();
        
        var themes = new[]
        {
            new ThemeOption { Name = "Default", DisplayName = "Automático (Sistema)", ThemeVariant = ThemeVariant.Default },
            new ThemeOption { Name = "Light", DisplayName = "Claro", ThemeVariant = ThemeVariant.Light },
            new ThemeOption { Name = "Dark", DisplayName = "Oscuro", ThemeVariant = ThemeVariant.Dark }
        };

        foreach (var theme in themes)
        {
            AvailableThemes.Add(theme);
        }

        // Seleccionar tema automático por defecto
        SelectedTheme = AvailableThemes.First(t => t.Name == "Default");
    }

    [RelayCommand]
    private void ApplyTheme()
    {
        if (SelectedTheme?.ThemeVariant != null && Avalonia.Application.Current != null)
        {
            Avalonia.Application.Current.RequestedThemeVariant = SelectedTheme.ThemeVariant;
            
            // Notificar el cambio de tema si hay un callback disponible
            OnThemeChanged?.Invoke($"[TEMA] Cambiado a: {SelectedTheme.DisplayName}");
        }
    }
    
    public Action<string>? OnThemeChanged { get; set; }

    partial void OnSelectedThemeChanged(ThemeOption? value)
    {
        ApplyTheme();
    }
}

public class ThemeOption
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ThemeVariant? ThemeVariant { get; set; }
    
    public override string ToString() => DisplayName;
}