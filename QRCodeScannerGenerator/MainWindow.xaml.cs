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
using System.Threading.Tasks;
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
        #region GeneralVariables
        FilterInfoCollection filterInfoCollection;
        FilterInfo currectDevice = null;
        VideoCaptureDevice captureDevice;
        DispatcherTimer scanDevices;
        List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX, BarcodeFormat.CODE_128 };
        System.Windows.Forms.NotifyIcon notifyIcon;
        WindowState storedWindowState = WindowState.Normal;
        bool forceClosing = false;
        #endregion

        #region ScanPageVariables
        System.Windows.Forms.PictureBox pictureBoxScan;
        DispatcherTimer scanCodeTimer;
        bool showNoDeviceError = false;
        bool startUp = true;
        bool blockScanning = false;
        long millisecondsFromLastScan = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        Rectangle scanRect;
        bool lowerRectangle = false;
        #endregion

        #region FocusCircle
        DispatcherTimer focusCircleTimer;
        int currentOuterDiameter;
        int currentInnerDiameter;
        int focusCircleOuterDiameter = 50;
        int focusCircleOuterDiameterBounce = 5;
        int focusCircleInnerDiameter = 10;
        int focusCircleInnerDiameterBounce = 5;
        bool raising = true;
        bool moving = true;
        long focusCircleStart = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        System.Drawing.Point focusPt;
        #endregion

        #region GeneratePageVariables
        System.Windows.Forms.PictureBox pictureBoxGenerate;
        #endregion

        #region SettingsPageVariables
        List<Browser> browsers;
        #endregion

        #region HooksAndAutotype
        KeyboardHook hook = new KeyboardHook();
        bool autotype = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            // Language initialization
            LocUtil.SetDefaultLanguage(this);
            OnLanguageChanged(LocUtil.GetCurrentCultureName(this));

            // Overwrite main events
            Closing += MainWindow_Closing;
            Deactivated += MainWindow_Deactivated;
            Activated += MainWindow_Activated;
            ScanQR.IsVisibleChanged += ScanQR_IsVisibleChanged;
            StateChanged += MainWindow_StateChanged;
            IsVisibleChanged += MainWindow_IsVisibleChanged;

            // Initialize picture boxes
            InitializePictureBoxes();

            // Initialize timers
            InitializeTimers();

            // Initialize focus circle
            resetFocusCircleDimensions();

            // Initialize tray
            InitializeTray();

            // Register hotkey
            RegisterHotkeys();
        }

        private void InitializePictureBoxes()
        {
            pictureBoxScan = new System.Windows.Forms.PictureBox();
            pictureBoxScan.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBoxScan.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            pictureBoxScan.Paint += new System.Windows.Forms.PaintEventHandler(pictureBoxScan_Paint);
            pictureBoxScan.Click += PictureBoxScan_Click;
            windowsFormsHost1.Child = pictureBoxScan;

            pictureBoxGenerate = new System.Windows.Forms.PictureBox();
            pictureBoxGenerate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            pictureBoxGenerate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            windowsFormsHost2.Child = pictureBoxGenerate;
        }

        private void InitializeTimers()
        {
            scanCodeTimer = new DispatcherTimer();
            scanCodeTimer.Tick += ScanCodeTimer_Tick;
            scanCodeTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            scanDevices = new DispatcherTimer();
            scanDevices.Tick += ScanDevices_Tick;
            scanDevices.Interval = new TimeSpan(0, 0, 1);
            scanDevices.Start();

            focusCircleTimer = new DispatcherTimer();
            focusCircleTimer.Tick += FocusCircleTimer_Tick;
            focusCircleTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
        }

        private void InitializeComboboxes()
        {
            // Type for scan combobox
            comboBox_Scan_Type.Items.Add(LocUtil.TranslatedString("Auto", this));
            foreach (BarcodeFormat format in possibleFormats)
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

            // Type for generate combobox
            foreach (BarcodeFormat format in possibleFormats)
                comboBox_Generate_Type.Items.Add(format);
            string savedGenerateTypeName = Properties.Settings.Default.Generate_code;
            if (string.IsNullOrEmpty(savedGenerateTypeName))
                comboBox_Generate_Type.SelectedIndex = 0;
            else
            {
                BarcodeFormat read_format;
                if (Enum.TryParse(savedGenerateTypeName, out read_format))
                    comboBox_Generate_Type.SelectedItem = read_format;
                else
                    comboBox_Generate_Type.SelectedIndex = 0;
            }

            // Browsers combobox
            browsers = BrowserControl.GetBrowsers();
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

        private void InitializeTray()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipText = "The app has been minimised. Double click the tray icon to show.";
            notifyIcon.BalloonTipTitle = "QR Code Scanner";
            notifyIcon.Text = "QR Code Scanner";
            notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/images/qrcode.ico")).Stream);
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;           
            notifyIcon.ContextMenu = createTrayMenu();
        }

        private System.Windows.Forms.ContextMenu createTrayMenu()
        {
            System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem exitMenuItem = new System.Windows.Forms.MenuItem();
            exitMenuItem.Text = "Exit";
            exitMenuItem.Click += ExitMenuItem_Click;
            System.Windows.Forms.MenuItem showMenuItem = new System.Windows.Forms.MenuItem();
            showMenuItem.Text = "Show";
            showMenuItem.Click += ShowMenuItem_Click;
            contextMenu.MenuItems.Add(showMenuItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(exitMenuItem);

            return contextMenu;
        }

        private void InitializeCheckboxes()
        {
            checkBox_HideToTrayOnMinimize.IsChecked = Properties.Settings.Default.HideOnMinimize;
            checkBox_HideToTrayOnClose.IsChecked = Properties.Settings.Default.HideOnClose;
        }

        private void RegisterHotkeys()
        {
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hotkey_Pressed);
            hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.D);
        }

        private void hotkey_Pressed(object sender, KeyPressedEventArgs e)
        {
            ShowFromTray();
            autotype = true;
        }

        private void ShowMenuItem_Click(object sender, EventArgs e)
        {
            ShowFromTray();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            forceClosing = true;
            Application.Current.Shutdown();
        }

        private void HideToTray()
        {
            Hide();
            if (notifyIcon != null)
                notifyIcon.ShowBalloonTip(2000);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = storedWindowState;
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (notifyIcon != null)
                notifyIcon.Visible = !IsVisible;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && (bool)checkBox_HideToTrayOnMinimize.IsChecked)
                HideToTray();
            else
                storedWindowState = WindowState;
        }

        // Stop camera on closing
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
            if (!(bool)checkBox_HideToTrayOnClose.IsChecked || forceClosing)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            else
            {
                e.Cancel = true;
                HideToTray();
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowFromTray();
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

        private void FocusCircleTimer_Tick(object sender, EventArgs e)
        {
            pictureBoxScan.Update();
            currentOuterDiameter = calculateDiameter(currentOuterDiameter, focusCircleOuterDiameter, focusCircleOuterDiameterBounce);
            currentInnerDiameter = calculateDiameter(currentInnerDiameter, focusCircleInnerDiameter, focusCircleInnerDiameterBounce);
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
                _ = Task.Run(() => CameraControl.autoFocus(captureDevice, currectDevice));
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

        // Timer for signal reveiving about new devices
        private void ScanDevices_Tick(object sender, EventArgs e)
        {
            long currentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if ((currentMilliseconds - millisecondsFromLastScan) < 3000)
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
                CameraControl.startCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
        }

        // When window is not activeate stop stream
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
        }

        // Change selection visibility marker
        private void MoveCursor(int index)
        {
            TransitionContentSlide.OnApplyTemplate();
            TransitionGrid.Margin = new Thickness(0, index * 60 + 150, 0, 0);
        }

        // Open URL from readed qrcode
        private void openURL(string URL)
        {
            Uri uriResult;
            bool UriParseResult = Uri.TryCreate(URL, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (UriParseResult)
                Process.Start(browsers[comboBox_Browsers.SelectedIndex].Path, URL);
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
            CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);

            initializeStream();

            CameraControl.startCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
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

        private void comboBox_Generate_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Generate_code = comboBox_Generate_Type.SelectedItem.ToString();
            Properties.Settings.Default.Save();
        }

        private Bitmap CropImage(System.Drawing.Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, PixelFormat.Format24bppRgb);
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
            reader.Options = new DecodingOptions { 
                PossibleFormats = comboBox_Scan_Type.SelectedIndex == 0 ? possibleFormats : new List<BarcodeFormat>() { (BarcodeFormat)comboBox_Scan_Type.SelectedItem }
            };

            var result = reader.Decode(bitmap);
            //Result [] result = reader.DecodeMultiple(bitmap);
            if (result != null && !blockScanning)
            {
                CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
                string scannedText = result.ToString();
                Text_Scan.Text = scannedText;
                saveInClipboard(scannedText);

                string text = LocUtil.TranslatedString("QRCodeScannedMessage", this) + " " + scannedText + ".\n" + "Text was copied to clipboard.";

                if (autotype)
                {
                    text += "\n" + LocUtil.TranslatedString("Do you want to autofill field with this text?", this);
                    MessageBoxResult messageBoxResult = MessageBox.Show(text,
                                              LocUtil.TranslatedString("QRCodeScannedTitle", this),
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        HideToTray();
                        Autotype(scannedText);
                    }
                    autotype = false;
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
                            openURL(scannedText);
                    }
                    else
                    {
                        MessageBox.Show(text, LocUtil.TranslatedString("QRCodeScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                blockScanning = true;
                Thread.Sleep(1000);
                CameraControl.startCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
            }
        }

        private void Autotype(string text)
        {
            foreach(char c in text)
            {
                System.Windows.Forms.SendKeys.SendWait(c.ToSendKeyNormalize());
                System.Windows.Forms.SendKeys.Flush();
                Thread.Sleep(20);
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

        // Stop / Start camera on mode changed
        private void ScanQR_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!ScanQR.IsVisible)
                CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
            else
                CameraControl.startCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
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
                    openURL(Text_Scan.Text);
                else
                    MessageBox.Show(LocUtil.TranslatedString("IncorrectURIMessage", this), LocUtil.TranslatedString("IncorrectURITitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("NoQRScannedMessage", this), LocUtil.TranslatedString("NoQRScannedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Generate QR Code
        private void GenerateQRCode()
        {
            if (!string.IsNullOrEmpty(QRText_Generate.Text))
            {
                BarcodeFormat format = (BarcodeFormat)comboBox_Generate_Type.SelectedItem;
                BarcodeWriter barcodeWriter = new BarcodeWriter();
                QrCodeEncodingOptions options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = 300,
                    Height = format == BarcodeFormat.CODE_128 ? 100 : 300,
                };
                barcodeWriter.Options = options;
                barcodeWriter.Format = (BarcodeFormat)comboBox_Generate_Type.SelectedItem;
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
            CameraControl.stopCameraStream(ref captureDevice, currectDevice, scanCodeTimer);
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

            // Initialize comboboxs
            InitializeComboboxes();

            // Initialize checkboxes
            InitializeCheckboxes();
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
                millisecondsFromLastScan = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
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

        private void checkBox_HideToTrayOnMinimize_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HideOnMinimize = (bool)checkBox_HideToTrayOnMinimize.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void checkBox_HideToTrayOnClose_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HideOnClose = (bool)checkBox_HideToTrayOnClose.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}
