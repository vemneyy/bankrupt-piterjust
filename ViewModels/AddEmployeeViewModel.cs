using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Services;
using System.ComponentModel;
using System.Windows;

namespace bankrupt_piterjust.ViewModels
{
    public class AddEmployeeViewModel : INotifyPropertyChanged
    {
        private readonly EmployeeService _employeeService;
        private bool _isBusy;

        // Person properties
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private bool _isMale = true;
        private string _phone = string.Empty;
        private string _email = string.Empty;

        // Employee properties
        private string _position = string.Empty;

        // Basis properties
        private bool _hasBasis = false;
        private string _basisType = "Доверенность";
        private string _documentNumber = string.Empty;
        private DateTime _documentDate = DateTime.Now;

        public string WindowTitle => "Регистрация нового сотрудника";

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        #region Person Properties
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); UpdateCanSave(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); UpdateCanSave(); }
        }

        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(nameof(MiddleName)); }
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
        #endregion

        #region Employee Properties
        public string Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); UpdateCanSave(); }
        }
        #endregion

        #region Basis Properties
        public bool HasBasis
        {
            get => _hasBasis;
            set { _hasBasis = value; OnPropertyChanged(nameof(HasBasis)); }
        }

        public string BasisType
        {
            get => _basisType;
            set { _basisType = value; OnPropertyChanged(nameof(BasisType)); }
        }

        public string DocumentNumber
        {
            get => _documentNumber;
            set { _documentNumber = value; OnPropertyChanged(nameof(DocumentNumber)); }
        }

        public DateTime DocumentDate
        {
            get => _documentDate;
            set { _documentDate = value; OnPropertyChanged(nameof(DocumentDate)); }
        }

        public string[] BasisTypes => ["Доверенность", "Приказ"];
        #endregion

        public bool CanSave => !string.IsNullOrWhiteSpace(LastName) &&
                               !string.IsNullOrWhiteSpace(FirstName) &&
                               !string.IsNullOrWhiteSpace(Position) &&
                               !IsBusy;

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public bool IsRegistrationSuccessful { get; private set; }

        public AddEmployeeViewModel()
        {
            _employeeService = new EmployeeService();

            SaveCommand = new RelayCommand(async o => await SaveEmployeeAsync(), o => CanSave);
            CancelCommand = new RelayCommand(o =>
            {
                var window = Window.GetWindow(o as DependencyObject);
                if (window != null)
                {
                    window.DialogResult = false;
                }
            });
        }

        private void UpdateCanSave()
        {
            OnPropertyChanged(nameof(CanSave));
        }

        private async Task SaveEmployeeAsync()
        {
            try
            {
                IsBusy = true;

                string? basisType = HasBasis ? BasisType : null;
                string? documentNumber = HasBasis && !string.IsNullOrWhiteSpace(DocumentNumber) ? DocumentNumber : null;
                DateTime? documentDate = HasBasis ? DocumentDate : null;

                await _employeeService.AddEmployeeAsync(
                    LastName.Trim(),
                    FirstName.Trim(),
                    string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim(),
                    IsMale,
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    Position.Trim(),
                    true, // isActive
                    basisType,
                    documentNumber,
                    documentDate);

                IsRegistrationSuccessful = true;

                var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                if (window != null)
                {
                    window.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}