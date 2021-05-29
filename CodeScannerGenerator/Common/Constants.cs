using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace CodeScannerGenerator.Common
{
    public static class Constants
    {
        //public static List<BarcodeFormat> PossibleFormats { get; set; } = new List<BarcodeFormat>() { BarcodeFormat.CODABAR, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.CODE_93, BarcodeFormat.DATA_MATRIX, BarcodeFormat.QR_CODE, BarcodeFormat.EAN_13, BarcodeFormat.EAN_8 };
        public static List<BarcodeFormat> PossibleFormats { get; set; } = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE, BarcodeFormat.CODE_128, BarcodeFormat.DATA_MATRIX };

        //public static List<BarcodeFormat> PossibleFormats { get; set; } = Enum.GetValues(typeof(BarcodeFormat)).Cast<BarcodeFormat>().Where(t => t != BarcodeFormat.PHARMA_CODE).ToList();
    }
}
