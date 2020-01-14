using System;
using System.Collections.Generic;

namespace SQRLUtilsLib
{
    public class SQRLOptions
    {

        [Flags]
        public enum SQRLOpts
        {
            SUK = 1,
            SQRLONLY = 2,
            HARDLOCK = 4,
            CPS = 8,
            NOIPTEST = 16
        }
        public bool NOIPTEST { get; set; }
        public bool SQRLONLY { get; set; }
        public bool HARDLOCK { get; set; }
        public bool CPS { get; set; }
        public bool SUK { get; set; }


        public SQRLOptions(SQRLOpts options)
        {
            this.NOIPTEST = (options & SQRLOpts.NOIPTEST) != 0;
            this.SUK = (options & SQRLOpts.SUK) != 0;
            this.SQRLONLY = (options & SQRLOpts.SQRLONLY) != 0;
            this.HARDLOCK = (options & SQRLOpts.HARDLOCK) != 0;
            this.CPS = (options & SQRLOpts.CPS) != 0;
        }



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
