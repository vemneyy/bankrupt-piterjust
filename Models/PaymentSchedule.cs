using System.ComponentModel;

namespace bankrupt_piterjust.Models
{
    public class PaymentSchedule : INotifyPropertyChanged
    {
        private int _stage;
        private string _description = string.Empty;
        private decimal _amount;
        private string? _amountWords;
        private DateTime? _dueDate;
        public int ScheduleId { get; set; }
        public int ContractId { get; set; }
        public int Stage
        {
            get => _stage;
            set { _stage = value; OnPropertyChanged(nameof(Stage)); }
        }
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }
        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }
        public string? AmountWords
        {
            get => _amountWords;
            set { _amountWords = value; OnPropertyChanged(nameof(AmountWords)); }
        }
        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); }
        }
        public Contract? Contract { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
