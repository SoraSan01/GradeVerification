using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GradeVerification.Converters
{
    public class GradeToColorConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string grade)
            {
                if (int.TryParse(grade, out int numericGrade))
                {
                    return numericGrade < 75 ? Brushes.Red : Brushes.Black;
                }
                else if (grade.Equals("INC", StringComparison.OrdinalIgnoreCase))
                {
                    return Brushes.Red;
                }
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
