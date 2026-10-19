using System.Globalization;
using System.Windows.Data;

namespace _curved__polygon_editor.Helpers
{
    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter is string param && double.TryParse(param, out var subtract))
                return d - subtract;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
