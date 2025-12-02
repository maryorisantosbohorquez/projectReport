using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectReport.Models.Geometry.Wellbore;

namespace ProjectReport.Views.Geometry
{
    public partial class WellboreVisualization : UserControl
    {
        private const double CanvasWidth = 380;
        private const double MaxSectionWidth = 200;
        private const double SectionSpacing = 10;
        private const double DepthScale = 0.5; // pixels per foot
        
        public static readonly DependencyProperty WellboreSectionsProperty =
            DependencyProperty.Register("WellboreSections", typeof(IEnumerable<WellboreComponent>), 
                typeof(WellboreVisualization), new PropertyMetadata(null, OnWellboreSectionsChanged));

        public IEnumerable<WellboreComponent> WellboreSections
        {
            get => (IEnumerable<WellboreComponent>)GetValue(WellboreSectionsProperty);
            set => SetValue(WellboreSectionsProperty, value);
        }

        public WellboreVisualization()
        {
            InitializeComponent();
        }

        private static void OnWellboreSectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WellboreVisualization visualization)
            {
                visualization.RenderWellbore();
            }
        }

        private void RenderWellbore()
        {
            VisualizationCanvas.Children.Clear();
            if (WellboreSections == null || !WellboreSections.Any())
                return;

            double currentY = 0;
            double maxDepth = WellboreSections.Max(ws => ws.BottomMD);
            double canvasHeight = maxDepth * DepthScale + 100; // Add some padding
            VisualizationCanvas.Height = canvasHeight;

            // Draw depth markers on the left
            for (double depth = 0; depth <= maxDepth; depth += 100)
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

            // Draw wellbore sections
            foreach (var section in WellboreSections.OrderBy(ws => ws.TopMD))
            {
                double height = (section.BottomMD - section.TopMD) * DepthScale;
                double width = section.OD > 0 ? Math.Min(section.OD * 20, MaxSectionWidth) : 100;
                double left = (CanvasWidth - width) / 2;
                
                // Draw casing/liner/open hole
                var sectionRect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = GetSectionBrush(section),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    ToolTip = GetSectionTooltip(section)
                };

                Canvas.SetLeft(sectionRect, left);
                Canvas.SetTop(sectionRect, section.TopMD * DepthScale);
                VisualizationCanvas.Children.Add(sectionRect);

                // Add section label
                var label = new TextBlock
                {
                    Text = $"{section.Name} ({section.SectionType})\nOD: {section.OD:F2}\" ID: {section.ID:F2}\"\n{section.TopMD:F1}-{section.BottomMD:F1} ft",
                    TextWrapping = TextWrapping.Wrap,
                    Width = width,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 10,
                    Foreground = Brushes.Black,
                    Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255))
                };

                Canvas.SetLeft(label, left);
                Canvas.SetTop(label, (section.TopMD * DepthScale) + 5);
                VisualizationCanvas.Children.Add(label);

                currentY += height + SectionSpacing;
            }
        }

        private static Brush GetSectionBrush(WellboreComponent section)
        {
            return section.SectionType switch
            {
                WellboreSectionType.Casing => new SolidColorBrush(Color.FromArgb(64, 0, 120, 215)), // Blue
                WellboreSectionType.Liner => new SolidColorBrush(Color.FromArgb(64, 0, 153, 51)),   // Green
                WellboreSectionType.OpenHole => Brushes.Transparent,
                _ => Brushes.LightGray
            };
        }

        private static string GetSectionTooltip(WellboreComponent section)
        {
            return $"Name: {section.Name}\n" +
                   $"Type: {section.SectionType}\n" +
                   $"OD: {section.OD:F2} in\n" +
                   $"ID: {section.ID:F2} in\n" +
                   $"Top MD: {section.TopMD:F2} ft\n" +
                   $"Bottom MD: {section.BottomMD:F2} ft\n" +
                   $"Length: {section.BottomMD - section.TopMD:F2} ft\n" +
                   $"Volume: {section.Volume:F2} bbl";
        }
    }
}
