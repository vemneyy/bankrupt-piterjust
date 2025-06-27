namespace bankrupt_piterjust.Models
{
    public class Person
    {
        public int PersonId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public bool IsMale { get; set; } = true;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }
}
