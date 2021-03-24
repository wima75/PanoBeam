using System;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;
using PanoBeam.Configuration;
using PanoBeam.Controls;
using PanoBeamLib;
using PanoBeam.Events;
using PanoBeam.Events.Events;
using System.Linq;
using PanoBeam.Mapper;
using System.Collections.Generic;
using System.Threading;

namespace PanoBeam
{
    public class ViewModel : ViewModelBase
    {
        private readonly ScreenView _screenView;
        private readonly MainWindow _mainWindow;
        private readonly PanoScreen _screen;

        public CameraUserControl CameraUserControl { get; }
        public CalibrationUserControl CalibrationUserControl { get; }
        public BlendingUserControl BlendingUserControl { get; }
        public TestImagesUserControl TestImagesUserControl { get; }
        private string _configFilename;
        private Point _mousePosition;
        private List<CalibrationStep> _calibrationSteps;
        private int _currentCalibrationStep;

        public ViewModel(ScreenView screen, MosaicInfo mosaicInfo, MainWindow mainWindow)
        {
            _screenView = screen;
            _mainWindow = mainWindow;
            CameraUserControl = new CameraUserControl();
            CalibrationUserControl = new CalibrationUserControl();
            BlendingUserControl = new BlendingUserControl();
            TestImagesUserControl = new TestImagesUserControl();
            CameraUserControl.Start += CameraUserControlOnStart;
            CameraUserControl.Cancel += CameraUserControlOnCancel;
            CameraUserControl.Continue += CameraUserControlOnContinue;
            CalibrationUserControl.Start += CalibrationUserControlOnStart;
            TestImagesUserControl.ShowImage += TestImagesUserControlOnShowImage;

            _screen = new PanoScreen
            {
                Resolution = _screenView.Resolution,
                Overlap = _screenView.Overlap,
                SaveCursorPosition = () => { _mousePosition = Win32.GetMousePosition(); },
                RestoreCursorPosition = () =>
                {
                    if (_mousePosition != null) Win32.SetCursorPos(_mousePosition.X, _mousePosition.Y);
                }
            };
            _screen.AddProjectors(mosaicInfo.DisplayId0, mosaicInfo.DisplayId1);
            CalibrationUserControl.Initialize();
            BlendingUserControl.Initialize(_screen.Projectors);
            _screen.CalculationProgress += ScreenOnCalculationProgress;
        }

        private void ScreenOnCalculationProgress(float progress)
        {
            _mainWindow.ReportProgress(progress);
        }

        public void Initialize()
        {
            _screenView.Initialize(_screen);
            CameraUserControl.Refresh();
            CalibrationUserControl.Refresh();
            BlendingUserControl.Refresh();

            EventHelper.SubscribeEvent<SettingsChanged, EventArgs>(OnSettingsChanged);
        }

        private void OnSettingsChanged(EventArgs obj)
        {
            _screenView.UpdateWarpControl();
            _screenView.Refresh(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
            SaveSettings();
        }

        public void CloseScreen()
        {
            _screenView.Close();
        }

        public bool IsScreenVisible
        {
            get => _screenView.IsVisible;
            set
            {
                if (_screenView.IsVisible)
                {
                    _screenView.Hide();
                }
                else
                {
                    _screenView.Show();
                    _mainWindow.Activate();
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScreenButtonToolTip));
            }
        }

        public string ScreenButtonToolTip => IsScreenVisible ? "Screen-Fenster ausblenden" : "Screen-Fenster anzeigen";

        private void TestImagesUserControlOnShowImage(BitmapImage image)
        {
            _screenView.ShowImage(image);
        }

        private void CameraUserControlOnStart(int patternSize, Size patternCount)
        {
            _currentCalibrationStep = 0;
            CameraUserControl.SetInProgress(true);
            CameraUserControl.SetStepMessage("Bitte auf Weiter klicken, sobald beide Beamer eine weisse Fläche anzeigen.");
            _calibrationSteps = new[] {
                new CalibrationStep { CalibrationSteps = new [] { CalibrationSteps.White, CalibrationSteps.White }, Filename = Path.Combine(Helpers.TempDir, "capture_white.png"), Message = "Bitte auf Weiter klicken, sobald beide Beamer eine weisse Fläche anzeigen." },
                new CalibrationStep { CalibrationSteps = new [] { CalibrationSteps.White, CalibrationSteps.Black }, Filename = Path.Combine(Helpers.TempDir, "capture_white0.png"), Message = "Bitte auf Weiter klicken, sobald der linke Beamer eine weisse Fläche zeigt." },
                new CalibrationStep { CalibrationSteps = new [] { CalibrationSteps.Pattern, CalibrationSteps.Black}, Filename = Path.Combine(Helpers.TempDir, "capture_pattern0.png"), Message = "Bitte auf Weiter klicken, sobald der linke Beamer ein Muster zeigt." },
                new CalibrationStep { CalibrationSteps = new [] { CalibrationSteps.Black, CalibrationSteps.White}, Filename = Path.Combine(Helpers.TempDir, "capture_white1.png"), Message = "Bitte auf Weiter klicken, sobald der rechte Beamer eine weisse Fläche zeigt." },
                new CalibrationStep { CalibrationSteps = new [] { CalibrationSteps.Black, CalibrationSteps.Pattern}, Filename = Path.Combine(Helpers.TempDir, "capture_pattern1.png"), Message = "Bitte auf Weiter klicken, sobald der rechte Beamer ein Muster zeigt." }
            }.ToList();
            /*_screen.CalibrationDone = () =>
            {
                CalibrationUserControl.SetInProgress(false);
                _screenView.Refresh(ControlPointsMode.None, false);
                _screen.Warp();
                _mainWindow.CalibrationDone();
                EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);
            };*/
            //_screen.AwaitCalculationsReady = _mainWindow.AwaitCalculationsReady;
            var rect = CameraUserControl.GetClippingRectangle();
            _screen.ClippingRectangle = rect.GetRectangle();
            EventHelper.SendEvent<CalibrationStarted, EventArgs>(null);
            _screen.SetPattern(patternSize, patternCount, false, false);
            _screen.Calibrate(true);
        }

        private void CameraUserControlOnCancel(object sender, EventArgs e)
        {
            CameraUserControl.SetInProgress(false);
            CameraUserControl.SetStepMessage("");
        }

        private void CameraUserControlOnContinue(object sender, EventArgs e)
        {
            var calibrationStep = _calibrationSteps[_currentCalibrationStep];
            CameraUserControl.SaveFrame(calibrationStep.Filename);
            _currentCalibrationStep++;

            if (_currentCalibrationStep >= _calibrationSteps.Count)
            {
                _mainWindow.InitializeCalculationProgress();
                var thread = new Thread(Calculate)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Normal
                };
                thread.Start();

                /*_screenView.Refresh(ControlPointsMode.None, false);
                _screen.Warp();
                _mainWindow.CalibrationDone();
                EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);*/
            }
            else
            {
                calibrationStep = _calibrationSteps[_currentCalibrationStep];
                CameraUserControl.SetStepMessage(calibrationStep.Message);
                _screen.ShowCalibrationStep(calibrationStep);
            }
        }

        private void Calculate()
        {
            _screen.CalibrationEnd();
            _screen.Detect();
            _screenView.Refresh(ControlPointsMode.None, false);
            _mainWindow.CalibrationDone();
            _screen.Warp();
            EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);
        }

        //private void SaveImage()
        //{
        //    //int count = 0;
        //    VideoCapture.Instance.SaveFrame = bitmap =>
        //    {
        //        //count++;
        //        //if (count > 2)
        //        //{
        //            VideoCapture.Instance.SaveFrame = null;
        //            bitmap.Save(Path.Combine(Helpers.TempDir, "capture_white.png"), ImageFormat.Png);
        //            bitmap.Dispose();
        //        //}
        //    };
        //}

        private void CalibrationUserControlOnStart(int patternSize, Size patternCount, bool keepCorners)
        {
            EventHelper.SendEvent<CalibrationStarted, EventArgs>(null);
            CalibrationUserControl.SetInProgress(true);
            _screen.CalibrationDone = () =>
            {
                CalibrationUserControl.SetInProgress(false);
                _screenView.Refresh(ControlPointsMode.None, false);
                _screen.Warp();
                _mainWindow.CalibrationDone();
                EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);
            };
            _screen.CalibrationError = (message) =>
            {
                EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);
                CalibrationUserControl.SetInProgress(false);
                _mainWindow.CalibrationError(message);
            };
            _screen.CalibrationCanceled = () =>
            {
                EventHelper.SendEvent<CalibrationFinished, EventArgs>(null);
                CalibrationUserControl.SetInProgress(false);
            };
            _screen.SetPattern(patternSize, patternCount, false, false);
            // TODO Marco: Kamera oder File
            if(Helpers.CameraCalibration)
            {
                _screen.AwaitProjectorsReady = _mainWindow.AwaitProjectorsReady;
            }
            else
            {
                _screen.AwaitProjectorsReady = AwaitProjectorsReadyAuto;
            }
            
            //_screen.AwaitCalculationsReady = _mainWindow.AwaitCalculationsReady;
            var rect = CameraUserControl.GetClippingRectangle();
            _screen.ClippingRectangle = rect.GetRectangle();
            
            if (_screen.ControlPointsAdjusted)
            {
                _screen.ShowImage = ShowImage;
                _screen.Calibrate(false);
            }
            else
            {
                _screen.Calibrate(true);
            }
        }

        private void ShowImage(string file)
        {
            _screenView.ShowImage(file);
        }

        private void AwaitProjectorsReadyAuto(Action continueAction, Action calibrationCanceled, CalibrationSteps[] calibrationSteps)
        {
            continueAction();
        }
        
        #region Commands
        private ICommand _warpCommand;
        public ICommand WarpCommand
        {
            get
            {
                return _warpCommand ?? (_warpCommand = new CommandHandler(Warp, param => true));
            }
        }

        private ICommand _unWarpCommand;
        public ICommand UnWarpCommand
        {
            get
            {
                return _unWarpCommand ?? (_unWarpCommand = new CommandHandler(UnWarp, param => true));
            }
        }

        private ICommand _blendCommand;
        public ICommand BlendCommand
        {
            get
            {
                return _blendCommand ?? (_blendCommand = new CommandHandler(Blend, param => true));
            }
        }

        private ICommand _unBlendCommand;
        public ICommand UnBlendCommand
        {
            get
            {
                return _unBlendCommand ?? (_unBlendCommand = new CommandHandler(UnBlend, param => true));
            }
        }

        private ICommand _loadCommand;
        public ICommand LoadCommand
        {
            get
            {
                return _loadCommand ?? (_loadCommand = new CommandHandler(Load, param => true));
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand = new CommandHandler(Save, param => true));
            }
        }

        #endregion

        private void Warp()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.Warp();
            Mouse.OverrideCursor = null;
        }

        private void UnWarp()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.UnWarp();
            Mouse.OverrideCursor = null;
        }

        private void Blend()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.Blend();
            Mouse.OverrideCursor = null;
        }

        private void UnBlend()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.UnBlend();
            Mouse.OverrideCursor = null;
        }

        private string GetProgramDataDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "alphasoft marco wittwer", "PanoBeam");
        }

        private string GetDefaultDataDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void Load()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "PanoBeam Config (*.config)|*.config",
                InitialDirectory = GetDefaultDataDirectory()
            };
            if (ofd.ShowDialog() == true)
            {
                _configFilename = ofd.FileName;
            }
            else
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            var xmlSerializer = new XmlSerializer(typeof(Configuration.Configuration));
            Configuration.Configuration config;
            using (var reader = new XmlTextReader(_configFilename))
            {
                config = (Configuration.Configuration)xmlSerializer.Deserialize(reader);
            }
            Configuration.Configuration.Instance.UpdateConfig(config);
            _screen.Update(config.Settings.PatternSize, new Size(config.Settings.PatternCountX, config.Settings.PatternCountY), config.Settings.KeepCorners, config.Settings.ControlPointsInsideOverlap);
            _screen.UpdateProjectorsFromConfig(ProjectorMapper.MapProjectorsData(Configuration.Configuration.Instance.Projectors));
            CameraUserControl.Refresh();
            CalibrationUserControl.Refresh();
            BlendingUserControl.Refresh();
            _screenView.Refresh(config.Settings.ControlPointsMode, config.Settings.ShowWireframe);

            Mouse.OverrideCursor = null;
        }

        public void LoadSettings()
        {
            var filename = Path.Combine(GetProgramDataDirectory(), "PanoBeamSettings.config");
            var xmlSerializer = new XmlSerializer(typeof(Settings));
            Settings settings;
            using (var reader = new XmlTextReader(filename))
            {
                settings = (Settings)xmlSerializer.Deserialize(reader);
            }
            Configuration.Configuration.Instance.Settings.UpdateSettings(settings);
            _screen.Update(settings.PatternSize, new Size(settings.PatternCountX, settings.PatternCountY), settings.KeepCorners, settings.ControlPointsInsideOverlap);
            CameraUserControl.Refresh();
            CalibrationUserControl.Refresh();
            _screenView.Refresh(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_configFilename))
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "PanoBeam Config (*.config)|*.config",
                    InitialDirectory = GetDefaultDataDirectory()
                };
                if (sfd.ShowDialog() == true)
                {
                    _configFilename = sfd.FileName;
                }
                else
                {
                    return;
                }
            }
            var xmlSerializer = new XmlSerializer(typeof(Configuration.Configuration));

            var projectorsData = _screen.GetProjectorsData();
            for(var i = 0;i<projectorsData.Length;i++)
            {
                Configuration.Configuration.Instance.Projectors[i].ControlPoints = projectorsData[i].ControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].BlacklevelControlPoints = projectorsData[i].BlacklevelControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].Blacklevel2ControlPoints = projectorsData[i].Blacklevel2ControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].BlendRegionControlPoints = projectorsData[i].BlendRegionControlPoints.Select(MapControlPoint).ToArray();
            }
            UpdateClippingRectangleSettings();

            using (var writer = new StreamWriter(_configFilename))
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
            {
                xmlSerializer.Serialize(xmlWriter, Configuration.Configuration.Instance);
            }
        }

        private static Configuration.ControlPoint MapControlPoint(PanoBeamLib.ControlPoint controlPoint)
        {
            return new Configuration.ControlPoint
            {
                X = controlPoint.X,
                Y = controlPoint.Y,
                U = controlPoint.U,
                V = controlPoint.V,
                ControlPointType = MapControlPointType(controlPoint.ControlPointType)
            };
        }

        private static Configuration.ControlPointType MapControlPointType(PanoBeamLib.ControlPointType controlPointType)
        {
            if (controlPointType == PanoBeamLib.ControlPointType.Default)
            {
                return Configuration.ControlPointType.Default;
            }
            if (controlPointType == PanoBeamLib.ControlPointType.IsEcke)
            {
                return Configuration.ControlPointType.IsEcke;
            }
            if (controlPointType == PanoBeamLib.ControlPointType.IsFix)
            {
                return Configuration.ControlPointType.IsFix;
            }
            throw new Exception($"Unknwon ControlPointType {controlPointType}");
        }

        private void SaveSettings()
        {
            var filename = Path.Combine(GetProgramDataDirectory(), "PanoBeamSettings.config");
            var xmlSerializer = new XmlSerializer(typeof(Settings));

            UpdateClippingRectangleSettings();

            using (var writer = new StreamWriter(filename))
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
            {
                xmlSerializer.Serialize(xmlWriter, Configuration.Configuration.Instance.Settings);
            }
        }

        private void UpdateClippingRectangleSettings()
        {
            var clippingRectangle = new SimpleRectangle(CameraUserControl.GetClippingRectangle());
            if (clippingRectangle.Width > 0 && clippingRectangle.Height > 0)
            {
                Configuration.Configuration.Instance.Settings.ClippingRectangle = clippingRectangle;
            }
        }
    }
}