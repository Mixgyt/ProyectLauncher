using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PinoLauncher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<RamOption> _availableRamOptions = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MemoryStatusText))]
    private RamOption? _selectedRamOption;

    [ObservableProperty]
    private ThemeManagerViewModel? _themeManager;

    [ObservableProperty]
    private bool _fullscreenMode = false;

    [ObservableProperty]
    private string _minecraftPath = "";

    public string MemoryStatusText => SelectedRamOption != null 
        ? $"RAM Asignada: {SelectedRamOption.DisplayName}"
        : "No configurada";

    public MainViewModel? ParentViewModel { get; set; }
    
    public Action? RequestClose { get; set; }

    public SettingsViewModel()
    {
        InitializeRamOptions();
        InitializeMinecraftPath();
    }

    private void InitializeRamOptions()
    {
        // Detectar RAM del sistema
        var totalRamGb = GetTotalSystemRam();
        
        AvailableRamOptions.Clear();
        
        var ramOptions = new[]
        {
            new RamOption { DisplayName = "1 GB", ValueMb = 1024 },
            new RamOption { DisplayName = "2 GB", ValueMb = 2048 },
            new RamOption { DisplayName = "3 GB", ValueMb = 3072 },
            new RamOption { DisplayName = "4 GB", ValueMb = 4096 },
            new RamOption { DisplayName = "6 GB", ValueMb = 6144 },
            new RamOption { DisplayName = "8 GB", ValueMb = 8192 },
            new RamOption { DisplayName = "12 GB", ValueMb = 12288 },
            new RamOption { DisplayName = "16 GB", ValueMb = 16384 }
        };

        // Solo agregar opciones que sean razonables para el sistema
        var maxRecommendedRam = Math.Max(1, (int)(totalRamGb * 0.75)); // 75% de la RAM total
        
        foreach (var option in ramOptions)
        {
            if (option.ValueMb / 1024 <= maxRecommendedRam)
            {
                AvailableRamOptions.Add(option);
            }
        }

        // No seleccionar RAM por defecto, se sincronizará desde MainViewModel
    }

    private void InitializeMinecraftPath()
    {
        // Detectar ruta de Minecraft automáticamente
        var defaultPath = GetDefaultMinecraftPath();
        MinecraftPath = Directory.Exists(defaultPath) ? defaultPath : "No encontrada";
    }

    private static string GetDefaultMinecraftPath()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, ".minecraft");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Library", "Application Support", "minecraft");
            }
            else // Linux
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".minecraft");
            }
        }
        catch
        {
            return "";
        }
    }

    private static double GetTotalSystemRam()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsSystemRam();
            }
            // Para otros sistemas operativos, usar un valor por defecto
            return 8.0; // GB
        }
        catch
        {
            return 8.0; // Valor por defecto en caso de error
        }
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private static double GetWindowsSystemRam()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            return memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0); // Convertir a GB
        }
        return 8.0; // Valor por defecto
    }

    [RelayCommand]
    private void OpenMinecraftFolder()
    {
        try
        {
            var minecraftPath = GetDefaultMinecraftPath();
            if (Directory.Exists(minecraftPath))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("explorer.exe", minecraftPath);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", minecraftPath);
                }
                else // Linux
                {
                    Process.Start("xdg-open", minecraftPath);
                }
                
                ParentViewModel?.AddLogMessage($"[CARPETA] Abriendo carpeta de Minecraft: {minecraftPath}");
            }
            else
            {
                ParentViewModel?.AddLogMessage("[ERROR] No se pudo encontrar la carpeta de Minecraft");
            }
        }
        catch (Exception ex)
        {
            ParentViewModel?.AddLogMessage($"[ERROR] Error al abrir carpeta: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ApplySettings()
    {
        if (ParentViewModel != null)
        {
            // Aplicar configuración de RAM
            if (SelectedRamOption != null)
            {
                ParentViewModel.SelectedRamOption = SelectedRamOption;
                ParentViewModel.AddLogMessage($"[RAM] Configurada: {SelectedRamOption.DisplayName}");
            }

            // Aplicar configuración de pantalla completa
            ParentViewModel.FullscreenMode = FullscreenMode;
            var fullscreenStatus = FullscreenMode ? "habilitado" : "deshabilitado";
            ParentViewModel.AddLogMessage($"[PANTALLA] Modo pantalla completa: {fullscreenStatus}");

            // Aplicar configuración de tema (ya se aplica automáticamente)
            ParentViewModel.AddLogMessage("[CONFIGURACIÓN] Todas las configuraciones aplicadas exitosamente");
            
            // Guardar configuraciones persistentemente
            _ = ParentViewModel.SaveSettingsAsync();
            
            // Cerrar la ventana después de aplicar
            RequestClose?.Invoke();
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        // Resetear RAM a 2GB
        SelectedRamOption = AvailableRamOptions.FirstOrDefault(x => x.ValueMb == 2048) 
                          ?? AvailableRamOptions.FirstOrDefault();
        
        // Resetear pantalla completa
        FullscreenMode = false;
        
        // Resetear tema a automático
        if (ThemeManager != null)
        {
            ThemeManager.SelectedTheme = ThemeManager.AvailableThemes.FirstOrDefault(x => x.Name == "Default");
        }
        
        ParentViewModel?.AddLogMessage("[CONFIGURACIÓN] Configuraciones restablecidas a valores por defecto");
    }

    public void SyncFromMainViewModel(MainViewModel mainViewModel)
    {
        // Usar el mismo ThemeManager del MainViewModel para evitar cambios automáticos
        ThemeManager = mainViewModel.ThemeManager;
        
        // Configurar el callback para el ThemeManager
        ThemeManager.OnThemeChanged = (message) =>
        {
            ParentViewModel?.AddLogMessage($"[TEMA] {message}");
        };

        // Sincronizar RAM
        if (mainViewModel.SelectedRamOption != null)
        {
            var matchingRamOption = AvailableRamOptions
                .FirstOrDefault(x => x.ValueMb == mainViewModel.SelectedRamOption.ValueMb);
            if (matchingRamOption != null)
            {
                SelectedRamOption = matchingRamOption;
            }
        }

        // Sincronizar pantalla completa
        FullscreenMode = mainViewModel.FullscreenMode;
        
        // El tema ya está sincronizado porque usamos el mismo ThemeManager
    }
}