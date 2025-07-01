using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    public class EmployeeSelectionViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Employee> Employees { get; }

        private Employee? _selectedEmployee;
        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set { _selectedEmployee = value; OnPropertyChanged(nameof(SelectedEmployee)); }
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public EmployeeSelectionViewModel(IEnumerable<Employee> employees)
        {
            Employees = new ObservableCollection<Employee>(employees);
            _selectedEmployee = Employees.FirstOrDefault();

            ConfirmCommand = new RelayCommand(o => CloseDialog(true), o => SelectedEmployee != null);
            CancelCommand = new RelayCommand(o => CloseDialog(false));
        }

        private void CloseDialog(bool result)
        {
            var window = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
