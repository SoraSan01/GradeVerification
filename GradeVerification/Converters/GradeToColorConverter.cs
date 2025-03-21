using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media; // Add this to fix Brushes error

namespace GradeVerification.Converters
{
    public class GradeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string grade)
            {
                if (int.TryParse(grade, out int numericGrade))
                {
                    return numericGrade >= 75 ? Brushes.Green : Brushes.Red;
                }
                else if (grade.Equals("INC", StringComparison.OrdinalIgnoreCase) ||
                    (grade.Equals("N/A", StringComparison.OrdinalIgnoreCase)) ||
                    (grade.Equals("NGS", StringComparison.OrdinalIgnoreCase)) ||
                    (grade.Equals("NN", StringComparison.OrdinalIgnoreCase)) ||
                    (grade.Equals("-", StringComparison.OrdinalIgnoreCase)) ||
                    (grade.Equals("DROP", StringComparison.OrdinalIgnoreCase)))
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Transparent; // Default color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
