namespace PaperCam
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using System.Runtime.InteropServices.WindowsRuntime;

    using Windows.Foundation;
    using Windows.Phone.Media.Capture;
    using Windows.Storage.Streams;

    using Nokia.Graphics;
    using Nokia.Graphics.Imaging;
    using Nokia.InteropServices.WindowsRuntime;

    /// <summary>
    /// A concrete implementation of the ICameraEffect.
    /// </summary>
    public class NokiaSketchEffect : ICameraEffect
    {
        private PhotoCaptureDevice captureDevice;
        private WriteableBitmap cameraBitmap;
        private Windows.Foundation.Size outputBufferSize;
        
        public String EffectName
        {
            get { return "Sketch"; }
        }

        public PhotoCaptureDevice CaptureDevice
        {
            set
            {
                captureDevice = value;
                if (captureDevice != null)
                {
                    Windows.Foundation.Size previewSize = captureDevice.PreviewResolution;
                    cameraBitmap = new WriteableBitmap((int)previewSize.Width, (int)previewSize.Height);
                }
            }
        }

        public Windows.Foundation.Size OutputBufferSize
        {
            set
            {
                outputBufferSize = value;
            }
        }

        public async Task GetNewFrameAndApplyEffect(IBuffer processedBuffer)
        {
            if (captureDevice == null)
            {
                return;
            }

            captureDevice.GetPreviewBufferArgb(cameraBitmap.Pixels);

            Bitmap outputBtm = new Bitmap(
                outputBufferSize,
                ColorMode.Bgra8888,
                (uint)outputBufferSize.Width * 4, // 4 bytes per pixel in BGRA888 mode
                processedBuffer);

            EditingSession session = new EditingSession(cameraBitmap.AsBitmap());
            session.AddFilter(FilterFactory.CreateSketchFilter(SketchMode.Gray));
            await session.RenderToBitmapAsync(outputBtm);

        }

    }
}
