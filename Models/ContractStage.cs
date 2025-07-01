namespace bankrupt_piterjust.Models
{
    public class ContractStage
    {
        public int ContractStageId { get; set; }
        public int ContractId { get; set; }
        public int Stage { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
    }
}
