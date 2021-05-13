using AForge.Video.DirectShow;
using CodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for ScanWidget.xaml
    /// </summary>
    public partial class ScanWidget : UserControl
    {
        public FilterInfo CurrectDevice { get; set; } = null;
        public FilterInfoCollection FilterInfoCollection { get; set; }
        private VideoCaptureDevice captureDevice;
        private DispatcherTimer scanCodeTimer;
        private bool blockScanning = false;

        public bool IsAutotype { get; set; } = false;

        public event Action<string> URLOpened;
        public event Action<string> Autotyped;

        public ScanWidget()
        {
            InitializeComponent();

            IsVisibleChanged += ScanWidget_IsVisibleChanged;
            VideoPreviewWidget.Autofocused += Autofocus;
        }

        public void stopCameraStream()
        {
            CameraControl.stopCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        public void startCameraStream()
        {
            CameraControl.startCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        private void comboBox_Scan_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Scan_Type.SelectedIndex != 0 && (BarcodeFormat)comboBox_Scan_Type.SelectedItem == BarcodeFormat.CODE_128)
                VideoPreviewWidget.LowerRectangle = true;
            else
                VideoPreviewWidget.LowerRectangle = false;

            Properties.Settings.Default.Scan_code = comboBox_Scan_Type.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            VideoPreviewWidget.Update();
        }

        private void ScanWidget_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                startCameraStream();
            else
                stopCameraStream();
        }


        public void InitializeTimers()
        {
            scanCodeTimer = new DispatcherTimer();
            scanCodeTimer.Tick += ScanCodeTimer_Tick;
            scanCodeTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            VideoPreviewWidget.InitializeTimers();
        }

        // On device change take video from camera (also on initialization)
        private void comboBox_Devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stopCameraStream();

            initializeStream();

            startCameraStream();
        }

        // Initilize camera stream
        private void initializeStream()
        {
            if (comboBox_Devices.SelectedIndex == -1)
                return;

            CurrectDevice = FilterInfoCollection[comboBox_Devices.SelectedIndex];
            captureDevice = new VideoCaptureDevice(FilterInfoCollection[comboBox_Devices.SelectedIndex].MonikerString);
            captureDevice.NewFrame += CaptureDevice_NewFrame;
            captureDevice.VideoResolution = CameraControl.selectResolution(captureDevice);
        }

        // Show camera video in picture box
        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            VideoPreviewWidget.SetImage((Bitmap)eventArgs.Frame.Clone());
            blockScanning = false;
        }

        // Open url when open button clicked
        private void ButtonOpenUrl_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Text_Scan.Text))
            {
                Uri uriResult;
                bool UriParseResult = Uri.TryCreate(Text_Scan.Text, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (UriParseResult)
                    URLOpened?.Invoke(Text_Scan.Text);
                else
                    MessageBox.Show(LocUtil.TranslatedString("IncorrectURIMessage", this), LocUtil.TranslatedString("IncorrectURITitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("NoQRScannedMessage", this), LocUtil.TranslatedString("NoQRScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Try read qr code every second
        private void ScanCodeTimer_Tick(object sender, EventArgs e)
        {
            Bitmap bitmap = prepareBitmapToScan(VideoPreviewWidget.GetImage());
            if (bitmap == null)
                return;

            if (comboBox_Scan_Type.SelectedIndex == -1)
                return;

            BarcodeReader reader = new BarcodeReader
                (null, newbitmap => new BitmapLuminanceSource(bitmap), luminance => new GlobalHistogramBinarizer(luminance));

            reader.AutoRotate = true;
            reader.TryInverted = true;
            reader.Options = new DecodingOptions
            {
                PossibleFormats = comboBox_Scan_Type.SelectedIndex == 0 ? Constants.PossibleFormats : new List<BarcodeFormat>() { (BarcodeFormat)comboBox_Scan_Type.SelectedItem }
            };

            var result = reader.Decode(bitmap);
            //Result [] result = reader.DecodeMultiple(bitmap);
            if (result != null && !blockScanning)
            {
                stopCameraStream();
                string scannedText = result.ToString();
                Text_Scan.Text = scannedText;
                saveInClipboard(scannedText);

                string text = LocUtil.TranslatedString("QRCodeScannedMessage", this) + " " + scannedText + ".\n" + "Text was copied to clipboard.";

                if (IsAutotype)
                {
                    text += "\n" + LocUtil.TranslatedString("Do you want to autofill field with this text?", this);
                    MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                              LocUtil.TranslatedString("QRCodeScannedTitle", this),
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        Autotyped?.Invoke(scannedText);
                    }
                    IsAutotype = false;
                }
                else
                {
                    Uri uriResult;
                    bool UriParseResult = Uri.TryCreate(scannedText, UriKind.Absolute, out uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    if (UriParseResult)
                    {
                        text += "\n" + LocUtil.TranslatedString("QRCodeScannedMessage2", this);
                        MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                                  LocUtil.TranslatedString("QRCodeScannedTitle", this),
                                                  MessageBoxButton.YesNo,
                                                  MessageBoxImage.Question);
                        if (messageBoxResult == MessageBoxResult.Yes)
                            URLOpened?.Invoke(scannedText);
                    }
                    else
                    {
                        MessageBox.Show(text, LocUtil.TranslatedString("QRCodeScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                blockScanning = true;
                startCameraStream();
            }
        }

        private void saveInClipboard(string text)
        {
            System.Windows.Forms.Clipboard.SetText(text);
        }

        private Bitmap prepareBitmapToScan(Bitmap bitmap)
        {
            if (bitmap != null)
            {
                double widthScale = bitmap.Width / VideoPreviewWidget.GetWidgetWidth();
                double heigthScale = bitmap.Height / VideoPreviewWidget.GetWidgetHeight();

                int newRectWidth = (int)(VideoPreviewWidget.ScanRect.Width * widthScale);
                int newRectHeight = (int)(VideoPreviewWidget.ScanRect.Height * heigthScale);

                Rectangle imgScanArea = new Rectangle(bitmap.Width / 2 - newRectWidth / 2, bitmap.Height / 2 - newRectHeight / 2, newRectWidth, newRectHeight);
                if (imgScanArea.Width > 0 && imgScanArea.Height > 0)
                    bitmap = CropImage(bitmap, imgScanArea);
            }

            return bitmap;
        }

        private Bitmap CropImage(System.Drawing.Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, PixelFormat.Format24bppRgb);
        }

        public void InitializePictureBoxes()
        {
            VideoPreviewWidget.InitializePictureBoxes();
        }

        public void InitializeComboboxes()
        {
            // Type for scan combobox
            comboBox_Scan_Type.Items.Add(LocUtil.TranslatedString("Auto", this));
            foreach (BarcodeFormat format in Constants.PossibleFormats)
                comboBox_Scan_Type.Items.Add(format);

            string savedScanTypeName = Properties.Settings.Default.Scan_code;
            if (string.IsNullOrEmpty(savedScanTypeName))
                comboBox_Scan_Type.SelectedIndex = 0;
            else
            {
                BarcodeFormat read_format;
                if (Enum.TryParse(savedScanTypeName, out read_format))
                    comboBox_Scan_Type.SelectedItem = read_format;
                else
                    comboBox_Scan_Type.SelectedIndex = 0;
            }
        }

        private void Autofocus()
        {
            _ = Task.Run(() => CameraControl.autoFocus(captureDevice, CurrectDevice));
        }
    }
}
