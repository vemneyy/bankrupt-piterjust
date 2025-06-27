namespace bankrupt_piterjust.Models
{
    public class Person
    {
        public int PersonId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }

    public class Passport
    {
        public int PassportId { get; set; }
        public int PersonId { get; set; }
        public string Series { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string IssuedBy { get; set; } = string.Empty;
        public string? DivisionCode { get; set; }
        public DateTime IssueDate { get; set; }
    }

    public enum AddressType
    {
        Registration,
        Residence,
        Mailing
    }

    public class Address
    {
        public int AddressId { get; set; }
        public int PersonId { get; set; }
        public AddressType AddressType { get; set; }
        public string AddressText { get; set; } = string.Empty;
    }

    public class Company
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Inn { get; set; } = string.Empty;
        public string? Kpp { get; set; }
        public string? Ogrn { get; set; }
        public string? Okpo { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Basis { get; set; }
        public int? PersonId { get; set; }
        
        // Navigation properties
        public Person? Person { get; set; }
        
        public string FullName => Person?.FullName ?? $"{Login}";
    }

    public class Contract
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime ContractDate { get; set; }
        public int DebtorId { get; set; }
        public int EmployeeId { get; set; }
        public decimal TotalCost { get; set; }
        public string? TotalCostWords { get; set; }
        public decimal MandatoryExpenses { get; set; }
        public string? MandatoryExpensesWords { get; set; }
        public decimal ManagerFee { get; set; }
        public decimal OtherExpenses { get; set; }
        
        // Navigation properties
        public Employee? Employee { get; set; }
        public Person? Debtor { get; set; }
    }

    public class PaymentSchedule
    {
        public int ScheduleId { get; set; }
        public int ContractId { get; set; }
        public int Stage { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? AmountWords { get; set; }
        public DateTime? DueDate { get; set; }
        
        // Navigation property
        public Contract? Contract { get; set; }
    }

    public class Status
    {
        public int StatusId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MainCategory
    {
        public int MainCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FilterCategory
    {
        public int FilterCategoryId { get; set; }
        public int MainCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Navigation property
        public MainCategory? MainCategory { get; set; }
    }

    public class DebtorEntity
    {
        public int DebtorId { get; set; }
        public int PersonId { get; set; }
        public int StatusId { get; set; }
        public int MainCategoryId { get; set; }
        public int FilterCategoryId { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public Person? Person { get; set; }
        public Status? Status { get; set; }
        public MainCategory? MainCategory { get; set; }
        public FilterCategory? FilterCategory { get; set; }
    }
}