using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using ProjectReport.Services;
using ProjectReport.Views;
using ProjectReport.Models;
using ProjectReport.Views.Geometry;
using ProjectReport.Views.Inventory;

namespace ProjectReport.Views
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;

        private GeometryView? _geometryView;
        private HomeView? _homeView;
        private WellDataView? _wellDataView;
        private Views.WellDashboardView? _wellDashboardView;

        // NUEVO: INVENTORY VIEW
        private InventoryView? _inventoryView;

        public Project CurrentProject { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Init services
            _databaseService = new DatabaseService();

            // Default project object (mock/demo)
            CurrentProject = new Project
            {
                Name = "Y-23A",
                WellName = "Well-04"
            };

            DataContext = this;

            // NavigationService event hookup
            NavigationService.Instance.NavigationRequested += OnNavigationRequested;

            // Clock for footer
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            // Start at Home
            NavigateToHome();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        //==========================================
        // NAVIGATION SYSTEM
        //==========================================

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
                    case NavigationTarget.WellDashboard:
                        if (e.WellId.HasValue)
                            NavigateToWellDashboard(e.WellId.Value);
                        break;
                }
            });
        }

        private void NavigateToHome()
        {
            SaveGeometryDataIfNeeded();

            if (_homeView == null)
            {
                _homeView = new HomeView();
                var vm = new ProjectReport.ViewModels.HomeViewModel(CurrentProject);
                _homeView.DataContext = vm;
            }

            ContentTitle.Text = "Home";
            ContentArea.Content = _homeView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }

        private void NavigateToWellData(int wellId)
        {
            SaveGeometryDataIfNeeded();

            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null)
                return;

            _wellDataView = new WellDataView();
            var vm = new ProjectReport.ViewModels.WellDataViewModel(CurrentProject);
            vm.LoadWell(well);
            _wellDataView.DataContext = vm;

            ContentTitle.Text = $"Well Data - {well.WellName}";
            ContentArea.Content = _wellDataView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }

        private void NavigateToGeometry(int wellId)
        {
            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null)
                return;

            if (_geometryView == null)
                _geometryView = new GeometryView();

            if (_geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.LoadWell(well);

            ContentTitle.Text = $"Geometry - {well.WellName}";
            ContentArea.Content = _geometryView;

            GeometrySubmenu.Visibility = Visibility.Visible;
        }

<<<<<<< HEAD
        private void NavigateToWellDashboard(int wellId)
        {
            var well = CurrentProject.Wells.FirstOrDefault(w => w.Id == wellId);
            if (well == null)
            {
                UpdateStatus($"Well with ID {wellId} not found");
                return;
            }

            _wellDashboardView = new Views.WellDashboardView();
            var vm = new ProjectReport.ViewModels.WellDashboardViewModel(CurrentProject);
            vm.LoadWell(well);
            _wellDashboardView.DataContext = vm;

            ContentTitle.Text = $"Dashboard - {well.WellName}";
            ContentArea.Content = _wellDashboardView;
            UpdateStatus($"Well Dashboard Loaded for {well.WellName}");
        }

        #endregion
=======
        //==========================================
        //  INVENTORY NAVIGATION (BOTÓN NUEVO)
        //==========================================
>>>>>>> 663420e4de4a240da11e20e9186403ee096c7896

        private void InventoryButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGeometryDataIfNeeded();

            if (_inventoryView == null)
            {
                _inventoryView = new InventoryView();
            }

            ContentTitle.Text = "Inventory";
            ContentArea.Content = _inventoryView;

            GeometrySubmenu.Visibility = Visibility.Collapsed;
            GeometrySubmenu.Height = 0;
        }

        //==========================================
        // GEOMETRY SUB-PAGES
        //==========================================

        private GeometryView GetOrCreateGeometryView()
        {
            if (_geometryView == null)
                _geometryView = new GeometryView();

            return _geometryView;
        }

        private void WellboreGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Wellbore Geometry";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 0;

            ContentArea.Content = view;
        }

        private void DrillStringGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Drill String Geometry";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 1;

            ContentArea.Content = view;
        }

        private void SurveyButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Survey";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 2;

            ContentArea.Content = view;
        }

        private void ThermalGradientButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Thermal Gradient";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 3;

            ContentArea.Content = view;
        }

        private void WellTestButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Well Test";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 4;

            ContentArea.Content = view;
        }

        private void SummaryButton_Click(object sender, RoutedEventArgs e)
        {
            ContentTitle.Text = "Summary";
            var view = GetOrCreateGeometryView();

            if (view.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
                vm.SelectedTabIndex = 5;

            ContentArea.Content = view;
        }

        //==========================================
        // UTILS
        //==========================================

        private void SaveGeometryDataIfNeeded()
        {
            if (_geometryView != null &&
                _geometryView.DataContext is ProjectReport.ViewModels.Geometry.GeometryViewModel vm)
            {
                vm.SaveToWell();
            }
        }

        private void GeometryButton_Click(object sender, RoutedEventArgs e)
        {
            GeometrySubmenu.Visibility =
                GeometrySubmenu.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            ContentTitle.Text = "Geometry";
            ContentArea.Content = GetOrCreateGeometryView();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToHome();
        }

        //==========================================
        // WINDOW BUTTONS + DRAG
        //==========================================

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); }
                catch { }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            NavigationService.Instance.NavigationRequested -= OnNavigationRequested;
            _databaseService?.Dispose();

            base.OnClosed(e);
        }
    }
}
