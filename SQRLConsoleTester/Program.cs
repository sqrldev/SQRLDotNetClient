using SQRLUtilsLib;
using System;

namespace SQRLConsoleTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            sqrl.NormalizeURL(new Uri("sqrl://Jonny:Secret@example.com/?nut=3434"));

        }
    }
}
