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
        public async void EnScryptTest()
        {
            using (WebClient wc = new WebClient())
            {
                String enScryptVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/enscrypt-vectors.txt");
                String[] lines = enScryptVectors.Split(Environment.NewLine);
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.Replace("\"", "").Split(',');
                    byte[] ary = await SQRL.EnScryptCT(data[0], Encoding.UTF8.GetBytes(data[1]), (int)Math.Pow(2, 9), int.Parse(data[2]));
                    string hex = data[4];
                    string result = Sodium.Utilities.BinaryToHex(ary);
                    Assert.Equal(hex.CleanUpString(), result.CleanUpString());
                }
            }
        }

        [Fact]
        public void EnHashTest()
        {
            using (WebClient wc = new WebClient())
            {
                String enHashVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/enhash-vectors.txt");
                String[] lines = enHashVectors.Split("\n");
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.Replace("\"", "").Split(',');
                    byte[] ary = SQRL.EnHash(Sodium.Utilities.Base64ToBinary(data[0], "", Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                    string hex = data[1];
                    string result = Sodium.Utilities.BinaryToBase64(ary, Sodium.Utilities.Base64Variant.UrlSafeNoPadding);
                    Assert.Equal(hex.CleanUpString(), result.CleanUpString());
                }
            }

        }

        [Fact]
        public void Base56EncodeTest()
        {
            using (WebClient wc = new WebClient())
            {
                String base56FullVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/base56-full-format-vectors.txt");
                String[] lines = base56FullVectors.Split("\n");
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.Replace("\"", "").Split(',');

                    string s = SQRL.GenerateTextualIdentityBase56(Sodium.Utilities.Base64ToBinary(data[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));



                    Assert.Equal(s.CleanUpString(), data[2].CleanUpString());
                }
            }

        }

        [Fact]
        public void Base56EncodeDecodeTest()
        {
            using (WebClient wc = new WebClient())
            {
                String base56FullVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/base56-full-format-vectors.txt");
                String[] lines = base56FullVectors.Split("\n");
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.Replace("\"", "").Split(',');

                    string s = SQRL.GenerateTextualIdentityBase56(Sodium.Utilities.Base64ToBinary(data[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding));
                    byte[] alpha = SQRL.Base56DecodeIdentity(s);
                    string inputData = Sodium.Utilities.BinaryToBase64(alpha, Sodium.Utilities.Base64Variant.UrlSafeNoPadding);

                    Assert.Equal(inputData.CleanUpString(), data[0].CleanUpString());
                }
            }
        }

        [Fact]
        public async void LMKILKPasswordEncryptDecryptTest()
        {
            for (int i = 0; i < 50; i++)
            {
                SQRLIdentity identity = new SQRLIdentity();
                byte[] iuk = SQRL.CreateIUK();
                string password = Sodium.Utilities.BinaryToHex(Sodium.SodiumCore.GetRandomBytes(32), Sodium.Utilities.HexFormat.None, Sodium.Utilities.HexCase.Lower);

                identity= await SQRL.GenerateIdentityBlock1(iuk, password, identity);
                byte[] imk = SQRL.CreateIMK(iuk);
                byte[] ilk = SQRL.CreateILK(iuk);
                var decryptedData = await SQRL.DecryptBlock1(identity, password);
                Assert.Equal(Sodium.Utilities.BinaryToHex(imk), Sodium.Utilities.BinaryToHex(decryptedData.Item2));
                Assert.Equal(Sodium.Utilities.BinaryToHex(ilk), Sodium.Utilities.BinaryToHex(decryptedData.Item3));
            }
        }

        [Fact]
        public async void IUKRescueCodeEncryptDecryptTest()
        {
            for (int i = 0; i < 10; i++)
            {
                SQRLIdentity identity = new SQRLIdentity();
                byte[] iuk = SQRL.CreateIUK();

                string rescueCode = SQRL.CreateRescueCode();
                identity= await SQRL.GenerateIdentityBlock2(iuk, rescueCode, identity);

                var t = await SQRL.DecryptBlock2(identity, rescueCode);
                Assert.Equal(Sodium.Utilities.BinaryToHex(iuk), Sodium.Utilities.BinaryToHex(t.Item2));
            }
        }

        [Fact]
        public void GenerateSiteKeysTest()
        {
            using (WebClient wc = new WebClient())
            {
                String identityVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/identity-vectors.txt");
                String[] lines = identityVectors.Split("\n");
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.CleanUpString().Replace("\"", "").Split(',');

                    var keys = SQRL.CreateSiteKey(new Uri($"sqrl://{data[3]}"), data[4], Sodium.Utilities.Base64ToBinary(data[2], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding), true);

                    Assert.Equal(Sodium.Utilities.BinaryToBase64(keys.PublicKey, Sodium.Utilities.Base64Variant.UrlSafeNoPadding), data[5]);
                }
            }
        }

        [Fact]
        public void GenerateIndexedSecretTest()
        {
            using (WebClient wc = new WebClient())
            {
                String insVectors = wc.DownloadString("https://raw.githubusercontent.com/sqrldev/sqrl-test-vectors/master/vectors/ins-vectors.txt");
                String[] lines = insVectors.Split("\n");
                bool first = true;

                foreach (var line in lines)
                {
                    if (first || string.IsNullOrEmpty(line))
                    {
                        first = false;
                        continue;
                    }
                    string[] data = line.CleanUpString().Replace("\"", "").Split(',');

                    var ins = SQRL.CreateIndexedSecret(
                        new Uri($"sqrl://{data[1]}"), 
                        string.Empty, 
                        Sodium.Utilities.Base64ToBinary(data[0], string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding),
                        Encoding.UTF8.GetBytes(data[2]), 
                        true);

                    Assert.Equal(Sodium.Utilities.BinaryToBase64(ins, Sodium.Utilities.Base64Variant.UrlSafeNoPadding), data[3]);
                }
            }
        }

    }
}
