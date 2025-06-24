// MainWindow.xaml.cs
using bankrupt_piterjust.Services;
using System.Windows;

namespace bankrupt_piterjust
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Display the current user in the title
            var currentEmployee = UserSessionService.Instance.CurrentEmployee;
            if (currentEmployee != null)
            {
                Title = $"ПитерЮст. Банкротство. - {currentEmployee.FullName}, {currentEmployee.Position}";
            }
        }
    }
}