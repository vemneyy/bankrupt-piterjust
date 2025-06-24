using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace bankrupt_piterjust
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Show login window
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();
            
            // If login was successful, store the user and show main window
            if (result == true && loginWindow.AuthenticatedEmployee != null)
            {
                UserSessionService.Instance.SetCurrentEmployee(loginWindow.AuthenticatedEmployee);
                
                var mainWindow = new MainWindow();
                mainWindow.Title = $"ПитерЮст. Банкротство. - {loginWindow.AuthenticatedEmployee.FullName}, {loginWindow.AuthenticatedEmployee.Position}";
                mainWindow.Show();
            }
            else
            {
                // If login was cancelled or failed, exit the application
                Shutdown();
            }
        }
    }
}
