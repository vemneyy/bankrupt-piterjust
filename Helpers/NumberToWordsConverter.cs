using System.Text;

namespace bankrupt_piterjust.Helpers
{
    /// <summary>
    /// Класс для преобразования чисел в текстовое представление на русском языке,
    /// включая корректное склонение слов «рубль», «копейка», «тысяча», «миллион».
    /// </summary>
    public static class NumberToWordsConverter
    {
        // Единицы и числа до 19 включительно
        private static readonly string[] units = [
            "", "один", "два", "три", "четыре", "пять",
            "шесть", "семь", "восемь", "девять", "десять",
            "одиннадцать", "двенадцать", "тринадцать", "четырнадцать",
            "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
        ];

        // Десятки
        private static readonly string[] tens = [
            "", "", "двадцать", "тридцать", "сорок",
            "пятьдесят", "шестьдесят", "семьдесят",
            "восемьдесят", "девяносто"
        ];

        // Сотни
        private static readonly string[] hundreds = [
            "", "сто", "двести", "триста", "четыреста",
            "пятьсот", "шестьсот", "семьсот",
            "восемьсот", "девятьсот"
        ];

        // Единицы женского рода для тысяч
        private static readonly string[] unitsForThousands = [
            "", "одна", "две", "три", "четыре",
            "пять", "шесть", "семь", "восемь", "девять"
        ];

        /// <summary>
        /// Преобразует десятичное число в текстовую строку с рублями и копейками.
        /// </summary>
        /// <param name="number">Число с двумя десятичными знаками (рубли.копейки)</param>
        /// <returns>Строка, представляющая число словами, с правильными падежами</returns>
        public static string ConvertToWords(decimal number)
        {
            number = Math.Round(number, 2);

            int rubles = (int)number;
            int kopecks = (int)((number - rubles) * 100);

            var result = new StringBuilder();

            result.Append(ConvertNumberToWords(rubles));
            result.Append(" ");
            result.Append(GetRubleForm(rubles));
            result.Append(" ");
            if (kopecks < 10)
                result.Append("0");
            result.Append(kopecks);
            result.Append(" ");
            result.Append(GetKopeckForm(kopecks));

            if (result.Length > 0)
                result[0] = char.ToUpper(result[0]);

            return result.ToString();
        }

        /// <summary>
        /// Преобразует целое число в текстовое представление.
        /// Обрабатывает миллионы, тысячи и остаток.
        /// </summary>
        private static string ConvertNumberToWords(int number)
        {
            if (number == 0)
                return "ноль";

            var words = new StringBuilder();

            if (number >= 1_000_000)
            {
                int millions = number / 1_000_000;
                words.Append(ConvertLessThanThousand(millions));
                words.Append(" ");
                words.Append(GetMillionForm(millions));
                number %= 1_000_000;
                if (number > 0)
                    words.Append(" ");
            }

            if (number >= 1_000)
            {
                int thousands = number / 1_000;
                words.Append(ConvertLessThanThousandForThousands(thousands));
                words.Append(" ");
                words.Append(GetThousandForm(thousands));
                number %= 1_000;
                if (number > 0)
                    words.Append(" ");
            }

            if (number > 0)
                words.Append(ConvertLessThanThousand(number));

            return words.ToString();
        }

        /// <summary>
        /// Преобразует число от 0 до 999 в слова (мужской род).
        /// </summary>
        private static string ConvertLessThanThousand(int number)
        {
            var words = new StringBuilder();

            if (number >= 100)
            {
                words.Append(hundreds[number / 100]);
                number %= 100;
                if (number > 0)
                    words.Append(" ");
            }

            if (number > 0)
            {
                if (number < 20)
                {
                    words.Append(units[number]);
                }
                else
                {
                    words.Append(tens[number / 10]);
                    int unit = number % 10;
                    if (unit > 0)
                    {
                        words.Append(' ');
                        words.Append(units[unit]);
                    }
                }
            }

            return words.ToString();
        }

        /// <summary>
        /// Преобразует число от 0 до 999 в слова (для тысяч, женский род).
        /// </summary>
        private static string ConvertLessThanThousandForThousands(int number)
        {
            var words = new StringBuilder();

            if (number >= 100)
            {
                words.Append(hundreds[number / 100]);
                number %= 100;
                if (number > 0)
                    words.Append(" ");
            }

            if (number > 0)
            {
                if (number < 20)
                {
                    if (number < 10)
                        words.Append(unitsForThousands[number]);
                    else
                        words.Append(units[number]);
                }
                else
                {
                    words.Append(tens[number / 10]);
                    int unit = number % 10;
                    if (unit > 0)
                    {
                        words.Append(' ');
                        if (unit < 3)
                            words.Append(unitsForThousands[unit]);
                        else
                            words.Append(units[unit]);
                    }
                }
            }

            return words.ToString();
        }

        /// <summary>
        /// Возвращает нужную форму слова «рубль» в зависимости от количества.
        /// </summary>
        private static string GetRubleForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "рублей";

            return lastDigit switch
            {
                1 => "рубль",
                >= 2 and <= 4 => "рубля",
                _ => "рублей"
            };
        }

        /// <summary>
        /// Возвращает нужную форму слова «копейка» в зависимости от количества.
        /// </summary>
        private static string GetKopeckForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "копеек";

            return lastDigit switch
            {
                1 => "копейка",
                >= 2 and <= 4 => "копейки",
                _ => "копеек"
            };
        }

        /// <summary>
        /// Возвращает нужную форму слова «тысяча» в зависимости от количества.
        /// </summary>
        private static string GetThousandForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "тысяч";

            return lastDigit switch
            {
                1 => "тысяча",
                >= 2 and <= 4 => "тысячи",
                _ => "тысяч"
            };
        }

        /// <summary>
        /// Возвращает нужную форму слова «миллион» в зависимости от количества.
        /// </summary>
        private static string GetMillionForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "миллионов";

            return lastDigit switch
            {
                1 => "миллион",
                >= 2 and <= 4 => "миллиона",
                _ => "миллионов"
            };
        }
    }
}
