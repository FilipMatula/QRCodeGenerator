using AForge.Video.DirectShow;
using CodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Tyrrrz.Extensions;
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

        private string CurrentVCard { get; set; }

        public ScanWidget()
        {
            InitializeComponent();

            // Language initialization
            LocUtil.SetDefaultLanguage(this);

            IsVisibleChanged += ScanWidget_IsVisibleChanged;
            VideoPreviewWidget.Autofocused += Autofocus;
        }

        public void SwitchLanguage(string culture)
        {
            LocUtil.SwitchLanguage(this, culture);
            if (comboBox_Scan_Type.Items.Count > 0)
            {
                int selectedIndex = comboBox_Scan_Type.SelectedIndex;
                comboBox_Scan_Type.Items.RemoveAt(0);
                comboBox_Scan_Type.Items.Insert(0, LocUtil.TranslatedString("Auto", this));
                comboBox_Scan_Type.Items.Refresh();
                comboBox_Scan_Type.SelectedIndex = selectedIndex;
            }
        }

        public void stopCameraStream()
        {
            CameraControl.stopCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        public void startCameraStream()
        {
            MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && !mainWindow.IsInTray())
                CameraControl.startCameraStream(ref captureDevice, CurrectDevice, scanCodeTimer);
        }

        private void comboBox_Scan_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_Scan_Type.SelectedItem != null)
            {
                if (comboBox_Scan_Type.SelectedIndex != 0 && ((BarcodeFormat)comboBox_Scan_Type.SelectedItem).IsEither(Constants.BarcodeFormats))
                    VideoPreviewWidget.LowerRectangle = true;
                else
                    VideoPreviewWidget.LowerRectangle = false;

                Properties.Settings.Default.Scan_code = comboBox_Scan_Type.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                VideoPreviewWidget.Update();
            }
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
            try
            {
                stopCameraStream();

                initializeStream();

                startCameraStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, LocUtil.TranslatedString("UnexpectedErrorTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Initilize camera stream
        private void initializeStream()
        {
            if (comboBox_Devices.SelectedIndex == -1)
                return;

            CurrectDevice = FilterInfoCollection[comboBox_Devices.SelectedIndex];
            captureDevice = new VideoCaptureDevice(FilterInfoCollection[comboBox_Devices.SelectedIndex].MonikerString);

            if (captureDevice.VideoCapabilities.Length > 0)
            {
                captureDevice.NewFrame += CaptureDevice_NewFrame;
                captureDevice.VideoResolution = CameraControl.selectResolution(captureDevice);
            }
            else
            {
                CurrectDevice = null;
                MessageBox.Show(LocUtil.TranslatedString("DeviceNotConnectedErrorMessage", this), LocUtil.TranslatedString("DeviceNotConnectedErrorTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Show camera video in picture box
        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            VideoPreviewWidget.SetImage((Bitmap)eventArgs.Frame.Clone());
            if (blockScanning)
            {
                Thread.Sleep(200);
                blockScanning = false;
            }
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
                MessageBox.Show(LocUtil.TranslatedString("NoScannedMessage", this), LocUtil.TranslatedString("NoScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                PossibleFormats = comboBox_Scan_Type.SelectedIndex == 0 ? Constants.PossibleFormats : new List<BarcodeFormat>() { (BarcodeFormat)comboBox_Scan_Type.SelectedItem },
                TryHarder = true
            };

            Result[] results = reader.DecodeMultiple(bitmap);
            if (results != null && results.Length > 0 && !blockScanning)
            {
                stopCameraStream();
                blockScanning = true;
                Button_VCard.Visibility = Visibility.Collapsed;
                if (results.Length == 1)
                {
                    string scannedText = results[0].ToString();
                    saveInClipboard(scannedText);

                    if (scannedText.StartsWith("BEGIN:VCARD"))
                    {
                        Text_Scan.Text = "VCARD";
                        Button_VCard.Visibility = Visibility.Visible;
                        CurrentVCard = scannedText;
                        VCardTemplate vcardTemplate = new VCardTemplate(LocUtil.GetCurrentCultureName(this), true);
                        vcardTemplate.Owner = Application.Current.MainWindow;
                        vcardTemplate.SetVCardText(scannedText);
                        vcardTemplate.ShowDialog();
                    }
                    else
                    {
                        Text_Scan.Text = scannedText;
                        if (IsAutotype)
                        {
                            if (Properties.Settings.Default.ConfirmAutotype)
                            {
                                string text = LocUtil.TranslatedString("CodeScannedMessage0") + " " + results[0].BarcodeFormat.ToString() + " " + LocUtil.TranslatedString("CodeScannedMessage1", this) + " \"" + scannedText + "\".\n\n" + LocUtil.TranslatedString("CodeScannedMessage2", this) + "\n\n" + LocUtil.TranslatedString("CodeScannedMessage3", this);
                                MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                                          LocUtil.TranslatedString("CodeScannedTitle", this),
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                                if (messageBoxResult == MessageBoxResult.Yes)
                                {
                                    Autotyped?.Invoke(scannedText);
                                    IsAutotype = false;
                                }
                            }
                            else
                            {
                                Autotyped?.Invoke(scannedText);
                                IsAutotype = false;
                            }
                        }
                        else
                        {
                            ShowScannedCodeInfo(results[0].BarcodeFormat, scannedText);
                        }
                    }
                }
                else
                {
                    MultipleScanWindow msw = new MultipleScanWindow(results, LocUtil.GetCurrentCultureName(this));
                    msw.Owner = Application.Current.MainWindow;
                    msw.ShowDialog();
                    if (msw.Result != null)
                    {
                        saveInClipboard(msw.Result.Text);

                        if (IsAutotype)
                        {
                            if (Properties.Settings.Default.ConfirmAutotype)
                            {
                                string text = LocUtil.TranslatedString("CodeScannedMessage0") + " " + msw.Result.Format.ToString() + " " + LocUtil.TranslatedString("CodeScannedMessage1", this) + " \"" + msw.Result.Text + "\".\n\n" + LocUtil.TranslatedString("CodeScannedMessage2", this) + "\n\n" + LocUtil.TranslatedString("CodeScannedMessage3", this);
                                MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                                          LocUtil.TranslatedString("CodeScannedTitle", this),
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                                if (messageBoxResult == MessageBoxResult.Yes)
                                {
                                    Autotyped?.Invoke(msw.Result.Text);
                                    IsAutotype = false;
                                }
                            }
                            else
                            {
                                Autotyped?.Invoke(msw.Result.Text);
                                IsAutotype = false;
                            }
                        }
                        else
                        {
                            ShowScannedCodeInfo(msw.Result.Format, msw.Result.Text);
                        }
                    }
                }

                startCameraStream();
            }
        }

        private void ShowScannedCodeInfo(BarcodeFormat format, string code)
        {
            string text = LocUtil.TranslatedString("CodeScannedMessage0") + " " + format.ToString() + " " + LocUtil.TranslatedString("CodeScannedMessage1", this) + " \"" + code + "\".\n\n" + LocUtil.TranslatedString("CodeScannedMessage2", this);
            Uri uriResult;
            bool UriParseResult = Uri.TryCreate(code, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (UriParseResult)
            {
                text += "\n\n" + LocUtil.TranslatedString("CodeScannedMessage4", this);
                MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                          LocUtil.TranslatedString("CodeScannedTitle2", this),
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.Yes)
                    URLOpened?.Invoke(code);
            }
            else
            {
                MessageBox.Show(text, LocUtil.TranslatedString("CodeScannedTitle2", this), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void saveInClipboard(string text)
        {
            if (!string.IsNullOrEmpty(text))
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

        private void ButtonCopy_Click(object sender, RoutedEventArgs e)
        {
            saveInClipboard(Text_Scan.Text);
        }

        private void ButtonVCard_Click(object sender, RoutedEventArgs e)
        {
            if (Text_Scan.Text == "VCARD")
            {
                VCardTemplate vcardTemplate = new VCardTemplate(LocUtil.GetCurrentCultureName(this), true);
                vcardTemplate.Owner = Application.Current.MainWindow;
                vcardTemplate.SetVCardText(CurrentVCard);
                vcardTemplate.ShowDialog();
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("NotValidVCARDErrorMessage", this), LocUtil.TranslatedString("NotValidVCARDErrorTitle", this), MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
