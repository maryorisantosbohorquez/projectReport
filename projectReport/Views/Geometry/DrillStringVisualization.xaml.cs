using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Views.Geometry
{
    public partial class DrillStringVisualization : UserControl
    {
        private const double CanvasWidth = 380;
        private const double MaxComponentWidth = 150;
        private const double DepthScale = 0.5; // pixels per foot
        
        public static readonly DependencyProperty DrillStringComponentsProperty =
            DependencyProperty.Register("DrillStringComponents", typeof(IEnumerable<DrillStringComponent>), 
                typeof(DrillStringVisualization), new PropertyMetadata(null, OnDrillStringComponentsChanged));

        public IEnumerable<DrillStringComponent> DrillStringComponents
        {
            get => (IEnumerable<DrillStringComponent>)GetValue(DrillStringComponentsProperty);
            set => SetValue(DrillStringComponentsProperty, value);
        }

        public DrillStringVisualization()
        {
            InitializeComponent();
        }

        private static void OnDrillStringComponentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrillStringVisualization visualization)
            {
                visualization.RenderDrillString();
            }
        }

        private void RenderDrillString()
        {
            VisualizationCanvas.Children.Clear();
            if (DrillStringComponents == null || !DrillStringComponents.Any())
            {
                TotalLengthText.Text = "Total Length: 0.00 ft";
                return;
            }

            double currentDepth = 0;
            double totalLength = DrillStringComponents.Sum(c => c.Length);
            TotalLengthText.Text = $"Total Length: {totalLength:F2} ft";

            double canvasHeight = totalLength * DepthScale + 100; // Add padding
            VisualizationCanvas.Height = canvasHeight;

            // Draw depth markers on the left
            for (double depth = 0; depth <= totalLength; depth += 100)
            {
                double y = depth * DepthScale;
                var depthText = new TextBlock
                {
                    Text = $"{depth:F0} ft",
                    Foreground = Brushes.Black,
                    FontSize = 10,
                    Margin = new Thickness(5, y - 8, 0, 0)
                };
                Canvas.SetLeft(depthText, 5);
                Canvas.SetTop(depthText, y);
                VisualizationCanvas.Children.Add(depthText);

                var line = new Line
                {
                    X1 = 60,
                    Y1 = y,
                    X2 = CanvasWidth,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection(new[] { 2.0, 2.0 })
                };
                VisualizationCanvas.Children.Add(line);
            }

            // Draw drill string components
            foreach (var component in DrillStringComponents)
            {
                double height = component.Length * DepthScale;
                double width = component.OD > 0 ? Math.Min(component.OD * 15, MaxComponentWidth) : 20;
                double left = (CanvasWidth - width) / 2;
                
                // Draw component
                var componentRect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = GetComponentBrush(component),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    ToolTip = GetComponentTooltip(component)
                };

                Canvas.SetLeft(componentRect, left);
                Canvas.SetTop(componentRect, currentDepth * DepthScale);
                VisualizationCanvas.Children.Add(componentRect);

                // Add component label if it's large enough
                if (height > 20)
                {
                    var label = new TextBlock
                    {
                        Text = $"{component.ComponentTypeString}\n{component.Length:F1} ft",
                        TextWrapping = TextWrapping.Wrap,
                        Width = width,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 9,
                        Foreground = Brushes.Black,
                        Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255))
                    };

                    Canvas.SetLeft(label, left);
                    Canvas.SetTop(label, (currentDepth * DepthScale) + (height / 2) - 10);
                    VisualizationCanvas.Children.Add(label);
                }

                currentDepth += component.Length;
            }
        }

        private static Brush GetComponentBrush(DrillStringComponent component)
        {
            return component.ComponentType switch
            {
                ComponentType.DrillPipe => new SolidColorBrush(Color.FromRgb(200, 200, 200)), // Light Gray
                ComponentType.HWDP => new SolidColorBrush(Color.FromRgb(169, 169, 169)),      // Dark Gray
                ComponentType.DC => new SolidColorBrush(Color.FromRgb(105, 105, 105)),        // Dim Gray
                ComponentType.Bit => new SolidColorBrush(Color.FromRgb(50, 50, 50)),          // Almost Black
                ComponentType.Motor => new SolidColorBrush(Color.FromRgb(100, 149, 237)),     // Cornflower Blue
                ComponentType.MWD => new SolidColorBrush(Color.FromRgb(255, 165, 0)),         // Orange
                ComponentType.LWD => new SolidColorBrush(Color.FromRgb(255, 140, 0)),         // Dark Orange
                ComponentType.Jar => new SolidColorBrush(Color.FromRgb(255, 69, 0)),          // Red Orange
                ComponentType.XO => new SolidColorBrush(Color.FromRgb(138, 43, 226)),         // Blue Violet
                _ => Brushes.LightBlue
            };
        }

        private static string GetComponentTooltip(DrillStringComponent component)
        {
            return $"Type: {component.ComponentTypeString}\n" +
                   $"Name: {component.Name}\n" +
                   $"Length: {component.Length:F2} ft\n" +
                   $"OD: {component.OD:F2} in\n" +
                   $"ID: {component.ID:F2} in\n" +
                   $"Weight: {component.WeightPerFoot:F2} lb/ft\n" +
                   $"Volume: {component.InternalVolume:F2} bbl";
        }
    }
}
