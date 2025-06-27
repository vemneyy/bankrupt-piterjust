using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

namespace bankrupt_piterjust.Helpers
{
    public static class CurrencyInputBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CurrencyInputBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static readonly CultureInfo _culture = new("en-US")
        {
            NumberFormat = { NumberGroupSeparator = "'", NumberDecimalSeparator = "." }
        };

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

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.CaretIndex = tb.Text.Length;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (e.Key == Key.Back)
            {
                e.Handled = true;
                var raw = GetRawText(tb);
                if (raw.Length > 0)
                {
                    raw = raw[..^1];
                    SetRawText(tb, raw);
                    UpdateText(tb);
                }
            }
            else if (e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

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

        private static readonly DependencyProperty RawTextProperty = DependencyProperty.RegisterAttached(
            "RawText", typeof(string), typeof(CurrencyInputBehavior), new PropertyMetadata(string.Empty));

        private static string GetRawText(TextBox tb) => (string)tb.GetValue(RawTextProperty);
        private static void SetRawText(TextBox tb, string value) => tb.SetValue(RawTextProperty, value);

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
            var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }
}
