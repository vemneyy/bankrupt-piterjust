// Views/DebtorWindow.xaml.cs
using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class DebtorWindow : Window
    {
        public Debtor? NewDebtor => (DataContext as AddDebtorViewModel)?.NewDebtor;

        public DebtorWindow()
        {
            InitializeComponent();
            SetDataContext(new AddDebtorViewModel());
        }

        public DebtorWindow(int personId)
        {
            InitializeComponent();
            SetDataContext(new EditDebtorViewModel(personId));
        }

        private void SetDataContext(object viewModel)
        {
            DataContext = viewModel;
        }
    }
}
