using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ProjectReport.Models;
using ProjectReport.Models.Geometry;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Models.Geometry.Survey;
using ProjectReport.Models.Geometry.WellTest;
using ProjectReport.Services;
using ProjectReport.Views.Geometry;
using ProjectReport.Views.Modals; // Added for Modal
using ProjectReport.Models.Geometry.ThermalGradient;

namespace ProjectReport.ViewModels.Geometry
{
    public class GeometryViewModel : BaseViewModel
    {
        private readonly GeometryCalculationService _geometryService;
        private readonly GeometryValidationService _validationService; // validation service
        private readonly DataPersistenceService _dataService;
        private readonly ThermalGradientService _thermalService;
        private Well? _currentWell; // Reference to the current well being edited
        private string _wellName = string.Empty;
        private string _reportNumber = string.Empty;
        private string _operator = string.Empty;
        private string _location = string.Empty;
        private string _rigName = string.Empty;
        private int _selectedTabIndex;
        
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public GeometryViewModel(GeometryCalculationService geometryService, DataPersistenceService dataService, ThermalGradientService thermalService)
        {
            _geometryService = geometryService ?? throw new ArgumentNullException(nameof(geometryService));
            _validationService = new GeometryValidationService(); // new instance
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _thermalService = thermalService ?? throw new ArgumentNullException(nameof(thermalService));

            // Initialize Sub-ViewModels
            ThermalGradientViewModel = new ThermalGradientViewModel(_thermalService);
            
            // Initialize collections
            WellboreComponents = new ObservableCollection<WellboreComponent>();
            DrillStringComponents = new ObservableCollection<DrillStringComponent>();
            SurveyPoints = new ObservableCollection<SurveyPoint>();
            WellTests = new ObservableCollection<WellTest>();
            AnnularVolumeDetails = new ObservableCollection<AnnularVolumeDetail>();

            // Initialize dropdown options
            WellboreSectionTypes = new ObservableCollection<WellboreSectionType>(
                Enum.GetValues(typeof(WellboreSectionType)).Cast<WellboreSectionType>());
            
            ComponentTypes = new ObservableCollection<ComponentType>(
                Enum.GetValues(typeof(ComponentType)).Cast<ComponentType>());

            WellTestTypes = new ObservableCollection<string> 
            { 
                "Leak Off", "Fracture gradient", "Pore pressure", "Integrity" 
            };

            // Subscribe to collection changes
            WellboreComponents.CollectionChanged += OnWellboreCollectionChanged;
            DrillStringComponents.CollectionChanged += OnDrillStringCollectionChanged;
            WellboreComponents.CollectionChanged += (s, e) => OnPropertyChanged(nameof(WellboreSectionNames));

            // Subscribe to property changes in components
            foreach (var component in WellboreComponents)
            {
                component.PropertyChanged += OnWellboreComponentChanged;
            }
            foreach (var component in DrillStringComponents)
            {
                component.PropertyChanged += OnDrillStringComponentChanged;
            }
        }



        // Dropdown options
        public ObservableCollection<WellboreSectionType> WellboreSectionTypes { get; }
        public ObservableCollection<ComponentType> ComponentTypes { get; }
        public ObservableCollection<string> WellTestTypes { get; }

        // Sub-ViewModels
        public ThermalGradientViewModel ThermalGradientViewModel { get; }
        
        // Wellbore section names for Well Test dropdown
        public ObservableCollection<string> WellboreSectionNames => 
            new ObservableCollection<string>(WellboreComponents.Select(w => w.Name).Where(n => !string.IsNullOrEmpty(n)));

        private void OnWellboreCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (WellboreComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnWellboreComponentChanged;
                    ValidateWellboreComponent(component);
                }
            }
            if (e.OldItems != null)
            {
                foreach (WellboreComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnWellboreComponentChanged;
                }
            }
            
            // Re-validate all components after collection change (order may have changed)
            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }
            
            RecalculateTotals();
        }

        private void OnDrillStringCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DrillStringComponent component in e.NewItems)
                {
                    component.PropertyChanged += OnDrillStringComponentChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (DrillStringComponent component in e.OldItems)
                {
                    component.PropertyChanged -= OnDrillStringComponentChanged;
                }
            }
            RecalculateTotals();
        }

        private void OnWellboreComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WellboreComponent.TopMD) || 
                e.PropertyName == nameof(WellboreComponent.BottomMD) ||
                e.PropertyName == nameof(WellboreComponent.ID) ||
                e.PropertyName == nameof(WellboreComponent.OD) ||
                e.PropertyName == nameof(WellboreComponent.SectionType) ||
                e.PropertyName == nameof(WellboreComponent.Washout))
            {
                if (sender is WellboreComponent component)
                {
                    _geometryService.CalculateWellboreComponentVolume(component, "Imperial");
                    ValidateWellboreComponent(component);
                }
                RecalculateTotals();
            }
        }

        /// <summary>
        /// Validates a wellbore component against all rules including telescoping and casing progression
        /// </summary>
        private void ValidateWellboreComponent(WellboreComponent component)
        {
            if (component == null) return;
            
            var sorted = WellboreComponents.OrderBy(c => c.TopMD).ToList();
            int index = sorted.IndexOf(component);
            
            if (index < 0) return;
            
            var previousComponent = index > 0 ? sorted[index - 1] : null;
            
            // Validate telescopic diameter (OD[n] < ID[n-1])
            component.ValidateTelescopicDiameter(previousComponent);
            
            // Validate casing depth progression
            component.ValidateCasingDepthProgression(previousComponent);
            
            // Handle casing override logic
            if (previousComponent != null && 
                (component.SectionType == WellboreSectionType.Casing || component.SectionType == WellboreSectionType.Liner) &&
                (previousComponent.SectionType == WellboreSectionType.Casing || previousComponent.SectionType == WellboreSectionType.Liner))
            {
                // Check for valid casing override: same TopMD, deeper or equal BottomMD
                bool isCasingOverride = Math.Abs(component.TopMD - previousComponent.TopMD) < 0.01 && 
                                       component.BottomMD >= previousComponent.BottomMD;
                
                if (isCasingOverride)
                {
                    // Valid override - previous casing is replaced/extended
                    // Show notification to user (matches spec message)
                    ToastNotificationService.Instance.ShowInfo(
                        "⚠ Casing Override detected → previous casing replaced.");
                }
            }
        }

        private void OnDrillStringComponentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrillStringComponent.Length) || 
                e.PropertyName == nameof(DrillStringComponent.OD) ||
                e.PropertyName == nameof(DrillStringComponent.ID))
            {
                if (sender is DrillStringComponent component)
                {
                    // Volume calculations are now handled automatically in the model
                }
                RecalculateTotals();
            }
        }

        // Header fields
        public string WellName
        {
            get => _wellName;
            set => SetProperty(ref _wellName, value);
        }

        public string ReportNumber
        {
            get => _reportNumber;
            set => SetProperty(ref _reportNumber, value);
        }

        public string Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        public string Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string RigName
        {
            get => _rigName;
            set => SetProperty(ref _rigName, value);
        }

        // Collections
        public ObservableCollection<WellboreComponent> WellboreComponents { get; }
        public ObservableCollection<DrillStringComponent> DrillStringComponents { get; }
        public ObservableCollection<SurveyPoint> SurveyPoints { get; }
        public ObservableCollection<WellTest> WellTests { get; }
        public ObservableCollection<AnnularVolumeDetail> AnnularVolumeDetails { get; }



        #region Commands

        public ICommand SaveCommand => new RelayCommand(async _ => await SaveProjectAsync());
        public ICommand LoadCommand => new RelayCommand(async _ => await LoadProjectAsync());
        public ICommand ExportToCsvCommand => new RelayCommand(ExportToCsv);
        public ICommand ShowVisualizationCommand => new RelayCommand(ShowVisualization);
        public ICommand ForceToBottomCommand => new RelayCommand(_ => ExecuteForceToBottom());
        
        // Export commands for individual tabs
        public ICommand ExportWellboreCsvCommand => new RelayCommand(ExportWellboreCsv);
        public ICommand ExportDrillStringCsvCommand => new RelayCommand(ExportDrillStringCsv);
        public ICommand ExportSurveyCsvCommand => new RelayCommand(ExportSurveyCsv);
        public ICommand ExportWellTestCsvCommand => new RelayCommand(ExportWellTestCsv);
        public ICommand ExportAnnularDetailsCsvCommand => new RelayCommand(ExportAnnularDetailsCsv);
        
        public ICommand ExportWellboreJsonCommand => new RelayCommand(ExportWellboreJson);
        public ICommand ExportDrillStringJsonCommand => new RelayCommand(ExportDrillStringJson);
        public ICommand ExportSurveyJsonCommand => new RelayCommand(ExportSurveyJson);
        public ICommand ExportWellTestJsonCommand => new RelayCommand(ExportWellTestJson);
        
        // Import commands
        public ICommand ImportWellboreDataCommand => new RelayCommand(ImportWellboreData);
        public ICommand ImportDrillStringDataCommand => new RelayCommand(ImportDrillStringData);
        
        // Survey row action commands
        public ICommand MoveSurveyPointUpCommand => new RelayCommand(MoveSurveyPointUp, CanMoveSurveyPointUp);
        public ICommand MoveSurveyPointDownCommand => new RelayCommand(MoveSurveyPointDown, CanMoveSurveyPointDown);
        public ICommand DeleteSurveyPointCommand => new RelayCommand(DeleteSurveyPoint, CanDeleteSurveyPoint);
        
        private async Task SaveProjectAsync()
        {
            try
            {
                // BR-WG-002: Check for continuity errors before saving
                if (!ShowContinuityErrorModal())
                {
                    // If user cancelled or errors exist, don't save
                    return;
                }

                // BR-WG-003: Check for other validation errors
                // Run detailed Geometry Validation
                var validationResult = _validationService.ValidateWellbore(WellboreComponents, 300.0); // Assuming 300.0 for now, should be derived from context? User prompt said "300.00 ft" in rules.
                
                // Clear existing UI errors
                foreach (var comp in WellboreComponents) comp.ClearValidationErrors();

                if (!validationResult.IsValid || validationResult.HasWarnings)
                {
                    // Map errors/warnings back to components for UI highlighting if needed
                    foreach (var item in validationResult.Items)
                    {
                        if (int.TryParse(item.ComponentId, out int index) && index >= 0 && index < WellboreComponents.Count)
                        {
                            WellboreComponents[index].AddValidationError(item.Message);
                        }
                    }

                    // Show Modal
                    var modal = new ValidationResultModal(validationResult);
                    if (Application.Current.MainWindow != null)
                        modal.Owner = Application.Current.MainWindow;
                        
                    modal.ShowDialog();

                    // Logic:
                    // If Critical Errors exist -> Stop (IsValid is false)
                    // If Only Warnings exist -> Check if user clicked "Continue"
                    if (validationResult.HasCriticalErrors)
                    {
                        return; // Block Save
                    }
                    
                    if (validationResult.HasWarnings && !modal.ContinueConfirmed)
                    {
                        return; // User cancelled warning
                    }
                    
                    // If we reach here, it's either Valid or Warnings were Confirmed.
                }

                if (WellboreComponents.Any(c => !c.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Wellbore Geometry before saving.");
                    return;
                }

                // BR-DS-001: Check for Drill String validation errors
                if (DrillStringComponents.Any(c => !c.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Drill String Geometry before saving.");
                    return;
                }

                // Check if drill string exceeds well MD (physically impossible)
                if (DrillStringExceedsMD)
                {
                    ToastNotificationService.Instance.ShowError(
                        $"Drill string exceeds well depth by {Math.Abs(DepthDifferential):F2} ft. " +
                        "Please shorten or revise components before saving.");
                    return;
                }

                // BR-SV-001, BR-SV-002, BR-SV-003: Check for Survey validation errors
                if (SurveyPoints.Any(p => !p.IsValid))
                {
                    ToastNotificationService.Instance.ShowError("Please fix validation errors in Survey module before saving.");
                    return;
                }

                // BR-TG-001, BR-TG-002, BR-TG-003, BR-TG-004: Check for Thermal Gradient validation issues
                if (ThermalGradientViewModel.HasValidationError)
                {
                    // Some thermal gradient issues are warnings (overrideable), some are errors.
                    // We'll ask the user for confirmation.
                    var result = MessageBox.Show(
                        $"Thermal Gradient module has validation issues:\n\n{ThermalGradientViewModel.ValidationMessage}\n\nDo you want to save anyway?",
                        "Thermal Gradient Validation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Project Files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create a new project with the current data
                    var project = new Project
                    {
                        Name = "Wellbore Project",
                        WellName = WellName
                    };
                    
                    // Save the project
                    await DataPersistenceService.SaveProjectAsync(saveFileDialog.FileName, project);
                    
                    // Save the wellbore components
                    var wellboreFilePath = Path.ChangeExtension(saveFileDialog.FileName, ".wellbore.json");
                    await DataPersistenceService.SaveWellboreComponentsAsync(WellboreComponents, wellboreFilePath);
                    
                    // Save the drill string components
                    var drillStringFilePath = Path.ChangeExtension(saveFileDialog.FileName, ".drillstring.json");
                    await DataPersistenceService.SaveDrillStringComponentsAsync(DrillStringComponents, drillStringFilePath);
                    ToastNotificationService.Instance.ShowSuccess("Project saved successfully.");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error saving project: {ex.Message}");
            }
        }
        
        private async Task LoadProjectAsync()
        {
            // Implementation preserved but LoadWell is preferred for app navigation
            await Task.CompletedTask; 
        }

        public void LoadWell(Well well)
        {
            if (well == null) return;

            _currentWell = well; // Store reference to the well
            WellName = well.WellName;
            ReportNumber = well.ReportFor;

            // Load Wellbore Components
            WellboreComponents.Clear();
            foreach (var component in well.WellboreComponents)
            {
                component.PropertyChanged += OnWellboreComponentChanged;
                WellboreComponents.Add(component);
            }
            
            // Validate all components after loading
            foreach (var component in WellboreComponents)
            {
                ValidateWellboreComponent(component);
            }

            // Load Drill String Components
            DrillStringComponents.Clear();
            foreach (var component in well.DrillStringComponents)
            {
                component.PropertyChanged += OnDrillStringComponentChanged;
                DrillStringComponents.Add(component);
            }

            // Load Survey Points
            SurveyPoints.Clear();
            foreach (var point in well.SurveyPoints)
            {
                SurveyPoints.Add(point);
            }

            // Load Well Tests
            WellTests.Clear();
            foreach (var test in well.WellTests)
            {
                WellTests.Add(test);
            }

            // Load Thermal Gradient Points
            ThermalGradientViewModel.ThermalGradientPoints.Clear();
            foreach (var point in well.ThermalGradientPoints)
            {
                ThermalGradientViewModel.ThermalGradientPoints.Add(point);
            }

            RecalculateTotals();
            
            // Update MaxWellboreTVD for thermal gradient validation
            if (ThermalGradientViewModel != null && WellboreComponents.Count > 0)
            {
                var maxTVD = WellboreComponents.Max(w => w.BottomMD);
                ThermalGradientViewModel.MaxWellboreTVD = maxTVD;
            }
        }

        /// <summary>
        /// Saves all geometry data back to the Well object for persistence
        /// </summary>
        public void SaveToWell()
        {
            if (_currentWell == null) return;

            // Sync Wellbore Components
            _currentWell.WellboreComponents.Clear();
            foreach (var component in WellboreComponents)
            {
                _currentWell.WellboreComponents.Add(component);
            }

            // Sync Drill String Components
            _currentWell.DrillStringComponents.Clear();
            foreach (var component in DrillStringComponents)
            {
                _currentWell.DrillStringComponents.Add(component);
            }

            // Sync Survey Points
            _currentWell.SurveyPoints.Clear();
            foreach (var point in SurveyPoints)
            {
                _currentWell.SurveyPoints.Add(point);
            }

            // Sync Well Tests
            _currentWell.WellTests.Clear();
            foreach (var test in WellTests)
            {
                _currentWell.WellTests.Add(test);
            }

            // Sync Thermal Gradient Points
            _currentWell.ThermalGradientPoints.Clear();
            foreach (var point in ThermalGradientViewModel.ThermalGradientPoints)
            {
                _currentWell.ThermalGradientPoints.Add(point);
            }
        }
        
        private void ExportToCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Export wellbore components
                    var wellboreCsv = new StringBuilder();
                    wellboreCsv.AppendLine("Type,Top MD (ft),Bottom MD (ft),ID (in),OD (in),Volume (bbl)");
                    foreach (var component in WellboreComponents)
                    {
                        wellboreCsv.AppendLine($"{component.SectionType},{component.TopMD:F2},{component.BottomMD:F2},{component.ID:F3},{component.OD:F3},{component.Volume:F2}");
                    }
                    
                    // Export drill string components
                    var drillStringCsv = new StringBuilder();
                    drillStringCsv.AppendLine("Type,Length (ft),ID (in),OD (in),Volume (bbl)");
                    foreach (var component in DrillStringComponents)
                    {
                        drillStringCsv.AppendLine($"{component.ComponentType},{component.Length:F2},{component.ID:F3},{component.OD:F3},{component.Volume:F2}");
                    }
                    
                    // Combine and save
                    var combinedCsv = $"=== WELLBORE COMPONENTS ===\n{wellboreCsv}\n\n=== DRILL STRING COMPONENTS ===\n{drillStringCsv}";
                    File.WriteAllText(saveFileDialog.FileName, combinedCsv);
                    
                    ToastNotificationService.Instance.ShowSuccess("Data exported to CSV successfully.");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting to CSV: {ex.Message}");
            }
        }
        
        private void ShowVisualization(object? parameter)
        {
            try
            {
                // This would typically open a visualization window or tab
                ToastNotificationService.Instance.ShowInfo("Visualization feature will be implemented here.");
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error showing visualization: {ex.Message}");
            }
        }

        #region Export Methods

        private void ExportWellboreCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Wellbore_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportWellboreToCsv(WellboreComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Wellbore data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting wellbore data: {ex.Message}");
            }
        }

        private void ExportDrillStringCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"DrillString_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportDrillStringToCsv(DrillStringComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Drill string data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting drill string data: {ex.Message}");
            }
        }

        private void ExportSurveyCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Survey_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportSurveyToCsv(SurveyPoints, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Survey data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting survey data: {ex.Message}");
            }
        }

        private void ExportWellTestCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"WellTest_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportWellTestsToCsv(WellTests, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Well test data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting well test data: {ex.Message}");
            }
        }

        private void ExportAnnularDetailsCsv(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"Annular_Volume_Details_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportAnnularVolumeDetailsToCsv(AnnularVolumeDetails, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Annular volume details exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting annular volume details: {ex.Message}");
            }
        }

        private void ExportWellboreJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"Wellbore_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(WellboreComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Wellbore data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting wellbore data: {ex.Message}");
            }
        }

        private void ExportDrillStringJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"DrillString_Geometry_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(DrillStringComponents, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Drill string data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting drill string data: {ex.Message}");
            }
        }

        private void ExportSurveyJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"Survey_Data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(SurveyPoints, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Survey data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting survey data: {ex.Message}");
            }
        }

        private void ExportWellTestJson(object? parameter)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"WellTest_Data_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportService = new ExportService();
                    exportService.ExportToJson(WellTests, saveFileDialog.FileName);
                    ToastNotificationService.Instance.ShowSuccess($"Well test data exported to {Path.GetFileName(saveFileDialog.FileName)}");
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error exporting well test data: {ex.Message}");
            }
        }

        #endregion

        #region Import Methods

        private void ImportWellboreData(object? parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    Title = "Import Wellbore Data"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var importService = new WellboreImportService();
                    WellboreImportService.ImportResult result;

                    if (openFileDialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        result = importService.ImportFromExcel(openFileDialog.FileName);
                    }
                    else
                    {
                        result = importService.ImportFromCsv(openFileDialog.FileName);
                    }

                    if (result.Success)
                    {
                        // Clear existing components and add imported ones
                        WellboreComponents.Clear();
                        foreach (var component in result.WellboreComponents)
                        {
                            component.PropertyChanged += OnWellboreComponentChanged;
                            WellboreComponents.Add(component);
                        }

                        var message = $"Imported {result.ImportedCount} wellbore component(s)";
                        if (result.ErrorCount > 0)
                        {
                            message += $" with {result.ErrorCount} error(s)";
                        }
                        ToastNotificationService.Instance.ShowSuccess(message);

                        if (result.DetailedErrors.Count > 0)
                        {
                            var errorSummary = string.Join("\n", result.DetailedErrors.Take(5));
                            if (result.DetailedErrors.Count > 5)
                            {
                                errorSummary += $"\n... and {result.DetailedErrors.Count - 5} more errors";
                            }
                            ToastNotificationService.Instance.ShowWarning($"Import warnings:\n{errorSummary}");
                        }
                    }
                    else
                    {
                        ToastNotificationService.Instance.ShowError($"Import failed: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing wellbore data: {ex.Message}");
            }
        }

        private void ImportDrillStringData(object? parameter)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    Title = "Import Drill String Data"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var importService = new DrillStringImportService();
                    DrillStringImportService.ImportResult result;

                    if (openFileDialog.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        result = importService.ImportFromExcel(openFileDialog.FileName);
                    }
                    else
                    {
                        result = importService.ImportFromCsv(openFileDialog.FileName);
                    }

                    if (result.Success)
                    {
                        // Clear existing components and add imported ones
                        DrillStringComponents.Clear();
                        foreach (var component in result.DrillStringComponents)
                        {
                            component.PropertyChanged += OnDrillStringComponentChanged;
                            DrillStringComponents.Add(component);
                        }

                        var message = $"Imported {result.ImportedCount} drill string component(s)";
                        if (result.ErrorCount > 0)
                        {
                            message += $" with {result.ErrorCount} error(s)";
                        }
                        ToastNotificationService.Instance.ShowSuccess(message);

                        if (result.DetailedErrors.Count > 0)
                        {
                            var errorSummary = string.Join("\n", result.DetailedErrors.Take(5));
                            if (result.DetailedErrors.Count > 5)
                            {
                                errorSummary += $"\n... and {result.DetailedErrors.Count - 5} more errors";
                            }
                            ToastNotificationService.Instance.ShowWarning($"Import warnings:\n{errorSummary}");
                        }
                    }
                    else
                    {
                        ToastNotificationService.Instance.ShowError($"Import failed: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastNotificationService.Instance.ShowError($"Error importing drill string data: {ex.Message}");
            }
        }

        #endregion

        #region Survey Row Actions

        private void MoveSurveyPointUp(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                if (index > 0)
                {
                    SurveyPoints.Move(index, index - 1);
                    ToastNotificationService.Instance.ShowSuccess("Survey point moved up");
                }
            }
        }

        private bool CanMoveSurveyPointUp(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                return index > 0;
            }
            return false;
        }

        private void MoveSurveyPointDown(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                if (index >= 0 && index < SurveyPoints.Count - 1)
                {
                    SurveyPoints.Move(index, index + 1);
                    ToastNotificationService.Instance.ShowSuccess("Survey point moved down");
                }
            }
        }

        private bool CanMoveSurveyPointDown(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                var index = SurveyPoints.IndexOf(point);
                return index >= 0 && index < SurveyPoints.Count - 1;
            }
            return false;
        }

        private void DeleteSurveyPoint(object? parameter)
        {
            if (parameter is SurveyPoint point)
            {
                SurveyPoints.Remove(point);
                ToastNotificationService.Instance.ShowSuccess("Survey point deleted");
            }
        }

        private bool CanDeleteSurveyPoint(object? parameter)
        {
            return parameter is SurveyPoint;
        }

        #endregion


        // Calculated totals
        public double TotalWellboreVolume { get; private set; }
        public double TotalDrillStringVolume { get; private set; }
        public double TotalAnnularVolume { get; private set; }
        public double TotalCirculationVolume { get; private set; }
        public double TotalWellboreMD { get; private set; }
        public string ContinuityError { get; private set; } = string.Empty;

        // Validation error counts for tab indicators
        public int WellboreErrorCount => ValidateWellboreContinuity().Count + WellboreComponents.Count(c => c.HasErrors);
        public int DrillStringErrorCount => DrillStringComponents.Count(c => c.HasErrors);
        public int SurveyErrorCount => SurveyPoints.Count(p => p.HasErrors);
        public int WellTestErrorCount => WellTests.Count(t => t.HasErrors);

        // Auto-increment ID counters
        private int _nextWellboreId = 1;
        private int _nextDrillStringId = 1;
        private int _nextSurveyId = 1;
        private int _nextWellTestId = 1;

        // Drill String Force to Bottom
        private bool _forceDrillStringToBottom = false;
        public bool ForceDrillStringToBottom
        {
            get => _forceDrillStringToBottom;
            set
            {
                SetProperty(ref _forceDrillStringToBottom, value);
                if (value)
                {
                    CalculateDrillStringToBottom();
                }
                OnPropertyChanged(nameof(FeetMissing));
                OnPropertyChanged(nameof(DepthDifferential));
            }
        }

        /// <summary>
        /// Forces the drill string to bottom by extending the last component
        /// </summary>
        private void ExecuteForceToBottom()
        {
            CalculateDrillStringToBottom();
        }

        public double FeetMissing
        {
            get
            {
                if (TotalWellboreMD <= 0) return 0;
                double totalDrillStringLength = DrillStringComponents.Sum(c => c.Length);
                return Math.Max(0, TotalWellboreMD - totalDrillStringLength);
            }
        }

        public double DepthDifferential
        {
            get
            {
                double totalDrillStringLength = DrillStringComponents.Sum(c => c.Length);
                return TotalWellboreMD - totalDrillStringLength;
            }
        }

        /// <summary>
        /// Gets the total drill string length (sum of all component lengths)
        /// </summary>
        public double TotalDrillStringLength => DrillStringComponents.Sum(c => c.Length);

        /// <summary>
        /// Gets the bottom differential (Well_MD - TotalStringLength)
        /// Positive = string is short, Negative = string exceeds TD, Zero = on bottom
        /// </summary>
        public double BottomDifferential => DepthDifferential;

        /// <summary>
        /// Gets the depth differential status for color coding
        /// </summary>
        public string DepthDifferentialStatus
        {
            get
            {
                double diff = DepthDifferential;
                if (Math.Abs(diff) < 0.01) return "OnBottom"; // 0 ft
                if (diff > 0) return "Short"; // Positive - not reaching
                return "Overrun"; // Negative - exceeds TD
            }
        }

        /// <summary>
        /// Gets the color for depth differential indicator
        /// </summary>
        public System.Windows.Media.Brush DepthDifferentialColor
        {
            get
            {
                return DepthDifferentialStatus switch
                {
                    "OnBottom" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
                    "Short" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange),
                    "Overrun" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                };
            }
        }

        /// <summary>
        /// Checks if drill string exceeds well MD (should block save)
        /// </summary>
        public bool DrillStringExceedsMD => DepthDifferential < -0.01;

        /// <summary>
        /// Gets BitToBottom calculation when last component is Bit
        /// BitToBottom = FinalStringLength - Well_MD
        /// </summary>
        public double? BitToBottom
        {
            get
            {
                if (DrillStringComponents.Count == 0) return null;
                var lastComponent = DrillStringComponents.LastOrDefault();
                if (lastComponent?.ComponentType != ComponentType.Bit) return null;
                
                return TotalDrillStringLength - TotalWellboreMD;
            }
        }

        /// <summary>
        /// Gets suggested BHA components when last component is DrillPipe
        /// </summary>
        public List<ComponentType> SuggestedBHAComponents
        {
            get
            {
                if (DrillStringComponents.Count == 0) return new List<ComponentType>();
                var lastComponent = DrillStringComponents.LastOrDefault();
                if (lastComponent?.ComponentType != ComponentType.DrillPipe) return new List<ComponentType>();
                
                return new List<ComponentType>
                {
                    ComponentType.DC,        // Drill Collar
                    ComponentType.HWDP,      // Heavy Weight
                    ComponentType.Stabilizer, // Stabilizer
                    ComponentType.Bit        // Bit
                };
            }
        }

        private void CalculateDrillStringToBottom()
        {
            if (TotalWellboreMD <= 0) return;
            if (DrillStringComponents.Count == 0) return;

            // Get the LAST component in the drill string (bottom-most)
            var lastComponent = DrillStringComponents.LastOrDefault();
            if (lastComponent == null)
            {
                ToastNotificationService.Instance.ShowWarning("No drill string components found to adjust.");
                return;
            }

            double totalOtherLength = DrillStringComponents
                .Where(c => c != lastComponent)
                .Sum(c => c.Length);

            double delta = TotalWellboreMD - (totalOtherLength + lastComponent.Length);

            // If string is shorter than MD, extend last component
            if (delta > 0.01)
            {
                double oldLength = lastComponent.Length;
                double newLength = lastComponent.Length + delta;
                
                // Update the last component
                lastComponent.Length = newLength;
                
                // Highlight the adjusted field
                lastComponent.IsHighlighted = true;
                
                // Remove highlight after 2 seconds
                Task.Delay(2000).ContinueWith(_ => 
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        lastComponent.IsHighlighted = false;
                    });
                });
                
                // Show notification
                ToastNotificationService.Instance.ShowSuccess(
                    $"Drill String forced to bottom. Last component length adjusted from {oldLength:F2} ft to {newLength:F2} ft (+{delta:F2} ft).");
            }
            // If string exceeds MD, show error (but don't auto-adjust)
            else if (delta < -0.01)
            {
                ToastNotificationService.Instance.ShowError(
                    $"Drill string exceeds well depth by {Math.Abs(delta):F2} ft. Please shorten components or revise the drill string configuration.");
            }
        }

        public void RecalculateTotals()
        {
            TotalWellboreVolume = _geometryService.CalculateTotalWellboreVolume(WellboreComponents, "Imperial");
            TotalDrillStringVolume = _geometryService.CalculateTotalDrillStringVolume(DrillStringComponents, false, "Imperial");
            TotalAnnularVolume = _geometryService.CalculateTotalAnnularVolume(TotalWellboreVolume, TotalDrillStringVolume);
            TotalCirculationVolume = TotalAnnularVolume + TotalDrillStringVolume;
            TotalWellboreMD = WellboreComponents.Count > 0 ? WellboreComponents.Max(w => w.BottomMD) : 0;
            if (ForceDrillStringToBottom)
            {
                CalculateDrillStringToBottom();
            }
            // Update continuity error
            var continuityErrors = ValidateWellboreContinuity();
            ContinuityError = continuityErrors.FirstOrDefault() ?? string.Empty;
            // Notify UI
            OnPropertyChanged(nameof(ContinuityError));
            // Raise total property changes
            OnPropertyChanged(nameof(TotalWellboreVolume));
            OnPropertyChanged(nameof(TotalDrillStringVolume));
            OnPropertyChanged(nameof(TotalAnnularVolume));
            OnPropertyChanged(nameof(TotalCirculationVolume));
            OnPropertyChanged(nameof(TotalWellboreMD));
            UpdateAnnularVolumeDetails();
            
            // Update drill string depth properties
            OnPropertyChanged(nameof(TotalDrillStringLength));
            OnPropertyChanged(nameof(BottomDifferential));
            OnPropertyChanged(nameof(DepthDifferentialStatus));
            OnPropertyChanged(nameof(DepthDifferentialColor));
            OnPropertyChanged(nameof(DrillStringExceedsMD));
            OnPropertyChanged(nameof(BitToBottom));
            OnPropertyChanged(nameof(SuggestedBHAComponents));
            
            // Update validation error counts
            OnPropertyChanged(nameof(WellboreErrorCount));
            OnPropertyChanged(nameof(DrillStringErrorCount));
            OnPropertyChanged(nameof(SurveyErrorCount));
            OnPropertyChanged(nameof(WellTestErrorCount));
        }


        private void UpdateAnnularVolumeDetails()
        {
            AnnularVolumeDetails.Clear();
            
            // Use the new service method for detailed calculation
            var details = _geometryService.CalculateAnnularVolumeDetails(
                WellboreComponents, 
                DrillStringComponents, 
                "Imperial"); // Assuming Imperial for now, should be dynamic based on settings
                
            foreach (var detail in details)
            {
                AnnularVolumeDetails.Add(detail);
            }
        }


        public int GetNextWellboreId()
        {
            return _nextWellboreId++;
        }

        public int GetNextDrillStringId()
        {
            return _nextDrillStringId++;
        }

        public int GetNextSurveyId()
        {
            return _nextSurveyId++;
        }

        public int GetNextWellTestId()
        {
            return _nextWellTestId++;
        }

        // Helper methods to convert between string and enum
        public static WellboreSectionType StringToSectionType(string value)
        {
            return value switch
            {
                "Casing" => WellboreSectionType.Casing,
                "Liner" => WellboreSectionType.Liner,
                _ => WellboreSectionType.OpenHole
            };
        }

        public static ComponentType StringToComponentType(string value)
        {
            return value switch
            {
                "Drill Pipe" => ComponentType.DrillPipe,
                "HWDP" => ComponentType.HWDP,
                "Casing" => ComponentType.Casing,
                "Liner" => ComponentType.Liner,
                "Setting Tool" => ComponentType.SettingTool,
                "DC" => ComponentType.DC,
                "LWD" => ComponentType.LWD,
                "MWD" => ComponentType.MWD,
                "PWD" => ComponentType.PWD,
                "Motor" => ComponentType.Motor,
                "XO" => ComponentType.XO,
                "JAR" => ComponentType.Jar,
                "Accelerator" => ComponentType.Accelerator,
                "Stabilizer" => ComponentType.Stabilizer,
                "Near Bit" => ComponentType.NearBit,
                "Bit Sub" => ComponentType.BitSub,
                "Bit" => ComponentType.Bit,
                _ => ComponentType.DrillPipe
            };
        }

        public static string ComponentTypeToString(ComponentType type)
        {
            return type switch
            {
                ComponentType.DrillPipe => "Drill Pipe",
                ComponentType.HWDP => "HWDP",
                ComponentType.Casing => "Casing",
                ComponentType.Liner => "Liner",
                ComponentType.SettingTool => "Setting Tool",
                ComponentType.DC => "DC",
                ComponentType.LWD => "LWD",
                ComponentType.MWD => "MWD",
                ComponentType.PWD => "PWD",
                ComponentType.Motor => "Motor",
                ComponentType.XO => "XO",
                ComponentType.Jar => "JAR",
                ComponentType.Accelerator => "Accelerator",
                ComponentType.NearBit => "Near Bit",
                ComponentType.BitSub => "Bit Sub",
                ComponentType.Bit => "Bit",
                _ => type.ToString()
            };
        }

        public static WellTestType StringToWellTestType(string value)
        {
            return value switch
            {
                "Leak Off" => WellTestType.LeakOff,
                "Fracture gradient" => WellTestType.FractureGradient,
                "Pore pressure" => WellTestType.PorePressure,
                "Integrity" => WellTestType.FormationIntegrity,
                _ => WellTestType.LeakOff
            };
        }

        public static string WellTestTypeToString(WellTestType type)
        {
            return type switch
            {
                WellTestType.LeakOff => "Leak Off",
                WellTestType.FractureGradient => "Fracture gradient",
                WellTestType.PorePressure => "Pore pressure",
                WellTestType.FormationIntegrity => "Integrity",
                _ => type.ToString()
            };
        }
        #endregion

        #region Validation Methods

        /// <summary>
        /// BR-WG-002: Validates depth continuity between wellbore sections
        /// BR-WG-003: Validates that Top MD < Bottom MD for each section
        /// </summary>
        public List<string> ValidateWellboreContinuity()
        {
            var errors = new List<string>();
            if (WellboreComponents == null || WellboreComponents.Count == 0)
                return errors;

            var sorted = WellboreComponents.OrderBy(c => c.TopMD).ToList();
            
            // Check individual sections (BR-WG-003)
            foreach (var section in sorted)
            {
                if (section.TopMD >= section.BottomMD && section.BottomMD > 0)
                    errors.Add($"Section '{section.Name}': Top MD must be less than Bottom MD.");
            }

            // Check continuity (BR-WG-002)
            var continuityErrors = GetContinuityErrors();
            foreach (var (prev, curr) in continuityErrors)
            {
                errors.Add($"Continuity Error: Section '{curr.Name}' Top MD ({curr.TopMD:F2}) does not match Section '{prev.Name}' Bottom MD ({prev.BottomMD:F2}).");
            }

            return errors;
        }

        private List<(WellboreComponent Prev, WellboreComponent Curr)> GetContinuityErrors()
        {
            var errors = new List<(WellboreComponent, WellboreComponent)>();
            if (WellboreComponents == null || WellboreComponents.Count < 2)
                return errors;

            var sorted = WellboreComponents.OrderBy(c => c.TopMD).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var prev = sorted[i];
                var curr = sorted[i + 1];
                
                // Use a small tolerance for floating point comparison
                if (Math.Abs(prev.BottomMD - curr.TopMD) > 0.01)
                {
                    errors.Add((prev, curr));
                }
            }
            return errors;
        }

        public bool ShowContinuityErrorModal()
        {
            var errors = GetContinuityErrors();
            if (errors.Count > 0)
            {
                var (prev, curr) = errors.First();
                
                // Show the dialog
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new ContinuityErrorDialog(prev, curr);
                    if (dialog.ShowDialog() == true)
                    {
                        // If fixed, recalculate
                        RecalculateTotals();
                        return true;
                    }
                    return false;
                });
            }
            return true; // No errors
        }


        #endregion
    }
}
