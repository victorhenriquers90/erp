using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProjetoVarejo.Desktop.Wpf.Converters;

/// <summary>String não-vazia → Visible; vazia/nula → Collapsed</summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => throw new NotImplementedException();
}

/// <summary>True → Visible; False → Collapsed</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => (Visibility)value == Visibility.Visible;
}
