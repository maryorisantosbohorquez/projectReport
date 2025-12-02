using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ProjectReport.Services;
using ProjectReport.Views;

using ProjectReport.Models;

namespace ProjectReport.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private GeometryView? _geometryView;
        private HomeView? _homeView;
        private WellDataView? _wellDataView;

        public Project CurrentProject { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _databaseService = new DatabaseService();

            // Initialize the current project
            CurrentProject = new Project
            {
                Name = "Y-23A",
                WellName = "Well-04"
            };

            // Set the data context
            DataContext = this;

            // Subscribe to navigation events
            NavigationService.Instance.NavigationRequested += OnNavigationRequested;

            // Setup status updates
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            UpdateStatus("Application initialized");
            
            // Start with Home view
            NavigateToHome();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            UpdateConnectionStatus();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_databaseService?.IsConnected ?? false)
                {
                    _databaseService.Disconnect();
                    UpdateStatus("Disconnected from database");
                }
                else
                {
                    UpdateStatus("Connecting to database...");
                    var connectionDialog = new ConnectionDialog();
                    if (connectionDialog.ShowDialog() == true)
                    {
                        // Use connection string provided by dialog for a persistent app-level connection
                        if (!string.IsNullOrWhiteSpace(connectionDialog.ConnectionString))
                        {
                            if (_databaseService != null)
                            {
                                if (_databaseService.Connect(connectionDialog.ConnectionString, out string? errorMessage))
                                {
                                    UpdateStatus("Connected to database successfully");
                                }
                                else
                                {
                                    string error = errorMessage ?? "Unknown error";
                                    UpdateStatus($"Failed to connect: {error}");
                                    MessageBox.Show($"Failed to connect: {error}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                UpdateStatus("Database service not available");
                                MessageBox.Show("Database service not available", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConnectionStatus()
        {
            bool isConnected = _databaseService?.IsConnected ?? false;
            if (isConnected)
            {
                ConnectionStatus.Text = "● Connected";
                ConnectionStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(39, 174, 96));
                ConnectButton.Content = "Disconnect";
                ConnectButton.Style = (Style)FindResource("DisconnectButton");
            }
            else
            {
                ConnectionStatus.Text = "● Disconnected";
                ConnectionStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(231, 76, 60));
                ConnectButton.Content = "Connect";
                ConnectButton.Style = (Style)FindResource("DefaultButton");
            }
        }

        #region Navigation

        private void OnNavigationRequested(object? sender, NavigationEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Target)
                {
                    case NavigationTarget.Home:
                        NavigateToHome();
                        break;
                    case NavigationTarget.WellData:
                        if (e.WellId.HasValue)
                            NavigateToWellData(e.WellId.Value);
                        break;
                    case NavigationTarget.Geometry:
                        if (e.WellId.HasValue)
                            NavigateToGeometry(e.WellId.Value);
                        break;
                }
            });
        }

        private void NavigateToHome()
        {
            // Save geometry data if coming from Geometry view
            SaveGeometryDataIfNeeded();

            if (_homeView == null)
            {
                _homeView = new HomeView();
                var viewModel = new ProjectReport.ViewModels.HomeViewModel(CurrentProject);
                _homeView.DataContext = viewModel;
            }

            ContentTitle.Text = "Home";
            ContentArea.Content = _homeView;
            UpdateStatus("Home View Loaded");
        }

        private void NavigateToWellData(int wellId)
        {
            // Save geometry data if coming from Geometry view
            SaveGeometryDataIfNeeded();

            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null)
            {
                UpdateStatus($"Well with ID {wellId} not found");
                return;
            }

            _wellDataView = new WellDataView();
            var viewModel = new ProjectReport.ViewModels.WellDataViewModel(CurrentProject);
            viewModel.LoadWell(well);
            _wellDataView.DataContext = viewModel;

            ContentTitle.Text = $"Well Data - {well.WellName}";
            ContentArea.Content = _wellDataView;
            UpdateStatus($"Well Data Module Loaded for {well.WellName}");
        }

        private void NavigateToGeometry(int wellId)
        {
            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null)
            {
                UpdateStatus($"Well with ID {wellId} not found");
                return;
            }

            if (_geometryView == null)
            {
                _geometryView = new GeometryView();
            }

            if (_geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
            {
                viewModel.LoadWell(well);
            }

            ContentTitle.Text = $"Geometry - {well.WellName}";
            ContentArea.Content = _geometryView;
            UpdateStatus($"Geometry Module Loaded for {well.WellName}");
        }

        #endregion

        /// <summary>
        /// Saves geometry data back to the Well object if currently viewing Geometry module
        /// </summary>
        private void SaveGeometryDataIfNeeded()
        {
            if (_geometryView != null && _geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
            {
                viewModel.SaveToWell();
            }
        }

        private GeometryView GetOrCreateGeometryView()
        {
            if (_geometryView == null)
            {
                _geometryView = new GeometryView();
            }
            return _geometryView;
        }

        private void WellboreGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Wellbore Geometry";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 0;
                }
                ContentArea.Content = view;
                UpdateStatus("Wellbore Geometry Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Wellbore Geometry Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrillStringGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Drill String Geometry";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 1;
                }
                ContentArea.Content = view;
                UpdateStatus("Drill String Geometry Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Drill String Geometry Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SurveyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Survey";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 2;
                }
                ContentArea.Content = view;
                UpdateStatus("Survey Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Survey Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WellTestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Well Test";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 4;
                }
                ContentArea.Content = view;
                UpdateStatus("Well Test Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Well Test Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SummaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Summary";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 5;
                }
                ContentArea.Content = view;
                UpdateStatus("Summary Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Summary Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThermalGradientButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ContentTitle.Text = "Thermal Gradient";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 3;
                }
                ContentArea.Content = view;
                UpdateStatus("Thermal Gradient Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Thermal Gradient Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeometryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle submenu visibility
                GeometrySubmenu.Visibility = GeometrySubmenu.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
                
                ContentTitle.Text = "Geometry";
                var view = GetOrCreateGeometryView();
                if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel viewModel)
                {
                    viewModel.SelectedTabIndex = 0;
                }
                ContentArea.Content = view;
                UpdateStatus("Geometry Module Loaded");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}");
                MessageBox.Show($"Failed to load Geometry Module: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHome();
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from navigation events
            NavigationService.Instance.NavigationRequested -= OnNavigationRequested;
            
            _databaseService?.Dispose();
            base.OnClosed(e);
        }
    }
}

