﻿using Size = System.Drawing.Size;

namespace PanoBeam.Controls
{
    public delegate void CalibrationStartDelegate(int patternSize, Size patternCount, bool keepCorners);
    public partial class CalibrationUserControl
    {
        public event CalibrationStartDelegate Start;
        private readonly CalibrationUserControlViewModel _viewModel;

        public int PatternSize => _viewModel.PatternSize;

        public Size PatternCount => _viewModel.PatternCount;

        public void SetInProgress(bool value)
        {
            _viewModel.SetInProgress(value);
        }

        public CalibrationUserControl()
        {
            InitializeComponent();
            _viewModel = new CalibrationUserControlViewModel {StartAction = RaiseStart};
            DataContext = _viewModel;
        }

        public void Initialize()
        {
        }

        public void Refresh()
        {
            _viewModel.PatternSize = Configuration.Configuration.Instance.Settings.PatternSize;
            _viewModel.ControlPointsCountX = Configuration.Configuration.Instance.Settings.PatternCountX;
            _viewModel.ControlPointsCountY = Configuration.Configuration.Instance.Settings.PatternCountY;
            _viewModel.KeepCorners = Configuration.Configuration.Instance.Settings.KeepCorners;
            _viewModel.ControlPointsMode = Configuration.Configuration.Instance.Settings.ControlPointsMode;
            _viewModel.ShowWireframe = Configuration.Configuration.Instance.Settings.ShowWireframe;
            _viewModel.ControlPointsInsideOverlap = Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap;
            _viewModel.ImmediateWarp = Configuration.Configuration.Instance.Settings.ImmediateWarp;
        }

        private void RaiseStart()
        {
            Start?.Invoke(_viewModel.PatternSize, _viewModel.PatternCount, _viewModel.KeepCorners);
        }
    }
}
