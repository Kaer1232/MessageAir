using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace МessageAir.Converters
{
    public class BoolToTextAlignmentConverter: IValueConverter
    {
        public TextAlignment TrueOption { get; set; } = TextAlignment.End;
        public TextAlignment FalseOption { get; set; } = TextAlignment.Start;

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
