using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class CompanyManagementViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private bool _isLoading;
        private Company? _selectedCompany;

        public ObservableCollection<Company> Companies { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public Company? SelectedCompany
        {
            get => _selectedCompany;
            set
            {
                _selectedCompany = value;
                OnPropertyChanged(nameof(SelectedCompany));
                OnPropertyChanged(nameof(IsCompanySelected));
            }
        }

        public bool IsCompanySelected => SelectedCompany != null;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand AddCompanyCommand { get; }
        public ICommand EditCompanyCommand { get; }
        public ICommand DeleteCompanyCommand { get; }

        public CompanyManagementViewModel()
        {
            _repository = new FullDatabaseRepository();
            Companies = [];

            RefreshCommand = new RelayCommand(async o => await LoadDataAsync());
            AddCompanyCommand = new RelayCommand(o => AddCompany());
            EditCompanyCommand = new RelayCommand(o => EditCompany(), o => IsCompanySelected);
            DeleteCompanyCommand = new RelayCommand(async o => await DeleteCompanyAsync(), o => IsCompanySelected);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var companies = await _repository.GetAllCompaniesAsync();
                Companies.Clear();
                foreach (var company in companies)
                {
                    Companies.Add(company);
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

        private void AddCompany()
        {
            var dialog = new CompanyEditDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.DataContext = new CompanyEditViewModel(_repository);

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditCompany()
        {
            if (SelectedCompany == null) return;

            var dialog = new CompanyEditDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.DataContext = new CompanyEditViewModel(_repository, SelectedCompany);

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private async Task DeleteCompanyAsync()
        {
            if (SelectedCompany == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить компанию '{SelectedCompany.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _repository.DeleteCompanyAsync(SelectedCompany.CompanyId);
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении компании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CompanyEditViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private readonly Company? _originalCompany;
        private bool _isEditMode;

        // Company properties
        private string _name = string.Empty;
        private string _inn = string.Empty;
        private string _kpp = string.Empty;
        private string _ogrn = string.Empty;
        private string _okpo = string.Empty;
        private string _address = string.Empty;
        private string _phone = string.Empty;
        private string _email = string.Empty;

        public bool IsEditMode => _isEditMode;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Inn
        {
            get => _inn;
            set { _inn = value; OnPropertyChanged(nameof(Inn)); }
        }

        public string Kpp
        {
            get => _kpp;
            set { _kpp = value; OnPropertyChanged(nameof(Kpp)); }
        }

        public string Ogrn
        {
            get => _ogrn;
            set { _ogrn = value; OnPropertyChanged(nameof(Ogrn)); }
        }

        public string Okpo
        {
            get => _okpo;
            set { _okpo = value; OnPropertyChanged(nameof(Okpo)); }
        }

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(nameof(Address)); }
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Constructor for new company
        public CompanyEditViewModel(FullDatabaseRepository repository)
        {
            _repository = repository;
            _originalCompany = null;
            _isEditMode = false;

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        // Constructor for editing existing company
        public CompanyEditViewModel(FullDatabaseRepository repository, Company company)
        {
            _repository = repository;
            _originalCompany = company;
            _isEditMode = true;

            LoadCompanyData(company);

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        private void LoadCompanyData(Company company)
        {
            Name = company.Name;
            Inn = company.Inn;
            Kpp = company.Kpp ?? string.Empty;
            Ogrn = company.Ogrn ?? string.Empty;
            Okpo = company.Okpo ?? string.Empty;
            Address = company.Address ?? string.Empty;
            Phone = company.Phone ?? string.Empty;
            Email = company.Email ?? string.Empty;
        }

        private bool CanSave(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Inn);
        }

        private async Task SaveAsync()
        {
            try
            {
                var company = new Company
                {
                    Name = Name,
                    Inn = Inn,
                    Kpp = string.IsNullOrWhiteSpace(Kpp) ? null : Kpp,
                    Ogrn = string.IsNullOrWhiteSpace(Ogrn) ? null : Ogrn,
                    Okpo = string.IsNullOrWhiteSpace(Okpo) ? null : Okpo,
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address,
                    Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email
                };

                if (_isEditMode && _originalCompany != null)
                {
                    company.CompanyId = _originalCompany.CompanyId;
                    await _repository.UpdateCompanyAsync(company);
                }
                else
                {
                    await _repository.CreateCompanyAsync(company);
                }

                CloseDialog(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении компании: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

    public class EmployeeManagementViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private bool _isLoading;
        private Employee? _selectedEmployee;

        public ObservableCollection<Employee> Employees { get; set; }
        public ObservableCollection<Person> Persons { get; set; }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged(nameof(SelectedEmployee));
                OnPropertyChanged(nameof(IsEmployeeSelected));
            }
        }

        public bool IsEmployeeSelected => SelectedEmployee != null;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand AddEmployeeCommand { get; }
        public ICommand EditEmployeeCommand { get; }
        public ICommand ToggleActiveCommand { get; }

        public EmployeeManagementViewModel()
        {
            _repository = new FullDatabaseRepository();
            Employees = [];
            Persons = [];

            RefreshCommand = new RelayCommand(async o => await LoadDataAsync());
            AddEmployeeCommand = new RelayCommand(o => AddEmployee());
            EditEmployeeCommand = new RelayCommand(o => EditEmployee(), o => IsEmployeeSelected);
            ToggleActiveCommand = new RelayCommand(async o => await ToggleActiveAsync(), o => IsEmployeeSelected);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var employees = await _repository.GetAllEmployeesAsync();
                Employees.Clear();
                foreach (var employee in employees)
                {
                    Employees.Add(employee);
                }

                // Load all persons for employee creation
                var debtorRepo = new DebtorRepository();
                var debtorData = await debtorRepo.GetAllDebtorsAsync();
                Persons.Clear();
                foreach (var debtor in debtorData.Where(d => d.PersonId.HasValue))
                {
                    var person = await _repository.GetPersonByIdAsync(debtor.PersonId.Value);
                    if (person != null)
                    {
                        Persons.Add(person);
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

        private void AddEmployee()
        {
            var dialog = new EmployeeEditDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.DataContext = new EmployeeEditViewModel(_repository, Persons.ToList());

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void EditEmployee()
        {
            if (SelectedEmployee == null) return;

            var dialog = new EmployeeEditDialog();
            dialog.Owner = Application.Current.MainWindow;
            dialog.DataContext = new EmployeeEditViewModel(_repository, Persons.ToList(), SelectedEmployee);

            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedEmployee == null) return;

            try
            {
                SelectedEmployee.IsActive = !SelectedEmployee.IsActive;
                await _repository.UpdateEmployeeAsync(SelectedEmployee);
                OnPropertyChanged(nameof(SelectedEmployee));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EmployeeEditViewModel : INotifyPropertyChanged
    {
        private readonly FullDatabaseRepository _repository;
        private readonly Employee? _originalEmployee;
        private bool _isEditMode;

        public List<Person> Persons { get; }

        // Employee properties
        private string _position = string.Empty;
        private string _login = string.Empty;
        private string _password = string.Empty;
        private bool _isActive = true;
        private string _basis = string.Empty;
        private Person? _selectedPerson;

        public bool IsEditMode => _isEditMode;

        public string Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); }
        }

        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(nameof(Login)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        public string Basis
        {
            get => _basis;
            set { _basis = value; OnPropertyChanged(nameof(Basis)); }
        }

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set { _selectedPerson = value; OnPropertyChanged(nameof(SelectedPerson)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Constructor for new employee
        public EmployeeEditViewModel(FullDatabaseRepository repository, List<Person> persons)
        {
            _repository = repository;
            _originalEmployee = null;
            _isEditMode = false;
            Persons = persons;

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        // Constructor for editing existing employee
        public EmployeeEditViewModel(FullDatabaseRepository repository, List<Person> persons, Employee employee)
        {
            _repository = repository;
            _originalEmployee = employee;
            _isEditMode = true;
            Persons = persons;

            LoadEmployeeData(employee);

            SaveCommand = new RelayCommand(async o => await SaveAsync(), CanSave);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        private void LoadEmployeeData(Employee employee)
        {
            Position = employee.Position;
            Login = employee.Login;
            IsActive = employee.IsActive;
            Basis = employee.Basis ?? string.Empty;
            SelectedPerson = Persons.FirstOrDefault(p => p.PersonId == employee.PersonId);
            // Don't load password for security reasons
        }

        private bool CanSave(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(Position) && !string.IsNullOrWhiteSpace(Login) &&
                   (_isEditMode || !string.IsNullOrWhiteSpace(Password));
        }

        private async Task SaveAsync()
        {
            try
            {
                var employee = new Employee
                {
                    Position = Position,
                    Login = Login,
                    IsActive = IsActive,
                    Basis = string.IsNullOrWhiteSpace(Basis) ? null : Basis,
                    PersonId = SelectedPerson?.PersonId,
                    CreatedDate = _isEditMode ? _originalEmployee?.CreatedDate : DateTime.Now
                };

                if (_isEditMode && _originalEmployee != null)
                {
                    employee.EmployeeId = _originalEmployee.EmployeeId;
                    // Only update password if a new one is provided
                    employee.PasswordHash = string.IsNullOrWhiteSpace(Password) ?
                        _originalEmployee.PasswordHash :
                        BCrypt.Net.BCrypt.HashPassword(Password);

                    await _repository.UpdateEmployeeAsync(employee);
                }
                else
                {
                    employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                    await _repository.CreateEmployeeAsync(employee);
                }

                CloseDialog(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    public class CompanyEditDialog : Window
    {
        public CompanyEditDialog()
        {
            Title = "Редактирование компании";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }

    public class EmployeeEditDialog : Window
    {
        public EmployeeEditDialog()
        {
            Title = "Редактирование сотрудника";
            Width = 450;
            Height = 350;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}