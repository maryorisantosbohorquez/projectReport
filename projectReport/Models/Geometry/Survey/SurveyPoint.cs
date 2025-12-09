using ProjectReport.Models;

namespace ProjectReport.Models.Geometry.Survey
{
    public class SurveyPoint : BaseModel
    {
        private string _section = string.Empty;
        private double _md;
        private double _tvd;
        private double _holeAngle;
        private double _azimuth;
        private double _doglegSeverity;
        private double _buildRate;
        private double _turnRate;
        private double _northing;
        private double _easting;
        private double _verticalSection;

        public string Section
        {
            get => _section;
            set => SetProperty(ref _section, value);
        }

        public double MD
        {
            get => _md;
            set
            {
                if (SetProperty(ref _md, value))
                {
                    ValidateMD();
                }
            }
        }

        public double TVD
        {
            get => _tvd;
            set
            {
                if (SetProperty(ref _tvd, value))
                {
                    ValidateTVD();
                }
            }
        }

        public double HoleAngle
        {
            get => _holeAngle;
            set
            {
                if (SetProperty(ref _holeAngle, value))
                {
                    ValidateHoleAngle();
                }
            }
        }

        public double Azimuth
        {
            get => _azimuth;
            set
            {
                if (SetProperty(ref _azimuth, value))
                {
                    ValidateAzimuth();
                }
            }
        }

        public double DoglegSeverity
        {
            get => _doglegSeverity;
            set
            {
                SetProperty(ref _doglegSeverity, value);
                OnPropertyChanged(nameof(IsHighDogleg));
            }
        }

        public double BuildRate
        {
            get => _buildRate;
            set => SetProperty(ref _buildRate, value);
        }

        public double TurnRate
        {
            get => _turnRate;
            set => SetProperty(ref _turnRate, value);
        }

        public bool IsHighDogleg => DoglegSeverity > 3.0;

        public double Northing
        {
            get => _northing;
            set
            {
                SetProperty(ref _northing, value);
                OnPropertyChanged(nameof(VerticalSection));
            }
        }

        public double Easting
        {
            get => _easting;
            set
            {
                SetProperty(ref _easting, value);
                OnPropertyChanged(nameof(VerticalSection));
            }
        }

        public double VerticalSection
        {
            get => _verticalSection;
            set => SetProperty(ref _verticalSection, value);
        }

        #region Validation

        private void ValidateMD()
        {
            ClearErrors(nameof(MD));
            if (MD < TVD)
            {
                AddError(nameof(MD), "MD must be greater than or equal to TVD");
            }
        }

        private void ValidateTVD()
        {
            ClearErrors(nameof(TVD));
            if (TVD > MD)
            {
                AddError(nameof(TVD), "TVD cannot be greater than MD");
            }
            // Trigger MD validation as well since they are related
            ValidateMD();
        }

        private void ValidateHoleAngle()
        {
            ClearErrors(nameof(HoleAngle));
            if (HoleAngle > 93)
            {
                AddError(nameof(HoleAngle), "Hole Angle cannot be greater than 93°");
            }
            if (HoleAngle < 0)
            {
                AddError(nameof(HoleAngle), "Hole Angle cannot be negative");
            }
        }

        private void ValidateAzimuth()
        {
            ClearErrors(nameof(Azimuth));
            if (Azimuth > 360)
            {
                AddError(nameof(Azimuth), "Azimuth cannot be greater than 360°");
            }
            if (Azimuth < 0)
            {
                AddError(nameof(Azimuth), "Azimuth cannot be negative");
            }
        }

        #endregion

        public bool IsValid => !HasErrors;
    }
}
