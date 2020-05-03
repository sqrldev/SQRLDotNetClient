using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using SQRLDotNetClientUI.IPC;
using Serilog;
using System.IO;
using System.Reflection;
using SQRLDotNetClientUI.Views;
using Avalonia.Dialogs;
using SQRLDotNetClientUI.DB.DBContext;
using Microsoft.EntityFrameworkCore;
using SQRLUtilsLib;

namespace SQRLDotNetClientUI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break. 
        public static void Main(string[] args)
        {
            const string mutexId = @"Global\{{83cfa3fa-72bd-4903-9b9d-ba90f7f6ba7f}}";
            Thread ipcThread = new Thread(StartIPCServer);

            // Set up logging
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logFilePath = Path.Combine(currentDir, "log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("New app instance is being launched on {OSDescription}", 
                RuntimeInformation.OSDescription);

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Log.Information($"Client version: {version.ToString()}");

            // Try to detect an existing instance of our app
            using (var mutex = new Mutex(false, mutexId, out bool created))
            {
                bool hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = mutex.WaitOne(500, false);
                        if(!hasHandle)
                        {
                            // Existing instance detected, forward the first 
                            // command line argument if present.
                            Log.Information("Existing app instance detected, forwarding data and shutting down");
                            ForwardToExistingInstance(args.Length > 0 ? args[0] : IPCServer.MAGIC_WAKEUP_STR);
                            Environment.Exit(1);
                        }
                    }
                    catch(AbandonedMutexException)
                    {
                        hasHandle = true;
                    }


                    // Adds event to handle abrupt program exits and mitigate CPS
                    AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);



                    // Perform database migrations
                    SQRLDBContext _db = new SQRLDBContext();
                    _db.Database.Migrate();

                    // No existing instance of the app running,
                    // so start the IPC server and run the app
                    ipcThread.Start();
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
                }
                finally
                {
                    Log.Information("App shutting down");

                    if (hasHandle)
                    {
                        mutex.ReleaseMutex();
                    }

                    HandleAbruptCPS();



                    //Remove the notify icon
                    (App.Current as App).NotifyIcon?.Remove();

                    if (ipcThread.IsAlive)
                    {
                        // Force close the app without waiting 
                        // for any threads to finish.
                        Log.Information("Forcing exit because of IPC thread still running.");
                        Environment.Exit(1);
                    }
                }
            }
        }


        /// <summary>
        /// Try to capture abrupt process exit to gracefully handle CPS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // One of the last ditch efforts at gracefully handling CPS, note there may be no localization here we are at this point throwing a hail marry
            HandleAbruptCPS();

        }

        /// <summary>
        /// Handles unclean process exit tries to save CPS
        /// </summary>
        private static void HandleAbruptCPS()
        {
            try
            {
                Log.Information("Attempting to End CPS Gracefully from Process Exit Event");
                var _loc = (App.Current as App)?.Localization;
                if (_loc != null)
                {
                    SQRLCPSServer.HandlePendingCPS(_loc.GetLocalizationValue("CPSAbortHeader"),
                                                   _loc.GetLocalizationValue("CPSAbortMessage"),
                                                   _loc.GetLocalizationValue("CPSAbortLinkText"));
                }
                else
                    SQRLCPSServer.HandlePendingCPS();
            }
            catch (Exception ex)
            {
                Log.Error("Failed to Cancel CPS Gracefully", ex);
            }
        }


        /// <summary>
        /// Starts the IPC server and also starts listening for incoming IPC queries.
        /// </summary>
        /// <param name="obj">Not used.</param>
        private static void StartIPCServer(object obj)
        {
            IPCServer nps = new IPCServer("127.0.0.1", 13000);
            nps.StartListening();
        }

        /// <summary>
        /// Forwards the given string to the existing app instance by estblishing
        /// a TCP connection to the existing instance's IPC server and sending the
        /// given data over that connection.
        /// </summary>
        /// <param name="url">The data to send to the exisnting app instance.</param>
        private static void ForwardToExistingInstance(string url)
        {
            try
            {
                Int32 port = 13000;
                TcpClient client = new TcpClient("127.0.0.1", port);
                NetworkStream stream = client.GetStream();

                // Translate the message into ASCII.
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(url);

                // Send the message to the connected TCP server. 
                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) })
                .LogToDebug()
                .UseReactiveUI()
                .UseManagedSystemDialogs(); //It is recommended by Avalonia Developers that we use Managed System Dialogs instead  of the native ones particularly for Linux
    }
}
