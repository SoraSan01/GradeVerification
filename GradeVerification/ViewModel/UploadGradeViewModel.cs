using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class UploadGradesViewModel : INotifyPropertyChanged
    {
        // Properties
        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Grade> _grades;
        public ObservableCollection<Grade> Grades
        {
            get { return _grades; }
            set
            {
                _grades = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand BrowseCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        // Constructor
        public UploadGradesViewModel()
        {
            Grades = new ObservableCollection<Grade>();

            // Initialize commands
            BrowseCommand = new RelayCommand(BrowseFile);
            SaveCommand = new RelayCommand(SaveGrades);
            CancelCommand = new RelayCommand(Cancel);
        }

        // Command Methods
        private void BrowseFile(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents|*.doc;*.docx",
                Title = "Select a Grade Document"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                ExtractGradesFromWord(FilePath);
            }
        }

        private void ExtractGradesFromWord(string filePath)
        {
            dynamic wordApp = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application"));
            dynamic doc = null;

            try
            {
                doc = wordApp.Documents.Open(filePath, ReadOnly: false, Visible: false);

                // Assuming the grades are in a table in the Word document
                dynamic table = doc.Tables[1]; // Assuming the first table contains the grades

                // Loop through the rows of the table (skip the header row)
                for (int i = 2; i <= table.Rows.Count; i++)
                {
                    // Extract the student name from the first column
                    string studentName = table.Cell(i, 1).Range.Text.Trim();

                    // Fetch the StudentId using the student name
                    string studentId = GetStudentIdByName(studentName);

                    // Extract the final grade from the fourth column
                    string finalGrade = table.Cell(i, 4).Range.Text.Trim();

                    // Create a Grade object and add it to the list
                    Grade grade = new Grade
                    {
                        StudentId = studentId, // Assign the fetched StudentId
                        SubjectId = "SUBJECT-ID", // Replace with actual subject ID logic
                        Score = finalGrade
                    };

                    Grades.Add(grade);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., file not found, invalid format, etc.)
                MessageBox.Show("Error extracting grades from Word document: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Close the document and release resources
                if (doc != null)
                {
                    doc.Close(false);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
            }
        }

        // Method to fetch StudentId by student name
        private string GetStudentIdByName(string studentName)
        {
            // TODO: Replace this with your actual logic to fetch the StudentId from the database
            // Example: Query the database or a service to get the StudentId by name
            using (var context = new ApplicationDbContext()) // Replace with your actual DbContext
            {
                var student = context.Students.FirstOrDefault(s => s.FullName == studentName);
                if (student != null)
                {
                    return student.Id;
                }
            }

            // If no student is found, return a default or throw an exception
            return "UNKNOWN-STUDENT-ID";
        }

        private void SaveGrades(object parameter)
        {
            // TODO: Implement logic to save grades to the database
            // Example: Use a service or repository to save the Grades collection
            MessageBox.Show("Grades saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel(object parameter)
        {
            // Close the window
            Application.Current.Windows.OfType<UploadGrades>().FirstOrDefault()?.Close();
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}