using Sodium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace SQRLUtilsLib
{
    public class SQRLServerResponse
    {
        public int Ver { get; set; }

        public string Nut { get; set; }

        public int Tif { get; set; }


        public string SuccessUrl { get; set; }

        public string Qry { get; set; }

        public string FullServerRequest { get; set; }

        public bool CurrentIDMatch { get { return (this.Tif & 0x01) != 0; } }

        public bool PreviousIDMatch { get { return (this.Tif & 0x02) != 0; } }

        public bool IPMatches { get { return (this.Tif & 0x04) != 0; } }

        public bool SQRLDisabled { get { return (this.Tif & 0x08) != 0; } }

        public bool FunctionNotSupported { get { return (this.Tif & 0x10) != 0; } }


        public bool TransientError { get { return (this.Tif & 0x20) != 0; } }


        public bool CommandFailed { get { return (this.Tif & 0x40) != 0; } }


        public bool ClientFailure { get { return (this.Tif & 0x80) != 0; } }


        public bool BadIDAssociation { get { return (this.Tif & 0x100) != 0; } }



        public bool IdentitySuperSeeded { get { return (this.Tif & 0x200) != 0; } }

        public string Host { get; set; }

        public int Port { get; set; }

        public string Ask { get; set; }

        public bool HasAsk { get; set; }

        public string SUK { get; set; }

        public string SIN { get; set; }

        public KeyValuePair<byte[],KeyPair> PriorMatchedKey { get; set; }
        public string AskMessage
        {
            get
            {
                return Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
            }
        }

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
                        buttons[1] = Encoding.UTF8.GetString(Sodium.Utilities.Base64ToBinary(this.Ask.Split('~')[1], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
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

        public Uri NewNutURL
        {
            get
            {
                return new Uri($"https://{this.Host}{(Port == 443 ? "" : $":{this.Port}")}{this.Qry}");
            }
        }
        public SQRLServerResponse(string response, string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.ParseServerResponse(response);
        }
        public SQRLServerResponse()
        {

        }
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
