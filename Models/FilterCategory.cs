namespace bankrupt_piterjust.Models
{
    public class FilterCategory
    {
        public int FilterCategoryId { get; set; }
        public int MainCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public MainCategory? MainCategory { get; set; }
    }
}
