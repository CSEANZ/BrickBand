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
    public sealed partial class CameraCalibrationView : ViewModelPageBase
    {
        MediaCapture _mediaCapture;
        bool _isPreviewing;

        DisplayRequest _displayRequest = new DisplayRequest();

        private ISettingsService _settingsService;

        public List<string> Devices { get; set; }
       
        private int _selectedDeviceIndex = 0;

        public CameraCalibrationView()
        {
            this.InitializeComponent();
            Application.Current.Suspending += Application_Suspending;

            this.Loaded += CameraCalibrationView_Loaded;
            this.Tapped += CameraCalibrationView_Tapped;

            _settingsService = App.Glue.Container.Resolve<ISettingsService>();//I hate service location, but this app is meant to be simple so we're not going for injectable view models etc. At least it's not service location once in model land :/

            _enumerateDevices();
        }

        private void CameraCalibrationView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _capture();
        }

        private async void _capture()
        {
            //using (var stream = new InMemoryRandomAccessStream())
            //{
            //    var properties = ImageEncodingProperties.CreateJpeg();
            //    await _mediaCapture.CapturePhotoToStreamAsync(properties, stream);
            //    var decoder = await BitmapDecoder.CreateAsync(stream);
            //    var sfbmp = await decoder.GetSoftwareBitmapAsync();
            //}

            //var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            //// Fall back to the local app storage if the Pictures Library is not available
            //var _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;

            ////var stream2 = new InMemoryRandomAccessStream();

            //try
            //{
            //    // Take and save the photo
            //    var file = await _captureFolder.CreateFileAsync("SimplePhoto.jpg", CreationCollisionOption.GenerateUniqueName);
            //    await _mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);
            //    //rootPage.NotifyUser("Photo taken, saved to: " + file.Path, NotifyType.StatusMessage);
            //}
            //catch (Exception ex)
            //{
            //    // File I/O errors are reported as exceptions.
            //    //rootPage.NotifyUser("Exception when taking a photo: " + ex.Message, NotifyType.ErrorMessage);
            //}



            // Prepare and capture photo
            var lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Bgra8));

            var capturedPhoto = await lowLagCapture.CaptureAsync();
            var softwareBitmap = capturedPhoto.Frame.SoftwareBitmap;

            await lowLagCapture.FinishAsync();
        }

        private async void CameraCalibrationView_Loaded(object sender, RoutedEventArgs e)
        {
            await _startPreviewAsync();
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

        private async void _enumerateDevices()
        {
            Devices = new List<string>();
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            foreach(var i in allVideoDevices)
            {
                Devices.Add(i.Name);
            }

            SelectedDeviceIndex = _settingsService.CameraIndex;
        }

        private async Task<DeviceInformation> _findDevice()
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            return allVideoDevices[SelectedDeviceIndex];
        }

        private async Task _startPreviewAsync()
        {
            try
            {

                _mediaCapture = new MediaCapture();

                var device = await _findDevice();

                await _mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                    VideoDeviceId = device.Id
                });
                

                var maxResolution = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Aggregate((i1, i2) => (i1 as VideoEncodingProperties).Width > (i2 as VideoEncodingProperties).Width ? i1 : i2);
               
                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maxResolution);
               


                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                _showMessageToUser("The app was denied access to the camera");
                return;
            }

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


        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                _showMessageToUser("The camera preview can't be displayed because another app has exclusive access");
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

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }

        }

        void _showMessageToUser(string message)
        {
            
        }

        public int SelectedDeviceIndex
        {
            get { return _selectedDeviceIndex; }
            set
            {
                _selectedDeviceIndex = value;
                _settingsService.CameraIndex = value;
                OnPropertyChanged();
            }
        }

    }
}
