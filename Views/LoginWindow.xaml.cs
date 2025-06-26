using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Windows;
using System.Windows.Controls;

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
    }
}