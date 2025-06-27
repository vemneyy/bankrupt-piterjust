namespace bankrupt_piterjust.Models
{
    public class Basis
    {
        public int BasisId { get; set; }
        public string BasisType { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
    }
}
