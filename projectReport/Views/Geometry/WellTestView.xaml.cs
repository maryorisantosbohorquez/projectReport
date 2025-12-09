using System.Windows;
using System.Windows.Controls;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class WellTestView : UserControl
    {
        private readonly GeometryViewModel _viewModel;

        public WellTestView()
        {
            InitializeComponent();
            
            // Initialize services
            var geometryService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            
            // Initialize ViewModel with required services
            _viewModel = new GeometryViewModel(geometryService, dataService, new ThermalGradientService());
            DataContext = _viewModel;
        }

        private void AddWellTest_Click(object sender, RoutedEventArgs e)
        {
            var newTest = new WellTest
            {
                Section = "Section 1",
                Type = WellTestType.LeakOff,
                TestValue = 0,
                MD = 0,
                TVD = 0
            };
            
            _viewModel.WellTests.Add(newTest);
        }

        private void DeleteWellTest_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WellTest wellTest)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this well test (Section: {wellTest.Section}, Type: {wellTest.TypeString})?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _viewModel.WellTests.Remove(wellTest);
                }
            }
        }
    }
}

