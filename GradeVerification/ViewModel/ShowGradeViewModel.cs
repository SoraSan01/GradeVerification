using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Helper;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.Windows.Shapes;

namespace GradeVerification.ViewModel
{
    public class ShowGradeViewModel : INotifyPropertyChanged
    {
        private readonly Student _student;
        private readonly ApplicationDbContext _context;
        private Notifier _notifier;
        private bool _isLoading;

        public ObservableCollection<Grade> Grades { get; set; } = new ObservableCollection<Grade>();

        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Expose the student to the view for binding
        public Student Student => _student;

        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        public ShowGradeViewModel(Student student)
        {
            _student = student ?? throw new ArgumentNullException(nameof(student));
            _context = new ApplicationDbContext();
            CloseCommand = new RelayCommand(CloseWindow);
            PrintCommand = new RelayCommand(PrintWindow);
            CurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            LoadGrades();
        }

        private async void LoadGrades()
        {
            try
            {
                IsLoading = true;
                var grades = await _context.Grade
                    .Include(g => g.Subject)
                    .Where(g => g.StudentId == _student.Id)
                    .ToListAsync();

                Grades.Clear();
                foreach (var grade in grades)
                {
                    Grades.Add(grade);
                }
                if (!Grades.Any())
                {
                    _notifier.ShowError("No grade records found for this student.");
                }
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Error loading grades: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrintWindow(object parameter)
        {
            // Retrieve the current ShowGradeWindow instance.
            var window = Application.Current.Windows.OfType<ShowGradeWindow>().FirstOrDefault();
            if (window == null || window.MainBorder == null)
            {
                MessageBox.Show("Could not find the main border control!");
                return;
            }

            // Determine which UIElement to print: use parameter if it is a UIElement; otherwise, fallback to MainBorder.
            UIElement printArea = parameter as UIElement ?? window.MainBorder;

            // Save original visibilities so they can be restored later.
            var originalPrintButtonVisibility = window.PrintButton.Visibility;
            var originalCloseButtonVisibility = window.CloseButton.Visibility;

            // Temporarily hide the Print and Close buttons.
            window.PrintButton.Visibility = Visibility.Collapsed;
            window.CloseButton.Visibility = Visibility.Collapsed;
            window.MainBorder.UpdateLayout();

            try
            {
                // Render the printArea to a bitmap.
                double dpi = 96;
                System.Windows.Size size = printArea.RenderSize;
                if (size.IsEmpty || size.Width == 0 || size.Height == 0)
                {
                    printArea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                    printArea.Arrange(new Rect(printArea.DesiredSize));
                    size = printArea.DesiredSize;
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(
                    (int)size.Width,
                    (int)size.Height,
                    dpi,
                    dpi,
                    PixelFormats.Pbgra32);
                rtb.Render(printArea);

                // Create an Image control (using System.Windows.Controls.Image) to host the bitmap.
                System.Windows.Controls.Image image = new System.Windows.Controls.Image
                {
                    Source = rtb,
                    Width = size.Width,
                    Height = size.Height
                };

                // Create a FixedDocument that will host the preview.
                FixedDocument fixedDoc = new FixedDocument();
                // Set the page size to the size of the UI element.
                fixedDoc.DocumentPaginator.PageSize = size;

                // Create a FixedPage, add the image to it, and wrap it in a PageContent.
                FixedPage page = new FixedPage
                {
                    Width = size.Width,
                    Height = size.Height
                };
                page.Children.Add(image);
                PageContent pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                fixedDoc.Pages.Add(pageContent);

                // Create and show the PrintPreviewWindow by passing the FixedDocument to its constructor.
                var previewWindow = new PrintPreviewWindow(fixedDoc);
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during print preview: " + ex.Message);
            }
            finally
            {
                // Restore the original visibility for the buttons.
                window.PrintButton.Visibility = originalPrintButtonVisibility;
                window.CloseButton.Visibility = originalCloseButtonVisibility;
                window.MainBorder.UpdateLayout();
            }
        }


        private void CloseWindow(object parameter)
        {
            Application.Current.Windows.OfType<ShowGradeWindow>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
