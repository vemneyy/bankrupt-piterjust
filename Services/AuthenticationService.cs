using bankrupt_piterjust.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace bankrupt_piterjust.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _databaseService;
        
        public AuthenticationService()
        {
            _databaseService = new DatabaseService();
        }

        /// <summary>
        /// Finds or creates an employee by full name and position
        /// </summary>
        public async Task<Employee> GetOrCreateEmployeeAsync(string lastName, string firstName, string? middleName, string position)
        {
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(position))
            {
                throw new ArgumentException("Фамилия, имя и должность обязательны для заполнения");
            }
            
            // First try to find an existing employee
            var employee = await FindEmployeeAsync(lastName, firstName, middleName, position);
            if (employee != null)
            {
                return employee;
            }
            
            // Create a new employee
            return await CreateEmployeeAsync(lastName, firstName, middleName, position);
        }

        /// <summary>
        /// Finds an employee by name and position
        /// </summary>
        private async Task<Employee?> FindEmployeeAsync(string lastName, string firstName, string? middleName, string position)
        {
            string sql = @"
                SELECT employee_id, last_name, first_name, middle_name, position, created_date, is_active
                FROM employee 
                WHERE last_name = @lastName 
                AND first_name = @firstName
                AND position = @position";

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                sql += " AND (middle_name = @middleName OR middle_name IS NULL)";
            }
            else
            {
                sql += " AND middle_name IS NULL";
            }
            
            sql += " AND is_active = true";

            var parameters = new Dictionary<string, object>
            {
                { "@lastName", lastName },
                { "@firstName", firstName },
                { "@position", position }
            };

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                parameters.Add("@middleName", middleName);
            }

            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var row = dataTable.Rows[0];
            return new Employee
            {
                // Fix casting issue by using Convert.ToInt32 with proper handling of Int64
                EmployeeId = row["employee_id"] is Int64 value ? (int)value : Convert.ToInt32(row["employee_id"]),
                LastName = row["last_name"].ToString()!,
                FirstName = row["first_name"].ToString()!,
                MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                Position = row["position"].ToString()!,
                CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                IsActive = Convert.ToBoolean(row["is_active"])
            };
        }

        /// <summary>
        /// Creates a new employee
        /// </summary>
        private async Task<Employee> CreateEmployeeAsync(string lastName, string firstName, string? middleName, string position)
        {
            // Check if employee table exists, create if not
            await EnsureEmployeeTableExistsAsync();
            
            string sql = @"
                INSERT INTO employee (last_name, first_name, middle_name, position, created_date, is_active)
                VALUES (@lastName, @firstName, @middleName, @position, NOW(), true)
                RETURNING employee_id, created_date";

            var parameters = new Dictionary<string, object>
            {
                { "@lastName", lastName },
                { "@firstName", firstName },
                { "@middleName", middleName ?? (object)DBNull.Value },
                { "@position", position }
            };

            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            
            if (dataTable.Rows.Count == 0)
            {
                throw new Exception("Не удалось создать сотрудника");
            }

            var row = dataTable.Rows[0];
            
            // Fix casting issue by using Convert.ToInt32 with proper handling of Int64
            int employeeId = row["employee_id"] is Int64 value ? (int)value : Convert.ToInt32(row["employee_id"]);
            
            return new Employee
            {
                EmployeeId = employeeId,
                LastName = lastName,
                FirstName = firstName,
                MiddleName = middleName,
                Position = position,
                CreatedDate = Convert.ToDateTime(row["created_date"]),
                IsActive = true
            };
        }
        
        /// <summary>
        /// Ensures that the employee table exists
        /// </summary>
        private async Task EnsureEmployeeTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS employee (
                    employee_id SERIAL PRIMARY KEY,
                    last_name VARCHAR(100) NOT NULL,
                    first_name VARCHAR(100) NOT NULL,
                    middle_name VARCHAR(100),
                    position VARCHAR(200) NOT NULL,
                    created_date TIMESTAMP NOT NULL DEFAULT NOW(),
                    is_active BOOLEAN NOT NULL DEFAULT true
                )";

            await _databaseService.ExecuteNonQueryAsync(sql);
        }
    }
}