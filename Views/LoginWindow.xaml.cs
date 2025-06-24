using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class LoginWindow : Window
    {
        public Employee? AuthenticatedEmployee => (DataContext as LoginViewModel)?.AuthenticatedEmployee;

        public LoginWindow()
        {
            InitializeComponent();
        }
    }
}