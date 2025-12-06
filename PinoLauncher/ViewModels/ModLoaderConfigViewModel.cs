using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PinoLauncher.ViewModels;

public partial class ModLoaderConfigViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<ModLoaderOption> _availableModLoaders = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModLoaderInfo))]
    private ModLoaderOption? _selectedModLoader;

    [ObservableProperty]
    private ObservableCollection<string> _availableVersionsForLoader = new();

    [ObservableProperty]
    private string _selectedLoaderVersion = "";

    [ObservableProperty]
    private bool _enableModLoader = false;

    public string SelectedModLoaderInfo => SelectedModLoader != null 
        ? $"{SelectedModLoader.DisplayName} - {SelectedModLoader.Description}"
        : "Sin mod loader seleccionado";

    public MainViewModel? ParentViewModel { get; set; }
    
    public Action? RequestClose { get; set; }

    public ModLoaderConfigViewModel()
    {
        InitializeModLoaders();
    }

    partial void OnSelectedModLoaderChanged(ModLoaderOption? value)
    {
        UpdateVersionsForSelectedLoader();
    }

    private void InitializeModLoaders()
    {
        AvailableModLoaders.Clear();

        var modLoaders = new[]
        {
            new ModLoaderOption 
            { 
                Name = "Forge", 
                DisplayName = "MinecraftForge", 
                Description = "El mod loader más popular y estable para Minecraft",
                SupportedVersions = new[] { "1.21.1", "1.20.6", "1.20.4", "1.20.1", "1.19.4", "1.19.2", "1.18.2", "1.17.1", "1.16.5", "1.12.2" }
            },
            new ModLoaderOption 
            { 
                Name = "Fabric", 
                DisplayName = "Fabric", 
                Description = "Mod loader ligero y moderno con gran rendimiento",
                SupportedVersions = new[] { "1.21.1", "1.21", "1.20.6", "1.20.4", "1.20.1", "1.19.4", "1.19.2", "1.18.2", "1.17.1" }
            },
            new ModLoaderOption 
            { 
                Name = "Quilt", 
                DisplayName = "Quilt", 
                Description = "Fork de Fabric con características adicionales y mejor compatibilidad",
                SupportedVersions = new[] { "1.21.1", "1.20.6", "1.20.4", "1.20.1", "1.19.4", "1.19.2", "1.18.2" }
            },
            new ModLoaderOption 
            { 
                Name = "NeoForge", 
                DisplayName = "NeoForge", 
                Description = "Fork moderno de Forge con mejoras de rendimiento",
                SupportedVersions = new[] { "1.21.1", "1.20.6", "1.20.4", "1.20.1" }
            }
        };

        foreach (var loader in modLoaders)
        {
            AvailableModLoaders.Add(loader);
        }

        // No seleccionar por defecto, se sincronizará desde MainViewModel
    }

    [RelayCommand]
    private void UpdateVersionsForSelectedLoader()
    {
        AvailableVersionsForLoader.Clear();
        
        if (SelectedModLoader != null)
        {
            // En una implementación real, aquí consultarías las versiones disponibles de la API
            // Por ahora, usamos versiones mock basadas en el loader seleccionado
            var versions = GetMockVersionsForLoader(SelectedModLoader.Name);
            
            foreach (var version in versions)
            {
                AvailableVersionsForLoader.Add(version);
            }

            // Seleccionar la primera versión disponible
            if (AvailableVersionsForLoader.Any())
            {
                SelectedLoaderVersion = AvailableVersionsForLoader.First();
            }
        }
    }

    private string[] GetMockVersionsForLoader(string loaderName)
    {
        return loaderName switch
        {
            "Forge" => new[] { "47.3.5", "47.3.0", "47.2.20", "47.2.17", "47.2.6" },
            "Fabric" => new[] { "0.15.11", "0.15.10", "0.15.7", "0.15.6", "0.15.3" },
            "Quilt" => new[] { "0.26.4", "0.26.3", "0.26.0", "0.25.4", "0.25.1" },
            "NeoForge" => new[] { "21.1.73", "21.1.68", "21.1.60", "21.1.57", "21.1.42" },
            _ => Array.Empty<string>()
        };
    }

    [RelayCommand]
    private void ApplySettings()
    {
        if (ParentViewModel != null)
        {
            // Aplicar configuración al ViewModel principal
            ParentViewModel.ModLoaderEnabled = EnableModLoader;
            ParentViewModel.SelectedModLoaderName = SelectedModLoader?.Name;
            ParentViewModel.SelectedModLoaderVersion = SelectedLoaderVersion;
            
            var message = EnableModLoader && SelectedModLoader != null
                ? $"Configuración aplicada: {SelectedModLoader.DisplayName} v{SelectedLoaderVersion}"
                : "Mod loader deshabilitado";
            
            ParentViewModel.AddLogMessage($"[MOD LOADER] {message}");
            
            // Cerrar la ventana después de aplicar
            RequestClose?.Invoke();
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        EnableModLoader = false;
        SelectedModLoader = AvailableModLoaders.FirstOrDefault(x => x.Name == "Forge");
        UpdateVersionsForSelectedLoader();
    }

    public void SyncFromMainViewModel(MainViewModel mainViewModel)
    {
        // Sincronizar estado de habilitado
        EnableModLoader = mainViewModel.ModLoaderEnabled;

        // Sincronizar mod loader seleccionado
        if (!string.IsNullOrEmpty(mainViewModel.SelectedModLoaderName))
        {
            var matchingLoader = AvailableModLoaders
                .FirstOrDefault(x => x.Name.Equals(mainViewModel.SelectedModLoaderName, StringComparison.OrdinalIgnoreCase));
            if (matchingLoader != null)
            {
                SelectedModLoader = matchingLoader;
            }
        }

        // Actualizar versiones para el loader seleccionado
        UpdateVersionsForSelectedLoader();

        // Sincronizar versión seleccionada
        if (!string.IsNullOrEmpty(mainViewModel.SelectedModLoaderVersion) && 
            AvailableVersionsForLoader.Contains(mainViewModel.SelectedModLoaderVersion))
        {
            SelectedLoaderVersion = mainViewModel.SelectedModLoaderVersion;
        }
    }
}

public class ModLoaderOption
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] SupportedVersions { get; set; } = Array.Empty<string>();
    
    public override string ToString() => DisplayName;
}