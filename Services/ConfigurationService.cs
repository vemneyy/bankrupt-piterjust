using bankrupt_piterjust.Configuration;
using System.IO;
using System.Text.Json;

namespace bankrupt_piterjust.Services
{
    public class ConfigurationService
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PiterJust",
            "database_config.json"
        );

        private static ConfigurationService? _instance;
        public static ConfigurationService Instance => _instance ??= new ConfigurationService();

        private DatabaseConfiguration? _databaseConfiguration;

        private ConfigurationService()
        {
            EnsureConfigDirectoryExists();
        }

        public DatabaseConfiguration GetDatabaseConfiguration()
        {
            if (_databaseConfiguration == null)
            {
                _databaseConfiguration = LoadDatabaseConfiguration();
            }
            return _databaseConfiguration;
        }

        public void SaveDatabaseConfiguration(DatabaseConfiguration configuration)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(ConfigFilePath, json);
                _databaseConfiguration = configuration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        private DatabaseConfiguration LoadDatabaseConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<DatabaseConfiguration>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
            }
            return new DatabaseConfiguration();
        }

        private void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}