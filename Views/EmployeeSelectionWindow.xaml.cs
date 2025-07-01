using bankrupt_piterjust.Models;
using bankrupt_piterjust.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class EmployeeSelectionWindow : Window
    {
        public Employee? SelectedEmployee => (DataContext as EmployeeSelectionViewModel)?.SelectedEmployee;

        public EmployeeSelectionWindow(IEnumerable<Employee> employees)
        {
            InitializeComponent();
            DataContext = new EmployeeSelectionViewModel(employees);
        }
    }
}
