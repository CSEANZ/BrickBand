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
using Services;

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

        private int _cameraIndex = 0;

        private BitmapImage _currentFrame = null;

        private Deskewer _deskewer = new Deskewer();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;

        }

        async void _timer()
        {
            while (true)
            {
                await Task.Delay(50);
                if (_currentFrame == null)
                {
                    continue;
                }

                try
                {
                    
                        var bm = BitmapImage2Bitmap(_currentFrame);

                        var result = _deskewer.Deskew(bm, 70, 70);
                    Dispatcher.Invoke(() =>
                    {
                        Canny.Source = Bitmap2BitmapImage(result.Item3);
                        Lines.Source = Bitmap2BitmapImage(result.Item4);
                        Finished.Source = Bitmap2BitmapImage(result.Item1);
                    });

                }
                catch (Exception ex)
                {
                    
                }
            }
        }

        //Thanks http://www.shujaat.net/2010/08/wpf-images-from-project-resource.html!

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmapInput)
        {
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
            _timer();
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
            await Task.Delay(1000);

            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (_cameraIndex > _videoDevices.Count - 1)
            {
                _cameraIndex = 0;
            }

            var selDevice = _videoDevices[0];

            _videoDevice = new VideoCaptureDevice(selDevice.MonikerString);

            _videoDevice.NewFrame += _videoDevice_NewFrame;

            var caps = _videoDevice.VideoCapabilities;

            _videoDevice.VideoResolution = caps[caps.Length - 1];

            _videoDevice.Start();
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
    }
}
