using System;
using System.Net.Sockets;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using SQRLDotNetClientUI.IPC;

namespace SQRLDotNetClientUI
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            Thread th = new Thread(StartNamePipe);
            const string mutexId = @"Global\{{83cfa3fa-72bd-4903-9b9d-ba90f7f6ba7f}}";
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
                            ForwardToExistingInstance(args.Length>0?args[0]:string.Empty);
                            System.Environment.Exit(1);
                        }
                    }
                    catch(AbandonedMutexException)
                    {
                        hasHandle = true;
                    }

                    th.Start();
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                    
                }
                finally
                {
                    if(hasHandle)
                    {
                        mutex.ReleaseMutex();
                    }
                    if(th.IsAlive)
                    {
                        System.Environment.Exit(1);
                    }
                }
            }
        }

        private static void StartNamePipe(object obj)
        {
            IPCServer nps = new IPCServer("127.0.0.1", 13000);
            nps.StartServer();
        }
       
        private static void ForwardToExistingInstance(string url)
        {
            try
            {
                Int32 port = 13000;
                TcpClient client = new TcpClient("127.0.0.1", port);
                NetworkStream stream = client.GetStream();

                // Translate the Message into ASCII.
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(url);
                // Send the message to the connected TcpServer. 
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
                .LogToDebug()
                .UseReactiveUI();
    }
}
