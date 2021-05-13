using QRCodeScannerGenerator.Common;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZXing;
using ZXing.QrCode;

namespace QRCodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for GenerateWidget.xaml
    /// </summary>
    public partial class GenerateWidget : UserControl
    {
        private System.Windows.Forms.PictureBox pictureBoxGenerate;
        public int CodeWidth { get { return Int32.Parse(Width_Generate.Text); } }
        public int CodeHeight { get { return Int32.Parse(Height_Generate.Text); } }

        public GenerateWidget()
        {
            InitializeComponent();

            InitializeTextboxes();
        }

        public void InitializePictureBoxes()
        {
            pictureBoxGenerate = new System.Windows.Forms.PictureBox();
            pictureBoxGenerate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            pictureBoxGenerate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            windowsFormsHost2.Child = pictureBoxGenerate;

            KeyDown += GenerateWidget_KeyDown;
        }

        private void InitializeTextboxes()
        {
            Width_Generate.Text = Properties.Settings.Default.GenerateWidth.ToString();
            Height_Generate.Text = Properties.Settings.Default.GenerateHeight.ToString();
        }

        private void GenerateWidget_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
                Save();
        }

        public void InitializeComboboxes()
        {
            // Type for generate combobox
            foreach (BarcodeFormat format in Constants.PossibleFormats)
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

            UpdateSizeVisibility();
        }

        private void comboBox_Generate_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Generate_code = comboBox_Generate_Type.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            UpdateSizeVisibility();

            if (!string.IsNullOrEmpty(Text_Generate.Text))
                GenerateCode();
        }

        private void UpdateSizeVisibility()
        {
            HeightColumn.Visibility = (BarcodeFormat)comboBox_Generate_Type.SelectedItem == BarcodeFormat.CODE_128 ? Visibility.Visible : Visibility.Hidden;
            HeightColumnDefinition.Width = HeightColumn.IsVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
        }

        // Generate Code
        private void GenerateCode()
        {
            if (!string.IsNullOrEmpty(Text_Generate.Text))
            {
                BarcodeFormat format = (BarcodeFormat)comboBox_Generate_Type.SelectedItem;
                BarcodeWriter barcodeWriter = new BarcodeWriter();
                QrCodeEncodingOptions options = new QrCodeEncodingOptions
                {
                    DisableECI = true,
                    CharacterSet = "UTF-8",
                    Width = CodeWidth,
                    Height = (BarcodeFormat)comboBox_Generate_Type.SelectedItem == BarcodeFormat.CODE_128 ? CodeHeight : CodeWidth,
                };
                barcodeWriter.Options = options;
                barcodeWriter.Format = (BarcodeFormat)comboBox_Generate_Type.SelectedItem;
                var result = new Bitmap(barcodeWriter.Write(Text_Generate.Text.Trim()));
                pictureBoxGenerate.Image = result;
            }
            else
                MessageBox.Show(LocUtil.TranslatedString("EmptyTextMessage", this), LocUtil.TranslatedString("EmptyTextTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Generate code
        private void ButtonGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateCode();
        }

        // Save code
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
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
                MessageBox.Show(LocUtil.TranslatedString("NoGeneratedMessage", this), LocUtil.TranslatedString("NoGeneratedTitle", this), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // On enter pressed generate Code
        private void Text_Generate_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                GenerateCode();
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]");
            return reg.IsMatch(str);
        }

        private void Width_Generate_KeyUp(object sender, KeyEventArgs e)
        {
            Properties.Settings.Default.GenerateWidth = CodeWidth;
            Properties.Settings.Default.Save();
        }

        private void Height_Generate_KeyUp(object sender, KeyEventArgs e)
        {
            Properties.Settings.Default.GenerateHeight = CodeHeight;
            Properties.Settings.Default.Save();
        }
    }
}
