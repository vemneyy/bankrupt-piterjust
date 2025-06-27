using System.Text;

namespace bankrupt_piterjust.Helpers
{
    public static class NumberToWordsConverter
    {
        private static readonly string[] units = new string[] { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять", "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
        private static readonly string[] tens = new string[] { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
        private static readonly string[] hundreds = new string[] { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
        private static readonly string[] unitsForThousands = new string[] { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };

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
            {
                result[0] = char.ToUpper(result[0]);
            }

            return result.ToString();
        }

        private static string ConvertNumberToWords(int number)
        {
            if (number == 0)
                return "ноль";

            var words = new StringBuilder();

            if (number >= 1000000)
            {
                int millions = number / 1000000;
                words.Append(ConvertLessThanThousand(millions));
                words.Append(" ");
                words.Append(GetMillionForm(millions));
                number %= 1000000;
                if (number > 0)
                    words.Append(" ");
            }

            if (number >= 1000)
            {
                int thousands = number / 1000;
                words.Append(ConvertLessThanThousandForThousands(thousands));
                words.Append(" ");
                words.Append(GetThousandForm(thousands));
                number %= 1000;
                if (number > 0)
                    words.Append(" ");
            }

            if (number > 0)
            {
                words.Append(ConvertLessThanThousand(number));
            }

            return words.ToString();
        }

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
                        words.Append(" ");
                        words.Append(units[unit]);
                    }
                }
            }

            return words.ToString();
        }

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
                        words.Append(" ");
                        if (unit < 3)
                            words.Append(unitsForThousands[unit]);
                        else
                            words.Append(units[unit]);
                    }
                }
            }

            return words.ToString();
        }

        private static string GetRubleForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "рублей";

            if (lastDigit == 1)
                return "рубль";

            if (lastDigit >= 2 && lastDigit <= 4)
                return "рубля";

            return "рублей";
        }

        private static string GetKopeckForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "копеек";

            if (lastDigit == 1)
                return "копейка";

            if (lastDigit >= 2 && lastDigit <= 4)
                return "копейки";

            return "копеек";
        }

        private static string GetThousandForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "тысяч";

            if (lastDigit == 1)
                return "тысяча";

            if (lastDigit >= 2 && lastDigit <= 4)
                return "тысячи";

            return "тысяч";
        }

        private static string GetMillionForm(int number)
        {
            number = Math.Abs(number);
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "миллионов";

            if (lastDigit == 1)
                return "миллион";

            if (lastDigit >= 2 && lastDigit <= 4)
                return "миллиона";

            return "миллионов";
        }
    }
}