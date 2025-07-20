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

        public static string GetConnectionString() => $"Data Source={DatabasePath}";

        public static async Task InitializeDatabaseAsync()
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            if (!File.Exists(DatabasePath))
            {
                await CreateDatabaseAsync();
                await CreateSampleDataAsync();
            }
        }

        private static async Task CreateDatabaseAsync()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            await connection.OpenAsync();

            // Create tables one by one
            await ExecuteCommandAsync(connection, "PRAGMA foreign_keys = ON;");
            
            await ExecuteCommandAsync(connection, @"
                CREATE TABLE person (
                    person_id INTEGER PRIMARY KEY,
                    last_name TEXT NOT NULL,
                    first_name TEXT NOT NULL,
                    middle_name TEXT NULL,
                    phone TEXT NULL,
                    email TEXT NULL,
                    is_male INTEGER NOT NULL
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE address (
                    person_id INTEGER NOT NULL PRIMARY KEY,
                    postal_code TEXT NULL,
                    country TEXT DEFAULT 'Россия' NOT NULL,
                    region TEXT NULL,
                    district TEXT NULL,
                    city TEXT NULL,
                    locality TEXT NULL,
                    street TEXT NULL,
                    house_number TEXT NULL,
                    building TEXT NULL,
                    apartment TEXT NULL,
                    FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE basis (
                    basis_id INTEGER PRIMARY KEY,
                    basis_type TEXT NOT NULL,
                    document_number TEXT NOT NULL,
                    document_date TEXT NOT NULL,
                    CONSTRAINT basis_basis_type_check CHECK (basis_type IN ('Доверенность', 'Приказ'))
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE main_category (
                    main_category_id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    CONSTRAINT main_category_name_key UNIQUE (name)
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE filter_category (
                    filter_category_id INTEGER PRIMARY KEY,
                    main_category_id INTEGER NOT NULL,
                    name TEXT NOT NULL,
                    CONSTRAINT filter_category_main_name_key UNIQUE (main_category_id, name),
                    CONSTRAINT filter_category_main_category_id_fkey FOREIGN KEY (main_category_id) REFERENCES main_category(main_category_id)
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE debtor (
                    debtor_id INTEGER PRIMARY KEY,
                    person_id INTEGER NOT NULL,
                    filter_category_id INTEGER NOT NULL,
                    created_date TEXT NOT NULL DEFAULT (CURRENT_DATE),
                    CONSTRAINT debtor_person_id_fkey FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE,
                    CONSTRAINT debtor_filter_category_id_fkey FOREIGN KEY (filter_category_id) REFERENCES filter_category(filter_category_id)
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE employee (
                    employee_id INTEGER PRIMARY KEY,
                    position TEXT NOT NULL,
                    created_date TEXT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    basis_id INTEGER NULL,
                    person_id INTEGER NULL,
                    CONSTRAINT employee_basis_fk FOREIGN KEY (basis_id) REFERENCES basis(basis_id) ON DELETE SET NULL,
                    CONSTRAINT employee_person_fk FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE contract (
                    contract_id INTEGER PRIMARY KEY,
                    contract_number TEXT NOT NULL,
                    city TEXT NOT NULL,
                    contract_date TEXT NOT NULL,
                    debtor_id INTEGER NOT NULL,
                    employee_id INTEGER NOT NULL,
                    total_cost NUMERIC NOT NULL,
                    mandatory_expenses NUMERIC NOT NULL,
                    manager_fee NUMERIC NOT NULL,
                    other_expenses NUMERIC NOT NULL,
                    services_amount NUMERIC NULL,
                    CONSTRAINT contract_debtor_fk FOREIGN KEY (debtor_id) REFERENCES debtor(debtor_id) ON DELETE CASCADE,
                    CONSTRAINT contract_employee_fk FOREIGN KEY (employee_id) REFERENCES employee(employee_id) ON DELETE CASCADE
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE contract_stage (
                    contract_stage_id INTEGER PRIMARY KEY,
                    contract_id INTEGER NOT NULL,
                    stage INTEGER NOT NULL,
                    amount NUMERIC NOT NULL,
                    due_date TEXT NOT NULL,
                    is_active INTEGER DEFAULT 0,
                    CONSTRAINT contract_stage_contract_fk FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE passport (
                    person_id INTEGER NOT NULL PRIMARY KEY,
                    series TEXT NOT NULL,
                    number TEXT NOT NULL,
                    issued_by TEXT NOT NULL,
                    division_code TEXT NULL,
                    issue_date TEXT NOT NULL,
                    CONSTRAINT passport_unique UNIQUE (number),
                    CONSTRAINT passport_person_id_fkey FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
                );");

            await ExecuteCommandAsync(connection, @"
                CREATE TABLE payment_schedule (
                    schedule_id INTEGER PRIMARY KEY,
                    contract_id INTEGER NOT NULL,
                    stage INTEGER NOT NULL,
                    description TEXT NOT NULL,
                    amount NUMERIC NOT NULL,
                    due_date TEXT NOT NULL,
                    is_paid INTEGER DEFAULT 0,
                    CONSTRAINT payment_schedule_amount_positive CHECK (amount > 0),
                    CONSTRAINT payment_schedule_contract_stage_unique UNIQUE (contract_id, stage),
                    CONSTRAINT payment_schedule_contract_id_fkey FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
                );");

            // Insert default data
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
            await ExecuteCommandAsync(connection, $"INSERT INTO filter_category (main_category_id, name) VALUES ({archiveId}, 'Все');");
        }

        private static async Task CreateSampleDataAsync()
        {
            using var connection = new SqliteConnection(GetConnectionString());
            await connection.OpenAsync();

            await ExecuteCommandAsync(connection, @"
                INSERT INTO person (last_name, first_name, middle_name, phone, email, is_male) 
                VALUES ('Иванов', 'Иван', 'Иванович', '+7 (999) 123-45-67', 'ivanov@piterjust.ru', 1);");

            var personId1 = await GetScalarAsync(connection, "SELECT last_insert_rowid();");
            await ExecuteCommandAsync(connection, $"INSERT INTO employee (position, created_date, is_active, person_id) VALUES ('Юрист', date('now'), 1, {personId1});");

            await ExecuteCommandAsync(connection, @"
                INSERT INTO person (last_name, first_name, middle_name, phone, email, is_male) 
                VALUES ('Петрова', 'Мария', 'Сергеевна', '+7 (999) 234-56-78', 'petrova@piterjust.ru', 0);");

            var personId2 = await GetScalarAsync(connection, "SELECT last_insert_rowid();");
            await ExecuteCommandAsync(connection, $"INSERT INTO employee (position, created_date, is_active, person_id) VALUES ('Менеджер', date('now'), 1, {personId2});");
        }

        private static async Task ExecuteCommandAsync(SqliteConnection connection, string commandText)
        {
            using var command = new SqliteCommand(commandText, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task<long> GetScalarAsync(SqliteConnection connection, string commandText)
        {
            using var command = new SqliteCommand(commandText, connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
    }
}