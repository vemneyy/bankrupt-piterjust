using Microsoft.Data.Sqlite;
using System.Data;
using System.IO;
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
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "piterjust.db");
            _connectionString = $"Data Source={dbPath}";
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
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = new SqliteCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();

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
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new SqliteCommand(sql, connection);

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
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new SqliteCommand(sql, connection);

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
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var command = new SqliteCommand(sql, connection);

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
        public Task<string> GetConnectionInfoAsync()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "piterjust.db");
            return Task.FromResult($"SQLite DB Path: {dbPath}");
        }

        public async Task<int?> AddEmployeeAsync(
            string lastName,
            string firstName,
            bool isMale,
            string position,
            string login,
            string passwordPlain,
            string? middleName = null,
            string? phone = null,
            string? email = null,
            bool isActive = true,
            string? basisType = null,
            string? documentNumber = null,
            DateTime? documentDate = null)
        {
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();

                // Insert person
                var personCmd = connection.CreateCommand();
                personCmd.Transaction = transaction;
                personCmd.CommandText = @"INSERT INTO person (last_name, first_name, middle_name, phone, email, is_male)
                                            VALUES (@ln, @fn, @mn, @ph, @em, @male);
                                            SELECT last_insert_rowid();";
                personCmd.Parameters.AddWithValue("@ln", lastName);
                personCmd.Parameters.AddWithValue("@fn", firstName);
                personCmd.Parameters.AddWithValue("@mn", (object?)middleName ?? DBNull.Value);
                personCmd.Parameters.AddWithValue("@ph", (object?)phone ?? DBNull.Value);
                personCmd.Parameters.AddWithValue("@em", (object?)email ?? DBNull.Value);
                personCmd.Parameters.AddWithValue("@male", isMale);
                var personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());

                int? basisId = null;
                if (basisType != null && documentNumber != null && documentDate.HasValue)
                {
                    var basisCmd = connection.CreateCommand();
                    basisCmd.Transaction = transaction;
                    basisCmd.CommandText = @"INSERT INTO basis (basis_type, document_number, document_date)
                                               VALUES (@bt, @dn, @dd);
                                               SELECT last_insert_rowid();";
                    basisCmd.Parameters.AddWithValue("@bt", basisType);
                    basisCmd.Parameters.AddWithValue("@dn", documentNumber);
                    basisCmd.Parameters.AddWithValue("@dd", documentDate.Value);
                    basisId = Convert.ToInt32(await basisCmd.ExecuteScalarAsync());
                }

                var empCmd = connection.CreateCommand();
                empCmd.Transaction = transaction;
                empCmd.CommandText = @"INSERT INTO employee (position, login, password_hash, created_date, is_active, basis_id, person_id)
                                        VALUES (@pos, @log, @hash, DATE('now'), @active, @bid, @pid);
                                        SELECT last_insert_rowid();";
                empCmd.Parameters.AddWithValue("@pos", position);
                empCmd.Parameters.AddWithValue("@log", login);
                empCmd.Parameters.AddWithValue("@hash", BCrypt.Net.BCrypt.HashPassword(passwordPlain));
                empCmd.Parameters.AddWithValue("@active", isActive);
                if (basisId.HasValue)
                    empCmd.Parameters.AddWithValue("@bid", basisId.Value);
                else
                    empCmd.Parameters.AddWithValue("@bid", DBNull.Value);
                empCmd.Parameters.AddWithValue("@pid", personId);

                var employeeId = Convert.ToInt32(await empCmd.ExecuteScalarAsync());

                await transaction.CommitAsync();

                return employeeId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }
    }
}