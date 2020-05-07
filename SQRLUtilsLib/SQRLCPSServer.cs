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
using System.Reflection;
using Serilog;

namespace SQRLUtilsLib
{
    /// <summary>
    /// This is a basic HTTP server implemented to handle the 
    /// "Client Provided Session" (CPS) requirements of SQRL.
    /// </summary>
    /// <remarks>
    /// For more information on SQRL's CPS system, please check out
    /// the chapter "Introducing client provided session (CPS)" in
    /// https://www.grc.com/sqrl/SQRL_Explained.pdf, starting on page 8.
    /// </remarks>
    public class SQRLCPSServer
    {
        private static SQRLCPSServer _instance = null;
        private Thread _serverThread;
        private HttpListener _listener;
        
        /// <summary>
        /// Default SQRL CPS port, do not change. 
        /// </summary>
        public static int Port { get; } = 25519;

        /// <summary>
        /// This event fires when a new CPS request arrives.
        /// </summary>
        public event EventHandler<CPSRequestReceivedEventArgs> CPSRequestReceived;

        /// <summary>
        /// A blocking collection of URLs, used to handle redirection 
        /// to cancellation or success URLs provided by the SQRL server.
        /// </summary>
        public BlockingCollection<Uri> cpsBC;

        /// <summary>
        /// The "nut" nonce of the CPS request provided by the SQRL server.
        /// </summary>
        public string Nut { get; set; }

        /// <summary>
        /// The cancellation URL provided by the SQRL server.
        /// </summary>
        public Uri Can { get; set; }

        /// <summary>
        /// Indicates whether a response to this request is already pending.
        /// </summary>
        public bool PendingResponse = false;

        /// <summary>
        /// Indicates whether the CPS HTTP server is running.
        /// </summary>
        public bool Running = false;

        /// <summary>
        /// Holds the HTML Content of the CPS Abort page pulled from an internal Resource File
        /// </summary>
        private string _abortHTML = "";

        /// <summary>
        /// Generic Abort URL
        /// </summary>
        public Uri AbortURL = new Uri($"http://ABORT_ALL_HOPE_LOST");

        /// <summary>
        /// Holds the header for the generic SQRL Abort Page
        /// </summary>
        public string CPSAbortHeader { get; set; }

        /// <summary>
        /// Holds the BODY Message for the generic SQRL Abort Page
        /// </summary>
        public string CPSAbortMessage { get; set; }

        /// <summary>
        /// Text Link for the Generic SQRL Abort Page
        /// </summary>
        public string CPSAbortLinkText { get; set; }

        /// <summary>
        /// Gets a singleton <c>SQRLCPSServer</c> instance.
        /// </summary>
        public static SQRLCPSServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SQRLCPSServer();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Instanciates a CPS HTTP server on the SQRL default port.
        /// This constructor is private. To get an instance, use
        /// <c>SQRLCPSServer.Instance</c>.
        /// </summary>
        private SQRLCPSServer()
        {
            this.cpsBC = new BlockingCollection<Uri>();
            this.Initialize();
            var _assembly = Assembly.GetExecutingAssembly();
            _abortHTML = new StreamReader(_assembly.GetManifestResourceStream("SQRLUtilsLib.Resources.CPS.html")).ReadToEnd();
        }

        /// <summary>
        /// Initializes and starts the server thread.
        /// </summary>
        private void Initialize()
        {
            Log.Information($"Starting CPS server");

            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }

        /// <summary>
        /// Listens for incoming connections to the CPS HTTP server.
        /// </summary>
        private void Listen()
        {
            try
            {
                var prefix = $"http://localhost:{Port}/";
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                Log.Information($"Start listening for CPS requests on {prefix}");
                
                _listener.Start();
                this.Running = true;

                while (true)
                {
                    try
                    {
                        HttpListenerContext context = _listener.GetContext();

                        // We need to drop all requests containing an "Origin:" header!
                        // Quoting from the spec:
                        // If JavaScript were to use an XML HTTP Request (XHR), or similar, to query the 
                        // SQRL web server at localhost: 25519, it would obtain the HTTP 302 redirect 
                        // information returned by the web server, which it could abuse. Fortunately, web 
                        // browser security strongly enforces the differentiation of any and all script-based 
                        // queries from browser page fetch queries. ALL script-driven queries contain an 
                        // “Origin:” header which is not under the script’s control – and browser page - 
                        // fetch queries do not. Therefore, all SQRL clients must drop any query received 
                        // which contains an “Origin:” header.
                        if (context.Request.Headers.AllKeys.Contains("Origin")) return;

                        // Looks like a legit request, so we can continue
                        CPSRequestReceivedEventArgs e = new CPSRequestReceivedEventArgs(context);
                        CPSRequestReceived?.Invoke(this, e);
                        if (e.ProcessEvent)
                        {
                            Process(context);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"CPS: Error while getting or processing a context:\r\n{ex}");
                    }
                }
            }
            catch(Exception)
            {
                this.Running = false;
            }
        }

        /// <summary>
        /// Handles the processing of any request received by the HTTP server.
        /// </summary>
        /// <param name="context">The HTTP context which provides access to the request and response objects.</param>
        private void Process(HttpListenerContext context)
        {
            Log.Information("CPS: Processing new request");
            string filename = context.Request.Url.AbsolutePath;
            
            string extension = Path.GetExtension(filename);
            //Checking if the request is for a gif. If it is we return a 1x1 clear gif and an OK response
            if(extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information($"CPS: Handling \"gif\" request");
                RespondWithGif(context);
            }
            else //All other requests are handled as a CPS Redirect Request
            {
                this.PendingResponse = true;
                RespondWithCPS(context);
                this.PendingResponse = false;
            }
        }

        /// <summary>
        /// All requests processed by this method are handled as a CPS redirect request.
        /// </summary>
        /// <param name="context">The HTTP context which provides access to the request and response objects.</param>
        private void RespondWithCPS(HttpListenerContext context)
        {
            Log.Information($"CPS: Handling CPS request");
            string data = context.Request.Url.AbsolutePath.Substring(1);
            Sodium.SodiumCore.Init();
            Uri cpsData = new Uri(Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(data, "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding)));
            var nvC= HttpUtility.ParseQueryString(cpsData.Query);

            //Get the nut from the CPS request
            if(nvC["nut"]!=null)
            {
                this.Nut = nvC["nut"];
            }

            //Try to get the "cancel url" from the CPS request
            if(nvC["can"]!=null)
            {
                this.Can = new Uri(Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(nvC["can"],string.Empty,Sodium.Utilities.Base64Variant.UrlSafeNoPadding)));
            }

            Log.Information($"CPS: Holding here till CPS is ready");

            //Hang here until we have a valid CPS then redirect the user
            //Porbably need to figure out how to handle a timeout.
            foreach (var x in cpsBC.GetConsumingEnumerable())
            {
                Log.Information($"CPS: Redirecting to (omitting query params): {x.GetLeftPart(UriPartial.Path)}");
                if (x.Equals(this.AbortURL))
                {

                    var htmlData = Encoding.ASCII.GetBytes(this._abortHTML.
                        Replace("{BACKLINK}", this.CPSAbortLinkText).
                        Replace("{HEADER}", this.CPSAbortHeader).
                        Replace("{MESSAGE}", this.CPSAbortMessage));

                    context.Response.ContentLength64 = htmlData.Length;
                    context.Response.StatusCode= (int)HttpStatusCode.OK;

                    using (Stream output = context.Response.OutputStream)
                    {
                        output.Write(htmlData, 0, htmlData.Length);
                        output.Close();
                    }
                    context.Response.Close();
                }
                else
                {
                    context.Response.Redirect(x.ToString());
                    context.Response.StatusCode = (int)HttpStatusCode.Redirect;
                    context.Response.Close();
                }
               break;
            }
            Log.Information($"CPS: Done with request");
        }

        /// <summary>
        /// Returns a clear 1x1 gif image to the user that requested it.
        /// </summary>
        /// <param name="context">The HTTP context which provides access to the request and response objects.</param>
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
            catch (Exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Generate and return a clear 1x1 pixel gif image.
        /// </summary>
        private byte[] GenerateGif()
        {
            //return empty gif
            const string clearGif1X1 = "R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==";
            return Convert.FromBase64String(clearGif1X1);
        }

        /// <summary>
        /// Handles CPS response for all requests
        /// </summary>
        /// <param name="header">Sets the title and heading line on the SQRL Abort HTML Generated page</param>
        /// <param name="message">Sets the body of the SQRL Abort HTML Generated page</param>
        /// <param name="backUrlText">Sets the text on the Back Now link on the SQRL Abort HTML Generated page</param>
        /// <param name="succesUrl">Success URL passed in if the response is a successful Auth Ident</param>
        public static void HandlePendingCPS(string header = "", string message = "", string backUrlText = "", Uri succesUrl = null)
        {
            /* There are some points in the application where all else fails and we still wantt o try and gracefully exit CPS
             * in these instances we may not have access to the localization so we are hard coding these here as backup
             * they only get called this way in extreme cases.
             */
            if (string.IsNullOrEmpty(header))
            {
                header = "Authentication Aborted";
            }
            if (string.IsNullOrEmpty(message))
            {
                message = "SQRL's CPS authentication has been aborted. You will be automatically sent back to the previous page in a few seconds. If this does not work, please press your browser's BACK button or click the link below.";
            }
            if (string.IsNullOrEmpty(backUrlText))
            {
                backUrlText = "Go Back Now";
            }

            // Note we want to handle CPS if it already exists, but we don't want to start up the CPS Server for no reason.          
            if (SQRL.CPS != null && SQRL.CPS.PendingResponse)
            {
                if (succesUrl == null)
                {
                    SQRL.CPS.CPSAbortHeader = header;
                    SQRL.CPS.CPSAbortMessage = message;
                    SQRL.CPS.CPSAbortLinkText = backUrlText;

                    if (SQRL.CPS.Can != null)
                        SQRL.CPS.cpsBC.Add(SQRL.CPS.Can);
                    else
                        SQRL.CPS.cpsBC.Add(SQRL.CPS.AbortURL);
                }
                else
                {
                    SQRL.CPS.cpsBC.Add(succesUrl);
                }
                while (SQRL.CPS.PendingResponse) ;
            }
        }
    }

    /// <summary>
    /// Provides infomation about a <c>CPSRequestReceived</c> event.
    /// </summary>
    public class CPSRequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the CPS request's HTTP listener context, providing access to the 
        /// request and response objects.
        /// </summary>
        public HttpListenerContext Context { get; }

        /// <summary>
        /// If set to <c>true</c>, the CPS request will be processed by the CPS server,
        /// otherwise it will be silently dropped. Default is <c>true</c>.
        /// </summary>
        public bool ProcessEvent { get; set; } = true;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="request">The CPS request received by the CPS server.</param>
        public CPSRequestReceivedEventArgs(HttpListenerContext context)
        {
            this.Context = context;
        }
    }
}
