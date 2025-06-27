using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Helpers;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class AddDebtorViewModel : INotifyPropertyChanged
    {
        private readonly DebtorRepository _repository;
        private bool _isBusy;

        // Person properties
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private bool _isMale = true;
        private string _phone = string.Empty;
        private string _email = string.Empty;

        // Passport properties
        private string _passportSeries = string.Empty;
        private string _passportNumber = string.Empty;
        private string _passportIssuedBy = string.Empty;
        private string _passportDivisionCode = string.Empty;
        private DateTime _passportIssueDate = DateTime.Now.AddYears(-5);

        // Address properties
        private string _registrationAddress = string.Empty;
        private string _residenceAddress = string.Empty;
        private bool _sameAsRegistration = false;
        private string _mailingAddress = string.Empty;
        private bool _sameAsResidence = false;

        // Contract properties
        private string _contractNumber = string.Empty;
        private string _contractCity = "Санкт-Петербург";
        private DateTime _contractDate = DateTime.Now;
        private decimal _totalCost;
        private string _totalCostWords = string.Empty;
        private decimal _mandatoryExpenses;
        private string _mandatoryExpensesWords = string.Empty;
        private decimal _managerFee;
        private decimal _otherExpenses;
        private decimal _servicesAmount;
        private decimal _expensesAmount;
        private decimal _stage1Amount;
        private decimal _stage2Amount;
        private decimal _stage3Amount;
        private decimal _scheduleTotal;

        // Default status values - No UI selection needed as per requirements
        private readonly string _status = "Сбор документов";
        private readonly string _mainCategory = "Клиенты";
        private readonly string _filterCategory = "Сбор документов";

        // Результат - новый должник
        public Debtor NewDebtor { get; private set; }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CalculateTotalWordsCommand { get; }
        public ICommand CalculateMandatoryWordsCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        #region Person Properties
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); UpdateFullName(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); UpdateFullName(); }
        }

        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(nameof(MiddleName)); UpdateFullName(); }
        }

        public bool IsMale
        {
            get => _isMale;
            set { _isMale = value; OnPropertyChanged(nameof(IsMale)); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(nameof(Phone)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            private set { _fullName = value; OnPropertyChanged(nameof(FullName)); }
        }
        #endregion

        #region Passport Properties
        public string PassportSeries
        {
            get => _passportSeries;
            set { _passportSeries = value; OnPropertyChanged(nameof(PassportSeries)); }
        }

        public string PassportNumber
        {
            get => _passportNumber;
            set { _passportNumber = value; OnPropertyChanged(nameof(PassportNumber)); }
        }

        public string PassportIssuedBy
        {
            get => _passportIssuedBy;
            set { _passportIssuedBy = value; OnPropertyChanged(nameof(PassportIssuedBy)); }
        }

        public string PassportDivisionCode
        {
            get => _passportDivisionCode;
            set { _passportDivisionCode = value; OnPropertyChanged(nameof(PassportDivisionCode)); }
        }

        public DateTime PassportIssueDate
        {
            get => _passportIssueDate;
            set { _passportIssueDate = value; OnPropertyChanged(nameof(PassportIssueDate)); }
        }
        #endregion

        #region Address Properties
        public string RegistrationAddress
        {
            get => _registrationAddress;
            set
            {
                _registrationAddress = value;
                OnPropertyChanged(nameof(RegistrationAddress));
                if (_sameAsRegistration)
                    ResidenceAddress = value;
            }
        }

        public string ResidenceAddress
        {
            get => _residenceAddress;
            set
            {
                _residenceAddress = value;
                OnPropertyChanged(nameof(ResidenceAddress));
                if (_sameAsResidence)
                    MailingAddress = value;
            }
        }

        public bool SameAsRegistration
        {
            get => _sameAsRegistration;
            set
            {
                _sameAsRegistration = value;
                OnPropertyChanged(nameof(SameAsRegistration));
                if (value)
                    ResidenceAddress = RegistrationAddress;
            }
        }

        public string MailingAddress
        {
            get => _mailingAddress;
            set { _mailingAddress = value; OnPropertyChanged(nameof(MailingAddress)); }
        }

        public bool SameAsResidence
        {
            get => _sameAsResidence;
            set
            {
                _sameAsResidence = value;
                OnPropertyChanged(nameof(SameAsResidence));
                if (value)
                    MailingAddress = ResidenceAddress;
            }
        }
        #endregion

        #region Contract Properties
        public string ContractNumber
        {
            get => _contractNumber;
            set { _contractNumber = value; OnPropertyChanged(nameof(ContractNumber)); }
        }

        public string ContractCity
        {
            get => _contractCity;
            set { _contractCity = value; OnPropertyChanged(nameof(ContractCity)); }
        }

        public DateTime ContractDate
        {
            get => _contractDate;
            set { _contractDate = value; OnPropertyChanged(nameof(ContractDate)); }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); UpdateContractSums(); }
        }

        public string TotalCostWords
        {
            get => _totalCostWords;
            set { _totalCostWords = value; OnPropertyChanged(nameof(TotalCostWords)); }
        }

        public decimal MandatoryExpenses
        {
            get => _mandatoryExpenses;
            set { _mandatoryExpenses = value; OnPropertyChanged(nameof(MandatoryExpenses)); UpdateContractSums(); }
        }

        public string MandatoryExpensesWords
        {
            get => _mandatoryExpensesWords;
            set { _mandatoryExpensesWords = value; OnPropertyChanged(nameof(MandatoryExpensesWords)); }
        }

        public decimal ManagerFee
        {
            get => _managerFee;
            set { _managerFee = value; OnPropertyChanged(nameof(ManagerFee)); UpdateContractSums(); }
        }

        public decimal OtherExpenses
        {
            get => _otherExpenses;
            set { _otherExpenses = value; OnPropertyChanged(nameof(OtherExpenses)); UpdateContractSums(); }
        }

        public decimal ServicesAmount
        {
            get => _servicesAmount;
            set { _servicesAmount = value; OnPropertyChanged(nameof(ServicesAmount)); }
        }

        public decimal ExpensesAmount
        {
            get => _expensesAmount;
            set { _expensesAmount = value; OnPropertyChanged(nameof(ExpensesAmount)); }
        }

        public decimal Stage1Amount
        {
            get => _stage1Amount;
            set { _stage1Amount = value; OnPropertyChanged(nameof(Stage1Amount)); }
        }

        public decimal Stage2Amount
        {
            get => _stage2Amount;
            set { _stage2Amount = value; OnPropertyChanged(nameof(Stage2Amount)); }
        }

        public decimal Stage3Amount
        {
            get => _stage3Amount;
            set { _stage3Amount = value; OnPropertyChanged(nameof(Stage3Amount)); }
        }

        public decimal ScheduleTotal
        {
            get => _scheduleTotal;
            set { _scheduleTotal = value; OnPropertyChanged(nameof(ScheduleTotal)); }
        }

        // Payment schedule properties
        private int _scheduleMonths = 12;
        private ObservableCollection<PaymentSchedule> _paymentSchedule = new();

        public int ScheduleMonths
        {
            get => _scheduleMonths;
            set { _scheduleMonths = value; OnPropertyChanged(nameof(ScheduleMonths)); }
        }

        public ObservableCollection<PaymentSchedule> PaymentSchedule
        {
            get => _paymentSchedule;
            set
            {
                if (_paymentSchedule != null)
                {
                    _paymentSchedule.CollectionChanged -= PaymentSchedule_CollectionChanged;
                    foreach (var item in _paymentSchedule)
                        item.PropertyChanged -= PaymentItem_PropertyChanged;
                }

                _paymentSchedule = value;

                if (_paymentSchedule != null)
                {
                    _paymentSchedule.CollectionChanged += PaymentSchedule_CollectionChanged;
                    foreach (var item in _paymentSchedule)
                        item.PropertyChanged += PaymentItem_PropertyChanged;
                }

                OnPropertyChanged(nameof(PaymentSchedule));
                UpdateScheduleTotal();
            }
        }

        public ICommand GenerateScheduleCommand { get; }
        #endregion

        public AddDebtorViewModel()
        {
            _repository = new DebtorRepository();

            // Initialize non-nullable properties and fields
            NewDebtor = new Debtor();
            _fullName = string.Empty;

            SaveCommand = new RelayCommand(async o => await SaveDataAsync(), CanSave);
            CancelCommand = new RelayCommand(o =>
            {
                var window = Window.GetWindow(o as DependencyObject);
                if (window != null)
                {
                    window.DialogResult = false;
                }
            });
            CalculateTotalWordsCommand = new RelayCommand(o => TotalCostWords = NumberToWordsConverter.ConvertToWords(TotalCost));
            CalculateMandatoryWordsCommand = new RelayCommand(o => MandatoryExpensesWords = NumberToWordsConverter.ConvertToWords(MandatoryExpenses));
            GenerateScheduleCommand = new RelayCommand(o => GenerateSchedule());

            PaymentSchedule.CollectionChanged += PaymentSchedule_CollectionChanged;
        }

        private void UpdateFullName()
        {
            FullName = $"{LastName} {FirstName} {MiddleName}".Trim();
        }

        private bool CanSave(object? parameter)
        {
            // Basic validation
            return !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(RegistrationAddress);
        }

        private async Task SaveDataAsync()
        {
            try
            {
                IsBusy = true;

                var fullRepository = new FullDatabaseRepository();

                // Create models from UI data
                var person = new Person
                {
                    LastName = LastName,
                    FirstName = FirstName,
                    MiddleName = MiddleName,
                    IsMale = IsMale,
                    Phone = Phone,
                    Email = Email
                };

                var passport = new Passport
                {
                    Series = PassportSeries,
                    Number = PassportNumber,
                    IssuedBy = PassportIssuedBy,
                    DivisionCode = PassportDivisionCode,
                    IssueDate = PassportIssueDate
                };

                var addresses = new List<Address>();

                // Registration address
                if (!string.IsNullOrWhiteSpace(RegistrationAddress))
                {
                    addresses.Add(new Address
                    {
                        AddressText = RegistrationAddress,
                        AddressType = AddressType.Registration
                    });
                }

                // Residence address (if different from registration)
                if (!SameAsRegistration && !string.IsNullOrWhiteSpace(ResidenceAddress))
                {
                    addresses.Add(new Address
                    {
                        AddressText = ResidenceAddress,
                        AddressType = AddressType.Residence
                    });
                }

                // Mailing address (if different from residence)
                if (!SameAsResidence && !string.IsNullOrWhiteSpace(MailingAddress))
                {
                    addresses.Add(new Address
                    {
                        AddressText = MailingAddress,
                        AddressType = AddressType.Mailing
                    });
                }

                // Save person and debtor to database
                int personId = await _repository.AddPersonWithDetailsAsync(person, passport, addresses, Status, MainCategory, FilterCategory);

                // Save contract if contract fields are filled
                if (ShouldSaveContract())
                {
                    int debtorId = await fullRepository.GetDebtorIdByPersonIdAsync(personId);

                    // Get current employee ID (you may need to implement this based on current user session)
                    int employeeId = GetCurrentEmployeeId();

                    UpdateScheduleTotal();
                    var contract = new Contract
                    {
                        ContractNumber = ContractNumber,
                        City = ContractCity,
                        ContractDate = ContractDate,
                        DebtorId = debtorId,
                        EmployeeId = employeeId,
                        TotalCost = TotalCost,
                        TotalCostWords = TotalCostWords,
                        MandatoryExpenses = MandatoryExpenses,
                        MandatoryExpensesWords = MandatoryExpensesWords,
                        ManagerFee = ManagerFee,
                        OtherExpenses = OtherExpenses,
                        Stage1Cost = Stage1Amount,
                        Stage2Cost = Stage2Amount,
                        Stage3Cost = Stage3Amount
                    };

                    int contractId = await fullRepository.CreateContractAsync(contract);

                    // Save payment schedule if it exists
                    if (PaymentSchedule.Any())
                    {
                        foreach (var payment in PaymentSchedule)
                        {
                            var schedule = new PaymentSchedule
                            {
                                ContractId = contractId,
                                Stage = payment.Stage,
                                Description = payment.Description,
                                Amount = payment.Amount,
                                AmountWords = payment.AmountWords,
                                DueDate = payment.DueDate
                            };

                            await fullRepository.CreatePaymentScheduleAsync(schedule);
                        }
                    }
                }

                // Create a Debtor object for UI display
                NewDebtor = new Debtor
                {
                    PersonId = personId,
                    FullName = FullName,
                    Region = RegistrationAddress,
                    Status = Status,
                    MainCategory = MainCategory,
                    FilterCategory = FilterCategory,
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                };

                // Close the window with success
                var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                if (window != null)
                {
                    window.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ShouldSaveContract()
        {
            return !string.IsNullOrWhiteSpace(ContractNumber) &&
                   (TotalCost > 0 || MandatoryExpenses > 0 || ManagerFee > 0 || OtherExpenses > 0);
        }

        private int GetCurrentEmployeeId()
        {
            // Get employee ID from user session
            var currentEmployee = UserSessionService.Instance.CurrentEmployee;
            if (currentEmployee != null)
            {
                return currentEmployee.EmployeeId;
            }

            // Default to 1 if no employee is logged in (you might want to handle this differently)
            return 1;
        }

        private void GenerateSchedule()
        {
            PaymentSchedule.Clear();
            if (ScheduleMonths <= 0)
                return;

            decimal monthly = ScheduleMonths > 0 ? Math.Round(ManagerFee / ScheduleMonths, 2) : 0m;
            for (int i = 1; i <= ScheduleMonths; i++)
            {
                PaymentSchedule.Add(new PaymentSchedule
                {
                    Stage = i,
                    Description = $"Платеж {i}",
                    Amount = monthly,
                    DueDate = ContractDate.AddMonths(i - 1)
                });
            }
            UpdateScheduleTotal();
        }

        private void UpdateContractSums()
        {
            ExpensesAmount = MandatoryExpenses + ManagerFee + OtherExpenses;
            ServicesAmount = TotalCost - ExpensesAmount;
        }

        private void UpdateScheduleTotal()
        {
            ScheduleTotal = PaymentSchedule.Sum(p => p.Amount);
        }

        private void PaymentSchedule_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PaymentSchedule item in e.OldItems)
                    item.PropertyChanged -= PaymentItem_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (PaymentSchedule item in e.NewItems)
                    item.PropertyChanged += PaymentItem_PropertyChanged;
            }
            UpdateScheduleTotal();
        }

        private void PaymentItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentSchedule.Amount))
            {
                UpdateScheduleTotal();
            }
        }

        // Status properties - readonly with default values
        public string Status => _status;
        public string MainCategory => _mainCategory;
        public string FilterCategory => _filterCategory;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}