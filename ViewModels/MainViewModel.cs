// ViewModels/MainViewModel.cs
using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;

namespace bankrupt_piterjust.ViewModels
{
    // Модель для вкладок, чтобы хранить имя, счетчик и состояние
    public class TabItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        private int _count;
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(nameof(Count)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private List<Debtor> _allDebtors; // Полный список всех должников

        public ObservableCollection<Debtor> DebtorsView { get; set; } // Отображаемый список
        public ObservableCollection<TabItem> MainTabs { get; set; }
        public ObservableCollection<TabItem> FilterTabs { get; set; }

        private TabItem _selectedMainTab;
        public TabItem SelectedMainTab
        {
            get => _selectedMainTab;
            set { _selectedMainTab = value; OnPropertyChanged(nameof(SelectedMainTab)); ApplyFilters(); }
        }

        private TabItem _selectedFilterTab;
        public TabItem SelectedFilterTab
        {
            get => _selectedFilterTab;
            set { _selectedFilterTab = value; OnPropertyChanged(nameof(SelectedFilterTab)); ApplyFilters(); }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); ApplyFilters(); }
        }

        public ICommand AddDebtorCommand { get; }

        public MainViewModel()
        {
            LoadData(); // Загружаем начальные данные

            // Инициализация коллекций для UI
            DebtorsView = new ObservableCollection<Debtor>();
            MainTabs = new ObservableCollection<TabItem>
            {
                new TabItem { Name = "Лиды" },
                new TabItem { Name = "Клиенты" },
                new TabItem { Name = "Отказ" },
                new TabItem { Name = "Архив" }
            };
            FilterTabs = new ObservableCollection<TabItem>
            {
                new TabItem { Name = "Все" },
                new TabItem { Name = "Сбор документов" },
                new TabItem { Name = "Подготовка заявления" },
                new TabItem { Name = "На рассмотрении" },
                new TabItem { Name = "Ходатайство" },
                new TabItem { Name = "Заседание" },
                new TabItem { Name = "Процедура введена" }
            };

            // Устанавливаем начальные активные вкладки
            SelectedFilterTab = FilterTabs.FirstOrDefault(t => t.Name == "Все");
            SelectedMainTab = MainTabs.FirstOrDefault(t => t.Name == "Клиенты");

            // Инициализация команд
            AddDebtorCommand = new RelayCommand(o => AddDebtor());

            UpdateTabCounts();
            ApplyFilters();
        }

        private void LoadData()
        {
            // Здесь в реальном приложении была бы загрузка из базы данных
            _allDebtors = new List<Debtor>
            {
                new Debtor
                {
                    FullName = "Лисина Ирина Викторовна",
                    Region = "Ленинградская область",
                    Status = "Подать заявление",
                    MainCategory = "Клиенты",
                    FilterCategory = "Подготовка заявления"
                },
                new Debtor
                {
                    FullName = "Петров Петр Петрович",
                    Region = "г. Санкт-Петербург",
                    Status = "Собрать документы",
                    MainCategory = "Клиенты",
                    FilterCategory = "Сбор документов"
                },
                new Debtor
                {
                    FullName = "Иванов Иван Иванович",
                    Region = "Московская область",
                    Status = "В архив",
                    MainCategory = "Архив",
                    FilterCategory = "Все"
                }
            };
        }

        private void ApplyFilters()
        {
            if (_allDebtors == null || SelectedMainTab == null || SelectedFilterTab == null)
                return;

            // 1. Фильтруем по основной вкладке
            var filtered = _allDebtors.Where(d => d.MainCategory == SelectedMainTab.Name);

            // 2. Фильтруем по вкладке-фильтру (кроме "Все")
            if (SelectedFilterTab.Name != "Все")
            {
                filtered = filtered.Where(d => d.FilterCategory == SelectedFilterTab.Name);
            }

            // 3. Фильтруем по поисковой строке
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(d => d.FullName.ToLower().Contains(SearchText.ToLower()));
            }

            // Обновляем отображаемую коллекцию
            DebtorsView.Clear();
            foreach (var debtor in filtered)
            {
                DebtorsView.Add(debtor);
            }
        }

        private void UpdateTabCounts()
        {
            foreach (var tab in MainTabs)
            {
                tab.Count = _allDebtors.Count(d => d.MainCategory == tab.Name);
            }

            // Для фильтров считаем внутри выбранной основной категории
            var debtorsInCurrentMainTab = _allDebtors.Where(d => d.MainCategory == SelectedMainTab.Name).ToList();
            foreach (var tab in FilterTabs)
            {
                if (tab.Name == "Все")
                    tab.Count = debtorsInCurrentMainTab.Count;
                else
                    tab.Count = debtorsInCurrentMainTab.Count(d => d.FilterCategory == tab.Name);
            }
        }

        private void AddDebtor()
        {
            var addWindow = new AddDebtorWindow();
            // Устанавливаем владельца, чтобы окно появилось по центру главного
            addWindow.Owner = Application.Current?.MainWindow;

            if (addWindow.ShowDialog() == true)
            {
                var newDebtor = addWindow.NewDebtor;
                _allDebtors.Add(newDebtor);

                // Переключаемся на вкладку, куда добавили должника
                SelectedMainTab = MainTabs.First(t => t.Name == newDebtor.MainCategory);
                SelectedFilterTab = FilterTabs.First(t => t.Name == newDebtor.FilterCategory);

                UpdateTabCounts();
                ApplyFilters(); // Применяем фильтры, чтобы сразу увидеть нового должника
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}