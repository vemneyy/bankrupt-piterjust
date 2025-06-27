using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace bankrupt_piterjust.Configuration
{
    public class DatabaseConfiguration : INotifyPropertyChanged
    {
        private string _host = "10.155.1.210";
        private int _port = 5432;
        private string _database = "piterjust";
        private string _username = "postgres";
        private string _password = "postgres";

        public string Host
        {
            get => _host;
            set { _host = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        public string Database
        {
            get => _database;
            set { _database = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Username={Username};Password={Password};Database={Database}";
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}