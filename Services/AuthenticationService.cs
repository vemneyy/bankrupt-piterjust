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
    e.employee_id, 
    p.last_name, 
    p.first_name, 
    p.middle_name, 
    e.position, 
    e.login,
    e.created_date, 
    e.is_active,
    (e.password_hash = crypt(@password, e.password_hash)) AS password_matches
FROM employee e
JOIN person p ON e.person_id = p.person_id
WHERE e.login = @login AND e.is_active = true;
";

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

                var person = new Person
                {
                    LastName = row["last_name"].ToString()!,
                    FirstName = row["first_name"].ToString()!,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null
                };

                return new Employee
                {
                    EmployeeId = row["employee_id"] is Int64 value ? (int)value : Convert.ToInt32(row["employee_id"]),
                    Position = row["position"].ToString()!,
                    Login = row["login"].ToString()!,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = Convert.ToBoolean(row["is_active"]),
                    Person = person
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