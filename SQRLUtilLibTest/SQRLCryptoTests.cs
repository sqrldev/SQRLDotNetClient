using SQRLUtilsLib;
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
            String enScryptVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/enscrypt-vectors.txt");
            String[] lines = enScryptVectors.Split(Environment.NewLine);
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach (var line in lines)
            {
                if (first || string.IsNullOrEmpty(line))
                {
                    first = false;
                    continue;
                }
                string[] data = line.Replace("\"", "").Split(',');
                byte[] ary = sqrl.enScryptCT(data[0], Encoding.UTF8.GetBytes(data[1]), (int)Math.Pow(2, 9), int.Parse(data[2]));
                string hex = data[4];
                string result = Sodium.Utilities.BinaryToHex(ary);
                Assert.Equal(hex.CleanUpString(), result.CleanUpString());
            }
        }

        [Fact]
        public void EnHashTest()
        {
            WebClient wc = new WebClient();
            String enScryptVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/enhash-vectors.txt");
            String[] lines = enScryptVectors.Split("\n");
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach (var line in lines)
            {
                if (first || string.IsNullOrEmpty(line))
                {
                    first = false;
                    continue;
                }
                string[] data = line.Replace("\"", "").Split(',');
                byte[] ary = sqrl.enHash(Sodium.Utilities.Base64ToBinary(data[0], "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                string hex = data[1];
                string result = Sodium.Utilities.BinaryToBase64(ary, Sodium.Utilities.Base64Variant.UrlSafeNoPadding);
                Assert.Equal(hex.CleanUpString(), result.CleanUpString());
            }

        }


        [Fact]
        public void Base56EncodeTest()
        {
            WebClient wc = new WebClient();
            String enScryptVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/base56-full-format-vectors.txt");
            String[] lines = enScryptVectors.Split("\n");
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach (var line in lines)
            {
                if (first || string.IsNullOrEmpty(line))
                {
                    first = false;
                    continue;
                }
                string[] data = line.Replace("\"", "").Split(',');

                string s = sqrl.GenerateTextualIdentityBase56(Sodium.Utilities.Base64ToBinary(data[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));



                Assert.Equal(s.CleanUpString(), data[2].CleanUpString());
            }

        }



        [Fact]
        public void Base56EncodeDecodeTest()
        {
            WebClient wc = new WebClient();
            String base56FullVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/base56-full-format-vectors.txt");
            String[] lines = base56FullVectors.Split("\n");
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach (var line in lines)
            {
                if (first || string.IsNullOrEmpty(line))
                {
                    first = false;
                    continue;
                }
                string[] data = line.Replace("\"", "").Split(',');

                string s = sqrl.GenerateTextualIdentityBase56(Sodium.Utilities.Base64ToBinary(data[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                byte[] alpha = sqrl.Base56DecodeIdentity(s);
                string inputData = Sodium.Utilities.BinaryToBase64(alpha, Sodium.Utilities.Base64Variant.UrlSafeNoPadding);

                Assert.Equal(inputData.CleanUpString(), data[0].CleanUpString());
            }
        }

        [Fact]
        public void LMKILKPasswordEncryptDecryptTest()
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            for (int i=0; i< 50; i++)
            {
                SQRLIdentity identity = new SQRLIdentity();
                byte[] iuk = sqrl.CreateIUK();
                string password = Sodium.Utilities.BinaryToHex(Sodium.SodiumCore.GetRandomBytes(32), Sodium.Utilities.HexFormat.None, Sodium.Utilities.HexCase.Lower);
                
                sqrl.GenerateIdentityBlock1(iuk, password, identity);
                byte[] imk = sqrl.CreateIMK(iuk);
                byte[] ilk = sqrl.CreateILK(iuk);
                byte[] decryptedImk;
                byte[] decryptedIlk;
                sqrl.DecryptBlock1(identity, password, out decryptedImk, out decryptedIlk);
                Assert.Equal(Sodium.Utilities.BinaryToHex(imk), Sodium.Utilities.BinaryToHex(decryptedImk));
                Assert.Equal(Sodium.Utilities.BinaryToHex(ilk), Sodium.Utilities.BinaryToHex(decryptedIlk));
            }
        }

        [Fact]
        public void IUKRescueCodeEncryptDecryptTest()
        {
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            for (int i = 0; i < 50; i++)
            {
                SQRLIdentity identity = new SQRLIdentity();
                byte[] iuk = sqrl.CreateIUK();
                
                string rescueCode = sqrl.CreateRescueCode();
                sqrl.GenerateIdentityBlock2(iuk, rescueCode, identity);
                
                byte[] decryptedIUK;
                
                sqrl.DecryptBlock2(identity, rescueCode, out decryptedIUK);
                Assert.Equal(Sodium.Utilities.BinaryToHex(iuk), Sodium.Utilities.BinaryToHex(decryptedIUK));
                
            }
        }

        [Fact]
        public void GenerateSiteKeysTest()
        {
            WebClient wc = new WebClient();
            String base56FullVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/identity-vectors.txt");
            String[] lines = base56FullVectors.Split("\n");
            bool first = true;
            SQRLUtilsLib.SQRL sqrl = new SQRLUtilsLib.SQRL();
            foreach (var line in lines)
            {
                if (first || string.IsNullOrEmpty(line))
                {
                    first = false;
                    continue;
                }
                string[] data = line.CleanUpString().Replace("\"", "").Split(',');

                var keys = sqrl.CreateSiteKey(new Uri($"sqrl://{data[3]}"), data[4], Sodium.Utilities.Base64ToBinary(data[2], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));

                Assert.Equal(Sodium.Utilities.BinaryToBase64(keys.PublicKey,Sodium.Utilities.Base64Variant.UrlSafeNoPadding), data[5]);
            }
        }

    }
}
