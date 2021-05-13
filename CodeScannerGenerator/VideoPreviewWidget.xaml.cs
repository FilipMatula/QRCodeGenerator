using CodeScannerGenerator.Common;
using System;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for VideoPreviewWidget.xaml
    /// </summary>
    public partial class VideoPreviewWidget : UserControl
    {
        private System.Windows.Forms.PictureBox pictureBoxScan;

        public bool LowerRectangle { get; set; } = false;
        public Rectangle ScanRect { get; set; }

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

        public event Action Autofocused;

        public VideoPreviewWidget()
        {
            InitializeComponent();

            // Initialize focus circle
            resetFocusCircleDimensions();
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

        public void InitializeTimers()
        {
            focusCircleTimer = new DispatcherTimer();
            focusCircleTimer.Tick += FocusCircleTimer_Tick;
            focusCircleTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
        }

        public void Update()
        {
            pictureBoxScan.Update();
        }

        public Bitmap GetImage()
        {
            return (Bitmap)pictureBoxScan.Image;
        }

        public void SetImage(Bitmap bitmap)
        {
            pictureBoxScan.Image = bitmap;
        }

        public double GetWidgetWidth()
        {
            return pictureBoxScan.Width;
        }

        public double GetWidgetHeight()
        {
            return pictureBoxScan.Height;
        }

        // Painting rectangles on the picturebox
        private void pictureBoxScan_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            ScanRect = PictureBoxPainter.PaintRectangle(pictureBoxScan, e, LowerRectangle);

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
            if (ScanRect.Contains(pt))
            {
                focusPt = pt;
                focusCircleStart = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                resetFocusCircleDimensions();
                focusCircleTimer.Start();
                Autofocused?.Invoke();
            }
        }

        private void FocusCircleTimer_Tick(object sender, EventArgs e)
        {
            pictureBoxScan.Update();
            currentOuterDiameter = calculateDiameter(currentOuterDiameter, focusCircleOuterDiameter, focusCircleOuterDiameterBounce);
            currentInnerDiameter = calculateDiameter(currentInnerDiameter, focusCircleInnerDiameter, focusCircleInnerDiameterBounce);
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

    }
}
