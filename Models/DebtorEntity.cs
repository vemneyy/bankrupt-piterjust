namespace bankrupt_piterjust.Models
{
    public class DebtorEntity
    {
        public int DebtorId { get; set; }
        public int PersonId { get; set; }
        public int StatusId { get; set; }
        public int MainCategoryId { get; set; }
        public int FilterCategoryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Person? Person { get; set; }
        public Status? Status { get; set; }
        public MainCategory? MainCategory { get; set; }
        public FilterCategory? FilterCategory { get; set; }
    }
}
