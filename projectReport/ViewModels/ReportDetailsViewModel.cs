using System;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectReport.Models;

namespace ProjectReport.ViewModels
{
    public class ReportDetailsViewModel : BaseViewModel
    {
        private readonly Well _well;

        public ReportDetailsViewModel(Well well)
        {
            _well = well ?? throw new ArgumentNullException(nameof(well));

            WellSections = new ObservableCollection<string> { "Original", "Sidetrack 1", "Sidetrack 2" };
            Activities = new ObservableCollection<string> { "Drilling", "Circulating", "Tripping", "Logging" };

            // Default date/time
            ReportDate = DateTime.Now.Date;
            ReportTime = DateTime.Now.ToString("HH:mm");

            // Inherit from last report if exists
            if (_well.LastReport != null)
            {
                var last = _well.LastReport;
                PresentActivity = last.PresentActivity;
                PrimaryFluidSet = last.PrimaryFluidSet;
                OtherActiveFluids = last.OtherActiveFluids;
                WellSection = last.WellSection;

                // Copy personnel lists
                OperatorReps = new ObservableCollection<string>(last.OperatorReps);
                ContractorReps = new ObservableCollection<string>(last.ContractorReps);
                BaroidReps = new ObservableCollection<string>(last.BaroidReps);
                InheritedFields = true;
            }
            else
            {
                OperatorReps = new ObservableCollection<string>();
                ContractorReps = new ObservableCollection<string>();
                BaroidReps = new ObservableCollection<string>();
                InheritedFields = false;
            }
            Validate();
        }

        public Well ParentWell => _well;

        /// <summary>
        /// When Geometry step saves a draft, this flag is set so callers know the report was persisted as draft.
        /// </summary>
        public bool SavedAsDraft { get; set; }

        public string IntervalNumber { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string ReportTime { get; set; } = string.Empty; // HH:mm
        public double? MD { get; set; }
        public double? TVD { get; set; }
        public ObservableCollection<string> WellSections { get; }
        public string WellSection { get; set; } = string.Empty;
        public double? MaxBHT { get; set; }
        public ObservableCollection<string> Activities { get; }
        public string PresentActivity { get; set; } = string.Empty;
        public string PrimaryFluidSet { get; set; } = string.Empty;
        public string OtherActiveFluids { get; set; } = string.Empty;
        public bool OperationalIssues { get; set; }

        public ObservableCollection<string> OperatorReps { get; set; }
        public ObservableCollection<string> ContractorReps { get; set; }
        public ObservableCollection<string> BaroidReps { get; set; }

        public bool InheritedFields { get; private set; }

        // Basic validation
        public bool IsValid { get; private set; }
        public string? ValidationMessage { get; private set; }

        public void Validate()
        {
            ValidationMessage = null;
            if (string.IsNullOrWhiteSpace(IntervalNumber))
            {
                IsValid = false;
                ValidationMessage = "Interval # is required.";
                return;
            }

            if (MD == null || MD <= 0)
            {
                IsValid = false;
                ValidationMessage = "Report MD must be a positive number.";
                return;
            }

            if (TVD == null || TVD <= 0)
            {
                IsValid = false;
                ValidationMessage = "Report TVD must be a positive number.";
                return;
            }

            if (string.IsNullOrWhiteSpace(WellSection))
            {
                IsValid = false;
                ValidationMessage = "Well Section is required.";
                return;
            }

            // Logical check: TVD cannot exceed MD
            if (MD.HasValue && TVD.HasValue && TVD > MD)
            {
                IsValid = false;
                ValidationMessage = "TVD cannot exceed MD.";
                return;
            }

            IsValid = true;
            ValidationMessage = null;
        }

        public void StartFresh()
        {
            PresentActivity = string.Empty;
            PrimaryFluidSet = string.Empty;
            OtherActiveFluids = string.Empty;
            WellSection = string.Empty;
            OperatorReps.Clear();
            ContractorReps.Clear();
            BaroidReps.Clear();
            InheritedFields = false;
        }

        public Report BuildReport()
        {
            var dateTime = ReportDate;
            if (TimeSpan.TryParse(ReportTime, out var t))
            {
                dateTime = dateTime.Add(t);
            }

            return new Report
            {
                IntervalNumber = IntervalNumber,
                ReportDateTime = dateTime,
                MD = MD,
                TVD = TVD,
                WellSection = WellSection,
                MaxBHT = MaxBHT,
                PresentActivity = PresentActivity,
                PrimaryFluidSet = PrimaryFluidSet,
                OtherActiveFluids = OtherActiveFluids,
                OperationalIssues = OperationalIssues,
                OperatorReps = new ObservableCollection<string>(OperatorReps),
                ContractorReps = new ObservableCollection<string>(ContractorReps),
                BaroidReps = new ObservableCollection<string>(BaroidReps),
                CreatedDate = DateTime.Now
            };
        }
    }
}
