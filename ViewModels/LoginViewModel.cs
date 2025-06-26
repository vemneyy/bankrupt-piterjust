using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authService;
        private readonly DatabaseService _databaseService;
        
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _middleName = string.Empty;
        private string _position = string.Empty;
        private bool _isBusy;
        private string _login = string.Empty;
        private string _password = string.Empty;
        
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(nameof(LastName)); UpdateCanLogin(); }
        }
        
        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(nameof(FirstName)); UpdateCanLogin(); }
        }
        
        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(nameof(MiddleName)); }
        }
        
        public string Position
        {
            get => _position;
            set { _position = value; OnPropertyChanged(nameof(Position)); UpdateCanLogin(); }
        }
        
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); UpdateCanLogin(); }
        }
        
        public string Login 
        { 
            get => _login;
            set { _login = value; OnPropertyChanged(nameof(Login)); UpdateCanLogin(); }
        }
        
        public string Password 
        { 
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); UpdateCanLogin(); }
        }

        public bool CanLogin => !string.IsNullOrWhiteSpace(Login) && 
                                !string.IsNullOrWhiteSpace(Password) &&
                                !IsBusy;
        
        public RelayCommand LoginCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand DatabaseSettingsCommand { get; }
        
        // Authentication result
        public Employee? AuthenticatedEmployee { get; private set; }
        
        public LoginViewModel()
        {
            _authService = new AuthenticationService();
            _databaseService = new DatabaseService();
            LoginCommand = new RelayCommand(async o => await LoginAsync(), o => CanLogin);
            CancelCommand = new RelayCommand(o => CancelLogin(o as Window));
            DatabaseSettingsCommand = new RelayCommand(o => OpenDatabaseSettings(o as Window), o => !IsBusy);
        }

        private void UpdateCanLogin()
        {
            OnPropertyChanged(nameof(CanLogin));
            LoginCommand.RaiseCanExecuteChanged();
            DatabaseSettingsCommand.RaiseCanExecuteChanged();
        }

        private void OpenDatabaseSettings(Window? parentWindow)
        {
            try
            {
                var settingsWindow = new DatabaseSettingsWindow();
                if (parentWindow != null)
                {
                    settingsWindow.Owner = parentWindow;
                }
                
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при открытии настроек: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private async Task LoginAsync()
        {
            if (!CanLogin) return;

            try
            {
                IsBusy = true;
                
                // Test database connection first
                if (!await _databaseService.TestConnectionAsync())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Не удалось подключиться к базе данных. Пожалуйста, проверьте подключение к сети и убедитесь, что база данных доступна.",
                            "Ошибка подключения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                    return;
                }
                
                // Authentication logic
                AuthenticatedEmployee = await _authService.AuthenticateAsync(Login.Trim(), Password);

                if (AuthenticatedEmployee == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Неверный логин или пароль. Пожалуйста, проверьте введенные данные.",
                            "Ошибка входа",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                    return;
                }
                
                // Close the login window with success
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.IsActive);
                    if (window != null)
                    {
                        window.DialogResult = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Ошибка при входе: {ex.Message}\n\nПроверьте подключение к базе данных и правильность введенных данных.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
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
                window.Close();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}