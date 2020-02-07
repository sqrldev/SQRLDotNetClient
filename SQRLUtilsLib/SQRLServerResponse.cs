using Sodium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents a SQRL server's response to a client's query
    /// and provides query status and transaction information.
    /// </summary>
    public class SQRLServerResponse
    {
        /// <summary>
        /// The SQRL protocol version specification, which consists of an unordered,
        /// comma-separated list of one or more integer version numbers and/or version 
        /// ranges in the form of:
        /// <para><c>1[,n],[n-m]</c></para>
        /// </summary>
        public int Ver { get; set; }

        /// <summary>
        /// The server-generated nonce "nut". SQRL servers issue these “nuts” 
        /// in an unpredictable sequence and track each nut issued so that 
        /// they can only be used once.The nuts serve the dual roles of 
        /// providing replay protection and forcing the client to sign unique, 
        /// nut-containing envelopes.
        /// </summary>
        public string Nut { get; set; }

        /// <summary>
        /// Transaction Information Flags. Indicates the success or failure of 
        /// the client’s command and holds additional informational status flags.
        /// </summary>
        public int Tif { get; set; }

        /// <summary>
        /// The content of the <c>url=</c> parameter, providing the successful 
        /// authentication browser redirection URL.
        /// </summary>
        public string SuccessUrl { get; set; }

        /// <summary>
        /// The content of the <c>qry=</c> parameter, providing the URL query
        /// path for the next client query.
        /// </summary>
        public string Qry { get; set; }

        /// <summary>
        /// The full, base64_url-encoded server response string.
        /// </summary>
        public string FullServerRequest { get; set; }

        /// <summary>
        /// When set, this bit indicates that the web server has found an identity 
        /// association for the user based upon the default (current) identity 
        /// credentials supplied by the client: the IDentity Key(IDK) and verified 
        /// by the IDentity Signature (IDS). 
        /// </summary>
        public bool CurrentIDMatch { get { return (this.Tif & 0x01) != 0; } }

        /// <summary>
        /// When set, this bit indicates that the web server has found an identity 
        /// association for the user based upon the previous identity credentials 
        /// supplied by the client in the previous IDentity Key(PIDK) and the 
        /// previous IDentity Signature (PIDS). 
        /// </summary>
        public bool PreviousIDMatch { get { return (this.Tif & 0x02) != 0; } }

        /// <summary>
        /// When set, this bit indicates that the IP address of the entity which
        /// requested the initial logon web page containing the SQRL link URL(and probably
        /// encoded into the SQRL link URL's “nut”) is the same IP address from which the SQRL
        /// client's query was received for this query & reply.
        /// </summary>
        public bool IPMatches { get { return (this.Tif & 0x04) != 0; } }

        /// <summary>
        /// When set, this bit indicates that SQRL authentication for this identity
        /// has previously been disabled.While this bit is set, the ident command and any 
        /// attempt at authentication will fail.
        /// </summary>
        public bool SQRLDisabled { get { return (this.Tif & 0x08) != 0; } }

        /// <summary>
        /// This bit indicates that the client requested one or more SQRL functions 
        /// (through command verbs) that the server does not currently support.
        /// </summary>
        public bool FunctionNotSupported { get { return (this.Tif & 0x10) != 0; } }

        /// <summary>
        /// The server replies with this bit set to indicate that the client's
        /// signature(s) are correct, but that something about the client's query 
        /// prevented the command from completing.This is the server's way of 
        /// instructing the client to retry and reissue the immediately previous 
        /// command using the fresh “nut=” crypto material and "qry=” url the 
        /// server will have also just returned in its reply.
        /// </summary>
        public bool TransientError { get { return (this.Tif & 0x20) != 0; } }

        /// <summary>
        /// When set, this bit indicates that the web server has encountered a
        /// problem processing the client's query. In any such case, no change 
        /// will be made to the user's account status.
        /// </summary>
        public bool CommandFailed { get { return (this.Tif & 0x40) != 0; } }

        /// <summary>
        /// This bit is set by the server when some aspect of the client's submitted
        /// query - other than expired but otherwise valid transaction state 
        /// information - was incorrect and prevented the server from understanding 
        /// and/or completing the requested action.
        /// </summary>
        public bool ClientFailure { get { return (this.Tif & 0x80) != 0; } }

        /// <summary>
        /// This bit is set by the server when a SQRL identity which may be
        /// associated with the query nut does not match the SQRL ID used to submit the query.
        /// </summary>
        public bool BadIDAssociation { get { return (this.Tif & 0x100) != 0; } }

        /// <summary>
        /// This bit is set by the server when the client has presented a current 
        /// identity (IDK) known to the server as having been superseded.
        /// </summary>
        public bool IdentitySuperSeeded { get { return (this.Tif & 0x200) != 0; } }

        /// <summary>
        /// The host part of the SQRL server URL.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The SQRL server's port number.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The content of the <c>ask=</c> parameter, containing the message text as
        /// well as optional buttons with associated URLs in the form of <br></br>
        /// <c>message text[~button1[;url][~button2[;url]]] </c>
        /// </summary>
        public string Ask { get; set; }

        /// <summary>
        /// This is set to <c>true</c> if the server's response carried an "ask=" 
        /// parameter, indicating that the client shall prompt the user with a 
        /// freeform question or action confirmation message.
        /// </summary>
        public bool HasAsk { get; set; }

        /// <summary>
        /// The content of the <c>suk=</c> parameter, providing the server-hosted
        /// Server Unlock Key (SUK).
        /// </summary>
        public string SUK { get; set; }

        /// <summary>
        /// The content of the <c>sin=</c> parameter, providing the name for a
        /// "Secret Index" (SIN), asking the client to provide the associated
        /// "Indexed Secret" (INS) with its next query.
        /// </summary>
        public string SIN { get; set; }

        /// <summary>
        /// The Previous Identity Key (PIDK) key of the matched previous identity.
        /// </summary>
        public KeyValuePair<byte[],Tuple<byte[], KeyPair>> PriorMatchedKey { get; set; }

        /// <summary>
        /// The decoded plain text ask message which was transmitted by the server
        /// using the <c>ask=</c> parameter.
        /// </summary>
        public string AskMessage
        {
            get
            {
                return Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
            }
        }

        /// <summary>
        /// The decoded plain text captions for the ask buttons 1 and 2 which were 
        /// transmitted by the server using the <c>ask=</c> parameter.
        /// </summary>
        public string[] GetAskButtons
        {
            get
            {
                string[] askData = this.Ask.Split('~');
                string[] buttons = null;
                if (askData.Length > 1)
                {
                    if (askData.Length == 3)
                    {
                        buttons = new string[2];
                        buttons[0] = Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[1], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                        buttons[1] = Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[2], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                    }
                    else if (askData.Length == 2)
                    {
                        buttons = new string[2];
                        buttons[0] = Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[1], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                        buttons[1] = "CANCEL";
                    }
                }
                else
                    buttons = new string[] { "OK", "CANCEL" };

                return buttons;
            }
        }

        /// <summary>
        /// Constructs a query URL based on the current response.
        /// </summary>
        public Uri NewNutURL
        {
            get
            {
                return new Uri($"https://{this.Host}{(Port == 443 ? "" : $":{this.Port}")}{this.Qry}");
            }
        }

        /// <summary>
        /// Creates a new <c>SQRLServerResponse</c> object by parsing the raw response
        /// string <paramref name="response"/> and setting the host and port to the 
        /// provided values.
        /// </summary>
        /// <param name="response">The raw server response string to be parsed.</param>
        /// <param name="host">The host part of the server's URL.</param>
        /// <param name="port">The SQRL server's port number.</param>
        public SQRLServerResponse(string response, string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.ParseServerResponse(response);
        }

        /// <summary>
        /// Creates a new, empty <c>SQRLServerResponse</c> object.
        /// </summary>
        public SQRLServerResponse()
        {

        }

        /// <summary>
        /// Parses the raw response string <paramref name="s"/> and populates the
        /// instance members accordingly.
        /// </summary>
        /// <param name="s">The raw server response string to be parsed.</param>
        public void ParseServerResponse(string s)
        {
            byte[] serverResponse = Sodium.Utilities.Base64ToBinary(s, "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding);
            string serverResponseStr = Encoding.UTF8.GetString(serverResponse);
            this.FullServerRequest = s;
            string[] serverResponseArray = serverResponseStr.Split("\r\n");
            foreach (var line in serverResponseArray.Where(x => !string.IsNullOrEmpty(x)))
            {
                string key = line.Substring(0, line.IndexOf("="));
                string value = line.Substring(line.IndexOf("=") + 1);
                if (key.Equals("ver", StringComparison.OrdinalIgnoreCase))
                {
                    this.Ver = int.Parse(value);
                }
                else if (key.Equals("nut", StringComparison.OrdinalIgnoreCase))
                {
                    this.Nut = value;
                }
                else if (key.Equals("tif", StringComparison.OrdinalIgnoreCase))
                {
                    this.Tif = int.Parse(value, System.Globalization.NumberStyles.HexNumber);
                }
                else if (key.Equals("qry", StringComparison.OrdinalIgnoreCase))
                {
                    this.Qry = value;
                }
                else if (key.Equals("url", StringComparison.OrdinalIgnoreCase))
                {
                    this.SuccessUrl = value;
                }
                else if (key.Equals("ask", StringComparison.OrdinalIgnoreCase))
                {
                    this.Ask = value;
                    this.HasAsk = true;
                }
                else if (key.Equals("suk", StringComparison.OrdinalIgnoreCase))
                {
                    this.SUK = value;
                }
                else if (key.Equals("sin", StringComparison.OrdinalIgnoreCase))
                {
                    this.SIN = value;
                }
            }
        }
    }
}
