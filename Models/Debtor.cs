// Models/Debtor.cs
namespace bankrupt_piterjust.Models
{
    public class Debtor
    {
        public string FullName { get; set; }
        public string Region { get; set; }
        public string Status { get; set; } // Например, "Подать заявление"
        public string MainCategory { get; set; } // "Клиенты", "Лиды" и т.д.
        public string FilterCategory { get; set; } // "Подготовка заявления", "Сбор документов" и т.д.
        public string Date { get; set; }
        
        // Сохраняем предыдущую категорию для возможности восстановления из архива
        public string PreviousMainCategory { get; set; }
        public string PreviousFilterCategory { get; set; }
    }
}