namespace PaperCam
{
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Shell;

    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Navigation;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Phone.Media.Capture;
    using System.Windows.Media.Imaging;
    using Size = Windows.Foundation.Size;
    using Microsoft.Xna.Framework.Media;
    using Windows.Storage.Streams;
    using Nokia.Graphics;
    using Nokia.Graphics.Imaging;

    using PaperCam.Resources;

    /// <summary>
    /// The application main page.
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        // Constants
        private const String AboutPageUri = "/About.xaml";
        private const double AspectRatio = 4.0 / 3.0;
        private const double MediaElementWidth = 640;
        private const double MediaElementHeight = 480;
        private const String FileNamePrefix = "PaperPhoto_";

        private bool _manuallyFocused = false;

        // Members
        private PhotoCaptureDevice camera;
        private readonly ICameraEffect cameraEffect;
        private CameraStreamSource source;
        //private Boolean isLoaded;
        private MediaElement mediaElement;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            cameraEffect = new NokiaSketchEffect();
            SetCameraButtonsEnabled(true);
        }

        private void OnAboutPageButtonClicked(object sender, EventArgs e)
        {
            if (camera == null)
            {
                return;
            }

           
            NavigationService.Navigate(new Uri(AboutPageUri, UriKind.Relative));
        }
        

        /// <summary>
        /// Opens and sets up the camera if not already. Creates a new
        /// CameraStreamSource with an effect and shows it on the screen via
        /// the media element.
        /// </summary>
        private async void Initialize()
        {
            Size mediaElementSize = new Size(MediaElementWidth, MediaElementHeight);

            if (camera == null)
            {
                // Resolve the capture resolution and open the camera
                var captureResolutions =
                    PhotoCaptureDevice.GetAvailableCaptureResolutions(CameraSensorLocation.Back);

                Size selectedCaptureResolution =
                    captureResolutions.Where(
                        resolution => Math.Abs(AspectRatio - resolution.Width / resolution.Height) <= 0.1)
                            .OrderBy(resolution => resolution.Width).Last();

                camera = await PhotoCaptureDevice.OpenAsync(
                    CameraSensorLocation.Back, selectedCaptureResolution);

                // Set the image orientation prior to encoding
                camera.SetProperty(KnownCameraGeneralProperties.EncodeWithOrientation,
                    camera.SensorLocation == CameraSensorLocation.Back
                    ? camera.SensorRotationInDegrees : -camera.SensorRotationInDegrees);

                // Resolve and set the preview resolution
                var previewResolutions =
                    PhotoCaptureDevice.GetAvailablePreviewResolutions(CameraSensorLocation.Back);

                Size selectedPreviewResolution =
                    previewResolutions.Where(
                        resolution => Math.Abs(AspectRatio - resolution.Width / resolution.Height) <= 0.1)
                            .Where(resolution => (resolution.Height >= mediaElementSize.Height)
                                   && (resolution.Width >= mediaElementSize.Width))
                                .OrderBy(resolution => resolution.Width).First();

                await camera.SetPreviewResolutionAsync(selectedPreviewResolution);

                cameraEffect.CaptureDevice = camera;
            }


            if (mediaElement == null)
            {
                mediaElement = new MediaElement();
                mediaElement.Stretch = Stretch.UniformToFill;
                mediaElement.BufferingTime = new TimeSpan(0);
                mediaElement.Tap += OnMyCameraMediaElementTapped;
                source = new CameraStreamSource(cameraEffect, mediaElementSize);
                mediaElement.SetSource(source);
                MediaElementContainer.Children.Add(mediaElement);
                
            } 
            
            // Show the index and the name of the current effect
            if (cameraEffect is NokiaSketchEffect)
            {
                NokiaSketchEffect effects = cameraEffect as NokiaSketchEffect;
            }
            
        }

        

        private void ApplySketchEffect()
        {
            NokiaSketchEffect effects = cameraEffect as NokiaSketchEffect;
        }




        /// <summary>
        /// From PhoneApplicationPage.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedTo()");
            Initialize();
            base.OnNavigatedTo(e);

        }

        /// <summary>
        /// From PhoneApplicationPage.
        /// Sets the media element source to null and disconnects the event
        /// handling.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("MainPage.OnNavigatedFrom()");
            //MyCameraMediaElement.Source = null;
            MediaElementContainer.Children.Remove(mediaElement); 
 		            mediaElement = null; 
 		 
 		            if (camera != null) 
 		            { 
 		                camera.Dispose(); 
 		                camera = null; 
 		            }
            
        }


        /// <summary>
        /// Enables or disables listening to hardware shutter release key events.
        /// </summary>
        /// <param name="enabled">True to enable listening, false to disable listening.</param>
        private void SetCameraButtonsEnabled(bool enabled)
        {
            if (enabled)
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed += ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed += ShutterKeyPressed;
            }
            else
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed -= ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed -= ShutterKeyPressed;
            }
        }

        /// <summary>
        /// Half-pressing the shutter key initiates autofocus.
        /// </summary>
        private async void ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (_manuallyFocused)
            {
                _manuallyFocused = false;
            }

            await camera.FocusAsync();
        }

        /// <summary>
        /// Completely pressing the shutter key initiates capturing a photo.
        /// </summary>
        private async void ShutterKeyPressed(object sender, EventArgs e)
        {
            await Capture();
        }


        /// <summary>
        /// Changes the current camera effect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMyCameraMediaElementTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
           ApplySketchEffect();
        }

        /// <summary>
        /// Clicking on the capture button initiates autofocus and captures a photo.
        /// </summary>
        private async void CaptureButton_Click(object sender, EventArgs e)
        {
            await Capture();

        }

        private async System.Threading.Tasks.Task Capture()
        {
            try
            {
                await camera.FocusAsync();

                MemoryStream imageStream = new MemoryStream();
                imageStream.Seek(0, SeekOrigin.Begin);

                CameraCaptureSequence sequence = camera.CreateCaptureSequence(1);
                sequence.Frames[0].CaptureStream = imageStream.AsOutputStream();

                await camera.PrepareCaptureSequenceAsync(sequence);
                await sequence.StartCaptureAsync();

                camera.SetProperty(
                KnownCameraPhotoProperties.LockedAutoFocusParameters,
                AutoFocusParameters.None);

                MediaLibrary library = new MediaLibrary();

                EditingSession session = new EditingSession(imageStream.GetWindowsRuntimeBuffer());

                using (session)
                {
                    session.AddFilter(FilterFactory.CreateSketchFilter(SketchMode.Gray));
                    IBuffer data = await session.RenderToJpegAsync();
                    library.SavePictureToCameraRoll(FileNamePrefix
                                + DateTime.Now.ToString() + ".jpg",
                                data.AsStream());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to save the image to camera roll: " + ex.ToString());
            }
        }

    }
}