using bankrupt_piterjust.ViewModels;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class AddEmployeeWindow : Window
    {
        public AddEmployeeViewModel ViewModel { get; }

        public AddEmployeeWindow()
        {
            InitializeComponent();
            ViewModel = new AddEmployeeViewModel();
            DataContext = ViewModel;
        }
    }
}