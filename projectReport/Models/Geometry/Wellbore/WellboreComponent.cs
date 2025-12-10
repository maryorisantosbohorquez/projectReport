using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private double _washout;
        private ObservableCollection<string> _validationErrors = new ObservableCollection<string>();

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        public bool HasValidationError => ValidationErrors.Count > 0;

        public void AddValidationError(string error)
        {
            if (!ValidationErrors.Contains(error))
            {
                ValidationErrors.Add(error);
                OnPropertyChanged(nameof(HasValidationError));
            }
        }

        public void ClearValidationErrors()
        {
            ValidationErrors.Clear();
            OnPropertyChanged(nameof(HasValidationError));
        }

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
                if (Length <= 0)
                    return 0;

                // For OpenHole: Use OD with washout applied
                // For Casing/Liner: Use ID (annular volume)
                if (SectionType == WellboreSectionType.OpenHole)
                {
                    if (OD <= 0)
                        return 0;
                    
                    // Apply washout to OD: effective_OD = OD × (1 + washout/100)
                    double effectiveOD = OD * (1 + Washout / 100.0);
                    
                    // Volume = (π × r²) × Length, converted to barrels
                    // Formula: (OD² / 1029.4) × Length
                    return (effectiveOD * effectiveOD / 1029.4) * Length;
                }
                else
                {
                    // Casing/Liner: Use ID for annular volume
                    if (ID <= 0)
                        return 0;
                    
                    return (ID * ID / 1029.4) * Length;
                }
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

        // For OpenHole: OD is editable (hole diameter), ID is disabled (always 0)
        // For Casing/Liner: Both OD and ID are editable
        public bool IsODEnabled => true; // OD is always editable
        public bool IsIDEnabled => SectionType != WellboreSectionType.OpenHole; // ID disabled for OpenHole



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
            
            // OpenHole must have ID = 0.000 (read-only)
            if (SectionType == WellboreSectionType.OpenHole)
            {
                if (ID > 0.001)
                {
                    AddError(nameof(ID), $"OpenHole must have ID = 0.000 (no inner pipe). Current value: {ID:F3} in");
                }
                return;
            }
            
            // For Casing/Liner: ID cannot be 0
            if (ID <= 0.001)
            {
                AddError(nameof(ID), "ID cannot be 0.000 in pipe sections (Casing/Liner)");
                return;
            }
            
            // Rule: ID < OD (Internal Diameter Logic) - ID must always be smaller than OD
            if (OD > 0 && ID >= OD)
            {
                AddError(nameof(ID), "ID must always be smaller than OD");
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
                    OnPropertyChanged(nameof(IsODEnabled)); 
                    OnPropertyChanged(nameof(IsIDEnabled)); // Notify ID enabled state change
                    
                    // CORRECTED LOGIC: For OpenHole, ID = 0 (no pipe), OD = hole diameter
                    if (value == WellboreSectionType.OpenHole)
                    {
                        _id = 0.0; // OpenHole has no inner pipe, so ID = 0
                        OnPropertyChanged(nameof(ID));
                        ClearErrors(nameof(ID)); // Remove any existing ID errors
                        ValidateWashout(); // Validate washout when switching to OpenHole
                    }
                    
                    ValidateSectionType();
                    ValidateOD(); // Re-validate OD based on new section type
                    ValidateID(); // Re-validate ID based on new section type
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

        public double Washout
        {
            get => _washout;
            set
            {
                if (SetProperty(ref _washout, value))
                {
                    ValidateWashout();
                    OnPropertyChanged(nameof(Volume));
                    OnPropertyChanged(nameof(AnnularVolume));
                }
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
            
            // Rule: OD cannot be 0.0 (except OpenHole which uses OD as hole diameter)
            if (OD <= 0.001)
            {
                if (SectionType == WellboreSectionType.OpenHole)
                {
                    AddError(nameof(OD), "OD cannot be 0.000. For OpenHole, enter the hole diameter.");
                }
                else
                {
                    AddError(nameof(OD), "OD cannot be 0.000. Enter the outer diameter of the pipe.");
                }
                return;
            }
            
            // Rule: ID < OD (Internal Diameter Logic)
            if (SectionType != WellboreSectionType.OpenHole && ID > 0 && OD <= ID)
            {
                AddError(nameof(OD), "ID ≥ OD is not allowed. Fix diameters before continuing.");
                AddError(nameof(ID), "ID ≥ OD is not allowed. Fix diameters before continuing.");
            }
        }

        /// <summary>
        /// Validates washout for OpenHole sections
        /// Rule C3: Minimum washout >= 0.01%
        /// Rule C4: Washout is mandatory for OpenHole
        /// </summary>
        private void ValidateWashout()
        {
            ClearErrors(nameof(Washout));
            
            if (SectionType == WellboreSectionType.OpenHole)
            {
                // Washout is mandatory for OpenHole
                if (double.IsNaN(Washout) || Washout < 0)
                {
                    AddError(nameof(Washout), "Error C4: Washout is required for Open Hole volume calculation.");
                }
                else if (Washout > 50)
                {
                    AddError(nameof(Washout), "Error C4: Washout value exceeds reasonable range (0-50%). Typical values: 5-25%.");
                }
                else if (Washout < 0.01)
                {
                    AddError(nameof(Washout), "Validación C3: Mínimo W ≥ 0.01%. Washout de 0% en OpenHole es poco común. Valores típicos: 5-25%.");
                }
            }
        }

        /// <summary>
        /// Validates telescopic diameter rule: OD[n] < ID[n-1]
        /// Rule A2: Telescopic Diameter Progression
        /// This should be called from the ViewModel with the previous component
        /// </summary>
        public void ValidateTelescopicDiameter(WellboreComponent? previousComponent)
        {
            ClearErrors(nameof(OD));
            
            if (previousComponent == null) return; // First component, no telescoping check
            
            // Rule A2: OD[n] < ID[n-1] (Telescopic Diameter)
            if (OD >= previousComponent.ID && previousComponent.ID > 0.001)
            {
                AddError(nameof(OD), $"Error A2: OD ({OD:F3} in) must be smaller than previous section ID ({previousComponent.ID:F3} in). Telescopic progression required.");
            }
        }

        /// <summary>
        /// Validates casing depth progression: BottomMD[n] >= BottomMD[n-1] for casing/liner
        /// Rule D3: Casing Depth Progression
        /// </summary>
        public void ValidateCasingDepthProgression(WellboreComponent? previousComponent)
        {
            if (previousComponent == null) return;
            
            // Only applies to Casing and Liner sections
            if ((SectionType == WellboreSectionType.Casing || SectionType == WellboreSectionType.Liner) &&
                (previousComponent.SectionType == WellboreSectionType.Casing || previousComponent.SectionType == WellboreSectionType.Liner))
            {
                // Check for valid casing override: same TopMD, deeper or equal BottomMD
                bool isCasingOverride = Math.Abs(TopMD - previousComponent.TopMD) < 0.01 && BottomMD >= previousComponent.BottomMD;
                
                if (!isCasingOverride && BottomMD < previousComponent.BottomMD)
                {
                    AddError(nameof(BottomMD), $"Error D3: Bottom MD ({BottomMD:F2} ft) cannot be less than previous casing depth ({previousComponent.BottomMD:F2} ft).");
                }
            }
        }
    }
}
