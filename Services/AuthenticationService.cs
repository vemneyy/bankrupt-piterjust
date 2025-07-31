using bankrupt_piterjust.Models;

namespace bankrupt_piterjust.Services
{
    public class AuthenticationService
    {
        private readonly EmployeeService _employeeService;

        public AuthenticationService()
        {
            _employeeService = new EmployeeService();
        }

        /// <summary>
        /// Возвращает список всех активных сотрудников для выбора.
        /// </summary>
        public async Task<List<Employee>> GetActiveEmployeesAsync()
        {
            return await _employeeService.GetAllActiveEmployeesAsync();
        }

        /// <summary>
        /// Аутентификация сотрудника по ID.
        /// </summary>
        public async Task<Employee?> AuthenticateByIdAsync(int employeeId)
        {
            return await _employeeService.GetEmployeeByIdAsync(employeeId);
        }
    }
}