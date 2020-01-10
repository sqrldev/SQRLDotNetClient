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

            string textID = sqrl.GenerateTextualIdentityBase56(identity.ToByteArray());
            byte[] input = identity.ToByteArray();
            
            byte[] output = sqrl.Base56DecodeIdentity("KSzZ 46JU 4vaG FB33 H");

            string text2=sqrl.GenerateTextualIdentityBase56(output);



        }
    }
}
