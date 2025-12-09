using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.Services;
using ProjectReport.ViewModels.Geometry;

namespace ProjectReport.Views.Geometry
{
    public partial class WellboreGeometryView : UserControl
    {
        private readonly GeometryViewModel _viewModel;
        private Point _startPoint;
        private int _draggedItemIndex = -1;
        private bool _isDragging = false;

        public WellboreGeometryView()
        {
            InitializeComponent();
            
            // Initialize services
            var geometryService = new GeometryCalculationService();
            var dataService = new DataPersistenceService();
            
            // Initialize ViewModel with required services
            _viewModel = new GeometryViewModel(geometryService, dataService, new ThermalGradientService());
            DataContext = _viewModel;

            // Set up initial state
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Add initial section if collection is empty
            if (!_viewModel.WellboreComponents.Any())
            {
                AddWellboreSection();
            }
        }

        private void AddWellboreSection_Click(object sender, RoutedEventArgs e)
        {
            AddWellboreSection();
        }

        private void AddWellboreSection()
        {
            double lastBottomMd = _viewModel.WellboreComponents.Any() 
                ? _viewModel.WellboreComponents.Max(s => s.BottomMD) 
                : 0;

            // BR-WG-005: Use auto-increment ID
            int newId = _viewModel.GetNextWellboreId();

            var newSection = new WellboreComponent
            {
                Id = newId,
                Name = $"Section {newId}", // Use ID in name by default
                SectionType = WellboreSectionType.Casing,
                TopMD = lastBottomMd,
                BottomMD = lastBottomMd + 100, // Default 100ft section
                ID = 8.5,
                OD = 9.625
            };
            
            // Calculate initial volume using the service directly
            var geometryService = new GeometryCalculationService();
            geometryService.CalculateWellboreComponentVolume(newSection, "Imperial");
            
            _viewModel.WellboreComponents.Add(newSection);
            
            // Select and scroll to the new section
            WellboreDataGrid.SelectedItem = newSection;
            WellboreDataGrid.ScrollIntoView(newSection);
            
            // Update section ordering (only depths, not IDs)
            UpdateSectionOrder();
        }

        private void DeleteSelectedSection_Click(object sender, RoutedEventArgs e)
        {
            if (WellboreDataGrid.SelectedItem is WellboreComponent selectedSection)
            {
                if (_viewModel.WellboreComponents.Count > 1)
                {
                    _viewModel.WellboreComponents.Remove(selectedSection);
                    UpdateSectionOrder();
                }
                else
                {
                    MessageBox.Show("At least one section must remain.", "Cannot Delete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void UpdateSectionOrder()
        {
            // Update Top/Bottom MDs based on the current order
            // BR-WG-005: Do NOT renumber IDs. IDs must persist.
            
            double currentDepth = 0;
            for (int i = 0; i < _viewModel.WellboreComponents.Count; i++)
            {
                var section = _viewModel.WellboreComponents[i];
                // section.Id = i + 1; // REMOVED: IDs should not be re-assigned
                
                // Only update TopMD if it's not the first section
                if (i > 0)
                {
                    section.TopMD = currentDepth;
                }
                
                // Update BottomMD based on TopMD and Length
                if (section.BottomMD <= section.TopMD)
                {
                    section.BottomMD = section.TopMD + 100; // Default 100ft length
                }
                
                currentDepth = section.BottomMD;
                
                // Recalculate volume using the service directly
                var geometryService = new GeometryCalculationService();
                geometryService.CalculateWellboreComponentVolume(section, "Imperial");
            }
            
            // Refresh the display
            _viewModel.RecalculateTotals();
        }

        #region Drag and Drop Implementation

        private void Row_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Store the starting point of the drag operation
            _startPoint = e.GetPosition(null);
            _isDragging = false;
            
            // Get the row that was clicked
            if (sender is DataGridRow row && row.DataContext is WellboreComponent)
            {
                _draggedItemIndex = WellboreDataGrid.ItemContainerGenerator.IndexFromContainer(row);
                row.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Row_MouseMove(object sender, MouseEventArgs e)
        {
            // If the mouse is not captured, we're not dragging
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItemIndex == -1)
                return;

            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            // Only start dragging if the mouse has moved a minimum distance
            if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragging = true;
                
                // Find the row being dragged
                var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
                if (row != null)
                {
                    // Start the drag operation
                    DataObject dragData = new DataObject("WellboreSection", row.DataContext);
                    DragDrop.DoDragDrop(row, dragData, DragDropEffects.Move);
                }
                
                // Release mouse capture
                if (row != null)
                {
                    row.ReleaseMouseCapture();
                }
            }
        }

        private void Row_Drop(object sender, DragEventArgs e)
        {
            // Get the target row
            var targetRow = sender as DataGridRow;
            if (targetRow == null || !e.Data.GetDataPresent("WellboreSection"))
                return;

            // Get the source and target items
            var sourceItem = e.Data.GetData("WellboreSection") as WellboreComponent;
            var targetItem = targetRow.Item as WellboreComponent;
            
            if (sourceItem == null || targetItem == null || sourceItem == targetItem)
                return;

            // Get the indices
            int sourceIndex = _viewModel.WellboreComponents.IndexOf(sourceItem);
            int targetIndex = _viewModel.WellboreComponents.IndexOf(targetItem);

            // Move the item in the collection
            if (sourceIndex != -1 && targetIndex != -1)
            {
                _viewModel.WellboreComponents.Move(sourceIndex, targetIndex);
                UpdateSectionOrder();
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null)
                return null;
                
            // Get the parent of the control
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            
            // If we've reached the top of the tree, return null
            if (parentObject == null)
                return null;
                
            // If the parent is of the requested type, return it
            if (parentObject is T parent)
                return parent;
                
            // Otherwise continue up the tree
            return FindVisualParent<T>(parentObject);
        }

        #endregion

        #region DataGrid Event Handlers

        private void WellboreDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Delete key to remove selected section
            if (e.Key == Key.Delete && WellboreDataGrid.SelectedItem != null)
            {
                DeleteSelectedSection_Click(sender, e);
                e.Handled = true;
            }
        }

        private void WellboreDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Prevent editing of certain columns based on section type
            if (e.Column.Header?.ToString() == "ID (in)" && e.Row.Item is WellboreComponent component)
            {
                if (component.SectionType == WellboreSectionType.OpenHole)
                {
                    e.Cancel = true;
                }
            }
        }

        private void WellboreDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is WellboreComponent component)
            {
                // Validate Bottom MD is greater than Top MD
                if (e.Column.Header.ToString() == "Bottom MD (ft)")
                {
                    if (component.BottomMD <= component.TopMD)
                    {
                        // Validation is handled by the model's ValidateBottomMD method
                        // which sets the errors that the DataGrid displays
                        e.Cancel = true;
                        return;
                    }
                    
                    // If this is not the last section, update the next section's Top MD
                    int currentIndex = _viewModel.WellboreComponents.IndexOf(component);
                    if (currentIndex < _viewModel.WellboreComponents.Count - 1)
                    {
                        var nextSection = _viewModel.WellboreComponents[currentIndex + 1];
                        nextSection.TopMD = component.BottomMD;
                        
                        // Recalculate volume for the next section using the service directly
                        var geometryService = new GeometryCalculationService();
                        geometryService.CalculateWellboreComponentVolume(nextSection, "Imperial");
                    }
                }
                
                // Recalculate volume for the current section using the service directly
                var geometryService2 = new GeometryCalculationService();
                geometryService2.CalculateWellboreComponentVolume(component, "Imperial");
                
                // Update totals
                _viewModel.RecalculateTotals();
                
                // Clear any previous validation message
                // Model handles this automatically via validation logic
            }
        }

        #endregion
    }
}

