using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ProjectReport.Models;
using ProjectReport.Models.Geometry.Wellbore;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry
{
    public class WellboreGeometryViewModel : BaseViewModel
    {
        private readonly ObservableCollection<WellboreSection> _sections = new();
        private readonly ICollectionView _sectionsView;
        private double _totalWellDepth;
        private readonly ICommand _addSectionCommand;
        private readonly ICommand _removeSectionCommand;
        private readonly ICommand _moveUpCommand;
        private readonly ICommand _moveDownCommand;

        public WellboreGeometryViewModel()
        {
            _addSectionCommand = new RelayCommand(_ => AddSection());
            _removeSectionCommand = new RelayCommand(RemoveSection, CanRemoveSection);
            _moveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
            _moveDownCommand = new RelayCommand(MoveDown, CanMoveDown);
            
            _sectionsView = CollectionViewSource.GetDefaultView(_sections);
            _sectionsView.SortDescriptions.Add(new SortDescription("BottomMD", ListSortDirection.Ascending));
            _sections.CollectionChanged += (s, e) => 
            {
                UpdateWellDepth();
                ValidateContinuity();
            };
        }
        public double? Washout { get; set; }  // porcentaje, 


        public ICollectionView Sections => _sectionsView;

        public double TotalWellDepth
        {
            get => _totalWellDepth;
            private set => SetProperty(ref _totalWellDepth, value);
        }

        public ICommand AddSectionCommand => _addSectionCommand;
        public ICommand RemoveSectionCommand => _removeSectionCommand;
        public ICommand MoveUpCommand => _moveUpCommand;
        public ICommand MoveDownCommand => _moveDownCommand;

        private void AddSection()
        {
            var newSection = new WellboreSection
            {
                Id = _sections.Any() ? _sections.Max(s => s.Id) + 1 : 1,
                SectionType = WellboreSectionType.Casing,
                Name = $"Section {_sections.Count + 1}",
                TopMD = _sections.Any() ? _sections.Max(s => s.BottomMD) : 0,
                BottomMD = _sections.Any() ? _sections.Max(s => s.BottomMD) + 100 : 100,
                OD = _sections.LastOrDefault()?.OD ?? 8.5,
                ID = _sections.LastOrDefault()?.ID ?? 7.0
            };

            newSection.PropertyChanged += OnSectionPropertyChanged;
            _sections.Add(newSection);
            UpdateWellDepth();
        }

        private void RemoveSection(object? parameter)
        {
            if (parameter is WellboreSection section)
            {
                section.PropertyChanged -= OnSectionPropertyChanged;
                _sections.Remove(section);
                UpdateWellDepth();
            }
            else if (_sectionsView.CurrentItem is WellboreSection currentSection)
            {
                currentSection.PropertyChanged -= OnSectionPropertyChanged;
                _sections.Remove(currentSection);
                UpdateWellDepth();
            }
        }

        private bool CanRemoveSection(object? parameter)
        {
            return parameter is WellboreSection || _sectionsView.CurrentItem is WellboreSection;
        }

        private void MoveUp(object? parameter)
        {
            if (parameter is WellboreSection section)
            {
                var index = _sections.IndexOf(section);
                if (index > 0)
                {
                    _sections.Move(index, index - 1);
                    UpdateSectionDepths();
                }
            }
        }

        private bool CanMoveUp(object? parameter)
        {
            if (parameter is WellboreSection section)
            {
                return _sections.IndexOf(section) > 0;
            }
            return false;
        }

        private void MoveDown(object? parameter)
        {
            if (parameter is WellboreSection section)
            {
                var index = _sections.IndexOf(section);
                if (index < _sections.Count - 1)
                {
                    _sections.Move(index, index + 1);
                    UpdateSectionDepths();
                }
            }
        }

        private bool CanMoveDown(object? parameter)
        {
            if (parameter is WellboreSection section)
            {
                int index = _sections.IndexOf(section);
                return index >= 0 && index < _sections.Count - 1;
            }
            return false;
        }

        private string _continuityError = string.Empty;
        public string ContinuityError
        {
            get => _continuityError;
            private set => SetProperty(ref _continuityError, value);
        }

        private void OnSectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is WellboreSection section)
            {
                if (e.PropertyName == nameof(WellboreSection.BottomMD) || 
                    e.PropertyName == nameof(WellboreSection.TopMD) ||
                    e.PropertyName == nameof(WellboreSection.SectionType))
                {
                    UpdateWellDepth();
                    ValidateContinuity();
                }
            }
        }

        private void UpdateSectionDepths()
        {
            // Removed automatic TopMD calculation to allow user input and validation
            UpdateWellDepth();
            ValidateContinuity();
        }

        private void ValidateContinuity()
        {
            ContinuityError = string.Empty;
            var sortedSections = _sections.OrderBy(s => s.TopMD).ToList();

            for (int i = 1; i < sortedSections.Count; i++)
            {
                var currentSection = sortedSections[i];
                var previousSection = sortedSections[i - 1];

                if (Math.Abs(currentSection.TopMD - previousSection.BottomMD) > 0.001)
                {
                    ContinuityError = $"ERROR DE CONTINUIDAD: El Top MD de la Sección {currentSection.Id} ({currentSection.TopMD} ft) debe ser exactamente igual al Bottom MD de la Sección {previousSection.Id} ({previousSection.BottomMD} ft).";
                    return;
                }
            }
        }

        private void UpdateWellDepth()
        {
            TotalWellDepth = _sections.Any() ? _sections.Max(s => s.BottomMD) : 0;
            
            // Update commands
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
