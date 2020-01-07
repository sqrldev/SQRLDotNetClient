using System;
using System.Net;
using System.Text;
using Xunit;

namespace SQRLUtilLibTest
{
    public class SQRLCryptoTests
    {
        [Fact]
        public void EnScryptTest()
        {
            WebClient wc = new WebClient();
            String enScryptVectors=wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/enscrypt-vectors.txt");
            String[] lines = enScryptVectors.Split(Environment.NewLine);
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach(var line in lines)
            {
                if(first)
                {
                    first = false;
                    continue;
                }
                string[] data = line.Replace("\"","").Split(',');
                byte[] ary = sqrl.enScriptCT(data[0], Encoding.UTF8.GetBytes(data[1]), (int)Math.Pow(2, 9), int.Parse(data[2]));
                string hex = data[4];
                string result = Sodium.Utilities.BinaryToHex(ary);
                Assert.Equal(hex, result);
            }
        }
    }
}
