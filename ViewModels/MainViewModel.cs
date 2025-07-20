// ViewModels/MainViewModel.cs
using bankrupt_piterjust.Commands;
using bankrupt_piterjust.Models;
using bankrupt_piterjust.Services;
using bankrupt_piterjust.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace bankrupt_piterjust.ViewModels
{
    // Модель для вкладок, чтобы хранить имя, счетчик и состояние
    public class TabItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
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
        private List<Debtor> _allDebtors = []; // Полный список всех должников
        private readonly Dictionary<string, ObservableCollection<TabItem>> _filterTabsByCategory;
        private readonly DebtorRepository _debtorRepository;
        private readonly DocumentGenerationService _documentGenerationService;

        public ObservableCollection<Debtor> DebtorsView { get; set; } // Отображаемый список
        public ObservableCollection<TabItem> MainTabs { get; set; }

        private ObservableCollection<TabItem> _currentFilterTabs = [];
        public ObservableCollection<TabItem> CurrentFilterTabs
        {
            get => _currentFilterTabs;
            set { _currentFilterTabs = value; OnPropertyChanged(nameof(CurrentFilterTabs)); }
        }

        private TabItem _selectedMainTab = new();
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
                    SelectedFilterTab = CurrentFilterTabs?.FirstOrDefault() ?? new TabItem { Name = "Все" };

                    UpdateTabCounts();
                    ApplyFilters(); // Apply filters when main tab changes
                }
            }
        }

        private TabItem _selectedFilterTab = new();
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

        private string _searchText = string.Empty;
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

                // Load detailed debtor information when selected
                if (_selectedDebtor?.PersonId != null)
                {
                    _ = LoadDebtorDetailsAsync(_selectedDebtor.PersonId.Value);
                }
                else
                {
                    ClearDebtorDetails();
                }
            }
        }

        // Status change functionality
        private bool _isStatusSelectionVisible;
        public bool IsStatusSelectionVisible
        {
            get => _isStatusSelectionVisible;
            set
            {
                _isStatusSelectionVisible = value;
                OnPropertyChanged(nameof(IsStatusSelectionVisible));
            }
        }

        private ObservableCollection<string> _availableStatuses;
        public ObservableCollection<string> AvailableStatuses
        {
            get => _availableStatuses;
            private set
            {
                _availableStatuses = value;
                OnPropertyChanged(nameof(AvailableStatuses));
            }
        }

        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged(nameof(SelectedStatus));
            }
        }

        // Person details for the selected debtor
        private Person? _selectedPerson;
        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged(nameof(SelectedPerson));
            }
        }

        // Passport details for the selected debtor
        private Passport? _selectedPassport;
        public Passport? SelectedPassport
        {
            get => _selectedPassport;
            set
            {
                _selectedPassport = value;
                OnPropertyChanged(nameof(SelectedPassport));
            }
        }

        // Addresses for the selected debtor
        private ObservableCollection<Address> _selectedAddresses;
        public ObservableCollection<Address> SelectedAddresses
        {
            get => _selectedAddresses;
            set
            {
                _selectedAddresses = value;
                OnPropertyChanged(nameof(SelectedAddresses));
                OnPropertyChanged(nameof(HasRegistrationAddress));
                OnPropertyChanged(nameof(HasResidenceAddress));
                OnPropertyChanged(nameof(HasMailingAddress));
                OnPropertyChanged(nameof(RegistrationAddress));
                OnPropertyChanged(nameof(ResidenceAddress));
                OnPropertyChanged(nameof(MailingAddress));
            }
        }

        // Helper properties for addresses
        public bool HasRegistrationAddress => SelectedAddresses?.Any() ?? false;
        public bool HasResidenceAddress => SelectedAddresses?.Count > 1;
        public bool HasMailingAddress => SelectedAddresses?.Count > 2;

        public string? RegistrationAddress => SelectedAddresses != null && SelectedAddresses.Count > 0 ? FormatAddress(SelectedAddresses[0]) : null;
        public string? ResidenceAddress => SelectedAddresses != null && SelectedAddresses.Count > 1 ? FormatAddress(SelectedAddresses[1]) : null;
        public string? MailingAddress => SelectedAddresses != null && SelectedAddresses.Count > 2 ? FormatAddress(SelectedAddresses[2]) : null;


        // Loading state
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        // Indicates if details view is in edit mode
        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
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
        public ICommand EditDebtorCommand { get; }
        public ICommand SaveDebtorCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ManageContractsCommand { get; }
        public ICommand ManageCompaniesCommand { get; }
        public ICommand ManageEmployeesCommand { get; }
        public ICommand ShowStatusSelectionCommand { get; }
        public ICommand ChangeStatusCommand { get; }
        public ICommand CancelStatusSelectionCommand { get; }

        public MainViewModel()
        {
            _debtorRepository = new DebtorRepository();
            _documentGenerationService = new DocumentGenerationService();
            _selectedAddresses = [];
            _availableStatuses = [];
            _selectedStatus = string.Empty;

            // Инициализация коллекций для UI
            DebtorsView = [];

            // Remove "Лиды" and "Отказ" categories
            MainTabs =
            [
                new() { Name = "Клиенты" },
                new() { Name = "Архив" }
            ];

            _filterTabsByCategory = new Dictionary<string, ObservableCollection<TabItem>>
            {
                { "Клиенты", new ObservableCollection<TabItem>
                    {
                        new() { Name = "Все" },
                        new() { Name = "Сбор документов" },
                        new() { Name = "Подготовка заявления" },
                        new() { Name = "На рассмотрении" },
                        new() { Name = "Ходатайство" },
                        new() { Name = "Заседание" },
                        new() { Name = "Процедура введена" }
                    }
                },
                { "Архив", new ObservableCollection<TabItem>
                    {
                        new() { Name = "Все" }
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
            EditDebtorCommand = new RelayCommand(o => EditDebtor(), o => SelectedDebtor?.PersonId != null);
            SaveDebtorCommand = new RelayCommand(async o => await SaveDebtorDetailsAsync(), o => IsEditMode);
            CancelEditCommand = new RelayCommand(o => CancelEdit(), o => IsEditMode);

            // Placeholder management commands
            ManageContractsCommand = new RelayCommand(o => { });
            ManageCompaniesCommand = new RelayCommand(o => { });
            ManageEmployeesCommand = new RelayCommand(o => ManageEmployees());

            // Status change commands
            ShowStatusSelectionCommand = new RelayCommand(o => ShowStatusSelection(), o => SelectedDebtor != null);
            ChangeStatusCommand = new RelayCommand(async o => await ChangeDebtorStatusAsync(), o => !string.IsNullOrEmpty(SelectedStatus) && SelectedDebtor?.PersonId.HasValue == true);
            CancelStatusSelectionCommand = new RelayCommand(o => IsStatusSelectionVisible = false);

            // Устанавливаем начальные активные вкладки
            SelectedMainTab = MainTabs.FirstOrDefault(t => t.Name == "Клиенты") ?? new TabItem { Name = "Клиенты" };

            // Загружаем данные асинхронно
            _ = LoadDataAsync();
        }

        private void ShowStatusSelection()
        {
            if (SelectedDebtor == null || SelectedMainTab == null)
                return;

            // Get statuses for the current main category
            AvailableStatuses = [];

            // For Clients category, add all client statuses
            if (SelectedMainTab.Name == "Клиенты")
            {
                AvailableStatuses.Add("Сбор документов");
                AvailableStatuses.Add("Подготовка заявления");
                AvailableStatuses.Add("На рассмотрении");
                AvailableStatuses.Add("Ходатайство");
                AvailableStatuses.Add("Заседание");
                AvailableStatuses.Add("Процедура введена");
            }
            // For Archive, just have one status
            else if (SelectedMainTab.Name == "Архив")
            {
                AvailableStatuses.Add("В архиве");
            }

            // Set current status as selected
            SelectedStatus = SelectedDebtor.Status;

            // Show status selection popup
            IsStatusSelectionVisible = true;
        }

        // Updated code to handle nullable value type safely
        private async Task ChangeDebtorStatusAsync()
        {
            if (SelectedDebtor == null || string.IsNullOrEmpty(SelectedStatus) || !SelectedDebtor.PersonId.HasValue)
            {
                MessageBox.Show("Не выбран должник или отсутствует идентификатор должника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                IsLoading = true;

                // Update the status
                string oldStatus = SelectedDebtor.Status;
                SelectedDebtor.Status = SelectedStatus;

                // Update filter category to match the status for Clients
                if (SelectedMainTab?.Name == "Клиенты")
                {
                    SelectedDebtor.FilterCategory = SelectedStatus;
                }

                // Save to database
                await _debtorRepository.UpdateDebtorInfoAsync(
                    SelectedDebtor.PersonId.Value,
                    SelectedDebtor.Status,
                    SelectedDebtor.MainCategory,
                    SelectedDebtor.FilterCategory);

                // Hide status selection
                IsStatusSelectionVisible = false;

                // Refresh counts
                UpdateTabCounts();

                // If filter changed, update selected tab before filtering
                if (SelectedDebtor != null &&
                    SelectedDebtor.FilterCategory != oldStatus &&
                    SelectedMainTab?.Name == "Клиенты" &&
                    SelectedFilterTab != null && CurrentFilterTabs != null &&
                    SelectedFilterTab.Name != "Все")
                {
                    SelectedFilterTab = CurrentFilterTabs.FirstOrDefault(t => t.Name == SelectedDebtor.FilterCategory) ??
                                       CurrentFilterTabs.FirstOrDefault(t => t.Name == "Все") ??
                                       CurrentFilterTabs.FirstOrDefault() ??
                                       new TabItem { Name = "Все" };
                }

                // Apply filters after potential tab change
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static void ShowLoginWindow()
        {
            var loginWindow = new LoginWindow
            {
                Owner = Application.Current?.MainWindow
            };

            if (loginWindow.ShowDialog() == true && loginWindow.AuthenticatedEmployee != null)
            {
                UserSessionService.Instance.SetCurrentEmployee(loginWindow.AuthenticatedEmployee);

                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.Title = $"ПитерЮст. Банкротство. - {loginWindow.AuthenticatedEmployee.FullName}, {loginWindow.AuthenticatedEmployee.Position}";
                }
            }
        }

        // Loads detailed information about the selected debtor
        private async Task LoadDebtorDetailsAsync(int personId)
        {
            try
            {
                IsLoading = true;

                // Get person details
                SelectedPerson = await _debtorRepository.GetPersonByIdAsync(personId);

                // Get passport details
                SelectedPassport = await _debtorRepository.GetPassportByPersonIdAsync(personId);

                // Get addresses
                var addresses = await _debtorRepository.GetAddressesByPersonIdAsync(personId);
                SelectedAddresses = new ObservableCollection<Address>(addresses);

                // Cancel any edit mode
                IsEditMode = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных о должнике: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Clears detailed information when no debtor is selected
        private void ClearDebtorDetails()
        {
            SelectedPerson = null;
            SelectedPassport = null;
            SelectedAddresses = [];
            IsEditMode = false;
        }

        // Cancels any edits in the detail view
        private void CancelEdit()
        {
            IsEditMode = false;
            if (SelectedDebtor?.PersonId != null)
            {
                _ = LoadDebtorDetailsAsync(SelectedDebtor.PersonId.Value);
            }
        }

        // Saves changes to the debtor details
        private async Task SaveDebtorDetailsAsync()
        {
            if (SelectedDebtor?.PersonId == null || SelectedPerson == null)
                return;

            try
            {
                await _debtorRepository.UpdatePersonAsync(SelectedPerson);
                await _debtorRepository.UpdateDebtorInfoAsync(
                    SelectedDebtor.PersonId.Value,
                    SelectedDebtor.Status,
                    SelectedDebtor.MainCategory,
                    SelectedDebtor.FilterCategory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEditMode = false;
                await LoadDebtorDetailsAsync(SelectedDebtor.PersonId.Value);
            }
        }

        private async Task GenerateContractAsync()
        {
            if (SelectedDebtor?.PersonId == null)
                return;

            try
            {
                var repo = new FullDatabaseRepository();
                List<Employee> activeEmployeesWithBasis;

                try
                {
                    activeEmployeesWithBasis = await repo.GetActiveEmployeesWithBasisAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении списка сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new EmployeeSelectionWindow(activeEmployeesWithBasis)
                {
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() != true || dialog.SelectedEmployee == null)
                    return;

                var outputPath = await DocumentGenerationService.GenerateContractAsync(
                    SelectedDebtor.PersonId.Value,
                    dialog.SelectedEmployee);

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
                MessageBox.Show($"Ошибка при генерации договора (Основное Меню): {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Загрузка данных из базы через репозиторий
                var debtors = await _debtorRepository.GetAllDebtorsAsync();
                _allDebtors = [.. debtors];

                // Update all debtors that were in Leads or Reject categories to Clients
                foreach (var debtor in _allDebtors)
                {
                    if (debtor.MainCategory == "Лиды" || debtor.MainCategory == "Отказ")
                    {
                        debtor.MainCategory = "Клиенты";
                        debtor.FilterCategory = "Сбор документов";
                        debtor.Status = "Сбор документов";

                        // Update in database
                        if (debtor.PersonId.HasValue)
                        {
                            await _debtorRepository.UpdateDebtorInfoAsync(
                                debtor.PersonId.Value,
                                debtor.Status,
                                debtor.MainCategory,
                                debtor.FilterCategory);
                        }
                    }
                }

                // Обновляем счетчики на всех вкладках
                UpdateTabCounts();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
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
                filtered = filtered.Where(d => d.FullName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
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

            if (CurrentFilterTabs == null || SelectedMainTab == null) return;

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
            var addWindow = new DebtorWindow
            {
                // Устанавливаем владельца, чтобы окно появилось по центру главного
                Owner = Application.Current?.MainWindow
            };

            if (addWindow.ShowDialog() == true && addWindow.NewDebtor != null)
            {
                var newDebtor = addWindow.NewDebtor;
                _allDebtors.Add(newDebtor);

                // Переключаемся на вкладку, куда добавили должника
                SelectedMainTab = MainTabs.First(t => t.Name == newDebtor.MainCategory);
                SelectedFilterTab = CurrentFilterTabs.First(t => t.Name == newDebtor.FilterCategory);

                UpdateTabCounts();
                ApplyFilters(); // Применяем фильтры, чтобы сразу увидеть нового должника

                // Select the new debtor to show details
                SelectedDebtor = newDebtor;
            }
        }
        private void EditDebtor()
        {
            if (SelectedDebtor?.PersonId == null)
                return;

            var editWindow = new DebtorWindow(SelectedDebtor.PersonId.Value)
            {
                Owner = Application.Current?.MainWindow
            };
            if (editWindow.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }


        private void ArchiveDebtor(object? parameter)
        {
            if (parameter is Debtor debtor && debtor.MainCategory != "Архив" && debtor.PersonId.HasValue)
            {
                // Сохраняем текущую категорию перед архивацией
                debtor.PreviousMainCategory = debtor.MainCategory;
                debtor.PreviousFilterCategory = debtor.FilterCategory;

                // Перемещаем в архив
                debtor.MainCategory = "Архив";
                debtor.FilterCategory = "Все";
                debtor.Status = "В архиве";

                // Save to database
                _ = _debtorRepository.UpdateDebtorInfoAsync(
                    debtor.PersonId.Value,
                    debtor.Status,
                    debtor.MainCategory,
                    debtor.FilterCategory);

                UpdateTabCounts();
                ApplyFilters();

                // If the archived debtor was selected, refresh the UI
                if (SelectedDebtor == debtor)
                {
                    OnPropertyChanged(nameof(SelectedDebtor));
                }
            }
        }

        private void RestoreDebtor(object? parameter)
        {
            if (parameter is Debtor debtor && debtor.MainCategory == "Архив" && debtor.PersonId.HasValue)
            {
                // Восстанавливаем из предыдущей категории, если есть
                debtor.MainCategory = debtor.PreviousMainCategory ?? "Клиенты";
                debtor.FilterCategory = debtor.PreviousFilterCategory ?? "Сбор документов";
                debtor.Status = "Сбор документов";

                // Save to database
                _ = _debtorRepository.UpdateDebtorInfoAsync(
                    debtor.PersonId.Value,
                    debtor.Status,
                    debtor.MainCategory,
                    debtor.FilterCategory);

                UpdateTabCounts();
                ApplyFilters();

                // If the restored debtor was selected, refresh the UI
                if (SelectedDebtor == debtor)
                {
                    OnPropertyChanged(nameof(SelectedDebtor));
                }
            }
        }

        private static string FormatAddress(Address address)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.PostalCode)) parts.Add(address.PostalCode);
            if (!string.IsNullOrWhiteSpace(address.Country)) parts.Add(address.Country);
            if (!string.IsNullOrWhiteSpace(address.Region)) parts.Add(address.Region);
            if (!string.IsNullOrWhiteSpace(address.District)) parts.Add(address.District);
            if (!string.IsNullOrWhiteSpace(address.City)) parts.Add(address.City);
            if (!string.IsNullOrWhiteSpace(address.Locality)) parts.Add(address.Locality);
            if (!string.IsNullOrWhiteSpace(address.Street)) parts.Add(address.Street);
            if (!string.IsNullOrWhiteSpace(address.HouseNumber)) parts.Add(address.HouseNumber);
            if (!string.IsNullOrWhiteSpace(address.Building)) parts.Add("к." + address.Building);
            if (!string.IsNullOrWhiteSpace(address.Apartment)) parts.Add("кв." + address.Apartment);
            return string.Join(", ", parts);
        }

        private void ManageEmployees()
        {
            var addEmployeeWindow = new AddEmployeeWindow
            {
                Owner = Application.Current?.MainWindow
            };

            if (addEmployeeWindow.ShowDialog() == true)
            {
                // Employee was successfully added, you can refresh any employee lists if needed
                MessageBox.Show("Сотрудник успешно зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}