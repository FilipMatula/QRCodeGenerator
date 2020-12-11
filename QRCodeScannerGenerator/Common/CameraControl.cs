using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRCodeScannerGenerator.Common
{
    class CameraControl
    {
        // Select proper resolution
        public static VideoCapabilities selectResolution(VideoCaptureDevice device)
        {
            foreach (var cap in device.VideoCapabilities)
            {
                if (cap.FrameSize.Height == 800)
                    return cap;
                if (cap.FrameSize.Width == 1280)
                    return cap;
            }
            return device.VideoCapabilities.Last();
        }
    }
}
