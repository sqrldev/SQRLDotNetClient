using Avalonia;
using SQRLDotNetClientUI.Views;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Serilog;

namespace SQRLDotNetClientUI.IPC
{
    /// <summary>
    /// Handles the inter-process-communication between client instances to ensure 
    /// that only one instance of the client is active at any given point in time. 
    /// If the new instance is started with any command line arguments and an instance
    /// of the app is already running, the first argument will be  forwarded to the 
    /// existing instance. If no command line arguments are present, the magic wakeup 
    /// string will be sent to show/activate the existing instance.
    /// </summary>
    /// <remarks>
    /// The communication between app instances is handled through a TCP connection 
    /// on port 1300.
    /// </remarks>
    public class IPCServer
    {
        private TcpListener _server = null;

        /// <summary>
        /// Creates a new <c>IPCServer</c> instance and starts the TCP server
        /// to listen for incoming IPC queries from other app instances.
        /// </summary>
        /// <param name="ip">The IP address to listen on, usually 127.0.0.1 (localhost).</param>
        /// <param name="port">The TCP port to listen on. Default is 13000.</param>
        public IPCServer(string ip, int port = 13000)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            _server = new TcpListener(localAddr, port);
            _server.Start();
        }

        /// <summary>
        /// Starts listening for incoming IPC queries from other app instances.
        /// </summary>
        public void StartListening()
        {
            try
            {
                Log.Information("IPC server starts listening...");

                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();
                    Log.Information("IPC client connected!");

                    Thread handleIncomingIPCThread = new Thread(new ParameterizedThreadStart(HandleIncomingIPC));
                    handleIncomingIPCThread.Start(client);
                }
            }
            catch (SocketException e)
            {
                Log.Error("IPC server SocketException: {0}", e.Message);
                _server.Stop();
            }
        }

        /// <summary>
        /// Processes incoming IPC data.
        /// </summary>
        /// <param name="tcpClient">The <c>TcpClient</c> object representing the incoming connection.</param>
        private void HandleIncomingIPC(Object tcpClient)
        {
            MainWindow mainWindow = AvaloniaLocator.Current.GetService<MainWindow>();
            TcpClient client = (TcpClient)tcpClient;
            var stream = client.GetStream();
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);

                    Log.Information("IPC server received data");
                    (App.Current as App).HandleIPC(data);
                }
            }
            catch (Exception e)
            {
                Log.Error("Exception in {MethodName}: {IPCServerException}",
                    nameof(HandleIncomingIPC), e.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
