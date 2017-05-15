using BrickBand.UWP.Model.Contract;

namespace BrickBand.UWP.Model.Services
{
    public class SettingsService : ISettingsService
    {
        Windows.Storage.ApplicationDataContainer _localSettings =
            Windows.Storage.ApplicationData.Current.LocalSettings;

        private const string CAMERA_INDEX = "CAMERAINDEX";

        public int CameraIndex
        {
            get
            {
                return int.TryParse(_localSettings.Values[CAMERA_INDEX]?.ToString(), out int parsed) ? parsed : 0;
            }
            set { _localSettings.Values[CAMERA_INDEX] = value.ToString(); }
        }
    }
}
