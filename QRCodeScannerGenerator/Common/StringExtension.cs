using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QRCodeScannerGenerator.Common
{
    public static class StringExtension
    {
        public static string ToSendKeyNormalize(this char c)
        {
            if (Char.IsLetterOrDigit(c))
                return "{" + c.ToString() + "}";

            return c.ToString();
        }
    }
}
