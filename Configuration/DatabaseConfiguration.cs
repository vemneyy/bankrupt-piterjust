using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bankrupt_piterjust.Configuration
{
    /// <summary>
    /// Модель конфигурации подключения к базе данных.
    /// Реализует INotifyPropertyChanged для поддержки привязки данных.
    /// </summary>
    public class DatabaseConfiguration : INotifyPropertyChanged
    {
        // Поля, хранящие значения свойств
        private string _host = "10.155.1.210";      // IP-адрес сервера базы данных
        private int _port = 5432;                   // Порт PostgreSQL по умолчанию
        private string _database = "piterjust";     // Название базы данных
        private string _username = "postgres";      // Имя пользователя
        private string _password = "postgres";      // Пароль

        /// <summary>
        /// IP-адрес или имя хоста сервера базы данных.
        /// </summary>
        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Номер порта для подключения к базе данных.
        /// </summary>
        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Название базы данных.
        /// </summary>
        public string Database
        {
            get => _database;
            set { _database = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Имя пользователя для подключения к базе данных.
        /// </summary>
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Пароль для подключения к базе данных.
        /// </summary>
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Формирует строку подключения на основе текущих параметров.
        /// </summary>
        /// <returns>Строка подключения PostgreSQL.</returns>
        public string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database}";
        }

        /// <summary>
        /// Создаёт копию текущей конфигурации.
        /// </summary>
        /// <returns>Новый экземпляр с теми же значениями свойств.</returns>
        public DatabaseConfiguration Clone()
        {
            return new DatabaseConfiguration
            {
                Host = Host,
                Port = Port,
                Database = Database,
                Username = Username,
                Password = Password
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
