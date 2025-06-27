using bankrupt_piterjust.ViewModels;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class EditDebtorWindow : Window
    {
        public EditDebtorWindow(int personId)
        {
            InitializeComponent();
            DataContext = new EditDebtorViewModel(personId);
        }
    }
}
