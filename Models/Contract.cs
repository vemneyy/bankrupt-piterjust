namespace bankrupt_piterjust.Models
{
    public class Contract
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime ContractDate { get; set; }
        public int DebtorId { get; set; }
        public int EmployeeId { get; set; }
        public decimal TotalCost { get; set; }
        public decimal MandatoryExpenses { get; set; }
        public decimal ManagerFee { get; set; }
        public decimal OtherExpenses { get; set; }
        /// <summary>
        /// Стоимость юридических услуг (общая сумма этапов).
        /// </summary>
        public decimal ServicesAmount { get; set; }
        public decimal Stage1Cost { get; set; }
        public decimal Stage2Cost { get; set; }
        public decimal Stage3Cost { get; set; }
        public Employee? Employee { get; set; }
        public Person? Debtor { get; set; }
    }
}
