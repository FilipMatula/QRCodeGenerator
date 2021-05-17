using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CodeScannerGenerator.Common
{
    public partial class CameraControl
    {
        // Start camera stream and timer
        public static void startCameraStream(ref VideoCaptureDevice captureDevice, FilterInfo currectDevice, DispatcherTimer dispatcherTimer)
        {
            if (currectDevice != null && captureDevice != null && !captureDevice.IsRunning)
            {
                captureDevice.Start();
                dispatcherTimer.Start();
            }
        }

        // Stop camera stream and timer
        public static void stopCameraStream(ref VideoCaptureDevice captureDevice, FilterInfo currectDevice, DispatcherTimer dispatcherTimer)
        {
            if (currectDevice != null && captureDevice != null && captureDevice.IsRunning)
            {
                dispatcherTimer.Stop();
                captureDevice.Stop();
            }
        }

        // Select proper resolution
        public static VideoCapabilities selectResolution(VideoCaptureDevice captureDevice)
        {
            foreach (var cap in captureDevice.VideoCapabilities)
            {
                if (cap.FrameSize.Height == 800)
                    return cap;
                if (cap.FrameSize.Width == 1280)
                    return cap;
            }
            return captureDevice.VideoCapabilities.Last();
        }

        public static List<int> getFocusValues(int min, int max, int step)
        {
            List<int> ret = new List<int>();
            int size = 100;
            int newStep = (max - min) / size;

            for (int value = min; value <= max; value += newStep)
                ret.Add(value);

                if (step != newStep && ret.Last() != max)
                    ret.Add(max);

            return ret;
        }

        // Autofocus method for camera
        public static void autoFocus(VideoCaptureDevice captureDevice, FilterInfo currectDevice)
        {
            if (currectDevice != null && captureDevice != null)
            {
                int minFocus, maxFocus, stepSize, defaultValue;
                CameraControlFlags flag;
                captureDevice.GetCameraPropertyRange(CameraControlProperty.Focus, out minFocus, out maxFocus, out stepSize, out defaultValue, out flag);

                foreach(int value in getFocusValues(minFocus, maxFocus, stepSize))
                {
                    captureDevice.SetCameraProperty(CameraControlProperty.Focus, value, CameraControlFlags.Manual);
                }

                captureDevice.SetCameraProperty(CameraControlProperty.Focus, defaultValue, flag);
            }
        }
    }
}
