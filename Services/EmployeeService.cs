using bankrupt_piterjust.Models;
using Microsoft.Data.Sqlite;
using System.Data;

namespace bankrupt_piterjust.Services
{
    public class EmployeeService
    {
        private readonly DatabaseService _databaseService;

        public EmployeeService()
        {
            _databaseService = new DatabaseService();
        }

        public async Task<List<Employee>> GetAllActiveEmployeesAsync()
        {
            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    return [];
                }

                string sql = @"
                    SELECT 
                        e.employee_id, 
                        p.last_name, 
                        p.first_name, 
                        p.middle_name, 
                        e.position, 
                        e.created_date, 
                        e.is_active
                    FROM employee e
                    JOIN person p ON e.person_id = p.person_id
                    WHERE e.is_active = 1
                    ORDER BY p.last_name, p.first_name;
                ";

                var dataTable = await _databaseService.ExecuteReaderAsync(sql);
                var employees = new List<Employee>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var person = new Person
                    {
                        LastName = row["last_name"].ToString()!,
                        FirstName = row["first_name"].ToString()!,
                        MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null
                    };

                    var employee = new Employee
                    {
                        EmployeeId = Convert.ToInt32(row["employee_id"]),
                        Position = row["position"].ToString()!,
                        CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                        IsActive = Convert.ToBoolean(row["is_active"]),
                        Person = person
                    };

                    employees.Add(employee);
                }

                return employees;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting employees: {ex.Message}");
                return [];
            }
        }

        public async Task<int> AddEmployeeAsync(
            string lastName,
            string firstName,
            string? middleName,
            bool isMale,
            string? phone,
            string? email,
            string position,
            bool isActive = true,
            string? basisType = null,
            string? documentNumber = null,
            DateTime? documentDate = null)
        {
            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    throw new InvalidOperationException("База данных недоступна");
                }

                using var connection = new SqliteConnection(SQLiteInitializationService.GetConnectionString());
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Шаг 1: Вставка в person
                    string personSql = @"
                        INSERT INTO person (last_name, first_name, middle_name, phone, email, is_male)
                        VALUES (@lastName, @firstName, @middleName, @phone, @email, @isMale);
                        SELECT last_insert_rowid();
                    ";

                    using var personCmd = new SqliteCommand(personSql, connection, transaction);
                    personCmd.Parameters.AddWithValue("@lastName", lastName);
                    personCmd.Parameters.AddWithValue("@firstName", firstName);
                    personCmd.Parameters.AddWithValue("@middleName", middleName ?? (object)DBNull.Value);
                    personCmd.Parameters.AddWithValue("@phone", phone ?? (object)DBNull.Value);
                    personCmd.Parameters.AddWithValue("@email", email ?? (object)DBNull.Value);
                    personCmd.Parameters.AddWithValue("@isMale", isMale ? 1 : 0);

                    var personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());

                    // Шаг 2: Вставка в basis, если переданы все нужные параметры
                    int? basisId = null;
                    if (!string.IsNullOrEmpty(basisType) && !string.IsNullOrEmpty(documentNumber) && documentDate.HasValue)
                    {
                        string basisSql = @"
                            INSERT INTO basis (basis_type, document_number, document_date)
                            VALUES (@basisType, @documentNumber, @documentDate);
                            SELECT last_insert_rowid();
                        ";

                        using var basisCmd = new SqliteCommand(basisSql, connection, transaction);
                        basisCmd.Parameters.AddWithValue("@basisType", basisType);
                        basisCmd.Parameters.AddWithValue("@documentNumber", documentNumber);
                        basisCmd.Parameters.AddWithValue("@documentDate", documentDate.Value.ToString("yyyy-MM-dd"));

                        basisId = Convert.ToInt32(await basisCmd.ExecuteScalarAsync());
                    }

                    // Шаг 3: Вставка в employee
                    string employeeSql = @"
                        INSERT INTO employee (position, created_date, is_active, basis_id, person_id)
                        VALUES (@position, @createdDate, @isActive, @basisId, @personId);
                        SELECT last_insert_rowid();
                    ";

                    using var employeeCmd = new SqliteCommand(employeeSql, connection, transaction);
                    employeeCmd.Parameters.AddWithValue("@position", position);
                    employeeCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd"));
                    employeeCmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
                    employeeCmd.Parameters.AddWithValue("@basisId", basisId ?? (object)DBNull.Value);
                    employeeCmd.Parameters.AddWithValue("@personId", personId);

                    var employeeId = Convert.ToInt32(await employeeCmd.ExecuteScalarAsync());

                    transaction.Commit();
                    return employeeId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding employee: {ex.Message}");
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
        {
            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    return null;
                }

                string sql = @"
                    SELECT 
                        e.employee_id, 
                        p.last_name, 
                        p.first_name, 
                        p.middle_name, 
                        e.position, 
                        e.created_date, 
                        e.is_active
                    FROM employee e
                    JOIN person p ON e.person_id = p.person_id
                    WHERE e.employee_id = @employeeId;
                ";

                var parameters = new Dictionary<string, object>
                {
                    { "@employeeId", employeeId }
                };

                var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

                if (dataTable.Rows.Count == 0)
                {
                    return null;
                }

                var row = dataTable.Rows[0];
                var person = new Person
                {
                    LastName = row["last_name"].ToString()!,
                    FirstName = row["first_name"].ToString()!,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null
                };

                return new Employee
                {
                    EmployeeId = Convert.ToInt32(row["employee_id"]),
                    Position = row["position"].ToString()!,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = Convert.ToBoolean(row["is_active"]),
                    Person = person
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting employee: {ex.Message}");
                return null;
            }
        }
    }
}