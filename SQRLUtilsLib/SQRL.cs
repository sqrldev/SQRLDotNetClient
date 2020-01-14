using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;
using System.IO;
using Sodium;
using System.Net;
using System.Net.Http;
using System.Web;

namespace SQRLUtilsLib
{
    /// <summary>
    /// This library performs a lot of the crypto needed for a SQRL Client.
    /// A lot of the code here was adapted from @AlexHouser's IdTool at https://github.com/sqrldev/IdTool
    /// </summary>
    public class SQRL
    {
        private static bool SodiumInitialized = false;

        private readonly char[] BASE56_ALPHABETH = { '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        private const int ENCODING_BASE = 56;
        private const int CLIENT_VERSION = 1;
        /// <summary>
        /// Creates a Random Identity Unlock Key
        /// </summary>
        /// <returns></returns>
        public byte[] CreateIUK()
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        /// <summary>
        /// Creates a 24 character random Rescue Code
        /// </summary>
        /// <returns></returns>
        public string CreateRescueCode()
        {
            if (!SodiumInitialized)
                SodiumInit();

            char[] tempBytes = new char[24];
            byte temp;

            for (int i = 0; i < tempBytes.Length; i += 2)
            {
                temp = 255;
                while (temp > 199)
                {
                    temp = Sodium.SodiumCore.GetRandomBytes(1)[0];
                }

                int n = temp % 100;
                tempBytes[i] = (char)('0' + (n / 10));
                tempBytes[i + 1] = (char)('0' + (n % 10));
            }

            return new String(tempBytes);
        }

        /// <summary>
        /// Creates an Identity Master Key derives from the Identity Unlock Key
        /// </summary>
        /// <param name="iuk"></param>
        /// <returns></returns>
        public byte[] CreateIMK(byte[] iuk)
        {
            if (!SodiumInitialized)
                SodiumInit();

            return enHash(iuk);
        }

        /// <summary>
        /// Creates an Identity Lock Key Derives from the Identity Unlock Key
        /// </summary>
        /// <param name="iuk"></param>
        /// <returns></returns>
        public byte[] CreateILK(byte[] iuk)
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.ScalarMult.Base(iuk);
        }


        public byte[] RandomLockKey()
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        public KeyValuePair<byte[],byte[]> GetSukVuk(byte[] ILK)
        {
            if (!SodiumInitialized)
                SodiumInit();

            var RLK = RandomLockKey();
            var SUK = Sodium.ScalarMult.Base(RLK);

            var bytesToSign = Sodium.ScalarMult.Mult(RLK, ILK);

            var vukKeyPair = Sodium.PublicKeyAuth.GenerateKeyPair(bytesToSign);

            var VUK = vukKeyPair.PublicKey;
            
            return KeyValuePair.Create(SUK, VUK);
        }


        private void SodiumInit()
        {
            Sodium.SodiumCore.Init();
            SodiumInitialized = true;
        }

       

        /// <summary>
        ///  EnHash Algorithm
        ///  SHA256 is iterated 16 times with each
        ///  successive output XORed to form a 1’s complement sum to produce the final result
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] enHash(byte[] data)
        {
            byte[] result = new byte[32];
            byte[] xor = new byte[32];
            for (int i = 0; i < 16; i++)
            {
                data = Sodium.CryptoHash.Sha256(data);
                if (i == 0)
                {
                    data.CopyTo(xor, 0);
                }
                else
                {
                    BitArray og = new BitArray(xor);
                    BitArray newG = new BitArray(data);
                    BitArray newXor = og.Xor(newG);
                    newXor.CopyTo(xor, 0);
                }
            }

            return xor;
        }

        /// <summary>
        /// Run Script Low Level API form Libsodium for a determined amount of time given a random Salt and N factor
        /// </summary>
        /// <param name="password">Password to Hash</param>
        /// <param name="randomSalt">Byte Array of Random Data (Salt)</param>
        /// <param name="logNFactor">Log N Factor for Scrypt</param>
        /// <param name="secondsToRun">Amount of time to Iterate</param>
        /// <param name="count">Output of how many iterations the above Time Took</param>
        /// <returns></returns>
        public byte[] enScryptTime(String password, byte[] randomSalt, int logNFactor, int secondsToRun, out int count)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            byte[] key = new byte[32];
            count = 0;
            while (Math.Abs((DateTime.Now - startTime).TotalSeconds) < secondsToRun)
            {
                key = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

                if (count == 0)
                {
                    key.CopyTo(xorKey, 0);

                }
                else
                {
                    BitArray og = new BitArray(xorKey);
                    BitArray newG = new BitArray(key);
                    BitArray newXor = og.Xor(newG);
                    newXor.CopyTo(xorKey, 0);

                }
                randomSalt = key;

                count++;
            }

            return xorKey;
        }

        /// <summary>
        /// Run Scrypt for a number count interations to recreate the Time Generated Value
        /// </summary>
        /// <param name="password">Password to Hash</param>
        /// <param name="randomSalt">Byte array of Random Salt Values</param>
        /// <param name="logNFactor">Log N Factor</param>
        /// <param name="intCount">Number of Iterations (inclusive)</param>
        /// <returns></returns>
        public byte[] enScryptCT(String password, byte[] randomSalt, int logNFactor, int intCount)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            byte[] key = new byte[32];
            int count = 0;
            while (count < intCount)
            {
                key = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

                if (count == 0)
                {
                    key.CopyTo(xorKey, 0);

                }
                else
                {
                    BitArray og = new BitArray(xorKey);
                    BitArray newG = new BitArray(key);
                    BitArray newXor = og.Xor(newG);
                    newXor.CopyTo(xorKey, 0);

                }
                randomSalt = key;

                count++;
            }

            return xorKey;
        }

        /// <summary>
        /// Generates a SQRL Identity Block Array
        /// </summary>
        /// <param name="iuk"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public void GenerateIdentityBlock1(byte[] iuk, String password, SQRLIdentity identity)
        {

            if (!SodiumInitialized)
                SodiumInit();
            byte[] initVector = Sodium.SodiumCore.GetRandomBytes(12);
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);
            byte[] key = new byte[32];
            
            int iterationCount = 0;
            byte[] imk = CreateIMK(iuk);
            byte[] ilk = CreateILK(iuk);
            key = enScryptTime(password, randomSalt, (int)Math.Pow(2, 9), 5, out iterationCount);

          
            identity.Block1.ScryptInitVector = initVector;
            identity.Block1.ScryptRandomSalt = randomSalt;
            identity.Block1.IterationCount = (uint)iterationCount;
            List<byte> plainText = new List<byte>();
            plainText.AddRange(GetBytes(identity.Block1.Length));
            plainText.AddRange(GetBytes(identity.Block1.Type));
            plainText.AddRange(GetBytes(identity.Block1.InnerBlockLength));
            plainText.AddRange(GetBytes(identity.Block1.ScryptInitVector));
            plainText.AddRange(GetBytes(identity.Block1.ScryptRandomSalt));
            plainText.Add(identity.Block1.LogNFactor);
            plainText.AddRange(GetBytes(identity.Block1.IterationCount));
            plainText.AddRange(GetBytes(identity.Block1.OptionFlags));
            plainText.Add(identity.Block1.HintLenght);
            plainText.Add(identity.Block1.PwdVerifySeconds);
            plainText.AddRange(GetBytes(identity.Block1.PwdTimeoutMins));
            
            
            IEnumerable<byte> unencryptedKeys = imk.Concat(ilk);
          

            byte[] encryptedData = aesGcmEncrypt(unencryptedKeys.ToArray(), plainText.ToArray(), initVector, key); //Should be 80 bytes
            identity.Block1.EncryptedIMK = encryptedData.ToList().GetRange(0, 32).ToArray();
            identity.Block1.EncryptedILK = encryptedData.ToList().GetRange(32, 32).ToArray();
            identity.Block1.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length - 16, 16).ToArray();
        }

        /// <summary>
        /// Generates SQRL Identity Block 2 from unencrypted IUK
        /// </summary>
        /// <param name="iuk"></param>
        /// <param name="rescueCode"></param>
        /// <param name="identity"></param>
        public void GenerateIdentityBlock2(byte[] iuk, String rescueCode, SQRLIdentity identity)
        {
            if (!SodiumInitialized)
                SodiumInit();
            byte[] initVector = new byte[12];
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);


            byte[] key = enScryptTime(rescueCode, randomSalt, (int)Math.Pow(2, 9), 5, out int iterationCount);
            identity.Block2.RandomSalt = randomSalt;
            
            identity.Block2.IterationCount = (uint)iterationCount;

            List<byte> plainText = new List<byte>();
            plainText.AddRange(GetBytes(identity.Block2.Length));
            plainText.AddRange(GetBytes(identity.Block2.Type));
            plainText.AddRange(identity.Block2.RandomSalt);
            plainText.Add(identity.Block2.LogNFactor);
            plainText.AddRange(GetBytes(identity.Block2.IterationCount));

            
            byte[] encryptedData = aesGcmEncrypt(iuk, plainText.ToArray(), initVector, key); //Should be 80 bytes
            identity.Block2.EncryptedIdentityLock = encryptedData.ToList().GetRange(0, 32).ToArray(); ;
            identity.Block2.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length - 16, 16).ToArray(); ;

        }

        /// <summary>
        /// Converts input to byte array for various data types
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private IEnumerable<byte> GetBytes(object v)
        {
            if (v.GetType() == typeof(UInt16))
            {
                return BitConverter.GetBytes((UInt16)v);
            }
            else if (v.GetType() == typeof(sbyte))
            {
                return BitConverter.GetBytes((sbyte)v);
            }
            else if (v.GetType() == typeof(sbyte))
            {
                return BitConverter.GetBytes((sbyte)v);
            }
            else if (v.GetType() == typeof(String))
            {
                return Sodium.Utilities.HexToBinary((String)v);
            }
            else if (v.GetType() == typeof(UInt32))
            {
                return BitConverter.GetBytes((UInt32)v);
            }
            else if (v.GetType() == typeof(byte[]))
            {
                return (byte[])v;
            }
            else return null;
        }

        /// <summary>
        /// AESEncrypts a Message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="additionalData"></param>
        /// <param name="iv"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] aesGcmEncrypt(byte[] message, byte[] additionalData, byte[] iv, byte[] key)
        {
            long length = message.Length + 16;
            byte[] cipherText = new byte[length];

            if (!SodiumInitialized)
                SodiumInit();

            //Had to override Sodium Core to allow more than 16 bytes of additional data
            cipherText = Sodium.SecretAeadAes.Encrypt(message, iv, key, additionalData);

            return cipherText;
        }

        /// <summary>
        /// Formats a rescue code string for display adding a dash every 4th character
        /// </summary>
        /// <param name="rescueCode"></param>
        /// <returns></returns>
        public string FormatRescueCodeForDisplay(string rescueCode)
        {
            return Regex.Replace(rescueCode, ".{4}(?!$)", "$0-");
        }


        /// <summary>
        /// Generates a Base56 Encoded Textual Identity from a byte array
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public string GenerateTextualIdentityBase56(byte[] identity)
        {
          
            int maxLength = (int)Math.Ceiling((double)(identity.Length * 8) / (Math.Log(ENCODING_BASE)/ Math.Log(2)));
            BigInteger bigNum = new BigInteger(identity.Concat(new byte[] { (byte)0 }).ToArray());
            List<byte> checksumBytes = new List<byte>();
            List<char> TextID = new List<char>();
            int charsOnLine = 0;
            byte lineNr = 0;
            for (int i = 0; i < maxLength; i++)
            {
                if (charsOnLine == 19)
                {
                    checksumBytes.Add((byte)lineNr);
                    TextID.Add(GetBase56CheckSum(checksumBytes.ToArray()));
                    checksumBytes.Clear();
                    lineNr++;
                    charsOnLine = 0;
                }
                if (bigNum.IsZero)
                {
                    TextID.Add(BASE56_ALPHABETH[0]);
                    checksumBytes.Add((byte)BASE56_ALPHABETH[0]);
                }
                else
                {
                    BigInteger bigRemainder;
                    bigNum = BigInteger.DivRem(bigNum, ENCODING_BASE, out bigRemainder);
                    TextID.Add(BASE56_ALPHABETH[(int)bigRemainder]);
                    checksumBytes.Add((byte)BASE56_ALPHABETH[(int)bigRemainder]);
                }
                charsOnLine++;
            }

            checksumBytes.Add((byte)lineNr);
            TextID.Add(GetBase56CheckSum(checksumBytes.ToArray()));
            return FormatTextualIdentity(TextID.ToArray());
        }


        /// <summary>
        /// Generates a CheckSum character for a Base56 Encoded Identity Line
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public char GetBase56CheckSum(byte[] dataBytes)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] hash = Sodium.CryptoHash.Sha256(dataBytes);
            BigInteger bigI = new BigInteger(hash.Concat(new byte[] { 0 }).ToArray());
            BigInteger remainder;
            BigInteger.DivRem(bigI, ENCODING_BASE, out remainder);
            return BASE56_ALPHABETH[(int)remainder];
        }

        

        /// <summary>
        /// Formats the Textual Identity for Displays using a format of 
        /// 20 characters per line and space separated quads
        /// </summary>
        /// <param name="textID"></param>
        /// <returns></returns>
        public string FormatTextualIdentity(char[] textID)
        {
            StringBuilder sb = new StringBuilder();
            for(int i=0; i< textID.Length; i++)
            {
                sb.Append(textID[i]);

                if(i+1 < textID.Length)
                {
                    if((i+1) %20 ==0)
                    {
                        sb.AppendLine();
                        continue;
                    }
                    if ((i + 1) % 4 == 0)
                        sb.Append(" ");
                }
            }
            return sb.ToString();
        }


        /// <summary>
        /// Decodes the Textual Identity into a byte array
        /// </summary>
        /// <param name="identityStr"></param>
        /// <param name="bypassCheck"></param>
        /// <returns></returns>
        public byte[] Base56DecodeIdentity(string identityStr, bool bypassCheck =false)
        {
            byte[] identity = null;
            if(VerifyEncodedIdentity(identityStr)|| bypassCheck)
            {
                identityStr = Regex.Replace(identityStr, @"\s+", "").Replace("\r\n", "");

                StringBuilder sb = new StringBuilder();
                for(int i=0;i < identityStr.Length-1;i++)
                {
                    if ((i + 1) % 20 == 0)
                        continue;
                    sb.Append(identityStr[i]);
                }
                identityStr = sb.ToString();

                int expectedNumberOfBytes = (int)(identityStr.Length * (Math.Log(ENCODING_BASE)/ Math.Log(2)) /8);
                BigInteger powVal = 0;
                BigInteger bigInt = 0;
                for(int i =0; i< identityStr.Length; i++)
                {
                    if (powVal.IsZero)
                        powVal = 1;
                    else
                        powVal *= ENCODING_BASE;

                    int idex = Array.IndexOf(BASE56_ALPHABETH, identityStr[i]);
                    BigInteger newVal = BigInteger.Multiply(powVal, idex);
                    bigInt=BigInteger.Add(bigInt, newVal);
                }

                //List<byte> identityArray = bigInt.ToByteArray().ToList();
                identity = bigInt.ToByteArray();
                if (identity.Length > expectedNumberOfBytes)
                    identity = identity.Take(identity.Length - 1).ToArray();

                int lengthDiff = expectedNumberOfBytes - identity.Length;
                if(lengthDiff > 0)
                {
                    for (int i = 0; i < lengthDiff; i++)
                        identity = identity.Concat(new byte[] { 0 }).ToArray();
                }


            }
            return identity;
        }

        /// <summary>
        /// Verifies the encodeded 56 bit identity is encoded in a valid format.
        /// </summary>
        /// <param name="identityStr"></param>
        /// <returns></returns>
        public bool VerifyEncodedIdentity(string identityStr)
        {
            //Remove White Space
            identityStr = Regex.Replace(identityStr, @"\s+", "").Replace("\r\n","");

            byte lineNr = 0;
            for(int i=0; i < identityStr.Length;i+=20)
            {
                int checkSumPosition = i + 19;
                int checkSumDataLength = 19;
                if (checkSumPosition >= identityStr.Length)
                {
                    checkSumPosition = identityStr.Length - 1;
                    checkSumDataLength = checkSumPosition-i;
                }

                List<byte> checkSumBytes = new List<byte>();
                foreach (char x in identityStr.Substring(i, checkSumDataLength).ToCharArray())
                {
                    checkSumBytes.Add((byte)x);
                }
                checkSumBytes.Add((byte)lineNr);
                char computerCheckSumChar = GetBase56CheckSum(checkSumBytes.ToArray());
                if (computerCheckSumChar != identityStr[checkSumPosition])
                        return false;

                lineNr++;
            }

            return true;
        }

        /// <summary>
        /// Imports the SQRL Identity from a File 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public SQRLIdentity ImportSqrlIdentityFromFile(string file)
        {
            SQRLIdentity id = null;

            if (File.Exists(file))
            {
                byte[] fileBytes = File.ReadAllBytes(file);
                string sqrlData = System.Text.Encoding.UTF8.GetString(fileBytes.Take(8).ToArray());
                if (sqrlData.Equals("sqrldata", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] block1 = fileBytes.Skip(8).Take(125).ToArray();
                    id = new SQRLIdentity();
                    id.Block1.FromByteArray(block1);
                    byte[] block2 = fileBytes.Skip(133).Take(73).ToArray();
                    id.Block2.FromByteArray(block2);

                }
                else
                    throw new IOException("Invalid File Exception, not a valid SQRL Identity File");
            }

            return id;
        }


        /// <summary>
        ///  //Decrypts SQRL Identity Block 1
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="password"></param>
        /// <param name="imk"></param>
        /// <param name="ilk"></param>
        /// <returns></returns>
        public bool DecryptBlock1(SQRLIdentity identity, string password, out byte[] imk, out byte[] ilk)
        {
            byte[] key = enScryptCT(password, identity.Block1.ScryptRandomSalt, (int)Math.Pow(2, identity.Block1.LogNFactor), (int)identity.Block1.IterationCount);

            List<byte> plainText = new List<byte>();
            byte[] ary = identity.Block1.ToByteArray();
          
            plainText.AddRange(GetBytes(identity.Block1.Length));
            plainText.AddRange(GetBytes(identity.Block1.Type));
            plainText.AddRange(GetBytes(identity.Block1.InnerBlockLength));
            plainText.AddRange(GetBytes(identity.Block1.ScryptInitVector));
            plainText.AddRange(GetBytes(identity.Block1.ScryptRandomSalt));
            plainText.Add(identity.Block1.LogNFactor);
            plainText.AddRange(GetBytes(identity.Block1.IterationCount));
            plainText.AddRange(GetBytes(identity.Block1.OptionFlags));
            plainText.Add(identity.Block1.HintLenght);
            plainText.Add(identity.Block1.PwdVerifySeconds);
            plainText.AddRange(GetBytes(identity.Block1.PwdTimeoutMins));


            byte[] encryptedKeys = identity.Block1.EncryptedIMK.Concat(identity.Block1.EncryptedILK).Concat(identity.Block1.VerificationTag).ToArray();
            byte[] result =Sodium.SecretAeadAes.Decrypt(encryptedKeys, identity.Block1.ScryptInitVector, key, plainText.ToArray());
            if (result != null)
            {
                imk = result.Skip(0).Take(32).ToArray();
                ilk = result.Skip(32).Take(32).ToArray();
                return true;
            }
            else
            {
                ilk = null;
                imk = null;
            }
                return false;
        }

        /// <summary>
        /// Decrypts SQRL identity Block 2 (requires rescue code)
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="rescueCode"></param>
        /// <param name="iuk"></param>
        /// <returns></returns>
        public bool DecryptBlock2(SQRLIdentity identity, string rescueCode, out byte[] iuk)
        {
            byte[] key = enScryptCT(rescueCode, identity.Block2.RandomSalt, (int)Math.Pow(2, identity.Block2.LogNFactor), (int)identity.Block2.IterationCount);

            List<byte> plainText = new List<byte>();
            
            plainText.AddRange(GetBytes(identity.Block2.Length));
            plainText.AddRange(GetBytes(identity.Block2.Type));
            plainText.AddRange(identity.Block2.RandomSalt);
            plainText.Add(identity.Block2.LogNFactor);
            plainText.AddRange(GetBytes(identity.Block2.IterationCount));
            byte[] initVector = new byte[12];



            byte[] encryptedKeys = identity.Block2.EncryptedIdentityLock.Concat(identity.Block2.VerificationTag).ToArray();
            byte[] result = Sodium.SecretAeadAes.Decrypt(encryptedKeys, initVector, key, plainText.ToArray());
            if (result != null)
            {
                iuk = result.Skip(0).Take(32).ToArray();
                return true;
            }
            else
            {
                iuk = null;
            }
            return false;
        }

        /// <summary>
        /// Creates a Site KeyValuePair from the IMK Domain and AltID (if available)
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="altID"></param>
        /// <param name="imk"></param>
        /// <param name="test">This is specifically for the vextor tests since they don't use the x param (should be fixed)</param>
        /// <returns></returns>
        public Sodium.KeyPair CreateSiteKey(Uri domain, String altID, byte[] imk, bool test =false)
        {
            byte[] domainBytes = Encoding.UTF8.GetBytes(domain.DnsSafeHost+(test?(domain.LocalPath.Equals("/")?"":domain.LocalPath):""));

            var nvC = HttpUtility.ParseQueryString(domain.Query);
            if(nvC["x"]!=null)
            {
                string extended = domain.LocalPath.Substring(0, int.Parse(nvC["x"]));
                domainBytes = domainBytes.Concat(Encoding.UTF8.GetBytes(extended)).ToArray();
            }

            if (!string.IsNullOrEmpty(altID))
            {
                domainBytes = domainBytes.Concat(new byte[] { 0 }).Concat(Encoding.UTF8.GetBytes(altID)).ToArray();
            }


            byte[] siteSeed = Sodium.SecretKeyAuth.SignHmacSha256(domainBytes, imk);

            Sodium.KeyPair kp = Sodium.PublicKeyAuth.GenerateKeyPair(siteSeed);

            return kp;
        }

    
        /// <summary>
        /// Generates an Ident Request to the server
        /// </summary>
        /// <param name="sqrl">Server URI</param>
        /// <param name="siteKP">Site Key Pair</param>
        /// <param name="priorServerMessaage">Prior Server Message (base64)</param>
        /// <param name="opts">Options (SUK, CPS etc)</param>
        /// <param name="message"></param>
        /// <param name="addClientData">Additional Client Data to Sendin VUK / SUK etc</param>
        /// <returns></returns>
        public SQRLServerResponse GenerateIdentCommand(Uri sqrl, KeyPair siteKP, string priorServerMessaage, string[] opts, out string message,StringBuilder addClientData=null )
        {
            SQRLServerResponse serverResponse = null;
            message = "";
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez SQRL Client");
                if (opts == null)
                {
                    opts = new string[]
                    {
                        "suk"
                    };
                }
                StringBuilder client = new StringBuilder();
                client.AppendLineWindows($"ver={CLIENT_VERSION}");
                client.AppendLineWindows($"cmd=ident");
                client.AppendLineWindows($"opt={string.Join("~", opts)}");
                if(addClientData!=null)
                    client.Append(addClientData);
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");

                
                Dictionary<string, string> strContent = GenerateResponse(sqrl, siteKP, client, priorServerMessaage);
                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrl.Host, sqrl.IsDefaultPort ? 443 : sqrl.Port);
               
            }

            return serverResponse;

            
        }

        /// <summary>
        /// Generates a Quer command (repeats the command up to 3 times if there is a transient error)
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="opts"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public SQRLServerResponse GenerateQueryCommand(Uri sqrl, KeyPair siteKP,string[] opts = null, int count=0)
        {
            SQRLServerResponse serverResponse = null;
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez SQRL Client");
                if(opts==null)
                {
                    opts = new string[]
                    {
                        "suk"
                    };
                }
                StringBuilder client = new StringBuilder();
                client.AppendLineWindows($"ver={CLIENT_VERSION}");
                client.AppendLineWindows($"cmd=query");
                client.AppendLineWindows($"opt={string.Join("~",opts)}");
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");


                StringBuilder server = new StringBuilder();
                server.Append($"{sqrl.OriginalString}");
                Dictionary<string, string> strContent = GenerateResponse(sqrl, siteKP, client, server);
                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort?"":$":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                    serverResponse = new SQRLServerResponse(result,sqrl.Host, sqrl.IsDefaultPort?443:sqrl.Port);
                if(serverResponse.TransientError && count <=3)
                {
                    serverResponse = GenerateQueryCommand(new Uri($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{serverResponse.Qry}"), siteKP, opts,++count); ;
                }
                
            }

            return serverResponse;
        }

        public SQRLServerResponse GenerateCommand(Uri sqrl, KeyPair siteKP, string priorServerMessaage, string command, string[] opts, out string message, StringBuilder addClientData = null)
        {
            SQRLServerResponse serverResponse = null;
            message = "";
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez SQRL Client");
                if (opts == null)
                {
                    opts = new string[]
                    {
                        "suk"
                    };
                }
                StringBuilder client = new StringBuilder();
                client.AppendLineWindows($"ver={CLIENT_VERSION}");
                client.AppendLineWindows($"cmd={command}");
                client.AppendLineWindows($"opt={string.Join("~", opts)}");
                if (addClientData != null)
                    client.Append(addClientData);
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");


                Dictionary<string, string> strContent = GenerateResponse(sqrl, siteKP, client, priorServerMessaage);
                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrl.Host, sqrl.IsDefaultPort ? 443 : sqrl.Port);

            }

            return serverResponse;

        }

        /// <summary>
        /// Generates a Client Response Message to the Server from a Client, Server Strings
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GenerateResponse(Uri sqrl, KeyPair siteKP, StringBuilder client, StringBuilder server)
        {
            
            
            string encodedServer = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(server.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            return GenerateResponse(sqrl, siteKP, client, encodedServer);
        }



        /// <summary>
        /// Generates a Client Message to the Server
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GenerateResponse(Uri sqrl, KeyPair siteKP, StringBuilder client, string server)
        {

            string encodedClient = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(client.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            string encodedServer = server;
            byte[] signature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), siteKP.PrivateKey);
            string encodedSignature = Sodium.Utilities.BinaryToBase64(signature, Utilities.Base64Variant.UrlSafeNoPadding);
            Dictionary<string, string> strContent = new Dictionary<string, string>()
                {
                    {"client",encodedClient },
                    {"server",encodedServer },
                    {"ids",encodedSignature },
                };
            return strContent;
        }

        
    }

    
}

public static class UtilClass
{
    public static string CleanUpString(this string s)
    {
        return s.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\\n","");
    }

    public static StringBuilder AppendLineWindows(this StringBuilder sb, string s)
    {
        sb.Append(s);
        sb.Append("\r\n");
        return sb;
    }
}
