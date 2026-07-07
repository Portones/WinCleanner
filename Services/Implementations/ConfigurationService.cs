using System;
using System.IO;
using System.Text.Json;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;
using Serilog;

namespace WinCleaner.Services.Implementations
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _settingsFilePath;
        public AppSettings CurrentSettings { get; private set; }

        public ConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WinCleaner");
            
            // Crear el directorio si no existe
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _settingsFilePath = Path.Combine(appFolder, "settings.json");
            CurrentSettings = new AppSettings();
            
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        CurrentSettings = settings;
                        Log.Information("Configuración cargada correctamente desde {Path}", _settingsFilePath);
                        return;
                    }
                }
                
                Log.Warning("No se encontró archivo de configuración. Usando valores predeterminados.");
                ResetToDefault();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cargar la configuración desde {Path}. Usando valores predeterminados.", _settingsFilePath);
                ResetToDefault();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(CurrentSettings, options);
                File.WriteAllText(_settingsFilePath, json);
                Log.Information("Configuración guardada correctamente en {Path}", _settingsFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al guardar la configuración en {Path}", _settingsFilePath);
            }
        }

        public void ResetToDefault()
        {
            CurrentSettings = new AppSettings();
            SaveSettings();
        }
    }
}
