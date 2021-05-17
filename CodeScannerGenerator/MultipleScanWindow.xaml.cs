using CodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
using ZXing;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for MultipleScanWindow.xaml
    /// </summary>
    public partial class MultipleScanWindow : Window
    {
        private Result[] results;
        public ScanResult Result { get; set; } = null;
        public MultipleScanWindow(Result[] results, string culture)
        {
            InitializeComponent();

            // Language initialization
            LocUtil.SwitchLanguage(this, culture);

            this.results = results;
            Scan_ListView.ItemsSource = LoadCollectionData();
            Scan_ListView.KeyDown += Scan_ListView_KeyDown;
            Scan_ListView.Loaded += Scan_ListView_Loaded;
        }

        private void Scan_ListView_Loaded(object sender, RoutedEventArgs e)
        {
            Scan_ListView.SelectedItem = Scan_ListView.Items[0];
            Scan_ListView.UpdateLayout(); // Pre-generates item containers 

            var listBoxItem = (ListBoxItem)Scan_ListView
                .ItemContainerGenerator
                .ContainerFromItem(Scan_ListView.SelectedItem);

            listBoxItem.Focus();
        }

        private void Scan_ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Result = (ScanResult)Scan_ListView.SelectedItem;
                Close();
            }
        }

        public class ScanResult
        {
            public BarcodeFormat Format { get; set; }
            public string Text { get; set; }
        }

        private List<ScanResult> LoadCollectionData()
        {
            List<ScanResult> scanResults = new List<ScanResult>();

            foreach (Result result in results)
            {
                scanResults.Add(new ScanResult() { Format = result.BarcodeFormat, Text = result.Text });
            }

            return scanResults;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
