using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge.Video;
using AForge.Video.DirectShow;
using ExtensionGoo.Standard.Extensions;
using Newtonsoft.Json;
using Services;
using Services.Entity;
using Services.ImageTools;

namespace ImageDeskew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoDevice;
        private VideoCapabilities[] _videoCapabilities;

        private int _cameraIndex = 1;

        private BitmapImage _currentFrame = null;

        private Deskewer _deskewer = new Deskewer();
        ColourFinder _finder = new ColourFinder();

        private ColourBoardProfile _current;
        private ColourBoardProfile _calibration;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
           

        }

        private string _funcUrl =
            "https://musicmaker.azurewebsites.net/api/MusicIn?code=Fa11szPiazzsgNFmKEXXoNuoa5yTZsGaUIahV1zyiDDwhOoDpGeh4g==";

        async void _runScan()
        {
            var d = Convert.ToDouble(OptHue.Text);

            //OptHue.Text = (d + 2).ToString();

            if (_currentFrame == null)
            {
                return;
            }

            try
            {

                var bm = BitmapImage2Bitmap(_currentFrame);



                var result = _deskewer.Deskew(bm,
                    Convert.ToDouble(Threshold.Text),
                    Convert.ToDouble(ThresholdLinking.Text),
                    Convert.ToInt32(OptSmooth.Text),
                    Convert.ToDouble(OptDistanceRes.Text),
                    Convert.ToInt32(OptLineThreshold.Text),
                    Convert.ToDouble(OptAngleRes.Text),
                    Convert.ToDouble(OptMinLineWidth.Text),
                    Convert.ToDouble(OptLineGap.Text),
                     Convert.ToDouble(OptHue.Text));


                try
                {
                    Lines.Source = Bitmap2BitmapImage(result.Item4);
                    Finished.Source = Bitmap2BitmapImage(result.Item1);

                    if (result.Item1 == null)
                    {
                        return;
                    }

                    if (_calibration == null)
                    {
                        var findResult = _finder.FindColors(result.Item1, true, 17);

                        _current = findResult;
                        var resultImage = _finder.VisualiseZones(findResult);

                        if (resultImage == null)
                        {
                            return;
                        }

                        Canny.Source = Bitmap2BitmapImage(resultImage);
                    }
                    else
                    {
                        var findResult = _finder.FindColors(result.Item1, true, 16);
                        var resultCompare = _finder.CompareColours(_calibration, findResult);
                        if (resultCompare == null)
                        {
                            return;
                        }
                        var resultImage = _finder.VisualiseZones(resultCompare);
                        if (resultImage == null)
                        {
                            return;
                        }

                        var me = new MusicEntry
                        {
                            SerialisedData = JsonConvert.SerializeObject(resultCompare)
                        };

                        var sendResult = await _funcUrl.Post(JsonConvert.SerializeObject(me));

                        Canny.Source = Bitmap2BitmapImage(resultImage);
                    }

                }
                catch { }




            }
            catch (Exception ex)
            {

            }
        }

        void _timer(object o)
        {
            Dispatcher.Invoke(_runScan);
        }

        //Thanks http://www.shujaat.net/2010/08/wpf-images-from-project-resource.html!

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmapInput)
        {
            if (bitmapInput == null)
            {
                return null;
            }
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            var memoryStream = new MemoryStream();

            // Save to a memory stream...
            bitmapInput.Save(memoryStream, ImageFormat.Bmp);

            // Rewind the stream...
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        //thanks http://stackoverflow.com/questions/6484357/converting-bitmapimage-to-bitmap-and-vice-versa!

        private Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _videoDevice?.Stop();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _init();
        }

        void _switchCamera()
        {
            if (_videoDevice != null)
            {
                _videoDevice?.Stop();
                _videoDevice.NewFrame -= _videoDevice_NewFrame;
            }

            _cameraIndex += 1;

            _init();
        }

        async void _init()
        {
            await Task.Delay(250);

            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (_cameraIndex > _videoDevices.Count - 1)
            {
                _cameraIndex = 0;
            }

            var selDevice = _videoDevices[_cameraIndex];

            _videoDevice = new VideoCaptureDevice(selDevice.MonikerString);

            _videoDevice.NewFrame += _videoDevice_NewFrame;

            var caps = _videoDevice.VideoCapabilities;

            _videoDevice.VideoResolution = caps[caps.Length - 1];

            _videoDevice.Start();

            var t = new System.Threading.Timer(_timer);
            t.Change(0, 1000);
        }

        private void _videoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                BitmapImage bi;
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    bi = new BitmapImage();
                    bi.BeginInit();
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Bmp);
                    bi.StreamSource = ms;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    _currentFrame = bi;
                }
                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate { frameHolder.Source = bi; }));


            }
            catch (Exception ex)
            {
                //catch your error here
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _switchCamera();
        }

        private void CalibrateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_calibration != null)
            {
                _calibration = null;
                return;
            }
            if (_current != null)
            {
                _calibration = _current;
            }
        }
    }
}
