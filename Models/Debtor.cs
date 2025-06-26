namespace bankrupt_piterjust.Models
{
    public class Debtor
    {
        // Existing UI properties
        public string FullName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Например, "Подать заявление"
        public string MainCategory { get; set; } = string.Empty; // "Клиенты", "Лиды" и т.д.
        public string FilterCategory { get; set; } = string.Empty; // "Подготовка заявления", "Сбор документов" и т.д.
        public string Date { get; set; } = string.Empty;

        // Сохраняем предыдущую категорию для возможности восстановления из архива
        public string? PreviousMainCategory { get; set; }
        public string? PreviousFilterCategory { get; set; }

        // Database-related properties
        public int? PersonId { get; set; } // ID in the database

        // Helper method to create a Debtor from a Person
        public static Debtor FromPerson(Person person)
        {
            return new Debtor
            {
                PersonId = person.PersonId,
                FullName = person.FullName,
                // Other properties need to be set separately
                Status = "Новый клиент",
                MainCategory = "Клиенты",
                FilterCategory = "Подготовка заявления",
                Date = DateTime.Now.ToString("dd.MM.yyyy")
            };
        }
    }
}