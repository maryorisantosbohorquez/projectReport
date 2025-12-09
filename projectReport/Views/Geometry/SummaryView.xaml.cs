using System.Windows.Controls;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class SummaryView : UserControl
    {
        private readonly GeometryViewModel _viewModel;
        private WellboreVisualizer? _visualizer;

        public SummaryView()
        {
            InitializeComponent();
            
            // Initialize services
            var geometryService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            
            // Initialize ViewModel with required services
            _viewModel = new GeometryViewModel(geometryService, dataService, new ThermalGradientService());
            DataContext = _viewModel;

            // Initialize visualizer when canvas is loaded
            VisualSchemeCanvas.Loaded += (s, e) =>
            {
                _visualizer = new WellboreVisualizer(VisualSchemeCanvas);
                UpdateVisualScheme();
            };

            // Subscribe to collection changes to update visual scheme
            _viewModel.WellboreComponents.CollectionChanged += (s, e) => UpdateVisualScheme();
            _viewModel.DrillStringComponents.CollectionChanged += (s, e) => UpdateVisualScheme();
        }

        private void UpdateVisualScheme()
        {
            if (_visualizer != null && VisualSchemeCanvas.ActualHeight > 0)
            {
                _visualizer.Draw(
                    _viewModel.WellboreComponents,
                    _viewModel.DrillStringComponents,
                    _viewModel.TotalWellboreMD
                );
            }
        }
    }
}

