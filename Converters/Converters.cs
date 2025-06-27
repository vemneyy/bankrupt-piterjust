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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
                return decimalValue.ToString("F2", CultureInfo.GetCultureInfo("ru-RU"));
            
            if (decimal.TryParse(value?.ToString(), out decimal parsedValue))
                return parsedValue.ToString("F2", CultureInfo.GetCultureInfo("ru-RU"));
            
            return "0,00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;
            
            string input = value.ToString() ?? string.Empty;
            
            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.GetCultureInfo("ru-RU"), out decimal result))
                return Math.Round(result, 2);
            
            return 0m;
        }
    }
}