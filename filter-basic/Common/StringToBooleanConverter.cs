using System.Globalization;
using System.Windows.Data;

namespace filter_basic.Common;

public class StringToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Trả về true nếu FolderPath không rỗng, ngược lại trả về false
        return !string.IsNullOrWhiteSpace(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}