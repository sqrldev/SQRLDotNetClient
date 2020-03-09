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
using SQRLDotNetClientUI.Platform.Win;

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
            NotifyIconWin32 trayIcon = new NotifyIconWin32();
            trayIcon.IconPath = @"C:\Users\Alex\Desktop\test.ico";
            trayIcon.Show();

            // Set up logging
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logFilePath = Path.Combine(currentDir, "log.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("New app instance is being launched on {Platform}", RuntimeInformation.OSDescription);

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

                    // No existing instance of the app running,
                    // so start the IPC server and run the app
                    ipcThread.Start();
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

                    // Remove the tray icon
                    trayIcon.Remove();

                    Log.Information("App shutting down");
                }
                finally
                {
                    if (hasHandle)
                    {
                        mutex.ReleaseMutex();
                    }
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
                .UseReactiveUI();
    }
}
