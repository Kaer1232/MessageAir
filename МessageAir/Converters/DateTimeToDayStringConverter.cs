using System.Globalization;

namespace МessageAir.Converters
{
    public class DateTimeToDayStringConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dateString)
            {
                if (DateTime.TryParse(dateString, out var date))
                {
                    var today = DateTime.Today;

                    if (date.Date == today)
                        return "Сегодня";

                    if (date.Date == today.AddDays(-1))
                        return "Вчера";

                    return date.ToString("MMMM dd, yyyy", culture).ToUpper();
                }
            }
            return value?.ToString()?.ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
