using System;

namespace SQRLConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            byte[]iuk = sqrl.CreateIUK();
            object[] idblock1 = sqrl.GenerateIdentityBlock1(iuk, "larry");
        }
    }
}
