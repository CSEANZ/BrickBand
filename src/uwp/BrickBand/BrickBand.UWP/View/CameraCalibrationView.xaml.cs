using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Autofac;
using BrickBand.UWP.Model.Contract;
using BrickBand.UWP.View.Base;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BrickBand.UWP.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CameraCalibrationView : CameraPageBase
    {
        public List<string> Devices { get; set; }

        bool _isPreviewing;

        private int _selectedDeviceIndex = 0;


        public CameraCalibrationView()
        {
            this.InitializeComponent();

            _enumerateDevices();

            Application.Current.Suspending += Application_Suspending;
            this.Loaded += CameraCalibrationView_Loaded;
            this.Tapped += CameraCalibrationView_Tapped;

           
        }

        private void CameraCalibrationView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Capture();
        }

        private async void CameraCalibrationView_Loaded(object sender, RoutedEventArgs e)
        {
            await _startPreviewAsync();
        }

        private async Task _startPreviewAsync()
        {
            await InitDevice();

            try
            {
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async void _enumerateDevices()
        {
            Devices = new List<string>();
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach(var i in allVideoDevices)
            {
                Devices.Add(i.Name);
            }

            SelectedDeviceIndex = SettingsService.CameraIndex;
        }

        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                ShowMessageToUser("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !_isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await _startPreviewAsync();
                });
            }
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await _cleanupCameraAsync();
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await _cleanupCameraAsync();
                deferral.Complete();
            }
        }

        private async Task _cleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    DisposeMediaDevice();
                });
            }

        }

        public int SelectedDeviceIndex
        {
            get { return _selectedDeviceIndex; }
            set
            {
                _selectedDeviceIndex = value;
                SettingsService.CameraIndex = value;
                OnPropertyChanged();
            }
        }
    }
}
