using bankrupt_piterjust.Models;

namespace bankrupt_piterjust.Services
{
    public class UserSessionService
    {
        private static readonly Lazy<UserSessionService> _instance = new(() => new UserSessionService());

        public static UserSessionService Instance => _instance.Value;

        private UserSessionService() { }

        public Employee? CurrentEmployee { get; private set; }

        public bool IsAuthenticated => CurrentEmployee != null;

        public void SetCurrentEmployee(Employee employee)
        {
            CurrentEmployee = employee;
        }

        public void ClearSession()
        {
            CurrentEmployee = null;
        }
    }
}