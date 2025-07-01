using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class ContractManagementViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private bool _isLoading;
        private Contract? _selectedContract;
        private ObservableCollection<PaymentSchedule> _paymentSchedules;

        public ObservableCollection<Contract> Contracts { get; set; }
        public ObservableCollection<Employee> Employees { get; set; }
        public ObservableCollection<Person> Debtors { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public Contract? SelectedContract
        {
            get => _selectedContract;
            set
            {
                _selectedContract = value;
                OnPropertyChanged(nameof(SelectedContract));
                OnPropertyChanged(nameof(IsContractSelected));
                _ = LoadPaymentScheduleAsync();
            }
        }

        public ObservableCollection<PaymentSchedule> PaymentSchedules
        {
            get => _paymentSchedules;
            set { _paymentSchedules = value; OnPropertyChanged(nameof(PaymentSchedules)); }
        }

        public bool IsContractSelected => SelectedContract != null;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand AddContractCommand { get; }
        public ICommand EditContractCommand { get; }
        public ICommand DeleteContractCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand EditPaymentCommand { get; }
        public ICommand DeletePaymentCommand { get; }

        public ContractManagementViewModel()
        {
            _repository = new FullDatabaseRepository();
            Contracts = [];
            Employees = [];
            Debtors = [];
            _paymentSchedules = [];

            RefreshCommand = new RelayCommand(async o => await LoadDataAsync());
            AddContractCommand = new RelayCommand(o => AddContract());
            EditContractCommand = new RelayCommand(o => EditContract(), o => IsContractSelected);
            DeleteContractCommand = new RelayCommand(async o => await DeleteContractAsync(), o => IsContractSelected);
            AddPaymentCommand = new RelayCommand(o => AddPayment(), o => IsContractSelected);
            EditPaymentCommand = new RelayCommand(o => EditPayment(o as PaymentSchedule), o => o is PaymentSchedule);
            DeletePaymentCommand = new RelayCommand(async o => await DeletePaymentAsync(o as PaymentSchedule), o => o is PaymentSchedule);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var contracts = await _repository.GetAllContractsAsync();
                Contracts.Clear();
                foreach (var contract in contracts)
                {
                    Contracts.Add(contract);
                }

                var employees = await _repository.GetAllEmployeesAsync();
                Employees.Clear();
                foreach (var employee in employees)
                {
                    Employees.Add(employee);
                }

                var debtorRepo = new DebtorRepository();
                var debtorData = await debtorRepo.GetAllDebtorsAsync();
                Debtors.Clear();
                foreach (var debtor in debtorData.Where(d => d.PersonId.HasValue))
                {
                    var personId = debtor.PersonId!.Value;
                    var person = await _repository.GetPersonByIdAsync(personId);
                    if (person != null)
                    {
                        Debtors.Add(person);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadPaymentScheduleAsync()
        {
            if (SelectedContract == null) return;

            try
            {
                var schedules = await _repository.GetPaymentScheduleByContractIdAsync(SelectedContract.ContractId);
                PaymentSchedules.Clear();
                foreach (var schedule in schedules)
                {
                    PaymentSchedules.Add(schedule);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке графика платежей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddContract()
        {
            var dialog = new ContractEditDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new ContractEditViewModel(_repository, [.. Employees], [.. Debtors])
            };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditContract()
        {
            if (SelectedContract == null) return;

            var dialog = new ContractEditDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new ContractEditViewModel(_repository, [.. Employees], [.. Debtors], SelectedContract)
            };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private async Task DeleteContractAsync()
        {
            if (SelectedContract == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить договор №{SelectedContract.ContractNumber}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.DeleteContractAsync(SelectedContract.ContractId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddPayment()
        {
            if (SelectedContract == null) return;

            var dialog = new PaymentEditDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new PaymentEditViewModel(_repository, SelectedContract.ContractId)
            };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadPaymentScheduleAsync();
            }
        }

        private void EditPayment(PaymentSchedule? payment)
        {
            if (payment == null) return;

            var dialog = new PaymentEditDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = new PaymentEditViewModel(_repository, payment)
            };

            if (dialog.ShowDialog() == true)
            {
                _ = LoadPaymentScheduleAsync();
            }
        }

        private async Task DeletePaymentAsync(PaymentSchedule? payment)
        {
            if (payment == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить платеж №{payment.Stage}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.DeletePaymentScheduleAsync(payment.ScheduleId);
                    await LoadPaymentScheduleAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении платежа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ContractEditViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private readonly Contract? _originalContract;
        private readonly bool _isEditMode;

        public List<Employee> Employees { get; }
        public List<Person> Debtors { get; }

        private string _contractNumber = string.Empty;
        private string _city = "Санкт-Петербург";
        private DateTime _contractDate = DateTime.Now;
        private Employee? _selectedEmployee;
        private Person? _selectedDebtor;
        private decimal _totalCost;
        private string _totalCostWords = string.Empty;
        private decimal _mandatoryExpenses;
        private string _mandatoryExpensesWords = string.Empty;
        private decimal _managerFee;
        private decimal _otherExpenses;

        public bool IsEditMode => _isEditMode;

        public string ContractNumber
        {
            get => _contractNumber;
            set { _contractNumber = value; OnPropertyChanged(nameof(ContractNumber)); }
        }

        public string City
        {
            get => _city;
            set { _city = value; OnPropertyChanged(nameof(City)); }
        }

        public DateTime ContractDate
        {
            get => _contractDate;
            set { _contractDate = value; OnPropertyChanged(nameof(ContractDate)); }
        }

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(nameof(SelectedEmployee)); }
        }

        public Person? SelectedDebtor
        {
            get => _selectedDebtor;
            set { _selectedDebtor = value; OnPropertyChanged(nameof(SelectedDebtor)); }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); }
        }

        public string TotalCostWords
        {
            get => _totalCostWords;
            set { _totalCostWords = value; OnPropertyChanged(nameof(TotalCostWords)); }
        }

        public decimal MandatoryExpenses
        {
            get => _mandatoryExpenses;
            set { _mandatoryExpenses = value; OnPropertyChanged(nameof(MandatoryExpenses)); }
        }

        public string MandatoryExpensesWords
        {
            get => _mandatoryExpensesWords;
            set { _mandatoryExpensesWords = value; OnPropertyChanged(nameof(MandatoryExpensesWords)); }
        }

        public decimal ManagerFee
        {
            get => _managerFee;
            set { _managerFee = value; OnPropertyChanged(nameof(ManagerFee)); }
        }

        public decimal OtherExpenses
        {
            get => _otherExpenses;
            set { _otherExpenses = value; OnPropertyChanged(nameof(OtherExpenses)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ContractEditViewModel(FullDatabaseRepository repository, List<Employee> employees, List<Person> debtors)
        {
            _repository = repository;
            _originalContract = null;
            _isEditMode = false;
            Employees = employees;
            Debtors = debtors;

            _managerFee = 25000m;
            _otherExpenses = 5000m;

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }
        public ContractEditViewModel(FullDatabaseRepository repository, List<Employee> employees, List<Person> debtors, Contract contract)
        {
            _repository = repository;
            _originalContract = contract;
            _isEditMode = true;
            Employees = employees;
            Debtors = debtors;

            LoadContractData(contract);

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        private void LoadContractData(Contract contract)
        {
            ContractNumber = contract.ContractNumber;
            City = contract.City;
            ContractDate = contract.ContractDate;
            SelectedEmployee = Employees.FirstOrDefault(e => e.EmployeeId == contract.EmployeeId);
            SelectedDebtor = Debtors.FirstOrDefault(d => d.PersonId == contract.DebtorId);
            TotalCost = contract.TotalCost;
            MandatoryExpenses = contract.MandatoryExpenses;
            ManagerFee = contract.ManagerFee;
            OtherExpenses = contract.OtherExpenses;
        }

        private bool CanSave(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(ContractNumber) &&
                   SelectedEmployee != null &&
                   SelectedDebtor != null;
        }

        private async Task SaveAsync()
        {
            try
            {
                int debtorId = await _repository.GetDebtorIdByPersonIdAsync(SelectedDebtor!.PersonId);

                var contract = new Contract
                {
                    ContractNumber = ContractNumber,
                    City = City,
                    ContractDate = ContractDate,
                    DebtorId = debtorId,
                    EmployeeId = SelectedEmployee!.EmployeeId,
                    TotalCost = TotalCost,
                    MandatoryExpenses = MandatoryExpenses,
                    ManagerFee = ManagerFee,
                    OtherExpenses = OtherExpenses
                };

                if (_isEditMode && _originalContract != null)
                {
                    contract.ContractId = _originalContract.ContractId;
                    await _repository.UpdateContractAsync(contract);
                }
                else
                {
                    await _repository.CreateContractAsync(contract);
                }

                CloseDialog(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog(bool result)
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PaymentEditViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private readonly PaymentSchedule? _originalPayment;
        private readonly int _contractId;
        private readonly bool _isEditMode;

        private int _stage = 1;
        private string _description = string.Empty;
        private decimal _amount;
        private string _amountWords = string.Empty;
        private DateTime? _dueDate = DateTime.Now.AddMonths(1);

        public bool IsEditMode => _isEditMode;

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

        public string AmountWords
        {
            get => _amountWords;
            set { _amountWords = value; OnPropertyChanged(nameof(AmountWords)); }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(nameof(DueDate)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Constructor for new payment
        public PaymentEditViewModel(FullDatabaseRepository repository, int contractId)
        {
            _repository = repository;
            _contractId = contractId;
            _originalPayment = null;
            _isEditMode = false;

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        // Constructor for editing existing payment
        public PaymentEditViewModel(FullDatabaseRepository repository, PaymentSchedule payment)
        {
            _repository = repository;
            _contractId = payment.ContractId;
            _originalPayment = payment;
            _isEditMode = true;

            LoadPaymentData(payment);

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        private void LoadPaymentData(PaymentSchedule payment)
        {
            Stage = payment.Stage;
            Description = payment.Description;
            Amount = payment.Amount;
            DueDate = payment.DueDate;
        }

        private bool CanSave(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Description) && Amount > 0;
        }

        private async Task SaveAsync()
        {
            try
            {
                var payment = new PaymentSchedule
                {
                    ContractId = _contractId,
                    Stage = Stage,
                    Description = Description,
                    Amount = Amount,
                    DueDate = DueDate
                };

                if (_isEditMode && _originalPayment != null)
                {
                    payment.ScheduleId = _originalPayment.ScheduleId;
                    await _repository.UpdatePaymentScheduleAsync(payment);
                }
                else
                {
                    await _repository.CreatePaymentScheduleAsync(payment);
                }

                CloseDialog(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении платежа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog(bool result)
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Placeholder classes for the dialogs - these would need to be implemented as actual WPF Windows
    public class ContractEditDialog : Window
    {
        public ContractEditDialog()
        {
            Title = "Редактирование договора";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }

    public class PaymentEditDialog : Window
    {
        public PaymentEditDialog()
        {
            Title = "Редактирование платежа";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}