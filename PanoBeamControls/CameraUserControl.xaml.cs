using PanoBeam.Events;
using PanoBeam.Events.Events;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PanoBeam.Controls
{
    public delegate void CalibrationStartDelegateNeu(int patternSize, System.Drawing.Size patternCount);
    /// <summary>
    /// Interaction logic for CameraUserControl.xaml
    /// </summary>
    public partial class CameraUserControl
    {
        public event CalibrationStartDelegateNeu Start;
        public event EventHandler Cancel;
        public event EventHandler Continue;
        CroppingAdorner _clp;
        FrameworkElement _felCur;
        private int _imageWidth;
        private int _imageHeight;
        private readonly CameraUserControlViewModel _viewModel;

        public CameraUserControl()
        {
            InitializeComponent();
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            _viewModel = new CameraUserControlViewModel {
                ParentWindow = Process.GetCurrentProcess().MainWindowHandle,
                StartAction = RaiseStart,
                CancelAction = RaiseCancel,
                ContinueAction = RaiseContinue
            };
            _viewModel.AddCropAdorner += AddCropAdorner;
            DataContext = _viewModel;
        }

        public void SetInProgress(bool value)
        {
            _viewModel.CalibrationInProgress = value;
        }

        public void SetStepMessage(string message)
        {
            _viewModel.CalibrationStepMessage = message;
        }

        public Rect GetClippingRectangle()
        {
            if (_clp == null) return new Rect(0,0, _imageWidth, _imageHeight);
            return _clp.GetScaledClippingRectangle(_imageWidth, _imageHeight);
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            _viewModel.DisconnectCommand.Execute(null);
        }

        private void AddCropAdorner(int width, int height)
        {
            _imageWidth = width;
            _imageHeight = height;
            var thread = new Thread(() =>
            {
                do
                {
                    Dispatcher.Invoke(() =>
                    {
                        Image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        Image.Arrange(new Rect(0, 0, width, height));
                        Image.UpdateLayout();
                    });
                    Thread.Sleep(100);
                } while (Image.ActualWidth <= 0);
                Dispatcher.Invoke(() => { AddCropToElement(Image); });
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            thread.Start();
        }

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_felCur != null)
            {
                return;
            }
            var rcInterior = new Rect(
                0,
                0,
                fel.ActualWidth,
                fel.ActualHeight);
            var adornerLayer = AdornerLayer.GetAdornerLayer(fel);
            _clp = new CroppingAdorner(fel, rcInterior);
            _felCur = fel;
            var color = Colors.Black;
            color.A = 180;
            _clp.Fill = new SolidColorBrush(color);
            adornerLayer.Add(_clp);
            adornerLayer.UpdateLayout();

            var dx = 1d / _clp.ClippingRectangle.Width * _imageWidth;
            var dy = 1d / _clp.ClippingRectangle.Height * _imageHeight;
            _clp.SetClippingRectangle(new Rect(
                Configuration.Configuration.Instance.Settings.ClippingRectangle.X / dx,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Y / dy,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Width / dx,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Height / dy
                ));

            UpdateClippingRectangle();
            _clp.CropChanged += (sender, args) =>
            {
                UpdateClippingRectangle();
            };
            Image.SizeChanged += ImageSizeChanged;
        }

        private void ImageSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            UpdateClippingRectangle();
        }

        private void UpdateClippingRectangle()
        {
            var rect = _clp.GetScaledClippingRectangle(_imageWidth, _imageHeight);
            _viewModel.SetClippingRectangle(rect);
            EventHelper.SendEvent<SettingsChanged, EventArgs>(null);
        }

        public void SaveFrame(string filename)
        {
            _viewModel.SaveFrame(filename);
        }

        public void Refresh()
        {
            _viewModel.PatternSize = Configuration.Configuration.Instance.Settings.PatternSize;
            _viewModel.ControlPointsCountX = Configuration.Configuration.Instance.Settings.PatternCountX;
            _viewModel.ControlPointsCountY = Configuration.Configuration.Instance.Settings.PatternCountY;
        }

        private void RaiseStart()
        {
            Start?.Invoke(_viewModel.PatternSize, _viewModel.PatternCount);
        }

        private void RaiseCancel()
        {
            Cancel?.Invoke(null, null);
        }

        private void RaiseContinue()
        {
            Continue?.Invoke(null, null);
        }
    }
}
