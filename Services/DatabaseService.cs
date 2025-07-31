using Microsoft.Data.Sqlite;
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
            _connectionString = SQLiteInitializationService.GetConnectionString();
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
                // Инициализируем базу данных, если необходимо
                await SQLiteInitializationService.InitializeDatabaseAsync();

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Test simple query
                using var testCmd = new SqliteCommand("SELECT 1", connection);
                await testCmd.ExecuteScalarAsync();

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

                System.Diagnostics.Debug.WriteLine($"Database connection error: {ex.Message}");

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    string errorMessage = "Не удалось подключиться к базе данных SQLite.";
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
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var value = param.Value ?? DBNull.Value;
                        command.Parameters.AddWithValue(param.Key, value);
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
                throw; // Re-throw so calling code can handle
            }
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, Dictionary<string, object>? parameters = null)
        {
            if (!await TestConnectionAsync())
                return default;

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var value = param.Value ?? DBNull.Value;
                        command.Parameters.AddWithValue(param.Key, value);
                    }
                }

                var result = await command.ExecuteScalarAsync();

                if (result == DBNull.Value)
                    return default;

                if (typeof(T) == typeof(int) && result is long longValue)
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
                throw; // Re-throw so calling code can handle
            }
        }

        public async Task<DataTable> ExecuteReaderAsync(string sql, Dictionary<string, object>? parameters = null)
        {
            if (!await TestConnectionAsync())
                return new DataTable();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = new SqliteCommand(sql, connection);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var value = param.Value ?? DBNull.Value;
                        command.Parameters.AddWithValue(param.Key, value);
                    }
                }

                using var reader = await command.ExecuteReaderAsync();
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
                throw; // Re-throw so calling code can handle
            }
        }

        public async Task<bool> ResetConnectionAsync()
        {
            lock (_connectionLock)
            {
                _connectionTested = false;
                _connectionAvailable = false;
            }

            // Clear connection pools to release any locks
            SqliteConnection.ClearAllPools();
            await Task.Delay(50); // Give it a moment

            return await TestConnectionAsync();
        }

        public async Task<string> GetConnectionInfoAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var cmd = new SqliteCommand("SELECT sqlite_version() as sqlite_version", connection);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return $"SQLite Version: {reader["sqlite_version"]}\n" +
                           $"Database: {SQLiteInitializationService.GetDatabasePath()}";
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