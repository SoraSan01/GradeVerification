using GradeVerification.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media; // Correct namespace for WPF Brushes

namespace GradeVerification.Converters
{
    public class GradeColorConverter : IValueConverter
    {
        private static readonly HashSet<string> NonPassingGrades = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INC", "N/A", "NGS", "NN", "-", "DROP"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Grade grade)
                {
                    var gradeValue = !string.IsNullOrWhiteSpace(grade.CompletionGrade)
                        ? grade.CompletionGrade.Trim().ToUpper()
                        : grade.Score?.Trim().ToUpper() ?? "";

                    if (string.IsNullOrWhiteSpace(gradeValue)) return Brushes.Red;
                    if (NonPassingGrades.Contains(gradeValue)) return Brushes.Red;
                    if (decimal.TryParse(gradeValue, out var numeric))
                        return numeric >= 75 ? Brushes.Black : Brushes.Red;

                    return Brushes.Red;
                }
                return Brushes.Black;
            }
            catch
            {
                return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}