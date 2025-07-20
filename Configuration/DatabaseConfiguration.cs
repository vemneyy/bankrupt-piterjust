using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bankrupt_piterjust.Configuration
{
    /// <summary>
    /// Модель конфигурации для SQLite базы данных.
    /// Реализует INotifyPropertyChanged для поддержки привязки данных.
    /// </summary>
    public class DatabaseConfiguration : INotifyPropertyChanged
    {
        private string _databasePath = string.Empty;

        /// <summary>
        /// Путь к файлу базы данных SQLite.
        /// </summary>
        public string DatabasePath
        {
            get => _databasePath;
            set { _databasePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Формирует строку подключения на основе пути к базе данных.
        /// </summary>
        /// <returns>Строка подключения SQLite.</returns>
        public string GetConnectionString()
        {
            return string.IsNullOrEmpty(DatabasePath)
                ? Services.SQLiteInitializationService.GetConnectionString()
                : $"Data Source={DatabasePath}";
        }

        /// <summary>
        /// Создаёт копию текущей конфигурации.
        /// </summary>
        /// <returns>Новый экземпляр с теми же значениями свойств.</returns>
        public DatabaseConfiguration Clone()
        {
            return new DatabaseConfiguration
            {
                DatabasePath = DatabasePath
            };
        }

        /// <summary>
        /// Событие, уведомляющее об изменении значения свойства.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Вызывает событие PropertyChanged для заданного свойства.
        /// Использует CallerMemberName для автоматического указания имени свойства.
        /// </summary>
        /// <param name="propertyName">Имя изменившегося свойства.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
