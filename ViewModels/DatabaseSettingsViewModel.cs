using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using Npgsql;
using System.ComponentModel;
using System.Windows;

namespace bankrupt_piterjust.ViewModels
{
    public class DatabaseSettingsViewModel : INotifyPropertyChanged
    {
        private readonly ConfigurationService _configurationService;
        private bool _isBusy;
        private string _busyMessage = string.Empty;

        public DatabaseConfiguration DatabaseConfiguration { get; private set; }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); UpdateCanExecute(); }
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set { _busyMessage = value; OnPropertyChanged(nameof(BusyMessage)); }
        }

        public bool CanTestConnection => !IsBusy &&
                                         !string.IsNullOrWhiteSpace(DatabaseConfiguration.Host) &&
                                         !string.IsNullOrWhiteSpace(DatabaseConfiguration.Database) &&
                                         !string.IsNullOrWhiteSpace(DatabaseConfiguration.Username) &&
                                         !string.IsNullOrWhiteSpace(DatabaseConfiguration.Password);

        public RelayCommand TestConnectionCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public DatabaseSettingsViewModel()
        {
            _configurationService = ConfigurationService.Instance;
            DatabaseConfiguration = _configurationService.GetDatabaseConfiguration().Clone();

            // Subscribe to property changes in DatabaseConfiguration
            DatabaseConfiguration.PropertyChanged += (s, e) => UpdateCanExecute();

            TestConnectionCommand = new RelayCommand(async o => await TestConnectionAsync(), o => CanTestConnection);
            SaveCommand = new RelayCommand(o => SaveSettings(o as Window), o => !IsBusy);
            CancelCommand = new RelayCommand(o => CancelSettings(o as Window), o => !IsBusy);
        }

        private void UpdateCanExecute()
        {
            OnPropertyChanged(nameof(CanTestConnection));
            TestConnectionCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }

        private async Task TestConnectionAsync()
        {
            if (!CanTestConnection) return;

            try
            {
                IsBusy = true;
                BusyMessage = "Проверка подключения...";

                // Create a temporary DatabaseService with the current configuration
                var connectionString = DatabaseConfiguration.GetConnectionString();

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Test basic functionality
                await using var cmd = new NpgsqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();

                // Test pgcrypto extension
                await using var cryptoCmd = new NpgsqlCommand("SELECT crypt('test', gen_salt('bf'))", connection);
                await cryptoCmd.ExecuteScalarAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Подключение к базе данных успешно установлено!",
                        "Тест подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    string errorMessage = "Не удалось подключиться к базе данных.";

                    if (ex.Message.Contains("pgcrypto"))
                    {
                        errorMessage += "\n\nРасширение pgcrypto недоступно. Убедитесь, что оно установлено:\nCREATE EXTENSION IF NOT EXISTS pgcrypto;";
                    }

                    errorMessage += $"\n\nПодробности: {ex.Message}";

                    MessageBox.Show(
                        errorMessage,
                        "Ошибка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private void SaveSettings(Window? window)
        {
            try
            {
                _configurationService.SaveDatabaseConfiguration(DatabaseConfiguration);

                MessageBox.Show(
                    "Настройки подключения к базе данных сохранены!",
                    "Настройки сохранены",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (window != null)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении настроек: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelSettings(Window? window)
        {
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}