﻿using AForge.Video.DirectShow;
using QRCodeScannerGenerator.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace QRCodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region GeneralVariables
        DispatcherTimer scanDevices;
        System.Windows.Forms.NotifyIcon notifyIcon;
        WindowState storedWindowState = WindowState.Normal;
        bool forceClosing = false;
        #endregion

        #region ScanPageVariables
       
        bool showNoDeviceError = false;
        bool startUp = true;
        long millisecondsFromLastScan = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        #endregion

        #region HooksAndAutotype
        KeyboardHook hook = new KeyboardHook();
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
            StateChanged += MainWindow_StateChanged;
            IsVisibleChanged += MainWindow_IsVisibleChanged;
            ScanWidget.URLOpened += openURL;
            ScanWidget.Autotyped += ScanWidget_Autotyped;

            // Initialize picture boxes
            InitializePictureBoxes();

            // Initialize timers
            InitializeTimers();

            // Initialize tray
            InitializeTray();

            // Register hotkey
            RegisterHotkeys();
        }

        private void ScanWidget_Autotyped(string obj)
        {
            HideToTray();
            Autotype(obj);
        }

        private void InitializePictureBoxes()
        {
            ScanWidget.InitializePictureBoxes();
            GenerateWidget.InitializePictureBoxes();
        }

        private void InitializeTimers()
        {
            scanDevices = new DispatcherTimer();
            scanDevices.Tick += ScanDevices_Tick;
            scanDevices.Interval = new TimeSpan(0, 0, 1);
            scanDevices.Start();

            ScanWidget.InitializeTimers();
        }

        private void InitializeComboboxes()
        {
            ScanWidget.InitializeComboboxes();
            GenerateWidget.InitializeComboboxes();
            SettingsWidget.InitializeComboboxes();
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
            SettingsWidget.InitializeCheckboxes();
        }

        private void RegisterHotkeys()
        {
            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hotkey_Pressed);
            hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.D);
        }

        private void hotkey_Pressed(object sender, KeyPressedEventArgs e)
        {
            ShowFromTray();
            ScanWidget.IsAutotype = true;
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
            if (WindowState == WindowState.Minimized && SettingsWidget.HideToTrayOnMinimize)
                HideToTray();
            else
                storedWindowState = WindowState;
        }

        // Stop camera on closing
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ScanWidget.stopCameraStream();
            if (!SettingsWidget.HideToTrayOnClose || forceClosing)
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

        // Timer for signal reveiving about new devices
        private void ScanDevices_Tick(object sender, EventArgs e)
        {
            long currentMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if ((currentMilliseconds - millisecondsFromLastScan) < 3000)
                return;

            FilterInfoCollection newfilterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (ScanWidget.FilterInfoCollection.Count == newfilterInfoCollection.Count)
                return;
            else
            {
                bool found = false;
                if (ScanWidget.CurrectDevice != null)
                {
                    foreach (FilterInfo filterInfo in newfilterInfoCollection)
                    {
                        if (ScanWidget.CurrectDevice.MonikerString == filterInfo.MonikerString)
                            found = true;
                    }
                }

                LoadDevices(found);
            }
        }

        // When window is activate start stream
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            if (ScanWidget.IsVisible)
                ScanWidget.startCameraStream();
        }

        // When window is not activeate stop stream
        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            ScanWidget.stopCameraStream();
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
                Process.Start(SettingsWidget.BrowserPath, URL);
        }

        // Show proper grid on type changed
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListViewMenu.SelectedIndex;
            MoveCursor(index);
            switch (index)
            {
                case 0:
                    ScanWidget.Visibility = Visibility.Visible;
                    GenerateWidget.Visibility = Visibility.Hidden;
                    SettingsWidget.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    ScanWidget.Visibility = Visibility.Hidden;
                    GenerateWidget.Visibility = Visibility.Visible;
                    SettingsWidget.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    ScanWidget.Visibility = Visibility.Hidden;
                    GenerateWidget.Visibility = Visibility.Hidden;
                    SettingsWidget.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
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
            ScanWidget.stopCameraStream();
            if (!startUp)
            {
                MessageBox.Show(LocUtil.TranslatedString("DeviceChangedMessage", this), LocUtil.TranslatedString("DeviceChangedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
                Thread.Sleep(2000);
            }

            if (!keepCurrent)
                ScanWidget.CurrectDevice = null;
            ScanWidget.comboBox_Devices.Items.Clear();

            ScanWidget.FilterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (ScanWidget.FilterInfoCollection.Count == 0)
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
                foreach (FilterInfo filterInfo in ScanWidget.FilterInfoCollection)
                    ScanWidget.comboBox_Devices.Items.Add(filterInfo.Name);

                if (keepCurrent)
                    ScanWidget.comboBox_Devices.SelectedItem = ScanWidget.CurrectDevice.Name;
                else
                    ScanWidget.comboBox_Devices.SelectedIndex = 0;
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
