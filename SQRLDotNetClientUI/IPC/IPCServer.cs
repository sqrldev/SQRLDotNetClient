using Avalonia;
using Avalonia.Threading;
using ProtoBuf;
using SQRLDotNetClientUI.ViewModels;
using SQRLDotNetClientUI.Views;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.IPC
{
    public class IPCServer
    {
        TcpListener server = null;

        public IPCServer(string ip, int port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            server = new TcpListener(localAddr, port);
            server.Start();
        }
        public void StartServer()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }

            
            
            
        }

        public void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            string imei = String.Empty;
            string data = null;
            Byte[] bytes = new Byte[256];
            int i;
            try
            {
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    string hex = BitConverter.ToString(bytes);
                    data = Encoding.ASCII.GetString(bytes, 0, i);
                    Dispatcher.UIThread.Post(() =>
                    {
                        var mm = ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).MainMenu;

                        AuthenticationViewModel authView = new AuthenticationViewModel(mm.sqrlInstance, mm.currentIdentity, new Uri(data));
                        mm.AuthVM = authView;
                        AvaloniaLocator.Current.GetService<MainWindow>().Width = 400;
                        AvaloniaLocator.Current.GetService<MainWindow>().Height = 200;
                        ((MainWindowViewModel)AvaloniaLocator.Current.GetService<MainWindow>().DataContext).Content = authView;

                        AvaloniaLocator.Current.GetService<MainWindow>().Focus();
                        AvaloniaLocator.Current.GetService<MainWindow>().Activate();
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                client.Close();
            }
        }

    }
}
