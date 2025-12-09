using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry
{
    public partial class VisualizationWindow : Window
    {
        private readonly IEnumerable<WellboreComponent> _wellboreComponents;
        private readonly IEnumerable<DrillStringComponent> _drillStringComponents;

        public VisualizationWindow(IEnumerable<WellboreComponent> wellboreComponents, IEnumerable<DrillStringComponent> drillStringComponents, string initialView = "Wellbore")
        {
            InitializeComponent();
            _wellboreComponents = wellboreComponents;
            _drillStringComponents = drillStringComponents;

            if (initialView == "DrillString")
            {
                VisualizationTypeComboBox.SelectedIndex = 1;
            }
            else
            {
                VisualizationTypeComboBox.SelectedIndex = 0;
            }
            
            UpdateVisualization();
        }

        private void VisualizationTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateVisualization();
        }

        private void UpdateVisualization()
        {
            if (VisualizationContent == null) return;

            if (VisualizationTypeComboBox.SelectedIndex == 0)
            {
                VisualizationContent.Content = new WellboreVisualization { WellboreSections = _wellboreComponents };
            }
            else
            {
                VisualizationContent.Content = new DrillStringVisualization { DrillStringComponents = _drillStringComponents };
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
