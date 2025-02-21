using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GradeVerification.Service
{
    public static class GradeDocumentParser
    {
        public static List<(string StudentName, string FinalGrade)> ParseDocumentContent(string filePath)
        {
            var grades = new List<(string StudentName, string FinalGrade)>();

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var tables = wordDoc.MainDocumentPart.Document.Body.Elements<Table>();

                foreach (var table in tables)
                {
                    var rows = table.Elements<TableRow>().Skip(2); // Skip header rows

                    foreach (var row in rows)
                    {
                        var cells = row.Elements<TableCell>().ToList();
                        if (cells.Count < 7) continue;

                        string studentName = FormatName(cells[1].InnerText.Trim());
                        string finalGrade = cells[6].InnerText.Trim();

                        if (studentName.Equals("Nothing Follows", StringComparison.OrdinalIgnoreCase))
                            continue;

                        finalGrade = finalGrade.Equals("Inc.", StringComparison.OrdinalIgnoreCase) ? "INC" : finalGrade;

                        grades.Add((studentName, finalGrade));
                    }
                }
            }

            return grades;
        }

        private static string FormatName(string originalName)
        {
            var parts = originalName.Split(new[] { ',' }, 2);
            return parts.Length == 2
                ? $"{parts[0].Trim()} {parts[1].Trim()}".Replace(",", "") // Keep LastName FirstName order
                : originalName.Replace(",", "");
        }
    }
}
