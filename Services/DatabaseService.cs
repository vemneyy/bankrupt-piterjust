using Npgsql;
using System.Data;
using System.Windows;

namespace bankrupt_piterjust.Services
{
    public class DatabaseService
    {
        private string _connectionString = string.Empty;
        private bool _connectionTested = false;
        private bool _connectionAvailable = false;
        private readonly Lock _connectionLock = new();

        public DatabaseService()
        {
            UpdateConnectionString();
        }

        private void UpdateConnectionString()
        {
            var config = ConfigurationService.Instance.GetDatabaseConfiguration();
            _connectionString = config.GetConnectionString();
        }

        public async Task<bool> TestConnectionAsync()
        {
            UpdateConnectionString();

            lock (_connectionLock)
            {
                if (_connectionTested && _connectionAvailable)
                    return true;
            }

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();
                await using var cryptoCmd = new NpgsqlCommand("SELECT crypt('test', gen_salt('bf'))", connection);
                await cryptoCmd.ExecuteScalarAsync();

                lock (_connectionLock)
                {
                    _connectionAvailable = true;
                    _connectionTested = true;
                }
                return true;
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    _connectionAvailable = false;
                    _connectionTested = true;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string errorMessage = "Не удалось подключиться к базе данных. Программа будет работать в автономном режиме.";

                    if (ex.Message.Contains("pgcrypto"))
                    {
                        errorMessage += "\n\nРасширение pgcrypto недоступно. Убедитесь, что оно установлено:\nCREATE EXTENSION IF NOT EXISTS pgcrypto;";
                    }

                    errorMessage += $"\n\nПодробности: {ex.Message}";

                    MessageBox.Show(
                        errorMessage,
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });

                return false;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (!await TestConnectionAsync())
                return 0;

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                return await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    _connectionAvailable = false;
                    _connectionTested = false;
                }

                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return 0;
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, Dictionary<string, object>? parameters = null)
        {
            if (!await TestConnectionAsync())
                return default;

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                var result = await command.ExecuteScalarAsync();

                if (result == DBNull.Value)
                    return default;

                if (typeof(T) == typeof(int) && result is Int64 longValue)
                    return (T)(object)(int)longValue;

                return (T?)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    _connectionAvailable = false;
                    _connectionTested = false;
                }

                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return default;
            }
        }

        public async Task<DataTable> ExecuteReaderAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (!await TestConnectionAsync())
                return new DataTable();

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                await using var reader = await command.ExecuteReaderAsync();
                var dataTable = new DataTable();
                dataTable.Load(reader);

                return dataTable;
            }
            catch (Exception ex)
            {
                lock (_connectionLock)
                {
                    _connectionAvailable = false;
                    _connectionTested = false;
                }

                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return new DataTable();
            }
        }

        public async Task<bool> ResetConnectionAsync()
        {
            lock (_connectionLock)
            {
                _connectionTested = false;
                _connectionAvailable = false;
            }
            return await TestConnectionAsync();
        }
        public async Task<string> GetConnectionInfoAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = new NpgsqlCommand(@"
                    SELECT 
                        version() as postgres_version,
                        current_database() as database_name,
                        current_user as username,
                        inet_server_addr() as server_address,
                        inet_server_port() as server_port
                ", connection);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return $"PostgreSQL Version: {reader["postgres_version"]}\n" +
                           $"Database: {reader["database_name"]}\n" +
                           $"User: {reader["username"]}\n" +
                           $"Server: {reader["server_address"]}:{reader["server_port"]}";
                }

                return "Подключение установлено, но информация недоступна";
            }
            catch (Exception ex)
            {
                return $"Ошибка получения информации о подключении: {ex.Message}";
            }
        }
    }
}