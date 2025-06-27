using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace bankrupt_piterjust.Helpers
{
    /// <summary>
    /// Поведение, добавляющее маску ввода для денежных значений в TextBox.
    /// Формат: группировка разрядов через апостроф, десятичный разделитель — точка.
    /// Пример: 1'234.56
    /// </summary>
    public static class CurrencyInputBehavior
    {
        /// <summary>
        /// Признак включения поведения для конкретного TextBox.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CurrencyInputBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Устанавливает значение свойства IsEnabled.
        /// </summary>
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// Возвращает значение свойства IsEnabled.
        /// </summary>
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        /// <summary>
        /// Настройка культуры отображения денежных значений.
        /// </summary>
        private static readonly CultureInfo _culture = new("en-US")
        {
            NumberFormat = { NumberGroupSeparator = "'", NumberDecimalSeparator = "." }
        };

        /// <summary>
        /// Обработчик изменения свойства IsEnabled.
        /// Подписывает/отписывает от нужных событий.
        /// </summary>
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox tb)
            {
                if ((bool)e.NewValue)
                {
                    tb.PreviewTextInput += OnPreviewTextInput;
                    tb.PreviewKeyDown += OnPreviewKeyDown;
                    DataObject.AddPastingHandler(tb, OnPaste);
                    tb.TextChanged += OnTextChanged;
                }
                else
                {
                    tb.PreviewTextInput -= OnPreviewTextInput;
                    tb.PreviewKeyDown -= OnPreviewKeyDown;
                    DataObject.RemovePastingHandler(tb, OnPaste);
                    tb.TextChanged -= OnTextChanged;
                }
            }
        }

        /// <summary>
        /// Устанавливает курсор в конец текста после изменения.
        /// </summary>
        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.CaretIndex = tb.Text.Length;
        }

        /// <summary>
        /// Блокирует вставку текста (Ctrl+V).
        /// </summary>
        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        /// <summary>
        /// Обработка клавиш: запрещает Delete, стрелки, Space.
        /// Обрабатывает Backspace вручную.
        /// </summary>
        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (e.Key == Key.Back)
            {
                e.Handled = true;
                var raw = GetRawText(tb);
                if (raw.Length > 0)
                {
                    raw = raw[..^1]; // Удаляет последний символ
                    SetRawText(tb, raw);
                    UpdateText(tb);
                }
            }
            else if (e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Обработка ввода символов.
        /// Разрешены только цифры и один десятичный разделитель.
        /// </summary>
        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;
            e.Handled = true;

            var raw = GetRawText(tb);

            if (e.Text == ".")
            {
                if (!raw.Contains('.'))
                {
                    raw += ".";
                    SetRawText(tb, raw);
                    UpdateText(tb);
                }
                return;
            }

            if (char.IsDigit(e.Text, 0))
            {
                if (raw.Contains('.'))
                {
                    var idx = raw.IndexOf('.');
                    var decimals = raw[(idx + 1)..];
                    if (decimals.Length < 2)
                        raw += e.Text;
                }
                else
                {
                    raw += e.Text;
                }

                SetRawText(tb, raw);
                UpdateText(tb);
            }
        }

        /// <summary>
        /// Хранит необработанный ввод пользователя.
        /// </summary>
        private static readonly DependencyProperty RawTextProperty = DependencyProperty.RegisterAttached(
            "RawText", typeof(string), typeof(CurrencyInputBehavior), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Получает необработанный текст.
        /// </summary>
        private static string GetRawText(TextBox tb) => (string)tb.GetValue(RawTextProperty);

        /// <summary>
        /// Устанавливает необработанный текст.
        /// </summary>
        private static void SetRawText(TextBox tb, string value) => tb.SetValue(RawTextProperty, value);

        /// <summary>
        /// Обновляет отображаемый текст в TextBox на основании необработанного значения.
        /// </summary>
        private static void UpdateText(TextBox tb)
        {
            var raw = GetRawText(tb);
            if (decimal.TryParse(raw, NumberStyles.Number, _culture, out var val))
            {
                tb.Text = val.ToString("N2", _culture);
            }
            else
            {
                tb.Text = raw;
            }

            tb.CaretIndex = tb.Text.Length;

            // Обновление источника привязки
            var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }
}
