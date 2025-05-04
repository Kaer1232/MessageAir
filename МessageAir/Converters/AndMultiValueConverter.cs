using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace МessageAir.Converters
{
    public class AndMultiValueConverter: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            // Проверяем, что оба значения не null и могут быть преобразованы в bool
            bool firstValue = values[0] is bool b1 && b1;
            bool secondValue = values[1] is bool b2 && b2;

            return firstValue && secondValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
