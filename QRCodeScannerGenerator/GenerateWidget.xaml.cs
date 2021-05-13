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
        public GenerateWidget()
        {
            InitializeComponent();
        }

        public void InitializePictureBoxes()
        {
            pictureBoxGenerate = new System.Windows.Forms.PictureBox();
            pictureBoxGenerate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            pictureBoxGenerate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            windowsFormsHost2.Child = pictureBoxGenerate;
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
        }

        private void comboBox_Generate_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Generate_code = comboBox_Generate_Type.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(Text_Generate.Text))
                GenerateCode();
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
                    Width = 300,
                    Height = format == BarcodeFormat.CODE_128 ? 100 : 300,
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
    }
}
