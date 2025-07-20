using Microsoft.Data.Sqlite;
using System.IO;

namespace bankrupt_piterjust.Services
{
    public class SQLiteInitializationService
    {
        private static readonly string DatabasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".piterjust",
            "data.db");

        public static string GetDatabasePath() => DatabasePath;

        public static string GetConnectionString() => $"Data Source={DatabasePath};";

        public static async Task InitializeDatabaseAsync()
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            try
            {
                // Check if database exists and is valid
                bool needsInit = !File.Exists(DatabasePath) || await NeedsDatabaseRecreation();

                if (needsInit)
                {
                    await CleanupExistingDatabase();
                    await CreateDatabaseAsync();
                }
                else
                {
                    await UpdateDatabaseSchemaAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                // Try to cleanup and create a new database
                await CleanupExistingDatabase();
                await CreateDatabaseAsync();
            }
        }

        private static async Task<bool> NeedsDatabaseRecreation()
        {
            try
            {
                using var connection = new SqliteConnection(GetConnectionString());
                await connection.OpenAsync();

                // Check if main tables exist
                using var cmd = new SqliteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='person';", connection);
                var result = await cmd.ExecuteScalarAsync();

                return result == null;
            }
            catch
            {
                return true;
            }
        }

        private static async Task CleanupExistingDatabase()
        {
            // Close any existing connections first
            SqliteConnection.ClearAllPools();

            // Wait a bit for connections to close
            await Task.Delay(100);

            try
            {
                // Delete main database file
                if (File.Exists(DatabasePath))
                {
                    File.Delete(DatabasePath);
                }

                // Delete WAL file if exists
                string walPath = DatabasePath + "-wal";
                if (File.Exists(walPath))
                {
                    File.Delete(walPath);
                }

                // Delete SHM file if exists
                string shmPath = DatabasePath + "-shm";
                if (File.Exists(shmPath))
                {
                    File.Delete(shmPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
                // Continue anyway
            }
        }

        private static async Task CreateDatabaseAsync()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            await connection.OpenAsync();

            // Set pragmas for better compatibility (but avoid WAL mode)
            await ExecuteCommandAsync(connection, "PRAGMA foreign_keys = ON;");
            await ExecuteCommandAsync(connection, "PRAGMA encoding = 'UTF-8';");
            await ExecuteCommandAsync(connection, "PRAGMA journal_mode = DELETE;"); // Use DELETE instead of WAL
            await ExecuteCommandAsync(connection, "PRAGMA synchronous = NORMAL;");

            // Create person table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE person (
                    person_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    last_name TEXT NOT NULL,
                    first_name TEXT NOT NULL,
                    middle_name TEXT,
                    phone TEXT,
                    email TEXT,
                    is_male INTEGER NOT NULL DEFAULT 1
                );");

            // Create address table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE address (
                    address_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    person_id INTEGER NOT NULL,
                    postal_code TEXT,
                    country TEXT NOT NULL DEFAULT 'Russia',
                    region TEXT,
                    district TEXT,
                    city TEXT,
                    locality TEXT,
                    street TEXT,
                    house_number TEXT,
                    building TEXT,
                    apartment TEXT,
                    FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            // Create basis table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE basis (
                    basis_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    basis_type TEXT NOT NULL,
                    document_number TEXT NOT NULL,
                    document_date TEXT NOT NULL
                );");

            // Create main_category table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE main_category (
                    main_category_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );");

            // Create filter_category table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE filter_category (
                    filter_category_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    main_category_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    UNIQUE (main_category_id, name),
                    FOREIGN KEY (main_category_id) REFERENCES main_category(main_category_id)
                );");

            // Create debtor table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE debtor (
                    debtor_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    person_id INTEGER NOT NULL,
                    filter_category_id INTEGER NOT NULL,
                    created_date TEXT NOT NULL DEFAULT (date('now')),
                    FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE,
                    FOREIGN KEY (filter_category_id) REFERENCES filter_category(filter_category_id)
                );");

            // Create employee table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE employee (
                    employee_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    position TEXT NOT NULL,
                    created_date TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    basis_id INTEGER,
                    person_id INTEGER,
                    FOREIGN KEY (basis_id) REFERENCES basis(basis_id) ON DELETE SET NULL,
                    FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            // Create contract table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE contract (
                    contract_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    contract_number TEXT NOT NULL,
                    city TEXT NOT NULL,
                    contract_date TEXT NOT NULL,
                    debtor_id INTEGER NOT NULL,
                    employee_id INTEGER NOT NULL,
                    total_cost REAL NOT NULL,
                    mandatory_expenses REAL NOT NULL,
                    manager_fee REAL NOT NULL,
                    other_expenses REAL NOT NULL,
                    services_amount REAL,
                    FOREIGN KEY (debtor_id) REFERENCES debtor(debtor_id) ON DELETE CASCADE,
                    FOREIGN KEY (employee_id) REFERENCES employee(employee_id) ON DELETE CASCADE
                );");

            // Create contract_stage table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE contract_stage (
                    contract_stage_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    contract_id INTEGER NOT NULL,
                    stage INTEGER NOT NULL,
                    amount REAL NOT NULL,
                    due_date TEXT NOT NULL,
                    is_active INTEGER DEFAULT 0,
                    FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
                );");

            // Create passport table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE passport (
                    person_id INTEGER NOT NULL PRIMARY KEY,
                    series TEXT NOT NULL,
                    number TEXT NOT NULL UNIQUE,
                    issued_by TEXT NOT NULL,
                    division_code TEXT,
                    issue_date TEXT NOT NULL,
                    FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            // Create payment_schedule table
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE payment_schedule (
                    schedule_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    contract_id INTEGER NOT NULL,
                    stage INTEGER NOT NULL,
                    description TEXT NOT NULL,
                    amount REAL NOT NULL CHECK (amount > 0),
                    due_date TEXT,
                    is_paid INTEGER DEFAULT 0,
                    UNIQUE (contract_id, stage),
                    FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
                );");

            // Insert default categories
            await ExecuteCommandAsync(connection, "INSERT INTO main_category (name) VALUES ('Клиенты');");
            await ExecuteCommandAsync(connection, "INSERT INTO main_category (name) VALUES ('Архив');");

            var clientsId = await GetScalarAsync(connection, "SELECT main_category_id FROM main_category WHERE name = 'Клиенты';");
            var archiveId = await GetScalarAsync(connection, "SELECT main_category_id FROM main_category WHERE name = 'Архив';");

            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Все');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Сбор документов');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Подготовка заявления');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'На рассмотрении');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Ходатайство');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Заседание');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({clientsId}, 'Процедура введена');");
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({archiveId}, 'Архив');");
        }

        private static async Task UpdateDatabaseSchemaAsync()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            await connection.OpenAsync();

            try
            {
                // For now, no schema updates needed
                // Employee information is stored in contract table, not debtor table
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
                // Continue anyway
            }
        }

        private static async Task<long> GetScalarAsync(SqliteConnection connection, string commandText)
        {
            using var command = new SqliteCommand(commandText, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result ?? 0);
        }

        private static async Task ExecuteCommandAsync(SqliteConnection connection, string commandText)
        {
            using var command = new SqliteCommand(commandText, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}