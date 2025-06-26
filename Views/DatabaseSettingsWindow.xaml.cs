using bankrupt_piterjust.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace bankrupt_piterjust.Views
{
    public partial class DatabaseSettingsWindow : Window
    {
        public DatabaseSettingsWindow()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is DatabaseSettingsViewModel viewModel)
            {
                viewModel.DatabaseConfiguration.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}