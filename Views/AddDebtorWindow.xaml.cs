// Views/AddDebtorWindow.xaml.cs
using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class AddDebtorWindow : Window
    {
        public Debtor NewDebtor => (DataContext as AddDebtorViewModel)?.NewDebtor;

        public AddDebtorWindow()
        {
            InitializeComponent();
        }
    }
}