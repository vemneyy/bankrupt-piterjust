using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private string _position = string.Empty;
        private bool _isBusy;
        
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); OnPropertyChanged(nameof(CanLogin)); }
        }
        
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); OnPropertyChanged(nameof(CanLogin)); }
        }
        
        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(nameof(MiddleName)); }
        }
        
        public string Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); OnPropertyChanged(nameof(CanLogin)); }
        }
        
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }
        
        public bool CanLogin => !string.IsNullOrWhiteSpace(LastName) && 
                                !string.IsNullOrWhiteSpace(FirstName) && 
                                !string.IsNullOrWhiteSpace(Position) &&
                                !IsBusy;
        
        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        
        // Authentication result
        public Employee? AuthenticatedEmployee { get; private set; }
        
        public LoginViewModel()
        {
            _authService = new AuthenticationService();
            LoginCommand = new RelayCommand(async o => await LoginAsync(), o => CanLogin);
            CancelCommand = new RelayCommand(o => CancelLogin(o as Window));
        }
        
        private async Task LoginAsync()
        {
            try
            {
                IsBusy = true;
                
                // Authentication logic
                AuthenticatedEmployee = await _authService.GetOrCreateEmployeeAsync(
                    LastName.Trim(),
                    FirstName.Trim(),
                    string.IsNullOrWhiteSpace(MiddleName) ? null : MiddleName.Trim(),
                    Position.Trim()
                );
                
                // Close the login window with success
                var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                if (window != null)
                {
                    window.DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                AuthenticatedEmployee = null;
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void CancelLogin(Window? window)
        {
            if (window != null)
            {
                window.DialogResult = false;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}