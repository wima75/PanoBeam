using System;
using System.Windows.Input;
using PanoBeam.Events;
using PanoBeam.Events.Events;
using Size = System.Drawing.Size;
using PanoBeam.Configuration;

namespace PanoBeam.Controls
{
    public class CalibrationUserControlViewModel : ViewModelBase
    {
        internal Action StartAction;

        public CalibrationUserControlViewModel()
        {
            _controlPointsCountXList = new[] {3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20};
            _controlPointsCountYList = new[] {2, 3, 4, 5, 6, 7, 8, 9, 10};

            ControlPointsCountX = 6;
            ControlPointsCountY = 3;

            _patternSizes = new[] {30, 40, 50, 60, 70, 80, 90, 100};
            PatternSize = 50;
        }

        public void SetInProgress(bool value)
        {
            _startCanExecute = !value;
        }

        #region Commands
        private bool _startCanExecute = true;
        private ICommand _startCommand;

        public ICommand StartCommand
        {
            get
            {
                return _startCommand ?? (_startCommand = new CommandHandler(StartAction, param => _startCanExecute));
            }
        }

        #endregion

        private int[] _controlPointsCountXList;
        public int[] ControlPointsCountXList
        {
            get => _controlPointsCountXList;
            set
            {
                _controlPointsCountXList = value;
                OnPropertyChanged();
            }
        }

        private int[] _controlPointsCountYList;
        public int[] ControlPointsCountYList
        {
            get => _controlPointsCountYList;
            set
            {
                _controlPointsCountYList = value;
                OnPropertyChanged();
            }
        }

        public int ControlPointsCountX
        {
            get => Configuration.Configuration.Instance.Settings.PatternCountX;
            set
            {
                Configuration.Configuration.Instance.Settings.PatternCountX = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public int ControlPointsCountY
        {
            get => Configuration.Configuration.Instance.Settings.PatternCountY;
            set
            {
                Configuration.Configuration.Instance.Settings.PatternCountY = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        private int[] _patternSizes;

        public int[] PatternSizes
        {
            get => _patternSizes;
            set
            {
                _patternSizes = value;
                OnPropertyChanged();
            }
        }

        public int PatternSize
        {
            get => Configuration.Configuration.Instance.Settings.PatternSize;
            set
            {
                Configuration.Configuration.Instance.Settings.PatternSize = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public ControlPointsMode ControlPointsMode
        {
            get => Configuration.Configuration.Instance.Settings.ControlPointsMode;
            set
            {
                Configuration.Configuration.Instance.Settings.ControlPointsMode = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public Size PatternCount => new Size(ControlPointsCountX, ControlPointsCountY);

        public bool ShowWireframe
        {
            get => Configuration.Configuration.Instance.Settings.ShowWireframe;
            set
            {
                Configuration.Configuration.Instance.Settings.ShowWireframe = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public bool ControlPointsInsideOverlap
        {
            get => Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap;
            set
            {
                Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public bool KeepCorners
        {
            get => Configuration.Configuration.Instance.Settings.KeepCorners;
            set
            {
                Configuration.Configuration.Instance.Settings.KeepCorners = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        public bool ImmediateWarp
        {
            get => Configuration.Configuration.Instance.Settings.ImmediateWarp;
            set
            {
                Configuration.Configuration.Instance.Settings.ImmediateWarp = value;
                OnPropertyChanged();
                SettingsChanged();
            }
        }

        private void SettingsChanged()
        {
            EventHelper.SendEvent<SettingsChanged, EventArgs>(null);
        }
    }
}