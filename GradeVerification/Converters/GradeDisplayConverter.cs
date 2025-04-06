using GradeVerification.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GradeVerification.Converters
{
    public class GradeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Grade grade)
            {
                if (!string.IsNullOrEmpty(grade.CompletionGrade))
                {
                    return $"{grade.CompletionGrade} (Completion)";
                }
                return grade.Score;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
