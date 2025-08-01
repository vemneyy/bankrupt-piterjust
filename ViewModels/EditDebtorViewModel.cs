using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class EditDebtorViewModel : INotifyPropertyChanged
    {
        private readonly DebtorRepository _repository;
        private readonly int _personId;
        private bool _isBusy;
        private int _originalEmployeeId; // Добавляем поле для хранения оригинального employee_id

        public string WindowTitle => "Редактирование должника";

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
        private readonly Address _registrationAddress = new();
        private readonly Address _residenceAddress = new();
        private bool _sameAsRegistration = false;
        private readonly Address _mailingAddress = new();
        private bool _sameAsResidence = false;

        // Contract properties
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
        private DateTime? _stage1DueDate;
        private DateTime? _stage2DueDate;
        private DateTime? _stage3DueDate;
        private decimal _scheduleTotal;
        private int _contractId;

        // Default status values - No UI selection needed as per requirements
        private readonly string _status = "Сбор документов";
        private readonly string _mainCategory = "Клиенты";
        private readonly string _filterCategory = "Сбор документов";

        // Commands
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

        private string _fullName = string.Empty;
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
                UpdateContractSums();
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
                ScheduleMonths = _paymentSchedule?.Count ?? 0;
                UpdateScheduleTotal();
            }
        }
        #endregion

        public EditDebtorViewModel(int personId)
        {
            _repository = new DebtorRepository();
            _personId = personId;

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

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var person = await _repository.GetPersonByIdAsync(_personId);
                if (person != null)
                {
                    LastName = person.LastName;
                    FirstName = person.FirstName;
                    MiddleName = person.MiddleName ?? string.Empty;
                    IsMale = person.IsMale;
                    Phone = person.Phone ?? string.Empty;
                    Email = person.Email ?? string.Empty;
                }

                var passport = await _repository.GetPassportByPersonIdAsync(_personId);
                if (passport != null)
                {
                    PassportSeries = passport.Series;
                    PassportNumber = passport.Number;
                    PassportIssuedBy = passport.IssuedBy;
                    PassportDivisionCode = passport.DivisionCode ?? string.Empty;
                    PassportIssueDate = passport.IssueDate;
                }

                var addresses = await _repository.GetAddressesByPersonIdAsync(_personId);
                if (addresses.Count > 0)
                    CopyAddress(addresses[0], RegistrationAddress);
                if (addresses.Count > 1)
                    CopyAddress(addresses[1], ResidenceAddress);
                if (addresses.Count > 2)
                    CopyAddress(addresses[2], MailingAddress);

                SameAsRegistration = FormatAddress(ResidenceAddress) == FormatAddress(RegistrationAddress) && !ResidenceAddress.IsEmpty();
                SameAsResidence = FormatAddress(MailingAddress) == FormatAddress(ResidenceAddress) && !MailingAddress.IsEmpty();

                var fullRepo = new FullDatabaseRepository();
                int debtorId = await fullRepo.GetDebtorIdByPersonIdAsync(_personId);
                var contract = await fullRepo.GetLatestContractByDebtorIdAsync(debtorId);
                if (contract != null)
                {
                    _contractId = contract.ContractId;
                    _originalEmployeeId = contract.EmployeeId; // Сохраняем оригинальный employee_id
                    ContractNumber = contract.ContractNumber;
                    ContractCity = contract.City;
                    ContractDate = contract.ContractDate;
                    TotalCost = contract.TotalCost;
                    MandatoryExpenses = contract.MandatoryExpenses;
                    ManagerFee = contract.ManagerFee;
                    OtherExpenses = contract.OtherExpenses;
                    var stages = await fullRepo.GetContractStagesByContractIdAsync(contract.ContractId);
                    foreach (var st in stages)
                    {
                        switch (st.Stage)
                        {
                            case 1:
                                Stage1Amount = st.Amount;
                                Stage1DueDate = st.DueDate;
                                break;
                            case 2:
                                Stage2Amount = st.Amount;
                                Stage2DueDate = st.DueDate;
                                break;
                            case 3:
                                Stage3Amount = st.Amount;
                                Stage3DueDate = st.DueDate;
                                break;
                        }
                    }

                    var schedule = await fullRepo.GetPaymentScheduleByContractIdAsync(contract.ContractId);
                    PaymentSchedule = new ObservableCollection<PaymentSchedule>(schedule);
                    ScheduleMonths = PaymentSchedule.Count;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                var person = new Person
                {
                    PersonId = _personId,
                    LastName = LastName,
                    FirstName = FirstName,
                    MiddleName = string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName,
                    IsMale = IsMale,
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email
                };

                var passport = new Passport
                {
                    PersonId = _personId,
                    Series = PassportSeries,
                    Number = PassportNumber,
                    IssuedBy = PassportIssuedBy,
                    DivisionCode = string.IsNullOrWhiteSpace(PassportDivisionCode) ? null : PassportDivisionCode,
                    IssueDate = PassportIssueDate
                };

                var addresses = new List<Address>();
                if (!RegistrationAddress.IsEmpty())
                {

                    addresses.Add(RegistrationAddress);
                }
                if (!SameAsRegistration && !ResidenceAddress.IsEmpty())
                {

                    addresses.Add(ResidenceAddress);
                }
                if (!SameAsResidence && !MailingAddress.IsEmpty())
                {

                    addresses.Add(MailingAddress);
                }

                await _repository.UpdatePersonAsync(person);
                await _repository.UpsertPassportAsync(passport);
                await _repository.ReplaceAddressesAsync(_personId, addresses);

                if (ShouldSaveContract())
                {
                    var fullRepo = new FullDatabaseRepository();
                    int debtorId = await fullRepo.GetDebtorIdByPersonIdAsync(_personId);

                    UpdateScheduleTotal();
                    var contract = new Contract
                    {
                        ContractId = _contractId,
                        ContractNumber = ContractNumber,
                        City = ContractCity,
                        ContractDate = ContractDate,
                        DebtorId = debtorId,
                        // При редактировании существующего контракта используем оригинальный EmployeeId
                        // При создании нового контракта используем текущего сотрудника
                        EmployeeId = _contractId > 0 ? _originalEmployeeId : CurrentEmployeeId,
                        TotalCost = TotalCost,
                        MandatoryExpenses = MandatoryExpenses,
                        ManagerFee = ManagerFee,
                        OtherExpenses = OtherExpenses,
                        ServicesAmount = ServicesAmount
                    };

                    if (_contractId > 0)
                    {
                        await fullRepo.UpdateContractAsync(contract);
                        await fullRepo.DeleteContractStagesByContractIdAsync(_contractId);
                        await fullRepo.DeletePaymentScheduleByContractIdAsync(_contractId);
                    }
                    else
                    {
                        _contractId = await fullRepo.CreateContractAsync(contract);
                    }

                    var stages = new List<ContractStage>
                    {
                        new ContractStage { ContractId = _contractId, Stage = 1, Amount = Stage1Amount, DueDate = Stage1DueDate ?? ContractDate },
                        new ContractStage { ContractId = _contractId, Stage = 2, Amount = Stage2Amount, DueDate = Stage2DueDate ?? ContractDate },
                        new ContractStage { ContractId = _contractId, Stage = 3, Amount = Stage3Amount, DueDate = Stage3DueDate ?? ContractDate }
                    };

                    foreach (var st in stages)
                        await fullRepo.CreateContractStageAsync(st);

                    foreach (var payment in PaymentSchedule)
                    {
                        var schedule = new PaymentSchedule
                        {
                            ContractId = _contractId,
                            Stage = payment.Stage,
                            Description = payment.Description,
                            Amount = payment.Amount,
                            DueDate = payment.DueDate
                        };

                        await fullRepo.CreatePaymentScheduleAsync(schedule);
                    }
                }

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
            ExpensesAmount = ManagerFee + OtherExpenses;
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

        private static string FormatAddress(Address address)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.PostalCode)) parts.Add(address.PostalCode);
            if (!string.IsNullOrWhiteSpace(address.Country)) parts.Add(address.Country);
            if (!string.IsNullOrWhiteSpace(address.Region)) parts.Add(address.Region);
            if (!string.IsNullOrWhiteSpace(address.District)) parts.Add(address.District);
            if (!string.IsNullOrWhiteSpace(address.City)) parts.Add(address.City);
            if (!string.IsNullOrWhiteSpace(address.Locality)) parts.Add(address.Locality);
            if (!string.IsNullOrWhiteSpace(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrWhiteSpace(address.HouseNumber)) parts.Add(address.HouseNumber);
            if (!string.IsNullOrWhiteSpace(address.Building)) parts.Add("к." + address.Building);
            if (!string.IsNullOrWhiteSpace(address.Apartment)) parts.Add("кв." + address.Apartment);
            return string.Join(", ", parts);
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
