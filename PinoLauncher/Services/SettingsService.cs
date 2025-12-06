using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PinoLauncher.Services;

public class SettingsService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PinoLauncher"
    );
    
    private static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");

    public static async Task<UserSettings> LoadSettingsAsync()
    {
        try
        {
            // Crear directorio si no existe
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            // Si el archivo no existe, devolver configuración por defecto
            if (!File.Exists(SettingsFilePath))
            {
                return new UserSettings();
            }

            // Leer y deserializar el archivo JSON
            var jsonString = await File.ReadAllTextAsync(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<UserSettings>(jsonString);

            return settings ?? new UserSettings();
        }
        catch (Exception ex)
        {
            // En caso de error, devolver configuración por defecto
            Console.WriteLine($"Error cargando configuración: {ex.Message}");
            return new UserSettings();
        }
    }

    public static async Task SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            // Crear directorio si no existe
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            // Serializar y guardar el archivo JSON
            var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true, // Formato legible
            });

            await File.WriteAllTextAsync(SettingsFilePath, jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error guardando configuración: {ex.Message}");
            throw;
        }
    }

    public static string GetSettingsPath()
    {
        return SettingsFilePath;
    }

    public static string GetAppDataPath()
    {
        return AppDataPath;
    }
}

public class UserSettings
{
    public string Username { get; set; } = "Player";
    public string SelectedVersion { get; set; } = "";
    public int RamMb { get; set; } = 2048;
    public string ThemeName { get; set; } = "Default";
    public bool FullscreenMode { get; set; } = false;
    public bool ModLoaderEnabled { get; set; } = false;
    public string SelectedModLoaderName { get; set; } = "";
    public string SelectedModLoaderVersion { get; set; } = "";
    public DateTime LastSaved { get; set; }

    public UserSettings()
    {
        LastSaved = DateTime.Now;
    }
}