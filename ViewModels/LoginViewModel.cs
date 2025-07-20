using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace bankrupt_piterjust.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly EmployeeService _employeeService;
        private readonly DatabaseService _databaseService;

        private bool _isBusy;
        private ObservableCollection<Employee> _employees = new ObservableCollection<Employee>();
        private Employee? _selectedEmployee;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); UpdateCanLogin(); }
        }

        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set { _employees = value; OnPropertyChanged(nameof(Employees)); }
        }

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(nameof(SelectedEmployee)); UpdateCanLogin(); }
        }

        public bool CanLogin => SelectedEmployee != null && !IsBusy;

        public RelayCommand LoginCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand RegisterCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // Authentication result
        public Employee? AuthenticatedEmployee { get; private set; }

        public LoginViewModel()
        {
            _employeeService = new EmployeeService();
            _databaseService = new DatabaseService();

            LoginCommand = new RelayCommand(async o => await LoginAsync(), o => CanLogin);
            CancelCommand = new RelayCommand(o => CancelLogin(o as Window));
            RegisterCommand = new RelayCommand(o => RegisterEmployee(o as Window), o => !IsBusy);
            RefreshCommand = new RelayCommand(async o => await LoadEmployeesAsync(), o => !IsBusy);

            _ = LoadEmployeesAsync();
        }

        private void UpdateCanLogin()
        {
            OnPropertyChanged(nameof(CanLogin));
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                IsBusy = true;

                // Test database connection first
                if (!await _databaseService.TestConnectionAsync())
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Не удалось подключиться к базе данных.",
                            "Ошибка подключения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                    return;
                }

                var employees = await _employeeService.GetAllActiveEmployeesAsync();
                Employees = new ObservableCollection<Employee>(employees);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Ошибка при загрузке списка сотрудников: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void RegisterEmployee(Window? parentWindow)
        {
            try
            {
                var addEmployeeWindow = new AddEmployeeWindow();
                if (parentWindow != null)
                {
                    addEmployeeWindow.Owner = parentWindow;
                }

                if (addEmployeeWindow.ShowDialog() == true && addEmployeeWindow.ViewModel.IsRegistrationSuccessful)
                {
                    MessageBox.Show(
                        "Сотрудник успешно зарегистрирован!",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Обновляем список сотрудников
                    _ = LoadEmployeesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при регистрации сотрудника: {ex.Message}",
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
                            "Не удалось подключиться к базе данных. Пожалуйста, проверьте подключение.",
                            "Ошибка подключения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                    return;
                }

                // Set authenticated employee
                AuthenticatedEmployee = SelectedEmployee;

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
                        $"Ошибка при входе: {ex.Message}",
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

        private static void CancelLogin(Window? window)
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