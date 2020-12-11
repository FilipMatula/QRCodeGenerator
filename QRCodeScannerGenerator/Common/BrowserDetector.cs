using Microsoft.Win32;
using QRCodeScannerGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRCodeScannerGenerator.Common
{
    class BrowserDetector
    {
        // Get installed browsers with paths
        public static List<Browser> GetBrowsers()
        {
            RegistryKey browserKeys;
            browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");
            if (browserKeys == null)
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            string[] browserNames = browserKeys.GetSubKeyNames();
            List<Browser> browsers = new List<Browser>();
            for (int i = 0; i < browserNames.Length; i++)
            {
                Browser browser = new Browser();
                RegistryKey browserKey = browserKeys.OpenSubKey(browserNames[i]);
                browser.Name = (string)browserKey.GetValue(null);
                RegistryKey browserKeyPath = browserKey.OpenSubKey(@"shell\open\command");
                if (browserKeyPath != null)
                {
                    browser.Path = browserKeyPath.GetValue(null).ToString().Trim('"');
                    browsers.Add(browser);
                    if (browser.Path != null)
                        browser.Version = FileVersionInfo.GetVersionInfo(browser.Path).FileVersion;
                    else
                        browser.Version = "unknown";
                }
            }

            return browsers;
        }
    }
}
