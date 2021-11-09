using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        private const string PIPE_NAME = "MutexQRCodePipe"; // Name of pipe

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

            bool scan_now = false;
            bool scan_web = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i] == "/ScanNow" || e.Args[i] == "qrcode://scannow/")
                {
                    scan_now = true;
                }
                else if (e.Args[i] == "/ScanWeb" || e.Args[i] == "qrcode://scanweb/")
                {
                    scan_web = true;
                }
            }

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                var thread = new Thread(
                    () =>
                    {
                        while (_eventWaitHandle.WaitOne())
                        {
                            // Start pipe server
                            NamedPipeServerStream pipeStream = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances); // Create Server pipe and listen for clients
                            pipeStream.WaitForConnection(); // Wait for client connection

                            using (StreamReader reader = new StreamReader(pipeStream)) // Read message from pipe stream
                            {
                                // Read message from pipe
                                string message = reader.ReadLine();

                                if (message == "ScanNow")
                                {
                                    Current.Dispatcher.BeginInvoke(
                                        (Action)(() => ((MainWindow)Current.MainWindow).setAutotype()));
                                }
                                else if (message == "ScanWeb")
                                {
                                    Current.Dispatcher.BeginInvoke(
                                        (Action)(() => ((MainWindow)Current.MainWindow).setAutotype(true)));
                                }
                                else
                                {
                                    Current.Dispatcher.BeginInvoke(
                                        (Action)(() => ((MainWindow)Current.MainWindow).ShowFromTray()));
                                }
                            }
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
                Application.Current.Properties.Add("Scan_Now", scan_now);
                Application.Current.Properties.Add("Scan_Web", scan_web);
                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            using (NamedPipeClientStream client = new NamedPipeClientStream(PIPE_NAME)) // Create connection to pipe
            {
                _eventWaitHandle.Set();
                client.Connect(5000); // Maximum wait 5 seconds
                using (StreamWriter writer = new StreamWriter(client))
                {
                    if (scan_now == true)
                        writer.WriteLine("ScanNow");
                    else if (scan_web == true)
                        writer.WriteLine("ScanWeb");
                    else
                        writer.WriteLine("");
                    //writer.WriteLine(scan_now ? "ScanNow" : "");
                    //if (scan_now)
                    //    writer.WriteLine("ScanNow"); // Write command line parameter to the first instance
                    //else if (scan_web)
                    //    writer.WriteLine("ScanWeb");
                    //else
                    //    writer.WriteLine("");
                }
            }
            //_eventWaitHandle.Set();

            // Terminate this instance.
            Application.Current.Properties.Add("Start_Minimized", false);
            Application.Current.Properties.Add("Scan_Now", false);
            Application.Current.Properties.Add("Scan_Web", false);
            Application.Current.Shutdown();
        }
    }
}
