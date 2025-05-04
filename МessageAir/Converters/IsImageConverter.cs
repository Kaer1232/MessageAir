using System.Globalization;

namespace МessageAir.Converters
{
    public class IsImageConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string contentType)
            {
                return contentType?.StartsWith("image/") == true;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
