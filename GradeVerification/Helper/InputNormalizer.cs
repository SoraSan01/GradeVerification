using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GradeVerification.Helper
{
    public static class InputNormalizer
    {
        public static string NormalizeYear(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Match patterns like "3rd Year", "Year 3", "3", etc.
            var match = Regex.Match(input, @"(\d+)(st|nd|rd|th)?\s*(Year|yr)?", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int yearNumber))
            {
                return yearNumber switch
                {
                    1 => "First Year",
                    2 => "Second Year",
                    3 => "Third Year",
                    4 => "Fourth Year",
                    5 => "Fifth Year",
                    _ => input // Fallback if number is out of expected range
                };
            }

            // Handle written formats (e.g., "Third Year" → no change)
            return input;
        }

        public static string NormalizeSemester(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // Match patterns like "1st Semester", "Sem 1", "1", etc.
            var match = Regex.Match(input, @"(\d+)(st|nd|rd|th)?\s*(Semester|sem)?", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int semesterNumber))
            {
                return semesterNumber switch
                {
                    1 => "First Semester",
                    2 => "Second Semester",
                    3 => "Third Semester",
                    _ => input // Fallback
                };
            }

            // Handle written formats (e.g., "First Semester" → no change)
            return input;
        }
    }
}
