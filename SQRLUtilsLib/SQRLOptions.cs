using System;
using System.Collections.Generic;

namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents the transaction options used by the SQRL protocol and
    /// trasmitted using to a SQRL server using the "opt" parameter.
    /// </summary>
    public class SQRLOptions
    {
        /// <summary>
        /// Represents an enumeration of SQRL transaction options.
        /// </summary>
        [Flags]
        public enum SQRLOpts
        {
            /// <summary>
            /// Instructs the SQRL server to return the stored server unlock key(SUK) 
            /// associated with whichever identity matches the identity supplied by 
            /// the SQRL client.
            /// </summary>
            SUK = 1,

            /// <summary>
            /// Requests the web server to set a flag on this user's account to 
            /// disable any alternative non-SQRL authentication capability, such as
            /// traditional username and password authentication.
            /// </summary>
            SQRLONLY = 2,

            /// <summary>
            ///  Requests the web server to set a flag on this user's account to 
            ///  disable any alternative out of band account recovery measures for this
            ///  user’s web account such as “I forgot my password” eMail or “what was 
            ///  the name of your first pet?” non-SQRL identity recovery.
            /// </summary>
            HARDLOCK = 4,

            /// <summary>
            ///  Client Provided Session. Informs the server that the client has 
            ///  established a secure and private means of returning a server-supplied 
            ///  logged-in session URL to the web browser after authentication has succeeded.
            /// </summary>
            CPS = 8,

            /// <summary>
            /// Instructs the server to ignore any IP mismatch and to proceed to 
            /// process the client's query even if the IPs do not match.
            /// </summary>
            NOIPTEST = 16
        }

        /// <summary>
        /// Instructs the server to ignore any IP mismatch and to proceed to 
        /// process the client's query even if the IPs do not match.
        /// </summary>
        public bool NOIPTEST { get; set; }

        /// <summary>
        /// Requests the web server to set a flag on this user's account to 
        /// disable any alternative non-SQRL authentication capability, such as
        /// traditional username and password authentication.
        /// </summary>
        public bool SQRLONLY { get; set; }

        /// <summary>
        ///  Requests the web server to set a flag on this user's account to 
        ///  disable any alternative out of band account recovery measures for this
        ///  user’s web account such as “I forgot my password” eMail or “what was 
        ///  the name of your first pet?” non-SQRL identity recovery.
        /// </summary>
        public bool HARDLOCK { get; set; }

        /// <summary>
        ///  Client Provided Session. Informs the server that the client has 
        ///  established a secure and private means of returning a server-supplied 
        ///  logged-in session URL to the web browser after authentication has succeeded.
        /// </summary>
        public bool CPS { get; set; }

        /// <summary>
        /// Instructs the SQRL server to return the stored server unlock key(SUK) 
        /// associated with whichever identity matches the identity supplied by 
        /// the SQRL client.
        /// </summary>
        public bool SUK { get; set; }

        /// <summary>
        /// Creates a new <c>SQRLOptions</c> object and initializes it
        /// with the values stored in <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The sqrl transaction options to be set.</param>
        public SQRLOptions(SQRLOpts options)
        {
            this.NOIPTEST = (options & SQRLOpts.NOIPTEST) != 0;
            this.SUK = (options & SQRLOpts.SUK) != 0;
            this.SQRLONLY = (options & SQRLOpts.SQRLONLY) != 0;
            this.HARDLOCK = (options & SQRLOpts.HARDLOCK) != 0;
            this.CPS = (options & SQRLOpts.CPS) != 0;
        }

        /// <summary>
        /// Returns a string representation of the currently enabled transaction options, 
        /// which consists of any number of tilde-separated option strings, for example:
        /// <para><c>noiptest~hardlock~suk</c></para>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> opts = new List<string>();
            if (NOIPTEST)
                opts.Add("noiptest");
            if (SQRLONLY)
                opts.Add("sqrlonly");
            if (HARDLOCK)
                opts.Add("hardlock");
            if (CPS)
                opts.Add("cps");
            if (SUK)
                opts.Add("suk");

            return string.Join("~", opts.ToArray());
        }
    }
}
