using PanoBeamLib;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PanoBeam.Configuration;
using PanoBeam.Events;
using PanoBeam.Mapper;
using PanoBeam.Events.Events;
using PanoBeam.Events.Data;
using Size = System.Drawing.Size;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace PanoBeam
{
    /// <summary>
    /// Interaction logic for ScreenView.xaml
    /// </summary>
    public partial class ScreenView : Window
    {
        public ScreenView()
        {
            InitializeComponent();
        }

        private PanoScreen _screen;

        public Size Resolution { get; set; }

        public int Overlap { get; set; }

        private bool _isShiftPressed;

        private BitmapImage _white;
        private BitmapImage _previousImage;

        public void Initialize(PanoScreen screen)
        {
            _screen = screen;
            var width = Resolution.Width;
            var height = Resolution.Height;
            Width = width;
            Height = height;
            Image1.Width = width;
            Image1.Height = height;
            WarpControl1.Initialize(screen);

            var white = new Bitmap(width, height);
            var g = Graphics.FromImage(white);
            g.FillRectangle(Brushes.White, 0, 0, width, height);
            var ms = new MemoryStream();
            white.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            _white = new BitmapImage();
            _white.BeginInit();
            _white.StreamSource = ms;
            _white.EndInit();

            _screen.SetPattern(
                Configuration.Configuration.Instance.Settings.PatternSize,
                new Size(Configuration.Configuration.Instance.Settings.PatternCountX, Configuration.Configuration.Instance.Settings.PatternCountY),
                Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap, false);

            _screen.UpdateProjectorsFromConfig(ProjectorMapper.MapProjectorsData(Configuration.Configuration.Instance.Projectors));

            EventHelper.SubscribeEvent<ControlPointsMoved, ControlPointData>(OnControlPointsMoved);
            EventHelper.SubscribeEvent<CalibrationStarted, EventArgs>(OnCalibrationStarted);
            EventHelper.SubscribeEvent<CalibrationFinished, EventArgs>(OnCalibrationFinished);
        }


        public void Refresh(ControlPointsMode controlPointsMode, bool wireframeVisible)
        {
            Dispatcher.Invoke(() => {
                WarpControl1.UpdateWarpControl(controlPointsMode);
                WarpControl1.SetVisibility(controlPointsMode, wireframeVisible);
            });
        }

        public void UpdateWarpControl()
        {
            if (_screen.SetPattern(Configuration.Configuration.Instance.Settings.PatternSize, GetPatternCount(), Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap, false))
            {
                WarpControl1.UpdateWarpControl(Configuration.Configuration.Instance.Settings.ControlPointsMode);
            }
            WarpControl1.SetVisibility(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
        }

        private Size GetPatternCount()
        {
            return new Size(Configuration.Configuration.Instance.Settings.PatternCountX, Configuration.Configuration.Instance.Settings.PatternCountY);
        }

        private void OnControlPointsMoved(ControlPointData controlPointData)
        {
            if (Configuration.Configuration.Instance.Settings.ImmediateWarp)
            {
                _screen.Warp();
            }
        }

        private void OnCalibrationStarted(EventArgs obj)
        {
            ShowImage(_white);
            Show();
            WarpControl1.Visibility = Visibility.Hidden;
        }

        private void OnCalibrationFinished(EventArgs obj)
        {
            Dispatcher.Invoke(() =>
            {
                WarpControl1.Visibility = Visibility.Visible;
                ShowImage(_previousImage);
            });
        }

        public void ShowImage(BitmapImage image)
        {
            if (image != _white)
            {
                _previousImage = image;
            }
            Image1.Source = image;
        }

        public void ShowImage(string file)
        {
            Dispatcher.Invoke(() =>
            {
                var image = new BitmapImage(new Uri(file));
                Image1.Source = image;
            });
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _isShiftPressed = false;
            }
            else if (e.Key == Key.D0)
            {
                WarpControl1.SetActiveProjector(0);
            }
            else if (e.Key == Key.D1)
            {
                WarpControl1.SetActiveProjector(1);
            }
            else if (e.Key == Key.Escape)
            {
                WarpControl1.DeactivateProjectors();
            }
            else if (e.Key == Key.W)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _screen.Warp();
                Mouse.OverrideCursor = null;
            }
            else if (e.Key == Key.B)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _screen.Blend();
                Mouse.OverrideCursor = null;
            }
            else
            {
                WarpControl1.KeyPressed(e, _isShiftPressed);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _isShiftPressed = true;
            }
        }
    }
}
