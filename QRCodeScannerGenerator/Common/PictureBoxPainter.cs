using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace QRCodeScannerGenerator.Common
{
    public partial class PictureBoxPainter
    {
        public static Rectangle PaintRectangle(System.Windows.Forms.PictureBox pictureBox, System.Windows.Forms.PaintEventArgs e, bool lowerRectangle)
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            int backgroundAlphaOpacity = 150;

            int square = (int)(pictureBox.Width / 3.0);
            int maxHeight = (int)(0.9 * pictureBox.Height);
            int squarePx = square <= maxHeight ? square : maxHeight;
            int width = squarePx;
            int height = squarePx;
            if (lowerRectangle)
            {
                height = (int)(width / 3.0);
            }

            Rectangle scanArea = new Rectangle(pictureBox.Width / 2 - width / 2, pictureBox.Height / 2 - height / 2, width, height);
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, scanArea);
            }
            Color newColor = Color.FromArgb(backgroundAlphaOpacity, Color.Black);
            Rectangle topRect = new Rectangle(0, 0, pictureBox.Width, scanArea.Top);
            e.Graphics.FillRectangle(new SolidBrush(newColor), topRect);

            Rectangle leftRect = new Rectangle(0, scanArea.Top, scanArea.Left, scanArea.Height);
            e.Graphics.FillRectangle(new SolidBrush(newColor), leftRect);

            Rectangle rightRect = new Rectangle(scanArea.Right, scanArea.Top, pictureBox.Width - scanArea.Width, scanArea.Height);
            e.Graphics.FillRectangle(new SolidBrush(newColor), rightRect);

            Rectangle bottomRect = new Rectangle(0, scanArea.Bottom, pictureBox.Width, pictureBox.Height - scanArea.Bottom);
            e.Graphics.FillRectangle(new SolidBrush(newColor), bottomRect);

            return scanArea;
        }

        public static void PaintCircle(System.Windows.Forms.PaintEventArgs e, Point pt, int outerDiameter, int innerDiameter)
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            using (Pen pen = new Pen(Color.Orange, 1))
            {
                e.Graphics.DrawEllipse(pen, pt.X - outerDiameter / 2, pt.Y - outerDiameter / 2, outerDiameter, outerDiameter);
                e.Graphics.DrawEllipse(pen, pt.X - innerDiameter / 2, pt.Y - innerDiameter / 2, innerDiameter, innerDiameter);
            }
        }
    }
}
