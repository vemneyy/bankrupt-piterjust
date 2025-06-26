using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace bankrupt_piterjust.Views
{
    public partial class LoginWindow : Window
    {
        public Employee? AuthenticatedEmployee => (DataContext as LoginViewModel)?.AuthenticatedEmployee;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle Enter key press to trigger login
            if (e.Key == Key.Enter)
            {
                if (DataContext is LoginViewModel viewModel && viewModel.CanLogin)
                {
                    // Execute the login command
                    if (viewModel.LoginCommand.CanExecute(null))
                    {
                        viewModel.LoginCommand.Execute(null);
                    }
                }
                e.Handled = true;
            }
        }
    }
}