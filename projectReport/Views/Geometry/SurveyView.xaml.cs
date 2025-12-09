using System.Windows;
using System.Windows.Controls;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class SurveyView : UserControl
    {
        private readonly GeometryViewModel _viewModel;

        public SurveyView()
        {
            InitializeComponent();
            
            // Initialize services
            var geometryService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            
            // Initialize ViewModel with required services
            _viewModel = new GeometryViewModel(geometryService, dataService, new ThermalGradientService());
            DataContext = _viewModel;
        }

        private void AddSurveyPoint_Click(object sender, RoutedEventArgs e)
        {
            var newPoint = new SurveyPoint
            {
                MD = 0,
                TVD = 0,
                HoleAngle = 0,
                Azimuth = 0
            };
            
            _viewModel.SurveyPoints.Add(newPoint);
        }

        private void ImportFromExcel_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Survey Data",
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            // Show dialog and get result
            if (openFileDialog.ShowDialog() == true)
            {
                var importService = new SurveyImportService();
                SurveyImportService.ImportResult result;

                // Determine file type and import accordingly
                string extension = System.IO.Path.GetExtension(openFileDialog.FileName).ToLower();
                if (extension == ".xlsx")
                {
                    result = importService.ImportFromExcel(openFileDialog.FileName);
                }
                else
                {
                    result = importService.ImportFromCsv(openFileDialog.FileName);
                }

                if (result.Success)
                {
                    // Add imported survey points to the collection
                    foreach (var surveyPoint in result.SurveyPoints)
                    {
                        _viewModel.SurveyPoints.Add(surveyPoint);
                    }

                    // Show success message
                    string message = $"Successfully imported {result.ImportedCount} survey point(s).";
                    if (result.ErrorCount > 0)
                    {
                        message += $"\n{result.ErrorCount} row(s) were skipped due to errors.";
                    }

                    MessageBox.Show(message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Show error message
                    MessageBox.Show($"Import failed: {result.ErrorMessage}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteSurveyPoint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SurveyPoint surveyPoint)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this survey point (MD: {surveyPoint.MD:F2} ft)?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.SurveyPoints.Remove(surveyPoint);
                }
            }
        }
    }
}

