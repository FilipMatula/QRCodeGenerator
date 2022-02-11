using CodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for FloatingButton.xaml
    /// </summary>
    public partial class FloatingButton : Window
    {
        public FloatingButton()
        {
            InitializeComponent();

            // Language initialization
            LocUtil.SetDefaultLanguage(this);

            ContextMenu = createMenu();
        }


        public void SwitchLanguage(string culture)
        {
            LocUtil.SwitchLanguage(this, culture);
            ContextMenu = createMenu();
        }

        private ContextMenu createMenu()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem hideMenuItem = new MenuItem();
            hideMenuItem.Header = LocUtil.TranslatedString("Hide", this);
            hideMenuItem.Click += HideMenuItem_Click;
            contextMenu.Items.Add(hideMenuItem);

            return contextMenu;
        }

        private void HideMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        private void MoveButton()
        {
            IntPtr handle = GetForegroundWindow();
            ReleaseCapture();
            SendMessage(handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);

            var areaWidth = SystemParameters.VirtualScreenWidth;
            var areaHeight = SystemParameters.VirtualScreenHeight;

            if (Left < 0)
                Left = 0;

            if (Left + Width > areaWidth)
                Left = areaWidth - Width;

            if (Top < 0)
                Top = 0;

            if (Top + Height > areaHeight)
                Top = areaHeight - Height;

            Properties.Settings.Default.FloatingButtonLeft = Left;
            Properties.Settings.Default.FloatingButtonTop = Top;
            Properties.Settings.Default.Save();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton == MouseButton.Left)
                MoveButton();
        }

        

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            //Left = desktopWorkingArea.Right - Width;
            //Top = desktopWorkingArea.Top - Height;
            if (Properties.Settings.Default.FloatingButtonLeft != 0)
                Left = Properties.Settings.Default.FloatingButtonLeft;
            else
                Left = desktopWorkingArea.Width - Width * 2;

            if (Properties.Settings.Default.FloatingButtonTop != 0)
                Top = Properties.Settings.Default.FloatingButtonTop;
            else
                Top = desktopWorkingArea.Height / 3;
        }
    }
}
