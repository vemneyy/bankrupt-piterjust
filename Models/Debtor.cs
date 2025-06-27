namespace bankrupt_piterjust.Models
{
    public class Debtor
    {
        public string FullName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MainCategory { get; set; } = string.Empty;
        public string FilterCategory { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        public string? PreviousMainCategory { get; set; }
        public string? PreviousFilterCategory { get; set; }

        public int? PersonId { get; set; }

        public static Debtor FromPerson(Person person)
        {
            return new Debtor
            {
                PersonId = person.PersonId,
                FullName = person.FullName,
                Status = "Новый клиент",
                MainCategory = "Клиенты",
                FilterCategory = "Подготовка заявления",
                Date = DateTime.Now.ToString("dd.MM.yyyy")
            };
        }
    }
}