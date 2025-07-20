using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace bankrupt_piterjust.Converters
{
    /// <summary>
    /// Конвертер: если строка значения равна параметру — показать элемент, иначе скрыть.
    /// </summary>
    public class StringEqualsToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает Visible, если value == parameter, иначе Collapsed.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обратное преобразование не реализовано.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: если строка значения НЕ равна параметру — показать элемент, иначе скрыть.
    /// </summary>
    public class StringNotEqualsToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает Visible, если value != parameter, иначе Collapsed.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Visible;

            return value.ToString() != parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Обратное преобразование не реализовано.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: строковое сравнение -> булево значение.
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает true, если строки совпадают.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        /// <summary>
        /// Возвращает параметр, если входной value = true.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
                return parameter.ToString()!;

            return null!;
        }
    }

    /// <summary>
    /// Конвертер: bool -> Visibility. true = Visible, false = Collapsed.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Преобразует логическое значение в Visibility.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Преобразует Visibility обратно в логическое значение.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    /// <summary>
    /// Конвертер: инвертирует значение типа bool.
    /// </summary>
    public class BooleanToInverseConverter : IValueConverter
    {
        /// <summary>
        /// Инвертирует логическое значение.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return true;
        }

        /// <summary>
        /// Инвертирует логическое значение обратно.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }

    /// <summary>
    /// Конвертер: инвертирует значение типа bool.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Инвертирует логическое значение.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return true;
        }

        /// <summary>
        /// Инвертирует логическое значение обратно.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }

    /// <summary>
    /// Конвертер: преобразует 0 в Visibility.Visible, любое другое число в Visibility.Collapsed.
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает Visible, если значение равно 0, иначе Collapsed.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue == 0 ? Visibility.Visible : Visibility.Collapsed;
            
            if (value is double doubleValue)
                return doubleValue == 0.0 ? Visibility.Visible : Visibility.Collapsed;
            
            if (value is decimal decimalValue)
                return decimalValue == 0m ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        /// <summary>
        /// Обратное преобразование не реализовано.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: bool -> строка из параметра "TrueValue|FalseValue".
    /// </summary>
    public class BoolToValueConverter : IValueConverter
    {
        /// <summary>
        /// Преобразует bool в одну из двух строк, заданных через параметр "x|y".
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string paramString)
            {
                string[] options = paramString.Split('|');
                if (options.Length == 2)
                {
                    return boolValue ? options[0] : options[1];
                }
            }

            return value;
        }

        /// <summary>
        /// Обратное преобразование не реализовано.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер: принимает серию паспорта (должно быть только 4 цифры). Убирает лишние символы, ограничивает длину.
    /// </summary>
    public class PassportSeriesConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает строковое представление значения.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Очищает строку от нецифровых символов, ограничивает длину до 4 цифр.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            return digitsOnly.Length > 4 ? digitsOnly[..4] : digitsOnly;
        }
    }
    /// <summary>
    /// Конвертер: принимает номер паспорта (6 цифр).
    /// </summary>
    public class PassportNumberConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает строковое представление значения.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Очищает строку от нецифровых символов, ограничивает длину до 6 цифр.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            return digitsOnly.Length > 6 ? digitsOnly[..6] : digitsOnly;
        }
    }

    /// <summary>
    /// Конвертер: форматирует код подразделения МВД в формате XXX-XXX.
    /// </summary>
    public class DivisionCodeConverter : IValueConverter
    {
        /// <summary>
        /// Преобразует строку из цифр в формат "XXX-XXX".
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");

            return digitsOnly.Length switch
            {
                <= 3 => digitsOnly,
                <= 6 => $"{digitsOnly[..3]}-{digitsOnly[3..]}",
                _ => $"{digitsOnly[..3]}-{digitsOnly.Substring(3, 3)}"
            };
        }

        /// <summary>
        /// Очищает строку от символов и форматирует в виде "XXX-XXX", не более 6 цифр.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString() ?? string.Empty;
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            digitsOnly = digitsOnly.Length > 6 ? digitsOnly[..6] : digitsOnly;

            return digitsOnly.Length > 3 ? $"{digitsOnly[..3]}-{digitsOnly[3..]}" : digitsOnly;
        }
    }

    /// <summary>
    /// Конвертер: форматирует денежную сумму с разделителями групп и двумя знаками после запятой.
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        private static readonly CultureInfo _culture = new("en-US")
        {
            NumberFormat = { NumberGroupSeparator = "'", NumberDecimalSeparator = "." }
        };

        /// <summary>
        /// Преобразует decimal или строку в строку с форматированием "#,##0.00".
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d) return d.ToString("N2", _culture);
            if (decimal.TryParse(value?.ToString()?.Replace("'", ""), NumberStyles.Any, _culture, out decimal parsed))
                return parsed.ToString("N2", _culture);

            return 0m.ToString("N2", _culture);
        }

        /// <summary>
        /// Удаляет разделители и преобразует строку обратно в decimal с округлением до двух знаков.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = value?.ToString()?.Replace("'", "") ?? string.Empty;
            return decimal.TryParse(input, NumberStyles.Any, _culture, out var result)
                ? Math.Round(result, 2)
                : 0m;
        }
    }

    /// <summary>
    /// Конвертер: форматирует номер телефона в формате +7 (XXX) XXX-XX-XX.
    /// </summary>
    public class PhoneConverter : IValueConverter
    {
        /// <summary>
        /// Форматирует номер телефона для отображения.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            FormatPhone(value?.ToString() ?? string.Empty);

        /// <summary>
        /// Форматирует номер телефона при вводе.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            FormatPhone(value?.ToString() ?? string.Empty);

        /// <summary>
        /// Форматирует строку с цифрами в формат +7 (XXX) XXX-XX-XX.
        /// </summary>
        private static string FormatPhone(string input)
        {
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");

            if (digitsOnly.StartsWith("8") && digitsOnly.Length == 11)
                digitsOnly = "7" + digitsOnly[1..];

            if (!digitsOnly.StartsWith("7") && digitsOnly.Length >= 10)
                digitsOnly = "7" + digitsOnly[..10];
            else if (!digitsOnly.StartsWith("7"))
                digitsOnly = "7" + digitsOnly;

            if (digitsOnly.Length < 2) return "+7";
            if (digitsOnly.Length <= 4) return $"+7 ({digitsOnly[1..]})";
            if (digitsOnly.Length <= 7) return $"+7 ({digitsOnly[1..4]}) {digitsOnly[4..]}";
            if (digitsOnly.Length <= 9) return $"+7 ({digitsOnly[1..4]}) {digitsOnly[4..7]}-{digitsOnly[7..]}";
            if (digitsOnly.Length == 10) return $"+7 ({digitsOnly[1..4]}) {digitsOnly[4..7]}-{digitsOnly[7..9]}-{digitsOnly[9..]}";
            if (digitsOnly.Length == 11) return $"+7 ({digitsOnly[1..4]}) {digitsOnly[4..7]}-{digitsOnly[7..9]}-{digitsOnly[9..]}";

            return input;
        }
    }

    /// <summary>
    /// Конвертер: нормализует email, удаляя пробелы и приводя к нижнему регистру. Также предоставляет статическую валидацию.
    /// </summary>
    public class EmailConverter : IValueConverter
    {
        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);

        /// <summary>
        /// Возвращает email без изменений.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.ToString() ?? string.Empty;

        /// <summary>
        /// Очищает строку от пробелов и приводит к нижнему регистру.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value?.ToString() ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>
        /// Проверяет, соответствует ли строка формату email.
        /// </summary>
        /// <param name="email">Email-адрес.</param>
        /// <returns>true, если строка — валидный email; иначе false.</returns>
        public static bool IsValidEmail(string email) =>
            string.IsNullOrWhiteSpace(email) || EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Конвертер: проверяет email на валидность для UI (напр., валидация на форме).
    /// </summary>
    public class EmailValidationConverter : IValueConverter
    {
        /// <summary>
        /// Возвращает true, если email валиден.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            EmailConverter.IsValidEmail(value?.ToString() ?? string.Empty);

        /// <summary>
        /// Обратное преобразование не реализовано.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
