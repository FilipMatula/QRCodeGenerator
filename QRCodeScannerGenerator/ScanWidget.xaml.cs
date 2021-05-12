using AForge.Video.DirectShow;
using QRCodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using ZXing;
using ZXing.Common;

namespace QRCodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for ScanWidget.xaml
    /// </summary>
    public partial class ScanWidget : UserControl
    {
        private System.Windows.Forms.PictureBox pictureBoxScan;
        public FilterInfo CurrectDevice { get; set; } = null;
        public FilterInfoCollection FilterInfoCollection { get; set; }
        private VideoCaptureDevice captureDevice;
        private DispatcherTimer scanCodeTimer;
        private bool blockScanning = false;

        private Rectangle scanRect;
        private bool lowerRectangle = false;
        private DispatcherTimer focusCircleTimer;
        private int currentOuterDiameter;
        private int currentInnerDiameter;
        private int focusCircleOuterDiameter = 50;
        private int focusCircleOuterDiameterBounce = 5;
        private int focusCircleInnerDiameter = 10;
        private int focusCircleInnerDiameterBounce = 5;
        private bool raising = true;
        private bool moving = true;
        private long focusCircleStart = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        private System.Drawing.Point focusPt;

        public bool IsAutotype { get; set; } = false;

        public event Action<string> URLOpened;
        public event Action<string> Autotyped;

        public ScanWidget()
        {
            InitializeComponent();

            IsVisibleChanged += ScanWidget_IsVisibleChanged;

            // Initialize focus circle
            resetFocusCircleDimensions();
        }

        public void stopCameraStream()
        {
            CameraControl.stopCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        public void startCameraStream()
        {
            CameraControl.startCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        private void FocusCircleTimer_Tick(object sender, EventArgs e)
        {
            pictureBoxScan.Update();
            currentOuterDiameter = calculateDiameter(currentOuterDiameter, focusCircleOuterDiameter, focusCircleOuterDiameterBounce);
            currentInnerDiameter = calculateDiameter(currentInnerDiameter, focusCircleInnerDiameter, focusCircleInnerDiameterBounce);
        }

        private void comboBox_Scan_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Scan_Type.SelectedIndex != 0 && (BarcodeFormat)comboBox_Scan_Type.SelectedItem == BarcodeFormat.CODE_128)
                lowerRectangle = true;
            else
                lowerRectangle = false;

            Properties.Settings.Default.Scan_code = comboBox_Scan_Type.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            pictureBoxScan.Update();
        }

        private void ScanWidget_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                stopCameraStream();
            else
                startCameraStream();
        }

        private void resetFocusCircleDimensions()
        {
            currentOuterDiameter = 50;
            currentInnerDiameter = 10;
            moving = true;
            raising = true;
        }
        private int calculateDiameter(int currentDiameter, int baseDiameter, int bounce)
        {
            if (moving)
            {
                if (currentDiameter > baseDiameter)
                {
                    if (currentDiameter < (baseDiameter + bounce))
                    {
                        if (raising)
                            currentDiameter++;
                        else
                            currentDiameter--;
                    }
                    else
                    {
                        currentDiameter--;
                        raising = false;
                    }
                }
                else if (currentDiameter == baseDiameter)
                {
                    if (raising)
                        currentDiameter++;
                    else
                    {
                        moving = false;
                    }
                }
                else
                {
                    if (currentDiameter > (baseDiameter - bounce))
                    {
                        if (raising)
                            currentDiameter++;
                        else
                            currentDiameter--;
                    }
                    else
                    {
                        currentDiameter++;
                        raising = true;
                    }
                }
            }
            return currentDiameter;
        }


        public void InitializeTimers()
        {
            scanCodeTimer = new DispatcherTimer();
            scanCodeTimer.Tick += ScanCodeTimer_Tick;
            scanCodeTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            focusCircleTimer = new DispatcherTimer();
            focusCircleTimer.Tick += FocusCircleTimer_Tick;
            focusCircleTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
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
            pictureBoxScan.Image = (Bitmap)eventArgs.Frame.Clone();
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
            Bitmap bitmap = prepareBitmapToScan((Bitmap)pictureBoxScan.Image);
            if (bitmap == null)
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
                double widthScale = bitmap.Width / (double)pictureBoxScan.Width;
                double heigthScale = bitmap.Height / (double)pictureBoxScan.Height;

                int newRectWidth = (int)(scanRect.Width * widthScale);
                int newRectHeight = (int)(scanRect.Height * heigthScale);

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
            pictureBoxScan = new System.Windows.Forms.PictureBox();
            pictureBoxScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBoxScan.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pictureBoxScan.Paint += new System.Windows.Forms.PaintEventHandler(pictureBoxScan_Paint);
            pictureBoxScan.Click += PictureBoxScan_Click;
            windowsFormsHost1.Child = pictureBoxScan;
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

        // Painting rectangles on the picturebox
        private void pictureBoxScan_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            scanRect = PictureBoxPainter.PaintRectangle(pictureBoxScan, e, lowerRectangle);

            long CurrentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (!focusPt.IsEmpty && (CurrentMilliseconds - focusCircleStart) < 1000)
                PictureBoxPainter.PaintCircle(e, focusPt, currentOuterDiameter, currentInnerDiameter);
            else
                focusCircleTimer.Stop();
        }

        // Event handler for clicking the pictureBox with Camera stream
        private void PictureBoxScan_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.MouseEventArgs e2 = (System.Windows.Forms.MouseEventArgs)e;
            System.Drawing.Point pt = new System.Drawing.Point(e2.X, e2.Y);
            if (scanRect.Contains(pt))
            {
                focusPt = pt;
                focusCircleStart = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                resetFocusCircleDimensions();
                focusCircleTimer.Start();
                _ = Task.Run(() => CameraControl.autoFocus(captureDevice, CurrectDevice));
            }
        }
    }
}
