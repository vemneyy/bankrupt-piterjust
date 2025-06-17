// Views/AddDebtorWindow.xaml.cs
using bankrupt_piterjust.Models;
using System.Windows;

namespace bankrupt_piterjust.Views
{
    public partial class AddDebtorWindow : Window
    {
        public Debtor NewDebtor { get; private set; }

        public AddDebtorWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Простая валидация
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text) || string.IsNullOrWhiteSpace(RegionTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните ФИО и Субъект РФ.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NewDebtor = new Debtor
            {
                FullName = FullNameTextBox.Text,
                Region = RegionTextBox.Text,
                Status = StatusTextBox.Text,
                // По умолчанию новый должник попадает в Клиенты -> Подготовка заявления
                MainCategory = "Клиенты",
                FilterCategory = "Подготовка заявления"
            };

            this.DialogResult = true;
        }
    }
}