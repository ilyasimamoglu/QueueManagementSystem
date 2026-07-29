using System;
using System.IO;
using System.Text.Json;
using QueueManagementSystem.Counter.Models;

namespace QueueManagementSystem.Counter.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public AppConfig LoadSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { }

            // Dosya yoksa veya okunamazsa varsayılan ayarları döndür
            return new AppConfig
            {
                ApiUrl = "https://localhost:7146/",
                PrinterName = ""
            };
        }

        public bool SaveSettings(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config);
                File.WriteAllText(_configFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}