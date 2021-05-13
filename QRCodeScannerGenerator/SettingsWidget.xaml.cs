using QRCodeScannerGenerator.Common;
using QRCodeScannerGenerator.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QRCodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for SettingsWidget.xaml
    /// </summary>
    public partial class SettingsWidget : UserControl
    {
        private List<Browser> browsers;
        public string BrowserPath { get { return browsers[comboBox_Browsers.SelectedIndex].Path; } }
        public bool HideToTrayOnMinimize { get { return (bool)checkBox_HideToTrayOnMinimize.IsChecked; } }
        public bool HideToTrayOnClose { get { return (bool)checkBox_HideToTrayOnClose.IsChecked; } }

        public SettingsWidget()
        {
            InitializeComponent();
        }

        public void InitializeComboboxes()
        {
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

        public void InitializeCheckboxes()
        {
            checkBox_HideToTrayOnMinimize.IsChecked = Properties.Settings.Default.HideOnMinimize;
            checkBox_HideToTrayOnClose.IsChecked = Properties.Settings.Default.HideOnClose;
        }

        // Save browser user settings
        private void comboBox_Browsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.Browser = comboBox_Browsers.SelectedItem.ToString();
            Properties.Settings.Default.Save();
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
