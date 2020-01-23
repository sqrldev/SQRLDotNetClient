using ProtoBuf;
using System;
using System.IO.Pipes;
using System.Net.Sockets;

namespace SQRLIPCTestClient
{
    class Program
    {
        static void Main(string[] args)
        {

            try
            {
                Int32 port = 13000;
                TcpClient client = new TcpClient("127.0.0.1", port);
                NetworkStream stream = client.GetStream();

                // Translate the Message into ASCII.
                Byte[] data = System.Text.Encoding.UTF8.GetBytes("https://google.com");
                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);


                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e);
            }
            Console.Read();
        }

    }

}
