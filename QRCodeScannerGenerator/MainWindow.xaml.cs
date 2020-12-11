using AForge.Video.DirectShow;
using QRCodeScannerGenerator.Common;
using QRCodeScannerGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace QRCodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FilterInfoCollection filterInfoCollection;
        FilterInfo currectDevice = null;
        VideoCaptureDevice captureDevice;
        System.Windows.Forms.PictureBox pictureBoxScan;
        System.Windows.Forms.PictureBox pictureBoxGenerate;
        DispatcherTimer dispatcherTimer;
        DispatcherTimer signalTimer;
        bool showNoDeviceError = false;
        bool startUp = true;
        bool blockScanning = false;
        List<Browser> browsers;
        long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        public MainWindow()
        {
            InitializeComponent();

            LocUtil.SetDefaultLanguage(this);
            OnLanguageChanged(LocUtil.GetCurrentCultureName(this));

            Closing += MainWindow_Closing;
            Deactivated += MainWindow_Deactivated;
            Activated += MainWindow_Activated;

            // Initialize picture boxes
            pictureBoxScan = new System.Windows.Forms.PictureBox();
            pictureBoxScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBoxScan.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            windowsFormsHost1.Child = pictureBoxScan;

            pictureBoxGenerate = new System.Windows.Forms.PictureBox();
            pictureBoxGenerate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            pictureBoxGenerate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            windowsFormsHost2.Child = pictureBoxGenerate;

            // Initialize timer
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            signalTimer = new DispatcherTimer();
            signalTimer.Tick += SignalTimer_Tick;
            signalTimer.Interval = new TimeSpan(0, 0, 1);
            signalTimer.Start();

            ScanQR.IsVisibleChanged += ScanQR_IsVisibleChanged;
        }

        // Timer for signal reveiving about new devices
        private void SignalTimer_Tick(object sender, EventArgs e)
        {
            long CurrentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if ((CurrentMilliseconds - milliseconds) < 3000)
                return;

            FilterInfoCollection newfilterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (filterInfoCollection.Count == newfilterInfoCollection.Count)
                return;
            else
            {
                bool found = false;
                if (currectDevice != null)
                {
                    foreach (FilterInfo filterInfo in newfilterInfoCollection)
                    {
                        if (currectDevice.MonikerString == filterInfo.MonikerString)
                            found = true;
                    }
                }

                LoadDevices(found);
            }
        }

        // When window is activate start stream
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if (ScanQR.IsVisible)
                startCameraStream();
        }

        // When window is not activeate stop stream
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            stopCameraStream();
        }

        // Change selection visibility marker
        private void MoveCursor(int index)
        {
            TransitionContentSlide.OnApplyTemplate();
            TransitionGrid.Margin = new Thickness(0, index * 60 + 150, 0, 0);
        }

        // Stop camera stream and timer
        private void stopCameraStream()
        {
            if (currectDevice != null && captureDevice != null)
            {
                dispatcherTimer.Stop();
                captureDevice.Stop();
            }
        }

        // Start camera stream and timer
        private void startCameraStream()
        {
            if (currectDevice != null && captureDevice != null)
            {
                captureDevice.Start();
                dispatcherTimer.Start();
            }
        }

        // Open URL from readed qrcode
        private void openURL()
        {
            if (!string.IsNullOrEmpty(QRText_Scan.Text))
            {
                Uri uriResult;
                bool UriParseResult = Uri.TryCreate(QRText_Scan.Text, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (UriParseResult)
                    Process.Start(browsers[comboBox_Browsers.SelectedIndex].Path, QRText_Scan.Text);
                else
                    MessageBox.Show(LocUtil.TranslatedString("IncorrectURIMessage", this), LocUtil.TranslatedString("IncorrectURITitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("NoQRScannedMessage", this), LocUtil.TranslatedString("NoQRScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Show proper grid on type changed
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListViewMenu.SelectedIndex;
            MoveCursor(index);
            switch (index)
            {
                case 0:
                    ScanQR.Visibility = Visibility.Visible;
                    GenerateQR.Visibility = Visibility.Hidden;
                    Settings.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    ScanQR.Visibility = Visibility.Hidden;
                    GenerateQR.Visibility = Visibility.Visible;
                    Settings.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    ScanQR.Visibility = Visibility.Hidden;
                    GenerateQR.Visibility = Visibility.Hidden;
                    Settings.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        // Show camera video in picture box
        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            pictureBoxScan.Image = (Bitmap)eventArgs.Frame.Clone();
            blockScanning = false;
        }

        // Initilize camera stream
        private void initializeStream()
        {
            if (comboBox_Devices.SelectedIndex == -1)
                return;

            currectDevice = filterInfoCollection[comboBox_Devices.SelectedIndex];
            captureDevice = new VideoCaptureDevice(filterInfoCollection[comboBox_Devices.SelectedIndex].MonikerString);
            captureDevice.NewFrame += CaptureDevice_NewFrame;
            captureDevice.VideoResolution = CameraControl.selectResolution(captureDevice);
        }

        // On device change take video from camera (also on initialization)
        private void comboBox_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stopCameraStream();

            initializeStream();

            startCameraStream();
        }

        // Try read qr code every second
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Bitmap bitmap = (Bitmap)pictureBoxScan.Image;
            if (bitmap == null)
                return;

            BarcodeReader reader = new BarcodeReader
                (null, newbitmap => new BitmapLuminanceSource(bitmap), luminance => new GlobalHistogramBinarizer(luminance));

            reader.AutoRotate = true;
            reader.TryInverted = true;
            List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };
            reader.Options = new DecodingOptions { TryHarder = true, PossibleFormats = possibleFormats };

            var result = reader.Decode(bitmap);
            if (result != null && !blockScanning)
            {
                string QRCodeText = result.ToString();
                QRText_Scan.Text = QRCodeText;
                stopCameraStream();
                string text = LocUtil.TranslatedString("QRCodeScannedMessage", this) + QRCodeText;
                Uri uriResult;
                bool UriParseResult = Uri.TryCreate(QRCodeText, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (UriParseResult)
                {
                    text += "\n" + LocUtil.TranslatedString("QRCodeScannedMessage2", this);
                    MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                              LocUtil.TranslatedString("QRCodeScannedTitle", this),
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                        openURL();
                }
                else
                {
                    MessageBox.Show(text, LocUtil.TranslatedString("QRCodeScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Information);
                }

                blockScanning = true;
                startCameraStream();
            }
        }

        // Stop / Start camera on mode changed
        private void ScanQR_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!ScanQR.IsVisible)
                stopCameraStream();
            else
                startCameraStream();
        }

        // Stop camera on closing
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopCameraStream();
        }

        // Open url when open button clicked
        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            openURL();
        }

        // Generate QR Code
        private void GenerateQRCode()
        {
            if (!string.IsNullOrEmpty(QRText_Generate.Text))
            {
                BarcodeWriter barcodeWriter = new BarcodeWriter();
                QrCodeEncodingOptions options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 300,
                    Height = 300,
                };
                barcodeWriter.Options = options;
                barcodeWriter.Format = BarcodeFormat.QR_CODE;
                var result = new Bitmap(barcodeWriter.Write(QRText_Generate.Text.Trim()));
                pictureBoxGenerate.Image = result;
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("EmptyTextTitle", this), LocUtil.TranslatedString("EmptyTextMessage", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Generate QR code
        private void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateQRCode();
        }

        // Save QR code
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (pictureBoxGenerate.Image != null)
            {
                System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
                sfd.Filter = "Images|*.png;";
                ImageFormat format = ImageFormat.Png;
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    pictureBoxGenerate.Image.Save(sfd.FileName, format);
                }
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("NoQRGeneratedMessage", this), LocUtil.TranslatedString("NoQRGeneratedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // On content renderered show initialization errors
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (showNoDeviceError)
                MessageBox.Show(LocUtil.TranslatedString("NoDeviceMessage", this), LocUtil.TranslatedString("NoDeviceTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);

            startUp = false;
        }

        // Load list of devices
        private void LoadDevices(bool keepCurrent = false)
        {
            stopCameraStream();
            if (!startUp)
            {
                MessageBox.Show(LocUtil.TranslatedString("DeviceChangedMessage", this), LocUtil.TranslatedString("DeviceChangedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
                Thread.Sleep(2000);
            }

            if (!keepCurrent)
                currectDevice = null;
            comboBox_Devices.Items.Clear();

            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (filterInfoCollection.Count == 0)
            {
                ListViewMenu.SelectedIndex = 1;
                ListViewQRScanItem.IsEnabled = false;
                if (startUp)
                    showNoDeviceError = true;
                else
                    MessageBox.Show(LocUtil.TranslatedString("NoDeviceMessage", this), LocUtil.TranslatedString("NoDeviceTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                foreach (FilterInfo filterInfo in filterInfoCollection)
                    comboBox_Devices.Items.Add(filterInfo.Name);

                if (keepCurrent)
                    comboBox_Devices.SelectedItem = currectDevice.Name;
                else
                    comboBox_Devices.SelectedIndex = 0;
                ListViewMenu.SelectedIndex = 0;
                ListViewQRScanItem.IsEnabled = true;
            }
        }

        // Initialize program after UI ready
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDevices();

            browsers = BrowserDetector.GetBrowsers();
            foreach (Browser browser in browsers)
                comboBox_Browsers.Items.Add(browser.Name);

            string savedBrowserName = Properties.Settings.Default.Browser;
            if (string.IsNullOrEmpty(savedBrowserName))
                comboBox_Browsers.SelectedIndex = 0;
            else
            {
                var savedBrowser = browsers.Find(b => b.Name == savedBrowserName);
                if (savedBrowser != null)
                    comboBox_Browsers.SelectedItem = savedBrowserName;
                else
                    comboBox_Browsers.SelectedIndex = 0;
            }
        }

        // Activate devices plug and unplug signals
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource src = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            src.AddHook(new HwndSourceHook(WndProc));
        }

        // Handler for devices signals
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DEVICECHANGE = 0x0219;
            if (msg == WM_DEVICECHANGE)
            {
                milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
            return IntPtr.Zero;
        }

        // Save browser user settings
        private void comboBox_Browsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Browser = comboBox_Browsers.SelectedItem.ToString();
            Properties.Settings.Default.Save();
        }

        // On enter pressed generate QR Code
        private void QRText_Generate_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) 
                GenerateQRCode();
        }

        private void Button_GermanLanguage_Click(object sender, RoutedEventArgs e)
        {
            LocUtil.SwitchLanguage(this, "de-DE");
            OnLanguageChanged(LocUtil.GetCurrentCultureName(this));
        }

        private void Button_EnglishLanguage_Click(object sender, RoutedEventArgs e)
        {
            LocUtil.SwitchLanguage(this, "en-US");
            OnLanguageChanged(LocUtil.GetCurrentCultureName(this));
        }

        private void OnLanguageChanged(string key)
        {
            switch (key)
            {
                case "en-US":
                    Button_GermanLanguage.Opacity = 0.5;
                    Button_EnglishLanguage.Opacity = 1;
                    break;
                case "de-DE":
                    Button_GermanLanguage.Opacity = 1;
                    Button_EnglishLanguage.Opacity = 0.5;
                    break;
                default:
                    break;
            }
        }
    }
}
