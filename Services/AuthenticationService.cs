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
        /// Authenticates an employee by login and password using PostgreSQL crypt function
        /// </summary>
        public async Task<Employee?> AuthenticateAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                // Test database connection first
                if (!await _databaseService.TestConnectionAsync())
                {
                    return null;
                }

                // Use PostgreSQL crypt function to verify password
                // The crypt function takes plain password and stored hash and returns the same hash if they match
                string sql = @"
                    SELECT 
                        employee_id, 
                        last_name, 
                        first_name, 
                        middle_name, 
                        position, 
                        login,
                        created_date, 
                        is_active,
                        (password_hash = crypt(@password, password_hash)) as password_matches
                    FROM employee 
                    WHERE login = @login AND is_active = true";

                var parameters = new Dictionary<string, object>
                {
                    { "@login", login },
                    { "@password", password }
                };

                var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

                if (dataTable.Rows.Count == 0)
                {
                    return null; // User not found
                }

                var row = dataTable.Rows[0];
                bool passwordMatches = Convert.ToBoolean(row["password_matches"]);

                if (!passwordMatches)
                {
                    return null; // Wrong password
                }

                return new Employee
                {
                    EmployeeId = row["employee_id"] is Int64 value ? (int)value : Convert.ToInt32(row["employee_id"]),
                    LastName = row["last_name"].ToString()!,
                    FirstName = row["first_name"].ToString()!,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                    Position = row["position"].ToString()!,
                    Login = row["login"].ToString()!,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = Convert.ToBoolean(row["is_active"])
                };
            }
            catch (Exception ex)
            {
                // Log error but don't expose details to UI
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a new employee with password hashed using PostgreSQL crypt function
        /// </summary>
        public async Task<bool> AddEmployeeAsync(string lastName, string firstName, string? middleName, string position, string login, string password)
        {
            if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || 
                string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(login) || 
                string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Все обязательные поля должны быть заполнены");
            }

            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    return false;
                }

                // Check if login already exists
                string checkSql = "SELECT COUNT(*) FROM employee WHERE login = @login";
                var checkParams = new Dictionary<string, object> { { "@login", login } };
                
                int existingCount = await _databaseService.ExecuteScalarAsync<int>(checkSql, checkParams);
                if (existingCount > 0)
                {
                    throw new Exception("Пользователь с таким логином уже существует");
                }

                // Insert new employee with PostgreSQL crypt function for password hashing
                string sql = @"
                    INSERT INTO employee (last_name, first_name, middle_name, position, login, password_hash, created_date, is_active)
                    VALUES (@lastName, @firstName, @middleName, @position, @login, crypt(@password, gen_salt('bf')), NOW(), true)";

                var parameters = new Dictionary<string, object>
                {
                    { "@lastName", lastName },
                    { "@firstName", firstName },
                    { "@middleName", middleName ?? (object)DBNull.Value },
                    { "@position", position },
                    { "@login", login },
                    { "@password", password }
                };

                int rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Add employee error: {ex.Message}");
                throw; // Re-throw to let caller handle the error message
            }
        }

        /// <summary>
        /// Gets an employee by ID
        /// </summary>
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
                        employee_id, 
                        last_name, 
                        first_name, 
                        middle_name, 
                        position, 
                        login,
                        created_date, 
                        is_active
                    FROM employee 
                    WHERE employee_id = @employeeId AND is_active = true";

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
                return new Employee
                {
                    EmployeeId = row["employee_id"] is Int64 value ? (int)value : Convert.ToInt32(row["employee_id"]),
                    LastName = row["last_name"].ToString()!,
                    FirstName = row["first_name"].ToString()!,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                    Position = row["position"].ToString()!,
                    Login = row["login"].ToString()!,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = Convert.ToBoolean(row["is_active"])
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get employee error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Changes employee password using PostgreSQL crypt function
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string login, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    return false;
                }

                // First verify the old password
                var employee = await AuthenticateAsync(login, oldPassword);
                if (employee == null)
                {
                    return false; // Wrong old password
                }

                // Update password with new hash
                string sql = @"
                    UPDATE employee 
                    SET password_hash = crypt(@newPassword, gen_salt('bf'))
                    WHERE login = @login AND is_active = true";

                var parameters = new Dictionary<string, object>
                {
                    { "@login", login },
                    { "@newPassword", newPassword }
                };

                int rowsAffected = await _databaseService.ExecuteNonQueryAsync(sql, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates default admin user if it doesn't exist
        /// </summary>
        public async Task EnsureDefaultAdminAsync()
        {
            try
            {
                if (!await _databaseService.TestConnectionAsync())
                {
                    return;
                }

                // Check if admin user exists
                string checkSql = "SELECT COUNT(*) FROM employee WHERE login = 'admin'";
                int adminCount = await _databaseService.ExecuteScalarAsync<int>(checkSql);

                if (adminCount == 0)
                {
                    // Create default admin user
                    await AddEmployeeAsync(
                        "Администратор",
                        "Система", 
                        null,
                        "Системный администратор",
                        "admin",
                        "admin123" // Default password - should be changed immediately
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ensure admin error: {ex.Message}");
            }
        }
    }
}