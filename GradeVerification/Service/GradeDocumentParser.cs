using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public static class GradeDocumentParser
    {
        public static List<(string StudentName, string FinalGrade)> ParseDocumentContent(string filePath)
        {
            var grades = new List<(string, string)>();
            var app = new Application();
            Document doc = null;

            try
            {
                doc = app.Documents.Open(filePath);
                string fullText = doc.Content.Text;
                var lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                bool tableStarted = false;

                foreach (var line in lines)
                {
                    if (line.Contains("STUDENT'S NAME IN ALPHABETICAL ORDER"))
                        tableStarted = true;

                    if (!tableStarted) continue;

                    if (line.StartsWith("|") && char.IsDigit(line.Trim()[1]))
                    {
                        var columns = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                        if (columns.Length >= 7)
                        {
                            // Format: | 1 | ACEBEEDO, ABCDE CZARRINA ERIKA | 81 | 83 | 82 | 8 | 83 |
                            var studentName = FormatName(columns[1].Trim());
                            var finalGrade = columns[6].Trim();

                            // Handle "Inc." grade
                            if (finalGrade.Equals("Inc.", StringComparison.OrdinalIgnoreCase))
                                finalGrade = "INC";

                            grades.Add((studentName, finalGrade));
                        }
                    }
                }

                return grades;
            }
            finally
            {
                doc?.Close();
                app.Quit();
            }
        }

        private static string FormatName(string originalName)
        {
            // Convert "LASTNAME, FIRSTNAME" to "FIRSTNAME LASTNAME"
            var parts = originalName.Split(new[] { ',' }, 2);
            return parts.Length == 2
                ? $"{parts[1].Trim()} {parts[0].Trim()}"
                : originalName;
        }
    }
}
