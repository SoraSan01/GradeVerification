using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GradeVerification.Converters
{
    public class RowCountToHeightConverter : IValueConverter
    {
        private const double RowHeight = 40; // Height of each row
        private const double HeaderHeight = 30; // Height of the header
        private const double Padding = 20; // Additional padding
        private const double OtherElementsHeight = 300; // Height of other elements in the window

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rowCount)
            {
                if (parameter as string == "Window")
                {
                    // Calculate window height: (rowCount * RowHeight) + HeaderHeight + Padding + OtherElementsHeight
                    return (rowCount * RowHeight) + HeaderHeight + Padding + OtherElementsHeight;
                }
                // Calculate DataGrid height: (rowCount * RowHeight) + HeaderHeight + Padding
                return (rowCount * RowHeight) + HeaderHeight + Padding;
            }
            return 300; // Default height if the binding fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
