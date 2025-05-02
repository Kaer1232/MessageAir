using System.Globalization;

namespace МessageAir.Converters
{
    public class BoolToLayoutOptionsConverter: IValueConverter
    {
        public LayoutOptions TrueOption { get; set; } = LayoutOptions.End;
        public LayoutOptions FalseOption { get; set; } = LayoutOptions.Start;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? TrueOption : FalseOption;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
