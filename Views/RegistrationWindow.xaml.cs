using bankrupt_piterjust.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace bankrupt_piterjust.Views
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegistrationViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
