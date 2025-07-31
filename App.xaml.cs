using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System.IO;
using System.Windows;

namespace bankrupt_piterjust
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DatabaseService? _databaseService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent the application from shutting down when the login window
            // is closed. We'll switch to the default behaviour once the main
            // window is shown.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // Ensure document directories are created
                EnsureDocumentDirectoriesExist();

                // Initialize database service for SQLite
                _databaseService = new DatabaseService();

                // Test SQLite connection (this will also initialize the database)
                bool connectionSuccess = await _databaseService.TestConnectionAsync();

                if (!connectionSuccess)
                {
                    MessageBox.Show(
                        "Не удалось подключиться к базе данных SQLite.\n\nПриложение будет закрыто.",
                        "Критическая ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                await ShowLoginWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Произошла критическая ошибка при запуске приложения:\n\n{ex.Message}\n\nДетали: {ex.InnerException?.Message}",
                    "Критическая ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private static void EnsureDocumentDirectoriesExist()
        {
            try
            {
                // Create Generated directory for output files if it doesn't exist
                string generatedPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "ПитерЮст", "Созданные договора");
                if (!Directory.Exists(generatedPath))
                {
                    Directory.CreateDirectory(generatedPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating directories: {ex.Message}");
            }
        }

        private async Task ShowLoginWindow()
        {
            try
            {
                // Create and show login window
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                // If login was successful, store the user and show main window
                if (result == true && loginWindow.AuthenticatedEmployee != null)
                {
                    // Set current employee in session
                    UserSessionService.Instance.SetCurrentEmployee(loginWindow.AuthenticatedEmployee);

                    if (_databaseService != null)
                    {
                        await _databaseService.ResetConnectionAsync();
                    }

                    // Create main window and make it the application's main window
                    var mainWindow = new MainWindow
                    {
                        Title = $"ПитерЮст. Банкротство. - {loginWindow.AuthenticatedEmployee.FullName}, {loginWindow.AuthenticatedEmployee.Position}"
                    };

                    // Assign the main window and restore normal shutdown behaviour
                    MainWindow = mainWindow;
                    ShutdownMode = ShutdownMode.OnMainWindowClose;

                    // Show main window
                    mainWindow.Show();
                }
                else
                {
                    // If login was cancelled or failed, exit the application
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при работе с окном входа: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
