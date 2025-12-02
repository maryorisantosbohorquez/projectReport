using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using Microsoft.Win32;
using ProjectReport.Models.Geometry.ThermalGradient;
using ProjectReport.Services;

namespace ProjectReport.ViewModels.Geometry
{
    public class ThermalGradientViewModel : BaseViewModel
    {
        private readonly ThermalGradientService _thermalService;
        private readonly ThermalGradientImportService _importService;
        private int _nextId = 1;

        public ThermalGradientViewModel(ThermalGradientService thermalService)
        {
            _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));
            _importService = new ThermalGradientImportService();
            
            ThermalGradientPoints = new ObservableCollection<ThermalGradientPoint>();
            ThermalGradientPoints.CollectionChanged += OnThermalPointsCollectionChanged;

            // Initialize Chart
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperature",
                    Values = new ChartValues<ObservablePoint>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10,
                    LineSmoothness = 0
                }
            };

            // X-axis = Temperature, Y-axis = TVD (inverted)
            XFormatter = value => $"{value:N1} °F";
            YFormatter = value => $"{Math.Abs(value):N0} ft";

            // Initialize commands
            AddPointCommand = new RelayCommand(_ => AddThermalPoint());
            DeletePointCommand = new RelayCommand(DeleteThermalPoint, CanDeletePoint);
            AutoSortCommand = new RelayCommand(_ => AutoSortPoints());
            ImportDataCommand = new RelayCommand(_ => ImportData());
            ExportDataCommand = new RelayCommand(_ => ExportData());
            ImportFromSurveyCommand = new RelayCommand(_ => ImportFromSurvey());
        }

        #region Properties

        public ObservableCollection<ThermalGradientPoint> ThermalGradientPoints { get; }

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public Func<double, string> XFormatter { get; set; }

        private double _surfaceTemperature;
        public double SurfaceTemperature
        {
            get => _surfaceTemperature;
            set => SetProperty(ref _surfaceTemperature, value);
        }

        private double _bottomHoleTemperature;
        public double BottomHoleTemperature
        {
            get => _bottomHoleTemperature;
            set => SetProperty(ref _bottomHoleTemperature, value);
        }

        private double _temperatureRange;
        public double TemperatureRange
        {
            get => _temperatureRange;
            set => SetProperty(ref _temperatureRange, value);
        }

        private double _averageGradient;
        public double AverageGradient
        {
            get => _averageGradient;
            set => SetProperty(ref _averageGradient, value);
        }

        private int _dataPointsCount;
        public int DataPointsCount
        {
            get => _dataPointsCount;
            set => SetProperty(ref _dataPointsCount, value);
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        private bool _hasValidationError;
        public bool HasValidationError
        {
            get => _hasValidationError;
            set => SetProperty(ref _hasValidationError, value);
        }

        private double _maxWellboreTVD = 0;
        public double MaxWellboreTVD
        {
            get => _maxWellboreTVD;
            set
            {
                if (SetProperty(ref _maxWellboreTVD, value))
                {
                    ValidateAllPoints();
                    RecalculateSummaryStatistics();
                }
            }
        }

        // Nueva propiedad calculada requerida por el código existente
        public bool ShowChart => ThermalGradientPoints.Count >= 2 && !HasValidationError;

        private ObservableCollection<SegmentGradient> _segmentGradients = new ObservableCollection<SegmentGradient>();
        public ObservableCollection<SegmentGradient> SegmentGradients
        {
            get => _segmentGradients;
            set => SetProperty(ref _segmentGradients, value);
        }

        private string _temperatureZones = string.Empty;
        public string TemperatureZones
        {
            get => _temperatureZones;
            set => SetProperty(ref _temperatureZones, value);
        }

        #endregion

        #region Commands

        public ICommand AddPointCommand { get; }
        public ICommand DeletePointCommand { get; }
        public ICommand AutoSortCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ImportFromSurveyCommand { get; }

        #endregion

        #region Command Implementations

        private void AddThermalPoint()
        {
            var newPoint = new ThermalGradientPoint(_nextId++, 0, 70);
            newPoint.PropertyChanged += OnThermalPointPropertyChanged;
            ThermalGradientPoints.Add(newPoint);
        }

        private void DeleteThermalPoint(object? parameter)
        {
            if (parameter is ThermalGradientPoint point)
            {
                point.PropertyChanged -= OnThermalPointPropertyChanged;
                ThermalGradientPoints.Remove(point);
            }
        }

        private bool CanDeletePoint(object? parameter)
        {
            return parameter is ThermalGradientPoint;
        }

        private void AutoSortPoints()
        {
            var sortedPoints = _thermalService.SortByTVD(ThermalGradientPoints.ToList());
            
            ThermalGradientPoints.Clear();
            foreach (var point in sortedPoints)
            {
                ThermalGradientPoints.Add(point);
            }

            ToastNotificationService.Instance.ShowSuccess("Thermal gradient points sorted by TVD");
        }

        private void ImportData()
        {
            try
            {
                var importedPoints = _importService.ShowImportDialog();
                
                if (importedPoints != null && importedPoints.Count > 0)
                {
                    // Validate imported data
                    var validationErrors = _importService.ValidateImportedData(importedPoints);
                    
                    if (validationErrors.Count > 0)
                    {
                        var message = "Imported data has warnings:\n" + string.Join("\n", validationErrors.Take(5));
                        ToastNotificationService.Instance.ShowWarning(message);
                    }

                    // Clear existing points and add imported ones
                    ThermalGradientPoints.Clear();
                    
                    foreach (var point in importedPoints)
                    {
                        point.PropertyChanged += OnThermalPointPropertyChanged;
                        ThermalGradientPoints.Add(point);
                    }

                    _nextId = importedPoints.Max(p => p.Id) + 1;
                    
                    ToastNotificationService.Instance.ShowSuccess($"Imported {importedPoints.Count} thermal points");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing data: {ex.Message}");
            }
        }

        private void ExportData()
        {
            try
            {
                if (ThermalGradientPoints.Count == 0)
                {
                    ToastNotificationService.Instance.ShowWarning("No data to export");
                    return;
                }

                _importService.ShowExportDialog(ThermalGradientPoints.ToList());
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting data: {ex.Message}");
            }
        }

        private void ImportFromSurvey()
        {
            // Placeholder
            ToastNotificationService.Instance.ShowInfo("Survey integration: This will import TVD values from Survey module and create thermal gradient rows automatically. User will then enter temperature values.");
        }

        #endregion

        #region Event Handlers

        private void OnThermalPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ThermalGradientPoint point in e.NewItems)
                {
                    point.PropertyChanged += OnThermalPointPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (ThermalGradientPoint point in e.OldItems)
                {
                    point.PropertyChanged -= OnThermalPointPropertyChanged;
                }
            }

            // Validar antes de recalcular para que ShowChart refleje el estado correcto
            ValidateAllPoints();
            RecalculateSummaryStatistics();
            UpdateChart();
        }

        private void OnThermalPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThermalGradientPoint.TVD) || 
                e.PropertyName == nameof(ThermalGradientPoint.Temperature))
            {
                // Validar primero
                ValidateAllPoints();
                RecalculateSummaryStatistics();
                UpdateChart();
            }
        }

        #endregion

        #region Validation

        private void ValidateAllPoints()
        {
            if (ThermalGradientPoints.Count == 0)
            {
                ValidationMessage = string.Empty;
                HasValidationError = false;
                return;
            }

            var errors = new List<string>();

            // BR-TG-001: TVD Ordering
            var orderingErrors = _thermalService.ValidateTVDOrdering(ThermalGradientPoints.ToList());
            errors.AddRange(orderingErrors);

            // BR-TG-002: TVD Range Validation
            if (MaxWellboreTVD > 0)
            {
                var rangeErrors = _thermalService.ValidateTVDRange(ThermalGradientPoints.ToList(), MaxWellboreTVD);
                errors.AddRange(rangeErrors);
            }

            // BR-TG-003: Temperature Gradient Logic
            var gradientWarnings = _thermalService.ValidateTemperatureGradient(ThermalGradientPoints.ToList());
            errors.AddRange(gradientWarnings);

            // BR-TG-004: Minimum Data Points
            if (ThermalGradientPoints.Count < 2)
            {
                errors.Add("Add at least 2 thermal points to generate temperature profile");
            }

            if (errors.Any())
            {
                ValidationMessage = string.Join("\n", errors);
                HasValidationError = true;
            }
            else
            {
                ValidationMessage = string.Empty;
                HasValidationError = false;
            }

            // Notificar que ShowChart puede haber cambiado
            OnPropertyChanged(nameof(ShowChart));
        }

        #endregion

        #region Summary Statistics

        private void RecalculateSummaryStatistics()
        {
            DataPointsCount = ThermalGradientPoints.Count;

            if (ThermalGradientPoints.Count == 0)
            {
                SurfaceTemperature = 0;
                BottomHoleTemperature = 0;
                TemperatureRange = 0;
                AverageGradient = 0;
                OnPropertyChanged(nameof(ShowChart));
                return;
            }

            var sortedPoints = ThermalGradientPoints.OrderBy(p => p.TVD).ToList();

            SurfaceTemperature = sortedPoints.First().Temperature;
            
            // Calculate bottom hole temperature using formula:
            // bottom_hole_temperature = (total_wellbore_depth * gradient) / 100 + ambient_temperature
            if (sortedPoints.Count >= 2 && MaxWellboreTVD > 0)
            {
                var gradient = _thermalService.CalculateAverageGradient(sortedPoints);
                BottomHoleTemperature = (MaxWellboreTVD * gradient) / 100 + SurfaceTemperature;
            }
            else
            {
                BottomHoleTemperature = sortedPoints.Last().Temperature;
            }
            
            TemperatureRange = BottomHoleTemperature - SurfaceTemperature;

            if (sortedPoints.Count >= 2)
            {
                AverageGradient = _thermalService.CalculateAverageGradient(sortedPoints);
                
                // Calculate segment gradients
                var segments = _thermalService.CalculateSegmentGradients(sortedPoints);
                SegmentGradients.Clear();
                foreach (var segment in segments)
                {
                    SegmentGradients.Add(segment);
                }
                
                // Calculate temperature zones
                CalculateTemperatureZones(sortedPoints);
            }
            else
            {
                AverageGradient = 0;
                SegmentGradients.Clear();
                TemperatureZones = string.Empty;
            }

            // Notificar cambio de ShowChart (depende de DataPointsCount y HasValidationError)
            OnPropertyChanged(nameof(ShowChart));
        }

        private void CalculateTemperatureZones(List<ThermalGradientPoint> sortedPoints)
        {
            var zones = new List<string>();
            
            // Find temperature ranges
            var minTemp = sortedPoints.Min(p => p.Temperature);
            var maxTemp = sortedPoints.Max(p => p.Temperature);
            
            if (minTemp < 150)
                zones.Add($"Cool (< 150°F): {sortedPoints.Where(p => p.Temperature < 150).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature < 150).Max(p => p.TVD):F0} ft");
            
            if (sortedPoints.Any(p => p.Temperature >= 150 && p.Temperature < 250))
                zones.Add($"Moderate (150-250°F): {sortedPoints.Where(p => p.Temperature >= 150 && p.Temperature < 250).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 150 && p.Temperature < 250).Max(p => p.TVD):F0} ft");
            
            if (sortedPoints.Any(p => p.Temperature >= 250 && p.Temperature < 350))
                zones.Add($"Hot (250-350°F): {sortedPoints.Where(p => p.Temperature >= 250 && p.Temperature < 350).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 250 && p.Temperature < 350).Max(p => p.TVD):F0} ft");
            
            if (maxTemp >= 350)
                zones.Add($"Very Hot (> 350°F): {sortedPoints.Where(p => p.Temperature >= 350).Min(p => p.TVD):F0}-{sortedPoints.Where(p => p.Temperature >= 350).Max(p => p.TVD):F0} ft");
            
            TemperatureZones = zones.Count > 0 ? string.Join(" | ", zones) : "No zones defined";
        }

        private void UpdateChart()
        {
            if (SeriesCollection != null && SeriesCollection.Count > 0)
            {
                var values = new ChartValues<ObservablePoint>();
                
                foreach (var point in ThermalGradientPoints.OrderBy(p => p.TVD))
                {
                    // X = Temperature, Y = TVD (negative for inversion)
                    values.Add(new ObservablePoint(point.Temperature, -point.TVD));
                }

                SeriesCollection[0].Values = values;
            }
        }

        #endregion

        #region Public Methods

        public int GetNextId()
        {
            return _nextId++;
        }

        public double GetTemperatureAtTVD(double tvd)
        {
            if (ThermalGradientPoints.Count < 2)
                return 0;

            return _thermalService.InterpolateTemperature(ThermalGradientPoints.ToList(), tvd);
        }

        #endregion
    }
}
