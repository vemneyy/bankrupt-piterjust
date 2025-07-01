using System.Windows.Input;

namespace bankrupt_piterjust.Commands
{
    /// <summary>
    /// RelayCommand — универсальная реализация интерфейса ICommand,
    /// позволяющая делегировать выполнение действия и проверку на возможность выполнения.
    /// </summary>
    /// <remarks>
    /// Конструктор команды.
    /// </remarks>
    /// <param name="execute">Метод, вызываемый при выполнении команды. Обязателен.</param>
    /// <param name="canExecute">Метод, определяющий доступность команды. Необязателен.</param>
    public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
    {
        // Делегат, содержащий логику выполнения команды.
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        // Делегат, определяющий, можно ли выполнить команду в текущем состоянии.
        private readonly Predicate<object?>? _canExecute = canExecute;

        /// <summary>
        /// Определяет, может ли команда быть выполнена.
        /// </summary>
        /// <param name="parameter">Параметр команды.</param>
        /// <returns>True, если команда может быть выполнена; иначе — false.</returns>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Выполняет команду.
        /// </summary>
        /// <param name="parameter">Параметр команды.</param>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Событие, уведомляющее систему об изменении доступности команды.
        /// Привязано к системному CommandManager для автоматической подписки.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value; // Добавление обработчика
            remove => CommandManager.RequerySuggested -= value; // Удаление обработчика
        }

        /// <summary>
        /// Вызывает переоценку доступности команды.
        /// Принудительно инициирует событие CanExecuteChanged через CommandManager.
        /// </summary>
        public static void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
