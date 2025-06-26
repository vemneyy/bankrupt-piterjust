using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace bankrupt_piterjust
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private DatabaseService _databaseService;

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
                
                // Initialize database service
                _databaseService = new DatabaseService();
                
                // Attempt initial database connection
                bool connectionSuccess = await _databaseService.TestConnectionAsync();
                
                if (connectionSuccess)
                {
                    // Ensure default admin user exists
                    var authService = new AuthenticationService();
                    await authService.EnsureDefaultAdminAsync();
                }
                
                await ShowLoginWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Произошла ошибка при запуске приложения: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void EnsureDocumentDirectoriesExist()
        {
            // Create Documents directory for templates if it doesn't exist
            string documentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
            if (!Directory.Exists(documentsPath))
            {
                Directory.CreateDirectory(documentsPath);
            }

            // Create Generated directory for output files if it doesn't exist
            string generatedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Generated");
            if (!Directory.Exists(generatedPath))
            {
                Directory.CreateDirectory(generatedPath);
            }
        }

        private async Task ShowLoginWindow()
        {
            // Create and show login window
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();

            // If login was successful, store the user and show main window
            if (result == true && loginWindow.AuthenticatedEmployee != null)
            {
                try
                {
                    // Set current employee in session
                    UserSessionService.Instance.SetCurrentEmployee(loginWindow.AuthenticatedEmployee);

                    // Reset database connection before showing main window
                    await _databaseService.ResetConnectionAsync();

                    // Create main window and make it the application's main window
                    var mainWindow = new MainWindow();
                    mainWindow.Title = $"ПитерЮст. Банкротство. - {loginWindow.AuthenticatedEmployee.FullName}, {loginWindow.AuthenticatedEmployee.Position}";

                    // Assign the main window and restore normal shutdown behaviour
                    MainWindow = mainWindow;
                    ShutdownMode = ShutdownMode.OnMainWindowClose;

                    // Show main window
                    mainWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при открытии главного окна: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown(1);
                }
            }
            else
            {
                // If login was cancelled or failed, exit the application
                Shutdown();
            }
        }
    }
}
