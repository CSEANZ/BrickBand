using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BrickBand.UWP.View.Base;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BrickBand.UWP.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameVIew : CameraPageBase
    {
        private DispatcherTimer _timer;
        public GameVIew()
        {
            
            this.InitializeComponent();
            this.Loaded += CameraCalibrationView_Loaded;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;
            _timer.Start();

        }

        private async void _timer_Tick(object sender, object e)
        {
            _doCap();
        }

        async void _doCap()
        {
            var swBitmap = await Capture();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _timer.Stop();
            DisposeMediaDevice();
        }

        private async void CameraCalibrationView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitDevice();
        }
    }
}
