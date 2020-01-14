using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Web;

namespace SQRLUtilsLib
{
    /// <summary>
    /// This is a basic HTTP Server implemented to handle the CPS
    /// requirements of SQRL
    /// </summary>
    public class CPSServer
    {
        private Thread _serverThread;
        private HttpListener _listener;
        
        /// <summary>
        /// Default SQRL CPS port do not change
        /// </summary>
        private int Port { get; } =25519;

        public BlockingCollection<Uri> cpsBC;
        public string Nut { get; set; }
        public Uri Can { get; set; }

        public bool PendingResponse = false;

        /// <summary>
        /// Instanciates a CPS Http Server on the SQRL Default Port
        /// </summary>
        public CPSServer()
        {
            this.cpsBC = new BlockingCollection<Uri>();
            this.Initialize();

        }

        private void Initialize()
        {
            
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + this.Port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    Console.WriteLine("Http Listening");
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error with CPS: {ex}");
                }
            }
        }

        /// <summary>
        /// Handles the Processing of any request received by the HTTP Server
        /// </summary>
        /// <param name="context"></param>
        private void Process(HttpListenerContext context)
        {
            Console.WriteLine("Processing Request");
            string filename = context.Request.Url.AbsolutePath;
            
            string extension = Path.GetExtension(filename);
            //Checking if the request is for a gif. If it is we return a 1x1 clear gif and an OK response
            if(extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Got Gif request");
                RespondWithGif(context);
                Console.WriteLine($"Responded to Gif request");
            }
            else //All other requests are handled as a CPS Redirect Request
            {
                this.PendingResponse = true;
                RespondWithCPS(context);
                this.PendingResponse = false;
            }
        }


        /// <summary>
        /// All Requests Processed by this Method are handled as a CPS Redirect Request
        /// </summary>
        /// <param name="context"></param>
        private void RespondWithCPS(HttpListenerContext context)
        {
            Console.WriteLine($"Got CPS Request");
            string data = context.Request.Url.AbsolutePath.Substring(1);
            Sodium.SodiumCore.Init();
            Uri cpsData = new Uri(Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(data, "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding)));
            var nvC= HttpUtility.ParseQueryString(cpsData.Query);

            //Get the nut from the CPS Request
            if(nvC["nut"]!=null)
            {
                this.Nut = nvC["nut"];
            }

            //Try to get the Cancel url from the CPS Request
            if(nvC["can"]!=null)
            {
                this.Can = new Uri(Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(nvC["can"],string.Empty,Sodium.Utilities.Base64Variant.UrlSafeNoPadding)));
            }

            Console.WriteLine($"Holding here till CPS is Ready");

            //Hang here until we have a valid CPS then redirect the user
            //Porbably need to figure out how to handle a timeout.
            foreach (var x in cpsBC.GetConsumingEnumerable())
            {
                Console.WriteLine($"Redirecting To: {x}");
                context.Response.Redirect(x.ToString());
                context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                context.Response.Close();
               break;
            }
            Console.WriteLine($"Done with Request");

        }

        /// <summary>
        /// Returns a clear 1x1 gif to the user that requested it.
        /// </summary>
        /// <param name="context"></param>
        private void RespondWithGif(HttpListenerContext context)
        {
            try
            {
                
                var gif = GenerateGif();
                //Adding permanent http response headers
                context.Response.ContentType = "image/gif";
                context.Response.ContentLength64 = gif.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
                context.Response.OutputStream.Write(gif, 0, gif.Length);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
                context.Response.Close();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        private byte[] GenerateGif()
        {
            //return empty gif
            const string clearGif1X1 = "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==";
            return Convert.FromBase64String(clearGif1X1);
        }
    }
}
