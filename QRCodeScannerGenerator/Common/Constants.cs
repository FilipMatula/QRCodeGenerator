using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace QRCodeScannerGenerator.Common
{
    public static class Constants
    {
        public static List<BarcodeFormat> PossibleFormats { get; set; } = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX, BarcodeFormat.CODE_128 };
    }
}
