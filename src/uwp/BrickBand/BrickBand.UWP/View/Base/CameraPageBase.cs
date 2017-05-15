using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Autofac;
using BrickBand.UWP.Model.Contract;

namespace BrickBand.UWP.View.Base
{
    public class CameraPageBase : ViewModelPageBase
    {
        protected MediaCapture _mediaCapture;

        protected DisplayRequest _displayRequest = new DisplayRequest();

        
        public CameraPageBase()
        {
            

          
           
        }

        protected async Task InitDevice()
        {
            try
            {

                _mediaCapture = new MediaCapture();

                var device = await FindDevice();

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
                ShowMessageToUser("The app was denied access to the camera");
                return;
            }
        }

        protected async Task<SoftwareBitmap> Capture()
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

            return softwareBitmap;
        }

        

        protected async Task<DeviceInformation> FindDevice()
        {
            // Get available devices for capturing pictures
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            return allVideoDevices[SettingsService.CameraIndex];
        }



        protected void DisposeMediaDevice()
        {
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }

     

       

        protected void ShowMessageToUser(string message)
        {

        }

        

    }
}
