using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using ProjectReport.Models.Geometry.ThermalGradient;

namespace ProjectReport.Views.Geometry.Components
{
    /// <summary>
    /// User control for displaying temperature profile chart
    /// </summary>
    public partial class TemperatureProfileChart : UserControl
    {
        public TemperatureProfileChart()
        {
            InitializeComponent();
            
            // Initialize chart configuration
            SeriesCollection = new SeriesCollection();
            
            // Configure Y-axis to be inverted (depth increases downward)
            YAxisFormatter = value => $"{value:F0} ft";
            XAxisFormatter = value => $"{value:F0}°F";
        }

        public static readonly DependencyProperty ThermalPointsProperty =
            DependencyProperty.Register(
                nameof(ThermalPoints),
                typeof(IEnumerable<ThermalGradientPoint>),
                typeof(TemperatureProfileChart),
                new PropertyMetadata(null, OnThermalPointsChanged));

        public IEnumerable<ThermalGradientPoint> ThermalPoints
        {
            get => (IEnumerable<ThermalGradientPoint>)GetValue(ThermalPointsProperty);
            set => SetValue(ThermalPointsProperty, value);
        }

        public SeriesCollection SeriesCollection { get; set; }
        public System.Func<double, string> YAxisFormatter { get; set; }
        public System.Func<double, string> XAxisFormatter { get; set; }

        private static void OnThermalPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TemperatureProfileChart chart)
            {
                chart.UpdateChart();
            }
        }

        private void UpdateChart()
        {
            SeriesCollection.Clear();

            if (ThermalPoints == null || !ThermalPoints.Any())
                return;

            var sortedPoints = ThermalPoints.OrderBy(p => p.TVD).ToList();

            // Create line series for temperature profile
            var lineSeries = new LineSeries
            {
                Title = "Temperature Profile",
                Values = new ChartValues<ObservablePoint>(
                    sortedPoints.Select(p => new ObservablePoint(p.Temperature, p.TVD))
                ),
                Stroke = System.Windows.Media.Brushes.Orange,
                Fill = System.Windows.Media.Brushes.Transparent,
                StrokeThickness = 2,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 8,
                PointForeground = System.Windows.Media.Brushes.OrangeRed
            };

            SeriesCollection.Add(lineSeries);
        }
    }
}
