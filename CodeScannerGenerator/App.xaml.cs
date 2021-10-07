using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        #region Constants and Fields

        /// <summary>The event mutex name.</summary>
        private const string UniqueEventName = "e2cd57fb-dc71-4a59-bed6-f37e7e89724a";

        /// <summary>The unique mutex name.</summary>
        private const string UniqueMutexName = "1004f9fc-c318-47e4-8b5b-55228322a9b0";

        /// <summary>The event wait handle.</summary>
        private EventWaitHandle _eventWaitHandle;

        /// <summary>The mutex.</summary>
        private Mutex _mutex;

        #endregion

        public App()
        {

        }

        // https://stackoverflow.com/questions/14506406/wpf-single-instance-best-practices
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool isOwned;
            _mutex = new Mutex(true, UniqueMutexName, out isOwned);
            _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            GC.KeepAlive(_mutex);

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (_eventWaitHandle.WaitOne())
                        {
                            Current.Dispatcher.BeginInvoke(
                                (Action)(() => ((MainWindow)Current.MainWindow).ShowFromTray()));
                        }
                    });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;
                bool start_minimized = false;
                for (int i = 0; i != e.Args.Length; ++i)
                {
                    if (e.Args[i] == "/StartMinimized")
                    {
                        start_minimized = true;
                    }
                }

                Application.Current.Properties.Add("Start_Minimized", start_minimized);
                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            _eventWaitHandle.Set();

            // Terminate this instance.
            Application.Current.Properties.Add("Start_Minimized", false);
            Application.Current.Shutdown();
        }
    }
}
