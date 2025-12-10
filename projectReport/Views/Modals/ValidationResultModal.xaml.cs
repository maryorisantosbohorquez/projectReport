using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ProjectReport.Services;

namespace ProjectReport.Views.Modals
{
    public partial class ValidationResultModal : Window
    {
        public class ItemViewModel
        {
            public string Message { get; set; }
            public Brush SeverityColor { get; set; }
        }

        public class GroupViewModel
        {
            public string SectionName { get; set; } = string.Empty;
            public string Icon { get; set; } = "❌";
            public List<ItemViewModel> Items { get; set; } = new List<ItemViewModel>();
        }

        public List<GroupViewModel> GroupedItems { get; private set; }
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public bool CanContinue { get; private set; }

        public bool ContinueConfirmed { get; private set; } = false;

        public ValidationResultModal(GeometryValidationService.ValidationResult result)
        {
            InitializeComponent();
            
            ErrorCount = result.Items.Count(x => x.Severity == GeometryValidationService.ValidationSeverity.Error);
            WarningCount = result.Items.Count(x => x.Severity == GeometryValidationService.ValidationSeverity.Warning);
            
            // Allow continue ONLY if there are NO Critical Errors, but there ARE Warnings
            CanContinue = ErrorCount == 0 && WarningCount > 0;

            // Group logic
            GroupedItems = result.Items
                .GroupBy(e => new { e.ComponentId, e.ComponentName })
                .Select(g => 
                {
                    bool hasError = g.Any(x => x.Severity == GeometryValidationService.ValidationSeverity.Error);
                    string icon = hasError ? "❌" : "⚠️";
                    string sectionInfo = (g.Key.ComponentId == "-") ? "General" : $"Sección {g.Key.ComponentId} - {g.Key.ComponentName}";
                    
                    return new GroupViewModel
                    {
                        SectionName = sectionInfo,
                        Icon = icon,
                        Items = g.Select(x => new ItemViewModel 
                        { 
                            Message = x.Message,
                            SeverityColor = x.Severity == GeometryValidationService.ValidationSeverity.Error ? Brushes.Red : Brushes.Orange
                        }).ToList()
                    };
                })
                .ToList();
                
            DataContext = this;
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            ContinueConfirmed = true;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
