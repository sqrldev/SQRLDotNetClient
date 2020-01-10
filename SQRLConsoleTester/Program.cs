using SQRLUtilsLib;
using System;

namespace SQRLConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            byte[]iuk = sqrl.CreateIUK();
            string rescueCode = sqrl.CreateRescueCode();
            SQRLIdentity identity = new SQRLIdentity();
            sqrl.GenerateIdentityBlock1(iuk, "larry", identity);
            sqrl.GenerateIdentityBlock2(iuk, rescueCode, identity);
            identity.Block1.ToByteArray();
            identity.Block2.ToByteArray();

            string s = sqrl.FormatRescueCodeForDisplay(rescueCode);

            sqrl.GenerateTextualIdentityBase56(identity.ToByteArray());
            
        }
    }
}
