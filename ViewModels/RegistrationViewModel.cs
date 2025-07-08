using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Services;
using System.ComponentModel;
using System.Windows;

namespace bankrupt_piterjust.ViewModels
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;

        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private string _position = string.Empty;
        private bool _isMale = true;
        private string _login = string.Empty;
        private string _password = string.Empty;
        private string _phone = string.Empty;
        private string _email = string.Empty;
        private bool _isBusy;

        public string LastName { get => _lastName; set { _lastName = value; OnPropertyChanged(nameof(LastName)); UpdateCanRegister(); } }
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(nameof(FirstName)); UpdateCanRegister(); } }
        public string MiddleName { get => _middleName; set { _middleName = value; OnPropertyChanged(nameof(MiddleName)); } }
        public string Position { get => _position; set { _position = value; OnPropertyChanged(nameof(Position)); UpdateCanRegister(); } }
        public bool IsMale { get => _isMale; set { _isMale = value; OnPropertyChanged(nameof(IsMale)); } }
        public string Login { get => _login; set { _login = value; OnPropertyChanged(nameof(Login)); UpdateCanRegister(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(nameof(Password)); UpdateCanRegister(); } }
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(nameof(Email)); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); UpdateCanRegister(); } }

        public bool CanRegister => !string.IsNullOrWhiteSpace(LastName) &&
                                    !string.IsNullOrWhiteSpace(FirstName) &&
                                    !string.IsNullOrWhiteSpace(Position) &&
                                    !string.IsNullOrWhiteSpace(Login) &&
                                    !string.IsNullOrWhiteSpace(Password) &&
                                    !IsBusy;

        public RelayCommand RegisterCommand { get; }
        public RelayCommand CancelCommand { get; }

        public RegistrationViewModel()
        {
            _authService = new AuthenticationService();
            RegisterCommand = new RelayCommand(async o => await RegisterAsync(o as Window), o => CanRegister);
            CancelCommand = new RelayCommand(o => CloseWindow(o as Window));
        }

        private void UpdateCanRegister()
        {
            OnPropertyChanged(nameof(CanRegister));
            RelayCommand.RaiseCanExecuteChanged();
        }

        private async Task RegisterAsync(Window? window)
        {
            if (!CanRegister) return;
            try
            {
                IsBusy = true;
                var id = await _authService.RegisterEmployeeAsync(LastName, FirstName, IsMale, Position, Login, Password, MiddleName, Phone, Email);
                if (id.HasValue)
                {
                    MessageBox.Show("Пользователь успешно зарегистрирован", "Регистрация", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseWindow(window);
                }
                else
                {
                    MessageBox.Show("Не удалось создать пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static void CloseWindow(Window? window)
        {
            window?.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
