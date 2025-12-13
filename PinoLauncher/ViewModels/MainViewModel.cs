using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using PinoLauncher.Services;
using System.Collections.Generic;
using CmlLib.Core.ModLoaders.FabricMC;
using System.Net.Http;
using System.Reflection;

namespace PinoLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MinecraftLauncher _launcher;
    private readonly MinecraftPath _minecraftPath;
    private readonly FabricInstaller _fabricInstaller;

    [ObservableProperty]
    private string _username = "Player";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModLoaderButtonEnabled))]
    private string _selectedVersion = "";

    // Versión real para el lanzamiento (puede ser diferente a SelectedVersion para casos especiales como ChafaServer)
    private string _actualMinecraftVersion = "";
    
    public string ActualMinecraftVersion 
    {
        get => string.IsNullOrEmpty(_actualMinecraftVersion) ? SelectedVersion : _actualMinecraftVersion;
        private set => _actualMinecraftVersion = value;
    }

    [ObservableProperty]
    private bool _isLaunching = false;

    [ObservableProperty]
    private double _progressValue = 0;

    [ObservableProperty]
    private string _progressText = "Listo para jugar";

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private ObservableCollection<string> _availableVersions = new();

    [ObservableProperty]
    private string _statusMessage = "Esperando...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MemoryStatusText))]
    [NotifyPropertyChangedFor(nameof(SelectedRamMb))]
    private RamOption? _selectedRamOption;

    [ObservableProperty]
    private bool _fullscreenMode = false;

    [ObservableProperty]
    private ObservableCollection<RamOption> _availableRamOptions = new();

    public int SelectedRamMb => SelectedRamOption?.ValueMb ?? 2048;

    private bool _isLoadingSettings = false;

    public Window? MainWindow { get; set; }
    
    public string MemoryStatusText => SelectedRamOption != null 
        ? $"RAM: {SelectedRamOption.DisplayName}" 
        : "RAM: 2GB";

    public string ModLoaderStatusText => ModLoaderEnabled && !string.IsNullOrEmpty(SelectedModLoaderName)
        ? $"{SelectedModLoaderName} v{SelectedModLoaderVersion}"
        : "Sin mod loader";
    
    public string WindowTitle
    {
        get
        {
            // Obtener la versión del assembly de entrada (el ejecutable)
            var entryAssembly = Assembly.GetEntryAssembly();
            var version = entryAssembly?.GetName().Version;
            var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            return $"PinoLauncher - Minecraft Launcher {versionString}";
        }
    }

    public bool IsModLoaderButtonEnabled => !string.Equals(SelectedVersion, "ChafaServer", StringComparison.OrdinalIgnoreCase);

    // Propiedades para descarga de mods desde Supabase
    [ObservableProperty]
    private bool _isDownloadModsButtonVisible = false;
    
    [ObservableProperty]
    private bool _isDownloadingMods = false;
    
    [ObservableProperty]
    private bool _isCleaningMods = false;
    
    private List<string> _availableModsFromSupabase = new();

    // Propiedades para configuración de mod loaders
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModLoaderStatusText))]
    private bool _modLoaderEnabled = false;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModLoaderStatusText))]
    private string? _selectedModLoaderName;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModLoaderStatusText))]
    private string? _selectedModLoaderVersion;
    
    // Gestor de temas
    public ThemeManagerViewModel ThemeManager { get; }

    public MainViewModel()
    {
        // Configurar la ruta de Minecraft (usar la carpeta estándar o una personalizada)
        var minecraftFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        _minecraftPath = new MinecraftPath(minecraftFolder);
        _fabricInstaller = new FabricInstaller(new HttpClient());
        
        // Crear el launcher
        _launcher = new MinecraftLauncher(_minecraftPath);
        
        // Inicializar gestor de temas
        ThemeManager = new ThemeManagerViewModel();
        
        // Cargar versiones y configuraciones de forma secuencial
        _ = InitializeAsync();
        if(string.Equals(SelectedVersion, "ChafaServer", StringComparison.OrdinalIgnoreCase)){
            _ = InitializeSupabaseAsync();
        }
    }

    private async Task InitializeSupabaseAsync()
    {
        try
        {
            AddLog("[SUPABASE] Inicializando conexión...");
            await SupabaseService.InitializeAsync();
            
            // Listar archivos del bucket 'mods'
            await ListModsFromSupabaseAsync();
        }
        catch (Exception ex)
        {
            AddLog($"[SUPABASE ERROR] Error al inicializar: {ex.Message}");
        }
    }
    
    private async Task ListModsFromSupabaseAsync()
    {
        try
        {
            AddLog("[SUPABASE] Obteniendo mods...");
            
            var client = SupabaseService.GetClient();
            var storage = client.Storage;
            var bucket = storage.From("mods");
            
            var files = await bucket.List();
            
            if (files != null && files.Count > 0)
            {
                AddLog($"[SUPABASE] Se han encontrado {files.Count} 'mods':");
                
                // Limpiar lista anterior y agregar nuevos archivos
                _availableModsFromSupabase.Clear();
                
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file.Name))
                    {
                        if(file.Name == ".emptyFolderPlaceholder") 
                        {
                            continue;
                        }
                        var lastModified = file.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                        AddLog($"  • {file.Name} (modificado: {lastModified})");
                        _availableModsFromSupabase.Add(file.Name);
                    }
                }
                
                // Verificar cuántos mods ya están descargados
                var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
                int modsAlreadyDownloaded = 0;
                
                if (Directory.Exists(modsFolder))
                {
                    foreach (var modName in _availableModsFromSupabase)
                    {
                        var localFilePath = Path.Combine(modsFolder, modName);
                        if (File.Exists(localFilePath))
                        {
                            modsAlreadyDownloaded++;
                        }
                    }
                }
                
                // Mostrar botón solo si hay mods pendientes de descarga
                var modsPendingDownload = _availableModsFromSupabase.Count - modsAlreadyDownloaded;
                if (modsPendingDownload > 0)
                {
                    IsDownloadModsButtonVisible = true;
                    AddLog($"[SUPABASE] {modsPendingDownload} mod{(modsPendingDownload > 1 ? "s" : "")} disponible{(modsPendingDownload > 1 ? "s" : "")} para descarga");
                }
                else
                {
                    IsDownloadModsButtonVisible = false;
                    AddLog("[SUPABASE] Todos los mods ya están descargados");
                }
            }
            else
            {
                AddLog("[SUPABASE] No se encontraron mods");
                IsDownloadModsButtonVisible = false;
                _availableModsFromSupabase.Clear();
            }
        }
        catch (Exception ex)
        {
            AddLog($"[SUPABASE ERROR] Error al listar archivos: {ex.Message}");
        }
    }

    private async Task InitializeAsync()
    {   
        _isLoadingSettings = true; // Prevenir autoguardado durante la carga

        // Inicializar opciones de RAM
        InitializeRamOptions();

        // Cargar versiones disponibles primero
        await LoadAvailableVersionsAsync();

        // Después cargar configuraciones guardadas
        await LoadSettingsAsync();
    }

    private async Task LoadAvailableVersionsAsync()
    {
        try
        {
            AddLog("Cargando versiones de Minecraft...");
            
            // Agregar versiones populares manualmente (se pueden obtener dinámicamente más tarde)
            AvailableVersions.Clear();
            
            var popularVersions = new[]
            {
                "ChafaServer",
                "1.21.1",
                "1.21",
                "1.20.6",
                "1.20.4",
                "1.20.1",
                "1.19.4",
                "1.19.2",
                "1.18.2",
                "1.17.1",
                "1.16.5",
                "1.12.2",
                "1.8.9"
            };
            
            foreach (var version in popularVersions)
            {
                AvailableVersions.Add(version);
            }
            
            // Seleccionar la versión más reciente por defecto
            if (AvailableVersions.Any())
            {
                SelectedVersion = AvailableVersions.First();
            }
            
            AddLog($"Cargadas {AvailableVersions.Count} versiones populares.");
        }
        catch (Exception ex)
        {
            AddLog($"Error al cargar versiones: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LaunchMinecraftAsync()
    {
        if (IsLaunching || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(SelectedVersion))
            return;

        try
        {
            IsLaunching = true;
            StatusMessage = "Lanzando Minecraft...";
            ProgressValue = 0;
            ProgressText = "Iniciando...";
            
            var versionToLaunch = ActualMinecraftVersion;
            AddLog($"Iniciando Minecraft {versionToLaunch} para {Username}");
            
            if (string.Equals(SelectedVersion, "ChafaServer", StringComparison.OrdinalIgnoreCase))
            {
                AddLog($"[CHAFASERVER] Lanzando como {versionToLaunch} (modo ChafaServer)");
                
                // Verificar y descargar mods automáticamente si faltan algunos
                await CheckAndDownloadMissingModsAsync();
            }
            
            // Mostrar información del mod loader si está habilitado
            if (ModLoaderEnabled && !string.IsNullOrEmpty(SelectedModLoaderName))
            {
                AddLog($"Mod Loader habilitado: {SelectedModLoaderName} v{SelectedModLoaderVersion}");
            }

            // Crear sesión offline (para cuentas premium necesitarías implementar autenticación)
            var session = MSession.CreateOfflineSession(Username);
            
            ProgressValue = 10;
            ProgressText = "Preparando sesión...";
            await Task.Delay(500); // Simular tiempo de preparación
            
            AddLog("Verificando archivos del juego...");
            ProgressValue = 30;
            ProgressText = "Verificando archivos...";
            
            // Configurar opciones de lanzamiento
            var launchOption = new MLaunchOption
            {
                MaximumRamMb = SelectedRamMb,
                Session = session
            };
            string modLoaderVersionName = "";
            // Configurar mod loader si está habilitado
            if (ModLoaderEnabled && !string.IsNullOrEmpty(SelectedModLoaderName))
            {
                // Para mod loaders, necesitamos configurar el perfil específico
                // Esto depende del tipo de mod loader (Forge, Fabric, etc.)
                switch (SelectedModLoaderName.ToLower())
                {
                    case "forge":
                        launchOption.VersionType = "forge";
                        AddLog($"[MOD LOADER] Configurado Forge {SelectedModLoaderVersion}");
                        break;
                    case "fabric":
                        modLoaderVersionName = await _fabricInstaller.Install(versionToLaunch,SelectedModLoaderVersion!, _minecraftPath);  
                        AddLog($"[MOD LOADER] Configurado Fabric {SelectedModLoaderVersion}");
                        break;
                    case "quilt":
                        launchOption.VersionType = "quilt";
                        AddLog($"[MOD LOADER] Configurado Quilt {SelectedModLoaderVersion}");
                        break;
                    case "neoforge":
                        launchOption.VersionType = "neoforge";
                        AddLog($"[MOD LOADER] Configurado NeoForge {SelectedModLoaderVersion}");
                        break;
                    default:
                        AddLog($"[ADVERTENCIA] Mod loader {SelectedModLoaderName} no está configurado específicamente");
                        break;
                }
            }

            if(!string.IsNullOrEmpty(modLoaderVersionName)){
                versionToLaunch = modLoaderVersionName;
            }
            
            // Configurar pantalla completa si está habilitado
            if (FullscreenMode)
            {
                launchOption.FullScreen = true;
                launchOption.ServerIp = null; // Asegurar que no hay servidor predefinido
                AddLog("Configuración aplicada: Pantalla completa habilitada (se aplicará al iniciar Minecraft)");
            }
            
            AddLog($"Configurando RAM: {SelectedRamMb}MB ({SelectedRamMb / 1024.0:F1}GB)");

            ProgressValue = 60;
            ProgressText = "Descargando archivos necesarios...";
            await Task.Delay(1000); // Simular tiempo de descarga

            AddLog("Instalando y preparando archivos...");
            ProgressValue = 70;
            ProgressText = "Instalando versión...";

            // Instalar la versión si es necesario
            await _launcher.InstallAsync(versionToLaunch);
            
            ProgressValue = 90;
            ProgressText = "Iniciando Minecraft...";

            // Crear el proceso
            var process = await _launcher.BuildProcessAsync(versionToLaunch, launchOption);
            
            ProgressValue = 100;
            ProgressText = "¡Minecraft iniciado!";
            AddLog("¡Minecraft se ha lanzado exitosamente!");
            
            // Ocultar la ventana principal antes de iniciar Minecraft
            if (MainWindow != null)
            {
                MainWindow.Hide();
                AddLog("Launcher oculto mientras Minecraft está ejecutándose...");
            }
            
            // Iniciar el proceso
            process.Start();
            
            // Monitorear cuando se cierre el proceso
            _ = Task.Run(async () =>
            {
                process.WaitForExit();
                
                // Actualizar UI en el hilo principal
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Mostrar la ventana principal cuando Minecraft se cierre
                    if (MainWindow != null)
                    {
                        MainWindow.Show();
                        MainWindow.Activate(); // Traer la ventana al frente
                        MainWindow.BringIntoView();
                    }
                    
                    // Restaurar estado del launcher
                    AddLog("Minecraft se ha cerrado. Launcher restaurado.");
                    ProgressText = "Listo para jugar";
                    ProgressValue = 0;
                    StatusMessage = "Esperando...";
                    IsLaunching = false;
                });
            });
            
            // Resetear después de un momento
            await Task.Delay(3000);
            if (IsLaunching)
            {
                ProgressText = "Minecraft ejecutándose...";
                StatusMessage = "Minecraft ejecutándose";
                ProgressValue = 0;
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error al lanzar Minecraft: {ex.Message}");
            ProgressText = "Error al lanzar";
            ProgressValue = 0;
            StatusMessage = "Error al lanzar";
            IsLaunching = false;
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogText = "";
    }

    [RelayCommand]
    private void OpenMinecraftFolder()
    {
        try
        {
            if (Directory.Exists(_minecraftPath.BasePath))
            {
                Process.Start("explorer.exe", _minecraftPath.BasePath);
            }
            else
            {
                AddLog("La carpeta de Minecraft no existe aún.");
            }
        }
        catch (Exception ex)
        {
            AddLog($"Error al abrir carpeta: {ex.Message}");
        }
    }

    private void InitializeRamOptions()
    {
        AvailableRamOptions.Clear();
        
        // Obtener RAM total del sistema
        var totalRamGb = GetTotalSystemRamGb();
        
        // Agregar opciones de RAM comunes
        var ramOptions = new[]
        {
            new RamOption { DisplayName = "1 GB", ValueMb = 1024 },
            new RamOption { DisplayName = "2 GB (Recomendado)", ValueMb = 2048 },
            new RamOption { DisplayName = "3 GB", ValueMb = 3072 },
            new RamOption { DisplayName = "4 GB", ValueMb = 4096 },
            new RamOption { DisplayName = "6 GB", ValueMb = 6144 },
            new RamOption { DisplayName = "8 GB", ValueMb = 8192 },
            new RamOption { DisplayName = "12 GB", ValueMb = 12288 },
            new RamOption { DisplayName = "16 GB", ValueMb = 16384 }
        };
        
        // Filtrar opciones basadas en RAM disponible (dejar al menos 2GB para el sistema)
        var maxRecommendedRamGb = Math.Max(1, totalRamGb - 2);
        var maxRecommendedRamMb = (int)(maxRecommendedRamGb * 1024);
        
        foreach (var option in ramOptions)
        {
            if (option.ValueMb <= maxRecommendedRamMb)
            {
                AvailableRamOptions.Add(option);
            }
        }
        
        // Si no hay opciones disponibles, agregar al menos 1GB
        if (!AvailableRamOptions.Any())
        {
            AvailableRamOptions.Add(new RamOption { DisplayName = "1 GB", ValueMb = 1024 });
        }
        
        // Seleccionar 2GB por defecto, o la primera opción disponible si 2GB no está disponible
        SelectedRamOption = AvailableRamOptions.FirstOrDefault(x => x.ValueMb == 2048) 
                           ?? AvailableRamOptions.First();
        
        AddLog($"RAM del sistema detectada: {totalRamGb:F1}GB. Opciones disponibles: {AvailableRamOptions.Count}");
    }
    
    private double GetTotalSystemRamGb()
    {
        try
        {
            // Para Windows, usar GlobalMemoryStatusEx a través de P/Invoke
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                return memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
            }
            
            // Fallback: usar GC como aproximación mínima
            var gcMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0 * 1024.0);
            return Math.Max(4.0, gcMemory * 4); // Multiplicar por 4 como estimación
        }
        catch
        {
            return 8.0; // Valor por defecto más realista para sistemas modernos
        }
    }
    
    // Estructura para obtener información de memoria en Windows
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
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
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogText += $"[{timestamp}] {message}\n";
    }
    
    public void AddLogMessage(string message)
    {
        AddLog(message);
    }
    
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        // Crear y abrir ventana de configuración general
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            var settingsWindow = new Views.SettingsWindow(this);
            await settingsWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            // Fallback para mostrar la ventana si no hay ventana principal
            var settingsWindow = new Views.SettingsWindow(this);
            settingsWindow.Show();
        }
    }

    [RelayCommand]
    private async Task CleanAndRedownloadModsAsync()
    {
        if (IsCleaningMods || IsDownloadingMods || !_availableModsFromSupabase.Any())
            return;

        try
        {
            IsCleaningMods = true;
            AddLog("[SUPABASE] Iniciando limpieza y redescarga de mods...");
            
            var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
            
            if (Directory.Exists(modsFolder))
            {
                // Obtener todos los archivos .jar en la carpeta de mods
                var allModFiles = Directory.GetFiles(modsFolder, "*.jar");
                
                // Separar mods que están en Supabase y los que no
                var supabaseMods = new List<string>();
                var orphanedMods = new List<string>();
                
                foreach (var modFile in allModFiles)
                {
                    var fileName = Path.GetFileName(modFile);
                    if (_availableModsFromSupabase.Contains(fileName))
                    {
                        supabaseMods.Add(modFile);
                    }
                    else
                    {
                        orphanedMods.Add(modFile);
                    }
                }
                
                // Eliminar mods huérfanos (que no están en Supabase)
                if (orphanedMods.Any())
                {
                    AddLog($"[SUPABASE] Eliminando {orphanedMods.Count} mod(s) que no están en el servidor...");
                    foreach (var orphanedMod in orphanedMods)
                    {
                        try
                        {
                            File.Delete(orphanedMod);
                            AddLog($"[SUPABASE] ✓ Eliminado: {Path.GetFileName(orphanedMod)}");
                        }
                        catch (Exception ex)
                        {
                            AddLog($"[SUPABASE ERROR] No se pudo eliminar {Path.GetFileName(orphanedMod)}: {ex.Message}");
                        }
                    }
                }
                
                // Eliminar mods de Supabase para redescargar
                if (supabaseMods.Any())
                {
                    AddLog($"[SUPABASE] Eliminando {supabaseMods.Count} mod(s) para redescarga...");
                    foreach (var supabaseMod in supabaseMods)
                    {
                        try
                        {
                            File.Delete(supabaseMod);
                            AddLog($"[SUPABASE] ✓ Eliminado para redescarga: {Path.GetFileName(supabaseMod)}");
                        }
                        catch (Exception ex)
                        {
                            AddLog($"[SUPABASE ERROR] No se pudo eliminar {Path.GetFileName(supabaseMod)}: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(modsFolder);
                AddLog($"[SUPABASE] Creada carpeta de mods: {modsFolder}");
            }
            
            AddLog("[SUPABASE] Limpieza completada. Iniciando redescarga...");
            
            // Descargar todos los mods de nuevo
            await DownloadAllModsFromSupabaseAsync();
            
            AddLog("[SUPABASE] ✓ Limpieza y redescarga completada exitosamente");
        }
        catch (Exception ex)
        {
            AddLog($"[SUPABASE ERROR] Error durante limpieza: {ex.Message}");
        }
        finally
        {
            IsCleaningMods = false;
            CheckAndUpdateDownloadButtonVisibility();
        }
    }
    
    [RelayCommand]
    private async Task DownloadModsFromSupabaseAsync()
    {
        if (IsDownloadingMods || !_availableModsFromSupabase.Any())
            return;

        try
        {
            IsDownloadingMods = true;
            AddLog($"[SUPABASE] Iniciando descarga de {_availableModsFromSupabase.Count} mods...");
            
            // Crear carpeta de mods si no existe
            var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                AddLog($"[SUPABASE] Creada carpeta de mods: {modsFolder}");
            }
            
            var client = SupabaseService.GetClient();
            var storage = client.Storage;
            var bucket = storage.From("mods");
            
            int downloadedCount = 0;
            int totalCount = _availableModsFromSupabase.Count;
            
            foreach (var modFileName in _availableModsFromSupabase)
            {
                try
                {
                    var localFilePath = Path.Combine(modsFolder, modFileName);
                    
                    if(modFileName == ".emptyFolderPlaceholder") 
                    {
                        continue;
                    }
                    // Verificar si el archivo ya existe
                    if (File.Exists(localFilePath))
                    {
                        AddLog($"[SUPABASE] • {modFileName} ya existe, omitiendo...");
                        continue;
                    }
                    
                    AddLog($"[SUPABASE] • Descargando {modFileName}...");
                    
                    // Descargar el archivo
                    var fileBytes = await bucket.Download(modFileName, null);
                    
                    if (fileBytes != null && fileBytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(localFilePath, fileBytes);
                        downloadedCount++;
                        AddLog($"[SUPABASE] ✓ {modFileName} descargado ({fileBytes.Length / 1024:F1} KB)");
                    }
                    else
                    {
                        AddLog($"[SUPABASE] ✗ Error: {modFileName} está vacío o no se pudo descargar");
                    }
                }
                catch (Exception modEx)
                {
                    AddLog($"[SUPABASE ERROR] Error descargando {modFileName}: {modEx.Message}");
                }
            }
            
            AddLog($"[SUPABASE] Descarga completada: {downloadedCount}/{totalCount} mods descargados en {modsFolder}");
            
            if (downloadedCount > 0)
            {
                AddLog("[SUPABASE] ¡Mods listos para usar con ChafaServer!");
            }
            
            // Verificar si todos los mods están ahora descargados y ocultar botón si es necesario
            CheckAndUpdateDownloadButtonVisibility();
        }
        catch (Exception ex)
        {
            AddLog($"[SUPABASE ERROR] Error durante la descarga: {ex.Message}");
        }
        finally
        {
            IsDownloadingMods = false;
        }
    }
    
    private async Task DownloadAllModsFromSupabaseAsync()
    {
        if (!_availableModsFromSupabase.Any())
            return;

        var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
        var client = SupabaseService.GetClient();
        var storage = client.Storage;
        var bucket = storage.From("mods");
        
        int downloadedCount = 0;
        int totalCount = _availableModsFromSupabase.Count;
        
        foreach (var modFileName in _availableModsFromSupabase)
        {
            try
            {
                var localFilePath = Path.Combine(modsFolder, modFileName);
                
                if (modFileName == ".emptyFolderPlaceholder")
                {
                    continue;
                }
                
                AddLog($"[SUPABASE] • Descargando {modFileName}...");
                
                var fileBytes = await bucket.Download(modFileName, null);
                
                if (fileBytes != null && fileBytes.Length > 0)
                {
                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                    downloadedCount++;
                    AddLog($"[SUPABASE] ✓ {modFileName} descargado ({fileBytes.Length / 1024:F1} KB)");
                }
                else
                {
                    AddLog($"[SUPABASE] ✗ Error: {modFileName} está vacío o no se pudo descargar");
                }
            }
            catch (Exception modEx)
            {
                AddLog($"[SUPABASE ERROR] Error descargando {modFileName}: {modEx.Message}");
            }
        }
        
        AddLog($"[SUPABASE] Descarga completada: {downloadedCount}/{totalCount} mods descargados");
    }
    
    private void CheckAndUpdateDownloadButtonVisibility()
    {
        if (!_availableModsFromSupabase.Any())
        {
            IsDownloadModsButtonVisible = false;
            return;
        }
        
        var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
        if (!Directory.Exists(modsFolder))
        {
            IsDownloadModsButtonVisible = true;
            return;
        }
        
        // Verificar mods faltantes y eliminar huérfanos
        var allModFiles = Directory.GetFiles(modsFolder, "*.jar");
        var orphanedMods = new List<string>();
        
        // Identificar mods huérfanos (que no están en Supabase)
        foreach (var modFile in allModFiles)
        {
            var fileName = Path.GetFileName(modFile);
            if (!_availableModsFromSupabase.Contains(fileName))
            {
                orphanedMods.Add(modFile);
            }
        }
        
        // Eliminar mods huérfanos silenciosamente
        if (orphanedMods.Any())
        {
            AddLog($"[SUPABASE] Eliminando {orphanedMods.Count} mod(s) obsoleto(s)...");
            foreach (var orphanedMod in orphanedMods)
            {
                try
                {
                    File.Delete(orphanedMod);
                    AddLog($"[SUPABASE] ✓ Eliminado mod obsoleto: {Path.GetFileName(orphanedMod)}");
                }
                catch (Exception ex)
                {
                    AddLog($"[SUPABASE ERROR] No se pudo eliminar {Path.GetFileName(orphanedMod)}: {ex.Message}");
                }
            }
        }
        
        // Verificar mods faltantes
        int modsAlreadyDownloaded = 0;
        foreach (var modName in _availableModsFromSupabase)
        {
            var localFilePath = Path.Combine(modsFolder, modName);
            if (File.Exists(localFilePath))
            {
                modsAlreadyDownloaded++;
            }
        }
        
        // Ocultar botón si todos los mods ya están descargados
        IsDownloadModsButtonVisible = modsAlreadyDownloaded < _availableModsFromSupabase.Count;
        
        if (!IsDownloadModsButtonVisible)
        {
            AddLog("[SUPABASE] Todos los mods están actualizados - botón ocultado");
        }
    }
    
    private async Task CheckAndDownloadMissingModsAsync()
    {
        try
        {
            if (!_availableModsFromSupabase.Any())
            {
                AddLog("[PANASERVER] No hay mods disponibles en Supabase para descargar");
                return;
            }
            
            var modsFolder = Path.Combine(_minecraftPath.BasePath, "mods");
            var missingMods = new List<string>();
            
            // Verificar qué mods faltan
            foreach (var modName in _availableModsFromSupabase)
            {
                var localFilePath = Path.Combine(modsFolder, modName);
                if (!File.Exists(localFilePath))
                {
                    missingMods.Add(modName);
                }
            }
            
            if (!missingMods.Any())
            {
                AddLog("[CHAFASERVER] Todos los mods ya están descargados");
                return;
            }
            
            AddLog($"[CHAFASERVER] Detectados {missingMods.Count} mod{(missingMods.Count > 1 ? "s" : "")} faltante{(missingMods.Count > 1 ? "s" : "")}, descargando automáticamente...");
            
            // Crear carpeta de mods si no existe
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                AddLog($"[CHAFASERVER] Creada carpeta de mods: {modsFolder}");
            }
            
            var client = SupabaseService.GetClient();
            var storage = client.Storage;
            var bucket = storage.From("mods");
            
            int downloadedCount = 0;
            
            foreach (var modFileName in missingMods)
            {
                try
                {
                    var localFilePath = Path.Combine(modsFolder, modFileName);
                    
                    AddLog($"[CHAFASERVER] • Descargando {modFileName}...");
                    
                    // Descargar el archivo
                    var fileBytes = await bucket.Download(modFileName, null);
                    
                    if (fileBytes != null && fileBytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(localFilePath, fileBytes);
                        downloadedCount++;
                        AddLog($"[CHAFASERVER] ✓ {modFileName} descargado ({fileBytes.Length / 1024:F1} KB)");
                    }
                    else
                    {
                        AddLog($"[CHAFASERVER] ✗ Error: {modFileName} está vacío o no se pudo descargar");
                    }
                }
                catch (Exception modEx)
                {
                    AddLog($"[CHAFASERVER ERROR] Error descargando {modFileName}: {modEx.Message}");
                }
            }
            
            if (downloadedCount > 0)
            {
                AddLog($"[CHAFASERVER] ✓ Descarga automática completada: {downloadedCount}/{missingMods.Count} mods descargados");
                
                // Actualizar visibilidad del botón después de la descarga automática
                CheckAndUpdateDownloadButtonVisibility();
            }
        }
        catch (Exception ex)
        {
            AddLog($"[CHAFASERVER ERROR] Error durante la descarga automática: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenModLoaderConfigAsync()
    {
        // Crear y abrir ventana de configuración de mod loaders
        var configWindow = new Views.ModLoaderConfigWindow();
        var configViewModel = new ModLoaderConfigViewModel 
        { 
            ParentViewModel = this,
            RequestClose = () => configWindow.Close()
        };
        
        // Sincronizar configuración actual
        configViewModel.SyncFromMainViewModel(this);
        
        configWindow.DataContext = configViewModel;
        
        // En Avalonia, necesitamos obtener la ventana padre para ShowDialog
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow != null)
        {
            await configWindow.ShowDialog(desktop.MainWindow);
        }
        else
        {
            // Fallback para mostrar la ventana si no hay ventana principal
            configWindow.Show();
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // Verificar si es la primera vez que se ejecuta la aplicación
            // Si el archivo de configuración no existe, significa que es primera ejecución
            var settingsFilePath = SettingsService.GetSettingsPath();
            bool isFirstRun = !File.Exists(settingsFilePath);
            var settings = await SettingsService.LoadSettingsAsync();
            
            if (isFirstRun)
            {
                AddLog("[PRIMERA EJECUCIÓN] Configurando ChafaServer por defecto...");
                
                // Configuración por defecto para primera ejecución
                Username = "Player";
                SelectedVersion = "ChafaServer";
                FullscreenMode = false;
                ModLoaderEnabled = true;
                SelectedModLoaderName = "Fabric";
                SelectedModLoaderVersion = "0.18.1";
                
                // Establecer versión actual para ChafaServer
                ActualMinecraftVersion = "1.21.1";
                
                // Aplicar tema por defecto
                var defaultTheme = ThemeManager.AvailableThemes.FirstOrDefault(x => x.Name == "Default");
                if (defaultTheme != null)
                {
                    ThemeManager.SelectedTheme = defaultTheme;
                }
                
                AddLog("[PRIMERA EJECUCIÓN] ✓ ChafaServer configurado automáticamente");
                AddLog("[PRIMERA EJECUCIÓN] ✓ Fabric 0.18.1 habilitado para Minecraft 1.21.1");
                
                // Inicializar Supabase para ChafaServer
                _ = InitializeSupabaseAsync();
                
                // Guardar configuración inicial
                await SaveSettingsAsync();
                AddLog("[PRIMERA EJECUCIÓN] Configuración guardada");
            }
            else
            {
                // Aplicar configuraciones cargadas (ejecución normal)
                Username = settings.Username;
                FullscreenMode = settings.FullscreenMode;
                ModLoaderEnabled = settings.ModLoaderEnabled;
                SelectedModLoaderName = settings.SelectedModLoaderName;
                SelectedModLoaderVersion = settings.SelectedModLoaderVersion;
                
                // Aplicar versión seleccionada si está disponible en la lista
                if (!string.IsNullOrEmpty(settings.SelectedVersion) && AvailableVersions.Contains(settings.SelectedVersion))
                {
                    SelectedVersion = settings.SelectedVersion;
                    if(string.Equals(settings.SelectedVersion, "ChafaServer", StringComparison.OrdinalIgnoreCase))
                    {
                        SelectedModLoaderVersion = "0.18.1";
                        SelectedModLoaderName = "Fabric";
                        ActualMinecraftVersion = "1.21.1";
                    }
                }
                
                // Aplicar configuración de RAM si la opción existe
                if (settings.RamMb > 0)
                {
                    var ramOption = AvailableRamOptions.FirstOrDefault(x => x.ValueMb == settings.RamMb);
                    if (ramOption != null)
                    {
                        SelectedRamOption = ramOption;
                    }
                }
                
                // Aplicar tema
                if (!string.IsNullOrEmpty(settings.ThemeName))
                {
                    var theme = ThemeManager.AvailableThemes.FirstOrDefault(x => x.Name == settings.ThemeName);
                    if (theme != null)
                    {
                        ThemeManager.SelectedTheme = theme;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error cargando configuraciones: {ex.Message}");
            
            // Si hay error cargando configuraciones, aplicar configuración por defecto de ChafaServer
            AddLog("[FALLBACK] Aplicando configuración por defecto de ChafaServer...");
            Username = "Player";
            SelectedVersion = "ChafaServer";
            ModLoaderEnabled = true;
            SelectedModLoaderName = "Fabric";
            SelectedModLoaderVersion = "0.18.1";
            ActualMinecraftVersion = "1.21.1";
            
            _ = InitializeSupabaseAsync();
        }
        finally
        {
            _isLoadingSettings = false; // Rehabilitar autoguardado
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new UserSettings
            {
                Username = Username,
                SelectedVersion = SelectedVersion,
                RamMb = SelectedRamOption?.ValueMb ?? 2048,
                ThemeName = ThemeManager.SelectedTheme?.Name ?? "Default",
                FullscreenMode = FullscreenMode,
                ModLoaderEnabled = ModLoaderEnabled,
                SelectedModLoaderName = SelectedModLoaderName ?? "",
                SelectedModLoaderVersion = SelectedModLoaderVersion ?? "",
                LastSaved = DateTime.Now
            };

            await SettingsService.SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            AddLog($"[ERROR] Error guardando configuraciones: {ex.Message}");
        }
    }

    // Métodos parciales para autoguardar cuando cambien propiedades importantes
    partial void OnUsernameChanged(string value)
    {
        // Guardar automáticamente cuando cambie el nombre de usuario
        if (!_isLoadingSettings)
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnSelectedVersionChanged(string value)
    {
        // Configuración especial para ChafaServer
        if (string.Equals(value, "ChafaServer", StringComparison.OrdinalIgnoreCase))
        {
            // Establecer la versión real para el lanzamiento sin cambiar la selección visible
            ActualMinecraftVersion = "1.21.1";
            
            // Configurar Fabric automáticamente
            if (!_isLoadingSettings)
            {
                ModLoaderEnabled = true;
                SelectedModLoaderName = "Fabric";
                SelectedModLoaderVersion = "0.18.1";
                AddLog("[CHAFASERVER] Configurado para Minecraft 1.21.1 con Fabric 0.18.1");
                
                // Inicializar Supabase y cargar archivos de mods solo para ChafaServer
                _ = InitializeSupabaseAsync();
            }
            
            
        }
        else
        {
            // Para versiones normales, la versión actual es la misma que la seleccionada
            ActualMinecraftVersion = value;
        }
        
        // Guardar automáticamente cuando cambie la versión
        if (!_isLoadingSettings && !string.IsNullOrEmpty(value))
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnSelectedRamOptionChanged(RamOption? value)
    {
        // Guardar automáticamente cuando cambie la configuración de RAM
        if (!_isLoadingSettings && value != null)
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnFullscreenModeChanged(bool value)
    {
        // Guardar automáticamente cuando cambie el modo pantalla completa
        if (!_isLoadingSettings)
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnModLoaderEnabledChanged(bool value)
    {
        // Guardar automáticamente cuando cambie el estado del mod loader
        if (!_isLoadingSettings)
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnSelectedModLoaderNameChanged(string? value)
    {
        // Guardar automáticamente cuando cambie el mod loader seleccionado
        if (!_isLoadingSettings)
        {
            _ = SaveSettingsAsync();
        }
    }

    partial void OnSelectedModLoaderVersionChanged(string? value)
    {
        // Guardar automáticamente cuando cambie la versión del mod loader
        if (!_isLoadingSettings)
        {
            _ = SaveSettingsAsync();
        }
    }
}

public class RamOption
{
    public string DisplayName { get; set; } = "";
    public int ValueMb { get; set; }
    
    public override string ToString() => DisplayName;
}
