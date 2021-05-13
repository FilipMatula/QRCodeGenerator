using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        public App()
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool start_minimized = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/StartMinimized")
                {
                    start_minimized = true;
                }
            }

            Application.Current.Properties.Add("Start_Minimized", start_minimized);
        }
    }
}
