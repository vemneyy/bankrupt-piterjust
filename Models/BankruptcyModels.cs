using System;

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

        // Helper property to get full name
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

    public class CompanyRepresentative
    {
        public int RepresentativeId { get; set; }
        public int CompanyId { get; set; }
        public int PersonId { get; set; }
        public string Basis { get; set; } = string.Empty; // основание_действий_представителя
    }

    public class Contract
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime ContractDate { get; set; }
        public int CustomerId { get; set; }
        public int ExecutorCompanyId { get; set; }
        public int RepresentativeId { get; set; }
        public decimal TotalCost { get; set; }
        public string TotalCostWords { get; set; } = string.Empty;
        public decimal MandatoryExpenses { get; set; }
        public string MandatoryExpensesWords { get; set; } = string.Empty;
        public decimal ManagerFee { get; set; }
        public decimal OtherExpenses { get; set; }
    }

    public class PaymentSchedule
    {
        public int ScheduleId { get; set; }
        public int ContractId { get; set; }
        public int Stage { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AmountWords { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}