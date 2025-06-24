using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace bankrupt_piterjust.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private bool _connectionTested = false;
        private bool _connectionAvailable = false;
        
        public DatabaseService()
        {
            // Connection string for PostgreSQL
            // Credentials from the task description: postgres:postgres@10.155.1.210/piterjust
            _connectionString = "Host=10.155.1.210;Username=postgres;Password=postgres;Database=piterjust";
        }

        /// <summary>
        /// Tests the database connection and returns true if successful
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            if (_connectionTested)
                return _connectionAvailable;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                _connectionAvailable = true;
                _connectionTested = true;
                return true;
            }
            catch (Exception ex)
            {
                _connectionAvailable = false;
                _connectionTested = true;
                
                MessageBox.Show(
                    $"Не удалось подключиться к базе данных. Программа будет работать в автономном режиме.\n\nПодробности: {ex.Message}", 
                    "Ошибка подключения", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                
                return false;
            }
        }

        /// <summary>
        /// Execute a SQL command that does not return data
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object> parameters = null)
        {
            if (!await TestConnectionAsync())
                return 0;
            
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new NpgsqlCommand(sql, connection);
                
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
                _connectionAvailable = false; // Reset status for next attempt
                _connectionTested = false; // Force retest on next connection attempt
                
                // Log the error but don't throw it - allow offline mode operation
                Console.WriteLine($"Database error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Execute a SQL query that returns a single value
        /// </summary>
        public async Task<T> ExecuteScalarAsync<T>(string sql, Dictionary<string, object> parameters = null)
        {
            if (!await TestConnectionAsync())
                return default;
            
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new NpgsqlCommand(sql, connection);
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }
                
                var result = await command.ExecuteScalarAsync();
                
                // Handle DBNull
                if (result == DBNull.Value)
                    return default;
                
                // Handle type conversion for Int64 to Int32
                if (typeof(T) == typeof(int) && result is Int64 longValue)
                    return (T)(object)(int)longValue;
                
                // General case
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                _connectionAvailable = false; // Reset status for next attempt
                _connectionTested = false; // Force retest on next connection attempt
                
                // Log the error but don't throw it - allow offline mode operation
                Console.WriteLine($"Database error: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Execute a SQL query that returns a data reader
        /// </summary>
        public async Task<DataTable> ExecuteReaderAsync(string sql, Dictionary<string, object> parameters = null)
        {
            if (!await TestConnectionAsync())
                return new DataTable(); // Return empty table when connection fails
            
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new NpgsqlCommand(sql, connection);
                
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }
                
                using var reader = await command.ExecuteReaderAsync();
                var dataTable = new DataTable();
                dataTable.Load(reader);
                
                return dataTable;
            }
            catch (Exception ex)
            {
                _connectionAvailable = false; // Reset status for next attempt
                _connectionTested = false; // Force retest on next connection attempt
                
                // Log the error but don't throw it - allow offline mode operation
                Console.WriteLine($"Database error: {ex.Message}");
                return new DataTable(); // Return empty table on error
            }
        }
    }
}