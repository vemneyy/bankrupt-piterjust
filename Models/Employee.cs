namespace bankrupt_piterjust.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int? BasisId { get; set; }
        public string? Basis { get; set; }
        public Basis? BasisInfo { get; set; }
        public int? PersonId { get; set; }
        public Person? Person { get; set; }
        public string FullName => Person?.FullName ?? $"{Login}";
    }
}
