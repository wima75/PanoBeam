﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PanoBeam.Events;
using PanoBeam.Events.Events;
using PanoBeamLib;

namespace PanoBeam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ViewModel _viewModel;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            PanoScreen.Initialize();
            var mosaicInfo = PanoScreen.GetMosaicInfo();

            var screens = Helpers.GetScreens();
            var mainScreen = Helpers.GetPanoScreen(screens);
            var secondScreen = screens.First(s => s.DeviceName != mainScreen.DeviceName);

            var screenView = new ScreenView
            {
                Left = mainScreen.Bounds.X,
                Top = mainScreen.Bounds.Y,
                Overlap = mosaicInfo.Overlap,
                Resolution = new Size((int)mosaicInfo.ProjectorWidth * 2 - mosaicInfo.Overlap,
                    (int)mosaicInfo.ProjectorHeight)
            };

            InitializeComponent();

            _viewModel = new ViewModel(screenView, mosaicInfo, this);
            DataContext = _viewModel;

            Loaded += (sender, eventArgs) =>
            {

                Left = secondScreen.Bounds.X + 50;
                Top = secondScreen.Bounds.Y + 50;

                _viewModel.Initialize();

                _viewModel.LoadSettings();

                EventHelper.SendEvent<ApplicationReady, EventArgs>(null);
            };
        }

        private ProgressDialogController _controller;

        public void ReportProgress(float progress)
        {
            _controller.SetMessage(GetProgressMessage(progress));
            _controller.SetProgress(progress);
        }

        public void CalibrationDone()
        {
            Dispatcher.Invoke(async () => {
                await _controller.CloseAsync();
            });            
        }

        public async void CalibrationError(string message)
        {
            if (_controller != null && _controller.IsOpen)
            {
                await _controller.CloseAsync();
            }
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                ColorScheme = MetroDialogColorScheme.Accented
            };
            await Dispatcher.Invoke(async () =>
            {
                await this.ShowMessageAsync("Fehler", message, MessageDialogStyle.Affirmative, settings);
            });
        }

        private string GetProgressMessage(float progress)
        {
            return $"Fortschritt: {Math.Round(100f * progress, MidpointRounding.AwayFromZero)}%";
        }

        internal async void InitializeCalculationProgress()
        {
            var settings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false
            };
            _controller = await this.ShowProgressAsync("Berechnung läuft...", GetProgressMessage(0), settings: settings);
            _controller.SetCancelable(false);
        }

        internal async void AwaitProjectorsReady(Action continueAction, Action calibrationCanceled, CalibrationSteps[] calibrationSteps)
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Abbrechen",
                DefaultButtonFocus = MessageDialogResult.Affirmative
            };
            string message;
            if (calibrationSteps.All(s => s == CalibrationSteps.White))
            {
                message = "Bitte auf OK klicken, sobald alle Beamer eine weisse Fläche anzeigen.";
            }
            else
            {
                var actualProjector = 0;
                for (var i = 0; i < calibrationSteps.Length; i++)
                {
                    if (calibrationSteps[i] != CalibrationSteps.Black)
                    {
                        actualProjector = i + 1;
                        break;
                    }
                }
                if (actualProjector == 0)
                {
                    throw new Exception("Unknown Calibration Step");
                }
                if (calibrationSteps[actualProjector - 1] == CalibrationSteps.White)
                {
                    message = $"Bitte auf OK klicken, sobald der {actualProjector}. Beamer eine weisse Fläche anzeigt.";
                }
                else if (calibrationSteps[actualProjector - 1] == CalibrationSteps.Pattern)
                {
                    message = $"Bitte auf OK klicken, sobald der {actualProjector}. Beamer ein Muster anzeigt.";
                }
                else
                {
                    throw new Exception("Unknown Calibration Step");
                }
            }
            if (message == null)
            {
                throw new Exception("Unknown Calibration Step");
            }
            await Dispatcher.Invoke(async () =>
            {
                var res = await this.ShowMessageAsync("Kalibrierung", message, MessageDialogStyle.AffirmativeAndNegative, settings);
                if (res == MessageDialogResult.Affirmative)
                {
                    continueAction();
                }
                else
                {
                    Console.WriteLine("Cancel");
                    calibrationCanceled();
                }
            });
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _viewModel.CloseScreen();
        }
    }
}
