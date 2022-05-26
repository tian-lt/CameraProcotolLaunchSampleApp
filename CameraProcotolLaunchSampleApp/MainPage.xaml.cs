using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CameraProcotolLaunchSampleApp
{
    public sealed partial class MainPage : Page
    {
        private const string PackageFamilyName = "Microsoft.WindowsCamera_8wekyb3d8bbwe";
        private const string SchemeCamera = "microsoft.windows.camera:";
        private const string SchemeCameraPicker = "microsoft.windows.camera.picker:";

        public static readonly DependencyProperty PreviewImageProperty =
            DependencyProperty.Register(nameof(PreviewImage), typeof(ImageSource), typeof(MainPage), new PropertyMetadata(null));

        public ImageSource PreviewImage
        {
            get { return (ImageSource)GetValue(PreviewImageProperty); }
            set { SetValue(PreviewImageProperty, value); }
        }

        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scenario: Simply launch the camera app and no more interaction.
        /// </summary>
        private async void Button_Camera_Click_Async(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(SchemeCamera));
        }

        /// <summary>
        /// Scenario: Use CameraCaptureUI to take a photo and then display the photo.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Camera_CaptureUI_Click_Async(object sender, RoutedEventArgs e)
        {
            var captureUi = new CameraCaptureUI();
            captureUi.PhotoSettings.CroppedAspectRatio = new Size(16, 9);
            var file = await captureUi.CaptureFileAsync(CameraCaptureUIMode.Photo);
            if (file != null)
            {
                await PreviewMediumFileAsync(file);
            }
            else
            {
                await ShowErrorMessage("Camera CaptureUI failed.");
            }
        }

        /// <summary>
        /// Scenario: Launch the camera app to take a photo, then display it within this application.
        /// </summary>
        private async void Button_Camera_Picker_Photo_Click_Async(object sender, RoutedEventArgs e)
        {
            var uri = new Uri(SchemeCameraPicker);
            var options = new LauncherOptions
            {
                TargetApplicationPackageFamilyName = PackageFamilyName,
            };

            var file = await GetTemporaryFileAsync("photo.jpeg");
            var token = SharedStorageAccessManager.AddFile(file);

            var values = new ValueSet();
            values["MediaType"] = "photo";
            values["PhotoFormat"] = 0; // jpeg is the default format 
            values["PhotoFileToken"] = token;

            var result = await Launcher.LaunchUriForResultsAsync(uri, options, values);
            if (result.Status == LaunchUriStatus.Success && result.Result != null)
            {
                await PreviewMediumFileAsync(file);
            }
            else
            {
                await ShowErrorMessage($"Failed to launch Camera App: {result.Status}");
            }
        }

        private async Task PreviewMediumFileAsync(StorageFile file)
        {
            if (file.ContentType == "image/jpeg")
            {
                await PreviewPhoto(file);
            }
            else if (file.ContentType.Contains("video")
                && (file.ContentType.Contains("mp4") || file.ContentType.Contains("wmv")))
            {
                // TODO: preview video
            }
        }

        private async Task PreviewPhoto(StorageFile file)
        {
            Trace.Assert(file.ContentType == "image/jpeg"); // this demo only supports previewing jpeg images.

            using (var stream = await file.OpenReadAsync())
            {
                var image = new BitmapImage();
                await image.SetSourceAsync(stream);
                PreviewImage = image;
            }
        }

        private async Task ShowErrorMessage(string message)
        {
            var dialog = new MessageDialog(message);
            dialog.Title = "Errored";
            await dialog.ShowAsync();
        }

        private async Task<StorageFile> GetTemporaryFileAsync(string filename)
            => await ApplicationData.Current.TemporaryFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
    }
}
