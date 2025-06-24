// ViewModels/MainViewModel.cs
using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private List<Debtor> _allDebtors; // Полный список всех должников
        private readonly Dictionary<string, ObservableCollection<TabItem>> _filterTabsByCategory;
        private readonly DebtorRepository _debtorRepository;
        private readonly DocumentGenerationService _documentGenerationService;

        public ObservableCollection<Debtor> DebtorsView { get; set; } // Отображаемый список
        public ObservableCollection<TabItem> MainTabs { get; set; }

        private ObservableCollection<TabItem> _currentFilterTabs;
        public ObservableCollection<TabItem> CurrentFilterTabs
        {
            get => _currentFilterTabs;
            set { _currentFilterTabs = value; OnPropertyChanged(nameof(CurrentFilterTabs)); }
        }

        private TabItem _selectedMainTab;
        public TabItem SelectedMainTab
        {
            get => _selectedMainTab;
            set
            {
                if (_selectedMainTab != value && value != null)
                {
                    _selectedMainTab = value;
                    OnPropertyChanged(nameof(SelectedMainTab));

                    // Обновляем список вкладок фильтров
                    CurrentFilterTabs = _filterTabsByCategory[_selectedMainTab.Name];
                    // Устанавливаем "Все" как активный фильтр по умолчанию
                    SelectedFilterTab = CurrentFilterTabs.FirstOrDefault();

                    UpdateTabCounts();
                    ApplyFilters(); // Apply filters when main tab changes
                }
            }
        }

        private TabItem _selectedFilterTab;
        public TabItem SelectedFilterTab
        {
            get => _selectedFilterTab;
            set
            {
                if (_selectedFilterTab != value)
                {
                    _selectedFilterTab = value;
                    OnPropertyChanged(nameof(SelectedFilterTab));
                    ApplyFilters();
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(nameof(SearchText)); ApplyFilters(); }
        }

        private Debtor? _selectedDebtor;
        public Debtor? SelectedDebtor
        {
            get => _selectedDebtor;
            set
            {
                _selectedDebtor = value;
                OnPropertyChanged(nameof(SelectedDebtor));
                OnPropertyChanged(nameof(CanGenerateContract));
                OnPropertyChanged(nameof(IsDebtorSelected));
            }
        }

        // Свойство для отображения/скрытия деталей должника
        public bool IsDebtorSelected => SelectedDebtor != null;

        public bool CanGenerateContract => SelectedDebtor?.PersonId != null;

        public ICommand AddDebtorCommand { get; }
        public ICommand ArchiveDebtorCommand { get; }
        public ICommand RestoreDebtorCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand GenerateContractCommand { get; }
        public ICommand ShowLoginWindowCommand { get; }

        public MainViewModel()
        {
            _debtorRepository = new DebtorRepository();
            _documentGenerationService = new DocumentGenerationService();

            // Инициализация коллекций для UI
            DebtorsView = new ObservableCollection<Debtor>();
            MainTabs = new ObservableCollection<TabItem>
            {
                new TabItem { Name = "Лиды" },
                new TabItem { Name = "Клиенты" },
                new TabItem { Name = "Отказ" },
                new TabItem { Name = "Архив" }
            };

            _filterTabsByCategory = new Dictionary<string, ObservableCollection<TabItem>>
            {
                { "Лиды", new ObservableCollection<TabItem>
                    {
                        new TabItem { Name = "Все" },
                        new TabItem { Name = "Консультация не назначена" },
                        new TabItem { Name = "Консультация назначена" },
                        new TabItem { Name = "Повторная консультация" }
                    }
                },
                { "Клиенты", new ObservableCollection<TabItem>
                    {
                        new TabItem { Name = "Все" },
                        new TabItem { Name = "Сбор документов" },
                        new TabItem { Name = "Подготовка заявления" },
                        new TabItem { Name = "На рассмотрении" },
                        new TabItem { Name = "Ходатайство" },
                        new TabItem { Name = "Заседание" },
                        new TabItem { Name = "Процедура введена" }
                    }
                },
                { "Отказ", new ObservableCollection<TabItem>
                    {
                        new TabItem { Name = "Все" },
                        new TabItem { Name = "Мой отказ" },
                        new TabItem { Name = "Отказ должника" }
                    }
                },
                { "Архив", new ObservableCollection<TabItem>
                    {
                        new TabItem { Name = "Все" }
                    }
                }
            };

            // Инициализация команд
            AddDebtorCommand = new RelayCommand(o => AddDebtor());
            ArchiveDebtorCommand = new RelayCommand(ArchiveDebtor);
            RestoreDebtorCommand = new RelayCommand(RestoreDebtor);
            RefreshDataCommand = new RelayCommand(async o => await LoadDataAsync());
            GenerateContractCommand = new RelayCommand(async o => await GenerateContractAsync(), o => CanGenerateContract);
            ShowLoginWindowCommand = new RelayCommand(o => ShowLoginWindow());

            // Устанавливаем начальные активные вкладки
            SelectedMainTab = MainTabs.FirstOrDefault(t => t.Name == "Клиенты");

            // Загружаем данные асинхронно
            _ = LoadDataAsync();
        }

        private void ShowLoginWindow()
        {
            var loginWindow = new LoginWindow();
            loginWindow.Owner = Application.Current?.MainWindow;
            
            if (loginWindow.ShowDialog() == true && loginWindow.AuthenticatedEmployee != null)
            {
                UserSessionService.Instance.SetCurrentEmployee(loginWindow.AuthenticatedEmployee);
                
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.Title = $"ПитерЮст. Банкротство. - {loginWindow.AuthenticatedEmployee.FullName}, {loginWindow.AuthenticatedEmployee.Position}";
                }
            }
        }

        private async Task GenerateContractAsync()
        {
            if (SelectedDebtor?.PersonId == null)
                return;

            try
            {
                var outputPath = await _documentGenerationService.GenerateContractAsync(SelectedDebtor.PersonId.Value);

                if (!string.IsNullOrEmpty(outputPath))
                {
                    var result = MessageBox.Show(
                        $"Договор успешно создан и сохранен по пути:\n{outputPath}\n\nОткрыть документ?",
                        "Успех",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = outputPath,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Загрузка данных из базы через репозиторий
                var debtors = await _debtorRepository.GetAllDebtorsAsync();
                _allDebtors = debtors.ToList();

                // Обновляем счетчики на всех вкладках
                UpdateTabCounts();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Если база недоступна, используем тестовые данные
                LoadDummyData();
            }
        }

        private void LoadDummyData()
        {
            // Тестовые данные для отладки
            _allDebtors = new List<Debtor>
            {
                new Debtor
                {
                    PersonId = 1,
                    FullName = "Лисина Ирина Викторовна",
                    Region = "Ленинградская область",
                    Status = "Подать заявление",
                    MainCategory = "Клиенты",
                    FilterCategory = "Подготовка заявления",
                    PreviousMainCategory = null,
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                },
                new Debtor
                {
                    PersonId = 2,
                    FullName = "Петров Петр Петрович",
                    Region = "г. Санкт-Петербург",
                    Status = "Собрать документы",
                    MainCategory = "Клиенты",
                    FilterCategory = "Сбор документов",
                    PreviousMainCategory = null,
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                },
                new Debtor
                {
                    PersonId = 3,
                    FullName = "Иванов Иван Иванович",
                    Region = "Московская область",
                    Status = "В архив",
                    MainCategory = "Архив",
                    FilterCategory = "Все",
                    PreviousMainCategory = "Клиенты",
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            UpdateTabCounts();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allDebtors == null || SelectedMainTab == null)
                return;

            // 1. Фильтруем по основной вкладке
            var filtered = _allDebtors.Where(d => d.MainCategory == SelectedMainTab.Name);

            // 2. Фильтруем по вкладке-фильтру (кроме "Все")
            if (SelectedFilterTab != null && SelectedFilterTab.Name != "Все")
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
            OnPropertyChanged(nameof(DebtorsView)); // For empty message trigger
        }

        private void UpdateTabCounts()
        {
            if (_allDebtors == null)
                return;

            foreach (var tab in MainTabs)
            {
                tab.Count = _allDebtors.Count(d => d.MainCategory == tab.Name);
            }

            if (CurrentFilterTabs == null) return;

            // Для фильтров считаем внутри выбранной основной категории
            var debtorsInCurrentMainTab = _allDebtors.Where(d => d.MainCategory == SelectedMainTab.Name).ToList();
            foreach (var tab in CurrentFilterTabs)
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

            if (addWindow.ShowDialog() == true && addWindow.NewDebtor != null)
            {
                var newDebtor = addWindow.NewDebtor;
                _allDebtors.Add(newDebtor);

                // Переключаемся на вкладку, куда добавили должника
                SelectedMainTab = MainTabs.First(t => t.Name == newDebtor.MainCategory);
                SelectedFilterTab = CurrentFilterTabs.First(t => t.Name == newDebtor.FilterCategory);

                UpdateTabCounts();
                ApplyFilters(); // Применяем фильтры, чтобы сразу увидеть нового должника
            }
        }

        private void ArchiveDebtor(object parameter)
        {
            if (parameter is Debtor debtor && debtor.MainCategory != "Архив")
            {
                // Сохраняем текущую категорию перед архивацией
                debtor.PreviousMainCategory = debtor.MainCategory;
                debtor.PreviousFilterCategory = debtor.FilterCategory;

                // Перемещаем в архив
                debtor.MainCategory = "Архив";
                debtor.FilterCategory = "Все";
                debtor.Status = "В архив";

                UpdateTabCounts();
                ApplyFilters();
            }
        }

        private void RestoreDebtor(object parameter)
        {
            if (parameter is Debtor debtor && debtor.MainCategory == "Архив")
            {
                // Восстанавливаем из предыдущей категории, если есть
                debtor.MainCategory = debtor.PreviousMainCategory ?? "Клиенты";
                debtor.FilterCategory = debtor.PreviousFilterCategory ?? "Подготовка заявления";
                debtor.Status = "Восстановлен";

                UpdateTabCounts();
                ApplyFilters();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}