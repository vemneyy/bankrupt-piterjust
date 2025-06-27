using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace bankrupt_piterjust.Converters
{
    public class StringEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringNotEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Visible;

            return value.ToString() != parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
                return parameter.ToString()!;

            return null!;
        }
    }
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    public class BooleanToInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }

    public class BoolToValueConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Конвертер для форматирования серии паспорта (4 цифры)
    /// </summary>
    public class PassportSeriesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            // Оставляем только цифры
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            // Ограничиваем до 4 символов
            return digitsOnly.Length > 4 ? digitsOnly.Substring(0, 4) : digitsOnly;
        }
    }

    /// <summary>
    /// Конвертер для форматирования номера паспорта (6 цифр)
    /// </summary>
    public class PassportNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            // Оставляем только цифры
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            // Ограничиваем до 6 символов
            return digitsOnly.Length > 6 ? digitsOnly.Substring(0, 6) : digitsOnly;
        }
    }

    /// <summary>
    /// Конвертер для форматирования кода подразделения (XXX-XXX)
    /// </summary>
    public class DivisionCodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            // Убираем все кроме цифр
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            
            if (digitsOnly.Length == 0) return string.Empty;
            if (digitsOnly.Length <= 3) return digitsOnly;
            if (digitsOnly.Length <= 6) return $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3)}";
            
            // Если больше 6 цифр, обрезаем
            return $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3, 3)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            // Убираем все кроме цифр
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            
            // Ограничиваем до 6 цифр
            if (digitsOnly.Length > 6)
                digitsOnly = digitsOnly.Substring(0, 6);
            
            // Форматируем с тире если есть более 3 цифр
            if (digitsOnly.Length > 3)
                return $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3)}";
            
            return digitsOnly;
        }
    }

    /// <summary>
    /// Конвертер для форматирования денежных сумм с двумя знаками после запятой
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        private static readonly CultureInfo _culture = new("en-US")
        {
            NumberFormat = { NumberGroupSeparator = "'", NumberDecimalSeparator = "." }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
                return decimalValue.ToString("N2", _culture);

            if (decimal.TryParse(value?.ToString()?.Replace("'", string.Empty), NumberStyles.Any, _culture, out decimal parsedValue))
                return parsedValue.ToString("N2", _culture);

            return 0m.ToString("N2", _culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;

            string input = value.ToString() ?? string.Empty;
            input = input.Replace("'", string.Empty);

            if (decimal.TryParse(input, NumberStyles.Any, _culture, out decimal result))
                return Math.Round(result, 2);

            return 0m;
        }
    }

    /// <summary>
    /// Конвертер для форматирования мобильного телефона в формате +7 (921) 444-31-23
    /// </summary>
    public class PhoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            return FormatPhone(input);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string input = value.ToString() ?? string.Empty;
            return FormatPhone(input);
        }

        private string FormatPhone(string input)
        {
            // Удаляем все символы кроме цифр
            string digitsOnly = Regex.Replace(input, @"[^\d]", "");
            
            // Если пустая строка, возвращаем пустую
            if (string.IsNullOrEmpty(digitsOnly)) return string.Empty;
            
            // Нормализуем номер: если начинается с 8 и длина 11, заменяем на 7
            if (digitsOnly.StartsWith("8") && digitsOnly.Length == 11)
            {
                digitsOnly = "7" + digitsOnly.Substring(1);
            }
            
            // Если номер начинается с 7 и длина больше 11, обрезаем
            if (digitsOnly.StartsWith("7") && digitsOnly.Length > 11)
            {
                digitsOnly = digitsOnly.Substring(0, 11);
            }
            
            // Если номер не начинается с 7 или 8
            if (!digitsOnly.StartsWith("7") && !digitsOnly.StartsWith("8"))
            {
                // Если длина 10 цифр, добавляем 7 в начало
                if (digitsOnly.Length == 10)
                {
                    digitsOnly = "7" + digitsOnly;
                }
                // Если больше 10, обрезаем до 10 и добавляем 7
                else if (digitsOnly.Length > 10)
                {
                    digitsOnly = "7" + digitsOnly.Substring(0, 10);
                }
                // Если меньше 10, добавляем 7 в начало
                else if (digitsOnly.Length > 0)
                {
                    digitsOnly = "7" + digitsOnly;
                }
            }
            
            // Теперь форматируем номер
            if (digitsOnly.Length == 0) return string.Empty;
            
            // Номер должен начинаться с 7 после нормализации
            if (!digitsOnly.StartsWith("7"))
            {
                return digitsOnly; // Возвращаем как есть если что-то пошло не так
            }
            
            // Форматируем поэтапно
            if (digitsOnly.Length == 1) return "+7";
            if (digitsOnly.Length <= 4)
            {
                return $"+7 ({digitsOnly.Substring(1)}";
            }
            if (digitsOnly.Length <= 7)
            {
                string code = digitsOnly.Substring(1, 3);
                string rest = digitsOnly.Substring(4);
                return $"+7 ({code}) {rest}";
            }
            if (digitsOnly.Length <= 9)
            {
                string code = digitsOnly.Substring(1, 3);
                string part1 = digitsOnly.Substring(4, 3);
                string part2 = digitsOnly.Substring(7);
                return $"+7 ({code}) {part1}-{part2}";
            }
            if (digitsOnly.Length == 10)
            {
                // Это случай когда у нас 10 цифр без 7, но мы уже добавили 7, значит теперь 11
                string code = digitsOnly.Substring(1, 3);
                string part1 = digitsOnly.Substring(4, 3);
                string part2 = digitsOnly.Substring(7, 2);
                string part3 = digitsOnly.Substring(9);
                return $"+7 ({code}) {part1}-{part2}-{part3}";
            }
            if (digitsOnly.Length == 11)
            {
                string code = digitsOnly.Substring(1, 3);
                string part1 = digitsOnly.Substring(4, 3);
                string part2 = digitsOnly.Substring(7, 2);
                string part3 = digitsOnly.Substring(9, 2);
                return $"+7 ({code}) {part1}-{part2}-{part3}";
            }
            
            return input; // Возвращаем исходную строку если не удалось форматировать
        }
    }

    /// <summary>
    /// Конвертер для валидации и форматирования email
    /// </summary>
    public class EmailConverter : IValueConverter
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string email = value.ToString() ?? string.Empty;
            
            // Приводим к нижнему регистру и удаляем лишние пробелы
            email = email.Trim().ToLowerInvariant();
            
            return email;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true; // Пустой email считается валидным (необязательное поле)
            return EmailRegex.IsMatch(email);
        }
    }

    /// <summary>
    /// Конвертер для валидации email с визуальной индикацией ошибки
    /// </summary>
    public class EmailValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return true;
            
            string email = value.ToString() ?? string.Empty;
            return EmailConverter.IsValidEmail(email);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}