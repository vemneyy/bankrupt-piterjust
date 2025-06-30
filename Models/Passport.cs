namespace bankrupt_piterjust.Models
{
    public class Passport
    {
        public int PersonId { get; set; }
        public string Series { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string IssuedBy { get; set; } = string.Empty;
        public string? DivisionCode { get; set; }
        public DateTime IssueDate { get; set; }
    }

}
