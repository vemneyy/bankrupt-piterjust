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
            _databaseConfiguration ??= LoadDatabaseConfiguration();
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
            catch (JsonException jsonEx)
            {
                System.Diagnostics.Debug.WriteLine($"JSON serialization error: {jsonEx.Message}");
            }
            catch (IOException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"IO error while saving configuration: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        private static DatabaseConfiguration LoadDatabaseConfiguration()
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

        private static void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}