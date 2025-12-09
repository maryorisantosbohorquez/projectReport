using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProjectReport.Models.Geometry.Wellbore
{
    // Using WellboreSectionType from WellboreSectionType.cs instead of SectionType

    public class WellboreComponent : BaseModel
    {
        private double _od;
        private double _id;
        private double _topMD;
        private double _bottomMD;
        private string _name = string.Empty;
        private WellboreSectionType _sectionType;
        public const double BBL_TO_CUBIC_FEET = 5.615;
        public const double CUBIC_FEET_TO_BBL = 1.0 / 5.615;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name 
        { 
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    ValidateName();
                }
            }
        }

        private double _cementVolume;
        private double _spacerVolume;
        private double _hydraulicRoughness = 0.0006;
        private string _material = string.Empty;
        private double _burstRating;
        private double _collapseRating;

        public string MechanicalState
        {
            get => string.Empty;
            set { }
        }

        public double CasingOD
        {
            get => OD; // Use the base class OD property
            set
            {
                OD = value; // Set the base class OD property
                OnPropertyChanged();
            }
        }

        public double CementVolume
        {
            get => _cementVolume;
            set { _cementVolume = value; OnPropertyChanged(); }
        }

        public double SpacerVolume
        {
            get => _spacerVolume;
            set { _spacerVolume = value; OnPropertyChanged(); }
        }

        public double Volume
        {
            get
            {
                // SRS Formula: Volume (bbl) = (ID² / 1029.4) × Length
                // Where ID = Inner Diameter (inches), Length = Bottom MD - Top MD (feet)
                if (ID <= 0 || Length <= 0)
                    return 0;

                return (ID * ID / 1029.4) * Length;
            }
        }

        public double Length => BottomMD - TopMD;

        [Range(0, double.MaxValue, ErrorMessage = "Top MD must be a positive number")]
        public double TopMD 
        { 
            get => _topMD;
            set
            {
                if (SetProperty(ref _topMD, value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Length));
                    ValidateTopMD();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private void ValidateTopMD()
        {
            ClearErrors(nameof(TopMD));
            
            if (TopMD < 0)
            {
                AddError(nameof(TopMD), "Top MD cannot be negative");
            }

            if (BottomMD > 0 && BottomMD <= TopMD)
            {
                AddError(nameof(TopMD), "Top MD must be less than Bottom MD");
            }
        }

        [Range(0.1, double.MaxValue, ErrorMessage = "Bottom MD must be greater than 0")]
        public double BottomMD 
        { 
            get => _bottomMD;
            set
            {
                if (SetProperty(ref _bottomMD, value))
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Length));
                    ValidateBottomMD();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private void ValidateBottomMD()
        {
            ClearErrors(nameof(BottomMD));
            
            if (BottomMD <= 0)
            {
                AddError(nameof(BottomMD), "Bottom MD must be greater than 0");
            }

            if (BottomMD <= TopMD)
            {
                AddError(nameof(BottomMD), "Bottom MD must be greater than Top MD");
            }
        }

        public double OD 
        { 
            get => _od;
            set
            {
                if (SetProperty(ref _od, value))
                {
                    ValidateOD();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        // BR-WG-001: OD should be disabled for "Open Hole" section type
        public bool IsODEnabled => SectionType != WellboreSectionType.OpenHole;



        public double ID 
        { 
            get => _id;
            set
            {
                if (SetProperty(ref _id, value))
                {
                    OnPropertyChanged();
                    ValidateID();
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        private void ValidateID()
        {
            ClearErrors(nameof(ID));
            
            if (ID < 0)
            {
                AddError(nameof(ID), "ID cannot be negative");
            }
            else if (ID >= OD)
            {
                AddError(nameof(ID), "ID must be less than OD");
            }
        }

        public double HydraulicRoughness
        {
            get => _hydraulicRoughness;
            set { _hydraulicRoughness = value; OnPropertyChanged(); }
        }

        public string Material
        {
            get => _material;
            set { _material = value; OnPropertyChanged(); }
        }

        public double BurstRating
        {
            get => _burstRating;
            set { _burstRating = value; OnPropertyChanged(); }
        }

        public double CollapseRating
        {
            get => _collapseRating;
            set { _collapseRating = value; OnPropertyChanged(); }
        }

        public WellboreSectionType SectionType 
        { 
            get => _sectionType;
            set
            {
                if (SetProperty(ref _sectionType, value))
                {
                    OnPropertyChanged(nameof(SectionType));
                    OnPropertyChanged(nameof(IsODEnabled)); // Notify OD enabled state change
                    
                    // BR-WG-001: Clear OD value and remove validation when switching to Open Hole
                    if (value == WellboreSectionType.OpenHole)
                    {
                        _od = 0;
                        OnPropertyChanged(nameof(OD));
                        ClearErrors(nameof(OD)); // Remove any existing OD errors
                    }
                    
                    ValidateSectionType();
                    ValidateOD(); // Re-validate OD based on new section type
                    OnPropertyChanged(nameof(Volume));
                    
                    // Update roughness based on section type
                    UpdateHydraulicRoughness();
                }
            }
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                AddError(nameof(Name), "Name is required");
            }
            else if (Name.Length > 100)
            {
                AddError(nameof(Name), "Name cannot exceed 100 characters");
            }
        }

        private void ValidateSectionType()
        {
            ClearErrors(nameof(SectionType));
            
            if (!Enum.IsDefined(typeof(WellboreSectionType), SectionType))
            {
                AddError(nameof(SectionType), "Invalid section type");
            }
        }

        public double AnnularVolume
        {
            get
            {
                if (ID > 0 && OD > 0 && Length > 0)
                    return ((ID * ID) - (OD * OD)) * Length / 1029.4;

                return 0;
            }
        }

        private void UpdateHydraulicRoughness()
        {
            HydraulicRoughness = SectionType switch
            {
                WellboreSectionType.OpenHole => 0.006,
                _ => 0.0006
            };
        }

        private void CalculateCementVolume()
        {
            if (AnnularVolume > 0 && CementVolume == 0)
                CementVolume = AnnularVolume;
        }

        public bool OverlapsWith(WellboreComponent other)
        {
            if (other == null) return false;
            return !(BottomMD <= other.TopMD || TopMD >= other.BottomMD);
        }

        public double GapWith(WellboreComponent other)
        {
            if (other == null) return 0;
            if (BottomMD <= other.TopMD) return other.TopMD - BottomMD;
            if (TopMD >= other.BottomMD) return TopMD - other.BottomMD;
            return 0;
        }

        /// <summary>
        /// Gets the first validation error message for this component
        /// </summary>
        public string ValidationMessage
        {
            get
            {
                var errors = GetErrors(null);
                if (errors != null)
                {
                    var errorList = errors.Cast<string>().ToList();
                    return errorList.Count > 0 ? errorList[0] : string.Empty;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets whether this component is valid (has no validation errors)
        /// </summary>
        public bool IsValid => !HasErrors;

        private void ValidateOD()
        {
            ClearErrors(nameof(OD));
            
            // BR-WG-001: Skip OD validation for Open Hole
            if (SectionType == WellboreSectionType.OpenHole)
            {
                return;
            }
            
            if (OD <= 0)
            {
                AddError(nameof(OD), "OD must be greater than 0");
            }
            else if (OD <= ID)
            {
                AddError(nameof(OD), "OD must be greater than ID");
            }
        }
    }
}
