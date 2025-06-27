using bankrupt_piterjust.Models;

namespace bankrupt_piterjust.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _databaseService;

        public AuthenticationService()
        {
            _databaseService = new DatabaseService();
        }

        public async Task<Employee?> AuthenticateAsync(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

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
                    return null;
                }

                var row = dataTable.Rows[0];
                bool passwordMatches = Convert.ToBoolean(row["password_matches"]);

                if (!passwordMatches)
                {
                    return null;
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
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return null;
            }
        }
    }
}