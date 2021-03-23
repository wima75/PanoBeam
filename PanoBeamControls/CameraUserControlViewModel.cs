using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PanoBeam.Events;
using PanoBeam.Events.Events;
using PanoBeamLib;

namespace PanoBeam.Controls
{
    public class CameraUserControlViewModel : ViewModelBase
    {
        internal Action StartAction;
        internal Action CancelAction;
        internal Action ContinueAction;

        private VideoDeviceCollection _videoDeviceCollection;
        private readonly VideoCapture _videoCapture;

        public Action<int, int> AddCropAdorner;

        private string _saveNextFrameAs;
        private bool _cropAdornerAdded;

        public CameraUserControlViewModel()
        {
            _videoDeviceCollection = new VideoDeviceCollection();
            _videoCapture = VideoCapture.Instance;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (_videoDeviceCollection.Count == 1)
            {
                Camera = _videoDeviceCollection[0].MonikerString;
            }
            else
            {
                Camera = Configuration.Configuration.Instance.Settings.Camera.MonikerString;
            }

            _controlPointsCountXList = new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            _controlPointsCountYList = new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            ControlPointsCountX = 6;
            ControlPointsCountY = 3;

            _patternSizes = new[] { 30, 40, 50, 60, 70, 80, 90, 100 };
            PatternSize = 50;
        }

        private ImageSource _imageSource;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public IntPtr ParentWindow { get; set; }

        public void SetClippingRectangle(Rect rectangle)
        {
            _videoCapture.ClippingRectangle = rectangle;
        }

        public string Camera
        {
            get => Configuration.Configuration.Instance.Settings.Camera.MonikerString;
            set
            {
                Disconnect();
                Configuration.Configuration.Instance.Settings.Camera.MonikerString = value;
                _videoCapture.SetCamera(Configuration.Configuration.Instance.Settings.Camera.MonikerString);
                OnPropertyChanged();
            }
        }

        public VideoDeviceCollection Cameras
        {
            get => _videoDeviceCollection;
            set
            {
                if (Equals(value, _videoDeviceCollection)) return;
                _videoDeviceCollection = value;
                OnPropertyChanged();
            }
        }

        #region Commands
        private bool _connectCanExecute = true;
        private ICommand _connectCommand;

        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new CommandHandler(Connect, param => _connectCanExecute));
            }
        }

        private bool _disconnectCanExecute;
        private ICommand _disconnectCommand;

        public ICommand DisconnectCommand
        {
            get
            {
                return _disconnectCommand ?? (_disconnectCommand = new CommandHandler(Disconnect, param => _disconnectCanExecute));
            }
        }

        private readonly bool _settingsCanExecute = true;
        private ICommand _settingsCommand;

        public ICommand SettingsCommand
        {
            get
            {
                return _settingsCommand ?? (_settingsCommand = new CommandHandler(Settings, param => _settingsCanExecute));
            }
        }

        private bool _startCanExecute = true;
        private ICommand _startCommand;

        public ICommand StartCommand
        {
            get
            {
                return _startCommand ?? (_startCommand = new CommandHandler(StartAction, param => _startCanExecute));
            }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new CommandHandler(CancelAction, param => true));
            }
        }

        private ICommand _continueCommand;
        public ICommand ContinueCommand
        {
            get
            {
                return _continueCommand ?? (_continueCommand = new CommandHandler(ContinueAction, param => true));
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

        public System.Drawing.Size PatternCount => new System.Drawing.Size(ControlPointsCountX, ControlPointsCountY);

        private bool _calibrationInProgress;
        public bool CalibrationInProgress
        {
            get => _calibrationInProgress;
            set
            {
                _calibrationInProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalibrationNotInProgress));
            }
        }

        public bool CalibrationNotInProgress => !_calibrationInProgress;

        private string _calibrationStepMessage;
        public string CalibrationStepMessage
        {
            get => _calibrationStepMessage;
            set
            {
                _calibrationStepMessage = value;
                OnPropertyChanged();
            }
        }

        private void Connect()
        {
            _connectCanExecute = false;
            //_videoCapture.FirstFrame += FirstFrame;
            _videoCapture.Frame += Frame;
            //_videoCapture.Threshold = Threshold;
            _videoCapture.Start();
            _disconnectCanExecute = true;
        }

        private void Disconnect()
        {
            _disconnectCanExecute = false;
            _videoCapture.Stop();
            _connectCanExecute = true;
        }

        private void Settings()
        {
            _videoCapture.ShowCameraSettings(ParentWindow);
        }

        //private void FirstFrame(BitmapSource bitmapSource, int width, int height)
        //{
        //    ImageSource = bitmapSource;
        //    AddCropAdorner(width, height);
        //}

        public void SaveFrame(string filename)
        {
            _saveNextFrameAs = filename;
        }

        private void Frame(Bitmap bitmap)
        {
            var bmp = (Bitmap)bitmap.Clone();
            if (_saveNextFrameAs != null)
            {
                var filename = _saveNextFrameAs;
                _saveNextFrameAs = null;
                bmp.Save(filename, ImageFormat.Png);
                bmp.Dispose();
            }
            else
            {
                if (!_cropAdornerAdded)
                {
                    _cropAdornerAdded = true;
                    AddCropAdorner(bmp.Width, bmp.Height);
                }

                ImageSource = GetBitmapSource(bmp);
            }
        }

        private static BitmapSource GetBitmapSource(Bitmap image)
        {
            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        private void SettingsChanged()
        {
            EventHelper.SendEvent<SettingsChanged, EventArgs>(null);
        }
    }
}