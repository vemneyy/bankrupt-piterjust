using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class AddDebtorViewModel : INotifyPropertyChanged
    {
        private readonly DebtorRepository _repository;
        private bool _isBusy;

        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private bool _isMale = true;
        private string _phone = string.Empty;
        private string _email = string.Empty;

        private string _passportSeries = string.Empty;
        private string _passportNumber = string.Empty;
        private string _passportIssuedBy = string.Empty;
        private string _passportDivisionCode = string.Empty;
        private DateTime _passportIssueDate = DateTime.Now.AddYears(-5);

        private readonly Address _registrationAddress = new();
        private readonly Address _residenceAddress = new();
        private bool _sameAsRegistration = false;
        private readonly Address _mailingAddress = new();
        private bool _sameAsResidence = false;

        private string _contractNumber = string.Empty;
        private string _contractCity = "Санкт-Петербург";
        private DateTime _contractDate = DateTime.Now;
        private decimal _totalCost;
        private decimal _mandatoryExpenses;
        private decimal _managerFee;
        private decimal _otherExpenses;
        private decimal _servicesAmount;
        private decimal _expensesAmount;
        private decimal _stage1Amount;
        private decimal _stage2Amount;
        private decimal _stage3Amount;
        private DateTime? _stage1DueDate = DateTime.Now;
        private DateTime? _stage2DueDate = DateTime.Now;
        private DateTime? _stage3DueDate = DateTime.Now;
        private decimal _scheduleTotal;

        private readonly string _status = "Сбор документов";
        private readonly string _mainCategory = "Клиенты";
        private readonly string _filterCategory = "Сбор документов";

        public Debtor NewDebtor { get; private set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand GenerateScheduleCommand { get; }

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

        private DateTime _birthDate = DateTime.Now.AddYears(-18);
        public DateTime BirthDate
        {
            get => _birthDate;
            set { _birthDate = value; OnPropertyChanged(nameof(BirthDate)); }
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
        public Address RegistrationAddress => _registrationAddress;

        public Address ResidenceAddress => _residenceAddress;

        public bool SameAsRegistration
        {
            get => _sameAsRegistration;
            set
            {
                _sameAsRegistration = value;
                OnPropertyChanged(nameof(SameAsRegistration));
                if (value)
                {
                    CopyAddress(RegistrationAddress, ResidenceAddress);
                }
            }
        }

        public Address MailingAddress => _mailingAddress;

        public bool SameAsResidence
        {
            get => _sameAsResidence;
            set
            {
                _sameAsResidence = value;
                OnPropertyChanged(nameof(SameAsResidence));
                if (value)
                {
                    CopyAddress(ResidenceAddress, MailingAddress);
                }
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
            set
            {
                _totalCost = Math.Round(value, 2);
                OnPropertyChanged(nameof(TotalCost));
                UpdateContractSums();
            }
        }

        public decimal MandatoryExpenses
        {
            get => _mandatoryExpenses;
            set
            {
                _mandatoryExpenses = Math.Round(value, 2);
                OnPropertyChanged(nameof(MandatoryExpenses));
            }
        }

        public decimal ManagerFee
        {
            get => _managerFee;
            set
            {
                _managerFee = Math.Round(value, 2);
                OnPropertyChanged(nameof(ManagerFee));
                UpdateContractSums();
            }
        }

        public decimal OtherExpenses
        {
            get => _otherExpenses;
            set
            {
                _otherExpenses = Math.Round(value, 2);
                OnPropertyChanged(nameof(OtherExpenses));
                UpdateContractSums();
            }
        }

        public decimal ServicesAmount
        {
            get => _servicesAmount;
            set
            {
                _servicesAmount = Math.Round(value, 2);
                OnPropertyChanged(nameof(ServicesAmount));
            }
        }

        public decimal ExpensesAmount
        {
            get => _expensesAmount;
            set
            {
                _expensesAmount = Math.Round(value, 2);
                OnPropertyChanged(nameof(ExpensesAmount));
            }
        }

        public decimal Stage1Amount
        {
            get => _stage1Amount;
            set
            {
                var newValue = Math.Round(value, 2);
                if (newValue + Stage2Amount + Stage3Amount > ServicesAmount)
                {
                    MessageBox.Show("Сумма этапов не может превышать сумму юридических услуг", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _stage1Amount = newValue;

                OnPropertyChanged(nameof(Stage1Amount));
            }
        }

        public decimal Stage2Amount
        {
            get => _stage2Amount;
            set
            {
                var newValue = Math.Round(value, 2);
                if (Stage1Amount + newValue + Stage3Amount > ServicesAmount)
                {
                    MessageBox.Show("Сумма этапов не может превышать сумму юридических услуг", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _stage2Amount = newValue;

                OnPropertyChanged(nameof(Stage2Amount));
            }
        }

        public decimal Stage3Amount
        {
            get => _stage3Amount;

            set
            {
                var newValue = Math.Round(value, 2);
                if (Stage1Amount + Stage2Amount + newValue > ServicesAmount)
                {
                    MessageBox.Show("Сумма этапов не может превышать сумму юридических услуг", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _stage3Amount = newValue;
                OnPropertyChanged(nameof(Stage3Amount));
            }
        }

        public DateTime? Stage1DueDate
        {
            get => _stage1DueDate;
            set { _stage1DueDate = value; OnPropertyChanged(nameof(Stage1DueDate)); }
        }

        public DateTime? Stage2DueDate
        {
            get => _stage2DueDate;
            set { _stage2DueDate = value; OnPropertyChanged(nameof(Stage2DueDate)); }
        }

        public DateTime? Stage3DueDate
        {
            get => _stage3DueDate;
            set { _stage3DueDate = value; OnPropertyChanged(nameof(Stage3DueDate)); }
        }

        public decimal ScheduleTotal
        {
            get => _scheduleTotal;
            set
            {
                _scheduleTotal = Math.Round(value, 2);
                OnPropertyChanged(nameof(ScheduleTotal));
            }
        }

        // Payment schedule properties
        private int _scheduleMonths = 12;
        private ObservableCollection<PaymentSchedule> _paymentSchedule = [];

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
        #endregion

        public AddDebtorViewModel()
        {
            _repository = new DebtorRepository();

            NewDebtor = new Debtor();
            _fullName = string.Empty;

            _stage1DueDate = DateTime.Now;
            _stage2DueDate = DateTime.Now;
            _stage3DueDate = DateTime.Now;

            ManagerFee = 25000m;
            OtherExpenses = 20000m;

            SaveCommand = new RelayCommand(async o => await SaveDataAsync(), CanSave);
            CancelCommand = new RelayCommand(o =>
            {
                var window = Window.GetWindow(o as DependencyObject);
                if (window != null)
                {
                    window.DialogResult = false;
                }
            });
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
                   !RegistrationAddress.IsEmpty();
        }

        private async Task SaveDataAsync()
        {
            try
            {
                IsBusy = true;

                var fullRepository = new FullDatabaseRepository();

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

                if (!RegistrationAddress.IsEmpty())
                {
                    addresses.Add(RegistrationAddress);
                }

                int personId = await _repository.AddPersonWithDetailsAsync(person, passport, addresses, Status, MainCategory, FilterCategory);

                if (ShouldSaveContract())
                {
                    int debtorId = await fullRepository.GetDebtorIdByPersonIdAsync(personId);

                    int employeeId = CurrentEmployeeId;

                    UpdateScheduleTotal();
                    var contract = new Contract
                    {
                        ContractNumber = ContractNumber,
                        City = ContractCity,
                        ContractDate = ContractDate,
                        DebtorId = debtorId,
                        EmployeeId = employeeId,
                        TotalCost = TotalCost,
                        MandatoryExpenses = MandatoryExpenses,
                        ManagerFee = ManagerFee,
                        OtherExpenses = OtherExpenses,
                        ServicesAmount = ServicesAmount
                    };

                    int contractId = await fullRepository.CreateContractAsync(contract);

                    var stages = new List<ContractStage>
                    {
                        new ContractStage { ContractId = contractId, Stage = 1, Amount = Stage1Amount, DueDate = Stage1DueDate ?? ContractDate },
                        new ContractStage { ContractId = contractId, Stage = 2, Amount = Stage2Amount, DueDate = Stage2DueDate ?? ContractDate },
                        new ContractStage { ContractId = contractId, Stage = 3, Amount = Stage3Amount, DueDate = Stage3DueDate ?? ContractDate }
                    };

                    foreach (var st in stages)
                    {
                        await fullRepository.CreateContractStageAsync(st);
                    }

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
                                DueDate = payment.DueDate
                            };

                            await fullRepository.CreatePaymentScheduleAsync(schedule);
                        }
                    }
                }

                NewDebtor = new Debtor
                {
                    PersonId = personId,
                    FullName = FullName,
                    Region = RegistrationAddress.Region ?? string.Empty,
                    Status = Status,
                    MainCategory = MainCategory,
                    FilterCategory = FilterCategory,
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                };

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

        private static int CurrentEmployeeId
        {
            get
            {
                var currentEmployee = UserSessionService.Instance.CurrentEmployee;
                if (currentEmployee != null)
                {
                    return currentEmployee.EmployeeId;
                }
                return 1;
            }
        }

        private void GenerateSchedule()
        {
            PaymentSchedule.Clear();
            if (ScheduleMonths <= 0)
                return;

            decimal monthly = ScheduleMonths > 0 ? Math.Round(ServicesAmount / ScheduleMonths, 2) : 0m;
            for (int i = 1; i <= ScheduleMonths; i++)
            {
                PaymentSchedule.Add(new PaymentSchedule
                {
                    Stage = i,
                    Description = "Оплата консультационных юридических услуг",
                    Amount = monthly,
                    DueDate = ContractDate.AddMonths(i - 1)
                });
            }
            UpdateScheduleTotal();
        }

        private void UpdateContractSums()
        {
            MandatoryExpenses = ManagerFee + OtherExpenses;
            ServicesAmount = TotalCost - MandatoryExpenses;
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
            if (e.PropertyName == nameof(bankrupt_piterjust.Models.PaymentSchedule.Amount))
            {
                UpdateScheduleTotal();
            }

        }

        private static void CopyAddress(Address source, Address target)
        {
            target.PostalCode = source.PostalCode;
            target.Country = source.Country;
            target.Region = source.Region;
            target.District = source.District;
            target.City = source.City;
            target.Locality = source.Locality;
            target.Street = source.Street;
            target.HouseNumber = source.HouseNumber;
            target.Building = source.Building;
            target.Apartment = source.Apartment;
        }

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