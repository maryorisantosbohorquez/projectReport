using System;
using System.Windows;
using System.Windows.Input;
using ProjectReport.Models.Geometry.DrillString;
using ProjectReport.ViewModels;

namespace ProjectReport.ViewModels.Geometry.Config
{
    public class ToolJointConfigViewModel : BaseViewModel
    {
        public ToolJointConfig Model { get; }

        private double _tjOD;
        private double _tjID;
        private double _tjLength;
        private double _weight;
        private double _tjIDLength;

        public double TJ_OD
        {
            get => Model.TJ_OD;
            set
            {
                if (SetProperty(ref _tjOD, value))
                {
                    Model.TJ_OD = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TJ_ID
        {
            get => Model.TJ_ID;
            set
            {
                if (SetProperty(ref _tjID, value))
                {
                    Model.TJ_ID = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TJ_Length
        {
            get => Model.TJ_Length;
            set
            {
                if (SetProperty(ref _tjLength, value))
                {
                    Model.TJ_Length = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Weight
        {
            get => Model.Weight;
            set
            {
                if (SetProperty(ref _weight, value))
                {
                    Model.Weight = value;
                    OnPropertyChanged();
                }
            }
        }

        public double TJ_ID_Length
        {
            get => Model.TJ_ID_Length;
            set
            {
                if (SetProperty(ref _tjIDLength, value))
                {
                    Model.TJ_ID_Length = value;
                    OnPropertyChanged();
                }
            }
        }

        private ComponentType _componentType;
        public ComponentType ComponentType
        {
            get => _componentType;
            set
            {
                if (SetProperty(ref _componentType, value))
                {
                    OnPropertyChanged(nameof(ShowToolIDLength));
                }
            }
        }

        public bool ShowToolIDLength => ComponentType != ComponentType.DC;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool>? RequestClose;

        public ToolJointConfigViewModel(ToolJointConfig model, ComponentType componentType = ComponentType.DrillPipe)
        {
            Model = model;
            _componentType = componentType;
            _tjOD = model.TJ_OD;
            _tjID = model.TJ_ID;
            _tjLength = model.TJ_Length;
            _weight = model.Weight;
            _tjIDLength = model.TJ_ID_Length;
            
            // Notify that ShowToolIDLength is initialized
            OnPropertyChanged(nameof(ShowToolIDLength));

            SaveCommand = new RelayCommand(_ =>
            {
                if (TJ_ID >= TJ_OD)
                {
                    MessageBox.Show("Tool Joint ID must be less than Tool Joint OD", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                RequestClose?.Invoke(true);
            });

            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
        }
    }
}

