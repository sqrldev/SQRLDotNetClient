using Sodium;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents all the available SQRL client commands.
    /// </summary>
    public enum SQRLCommands
    {
        query,
        ident,
        disable,
        enable,
        remove
    };

    /// <summary>
    /// This library performs a lot of the crypto needed for a SQRL Client.
    /// </summary>
    /// <remarks>
    /// A lot of the code here was adapted from @AlexHauser's IdTool at https://github.com/sqrldev/IdTool
    /// </remarks>
    public class SQRL
    {
        private static bool SodiumInitialized = false;

        private readonly char[] BASE56_ALPHABETH = { '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        private const int ENCODING_BASE = 56;
        private const int CLIENT_VERSION = 1;

        public CPSServer cps=null;

        /// <summary>
        /// Creates a new instance of the SQRL library and optionally
        /// starts the CPS server.
        /// </summary>
        /// <param name="startCPS">Set to true if the CPS server should be started, or false otherwise</param>
        public SQRL(bool startCPS=false)
        {
            SodiumInit();

            if (startCPS)
                this.cps = new CPSServer();
        }

        /// <summary>
        /// Creates a random Identity Unlock Key (IUK).
        /// </summary>
        public byte[] CreateIUK()
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        /// <summary>
        /// Creates a 24 character random Rescue Code.
        /// </summary>
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
        /// Creates an Unlock Request Signing Key (URSK) from the given
        /// Identity Unlock Key (IUK) and Server Unlock Key (SUK).
        /// </summary>
        /// <param name="IUK">The identity's Identity Unlock Key (IUK)</param>
        /// <param name="SUK">The Server Unlock Key (SUK)</param>
        public byte[] GetURSKey(byte[] IUK, byte[] SUK)
        {
            if (!SodiumInitialized)
                SodiumInit();

            var bytesToSign = Sodium.ScalarMult.Mult(IUK, SUK);
            var ursKeyPair = Sodium.PublicKeyAuth.GenerateKeyPair(bytesToSign);

            return ursKeyPair.PrivateKey;
        }

        /// <summary>
        /// Creates an Identity Master Key (IMK), derived from the given 
        /// Identity Unlock Key (IUK).
        /// </summary>
        /// <param name="iuk">The identity's Identity Unlock Key (IUK).</param>
        public byte[] CreateIMK(byte[] iuk)
        {
            if (!SodiumInitialized)
                SodiumInit();

            return EnHash(iuk);
        }

        /// <summary>
        /// Creates an Identity Lock Key (ILK), derived from the given
        /// Identity Unlock Key (IUK).
        /// </summary>
        /// <param name="iuk"></param>
        public byte[] CreateILK(byte[] iuk)
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.ScalarMult.Base(iuk);
        }

        /// <summary>
        /// Generates a Random Lock Key (RLK).
        /// </summary>
        public byte[] RandomLockKey()
        {
            if (!SodiumInitialized)
                SodiumInit();

            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        /// <summary>
        /// Creates a Server Unlock Key (SUK) / Verification Unlock Key (VUK) keypair,
        /// derived from the given Identity Lock Key (ILK).
        /// </summary>
        /// <param name="ILK">The Identity Lock Key (ILK)</param>
        public KeyValuePair<byte[], byte[]> GetSukVuk(byte[] ILK)
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

        /// <summary>
        /// Initializes the "Libsodium" crypto library.
        /// </summary>
        private void SodiumInit()
        {
            Sodium.SodiumCore.Init();
            SodiumInitialized = true;
        }

        /// <summary>
        /// Runs the given data trough the "EnHash" algorithm.
        /// </summary>
        /// <remarks>
        /// SHA256 is iterated 16 times with each successive output XORed to 
        /// form a 1’s complement sum to produce the final result.
        /// </remarks>
        /// <param name="data">The input data to be EnHash'ed.</param>
        public byte[] EnHash(byte[] data)
        {
            if (!SodiumInitialized)
                SodiumInit();

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
        /// Run the "Scrypt" memory hard key derivation function on the given 
        /// password for a determined amount of time, using the given random 
        /// salt and logNFactor.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="randomSalt">Random data which is being used as salt for Scrypt</param>
        /// <param name="logNFactor">Log N Factor for Scrypt</param>
        /// <param name="secondsToRun">Amount of time to run Scrypt (determines iteration count)</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional)</param>
        /// <param name="progressText">A string representing a text descrition for the progress indicator (optional)</param>
        public async Task<KeyValuePair<int, byte[]>> EnScryptTime(String password, byte[] randomSalt, int logNFactor, int secondsToRun, IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            int count = 0;
            var kvp =await Task.Run(() =>
            {
                count = 0;
                while (Math.Abs((DateTime.Now - startTime).TotalSeconds) < secondsToRun)
                {
                    
                    byte[] key = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

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
                    if (progress != null)
                    {
                        double totSecs = (DateTime.Now - startTime).TotalSeconds;
                        int report = (int) ((totSecs / (double)secondsToRun) * 100);
                        if (report > 100)
                            report = 100;
                        if (progressText == null)
                            progressText = "Encrypting Data for {secondsToRun} seconds:";
                        var reportKvp = new KeyValuePair<int, string>(report, progressText);
                        progress.Report(reportKvp);
                        
                    }
                    count++;
                }
                return new KeyValuePair<int, byte[]>(count, xorKey);
            });

            return kvp;
        }

        /// <summary>
        /// Run the "Scrypt" memory hard key derivation function for a specified 
        /// number of interations to recreate the time-generated value.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="randomSalt">Random data which is being used as salt for Scrypt</param>
        /// <param name="logNFactor">Log N Factor for Scrypt</param>
        /// <param name="intCount">Number of Scrypt iterations (inclusive)</param>
        public async Task<byte[]> EnScryptCT(String password, byte[] randomSalt, int logNFactor, int intCount, IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            
            
            var kvp = await Task.Run(() =>
            {
                byte[] xorKey = new byte[32];
                int count = 0;
                while (count < intCount)
                {
                    byte[] key = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

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
                    if(progress!=null)
                    {
                        int prog = (int)(((double)count / (double)intCount) * 100);
                        if (progressText == null)
                            progressText = "Encrypting Data:";
                        var reportKvp = new KeyValuePair<int, string>(prog, progressText);
                        progress.Report(reportKvp);
                    }
                }
                return xorKey;
            });

            return kvp;
        }

        /// <summary>
        /// Generates a copy of the given identity, and replaces its type 1 block
        /// with a newly created block based on the given parameters. If no block
        /// of type 1 is present in the given identity, it will be created.
        /// Also, a new random initialization vector and scrypt random salt will be
        /// created and used for the generation of the type 1 block.
        /// </summary>
        /// <param name="iuk">The unencrypted Identity Unlock Key (IUK) for creating the block 1 keys (IMK/ILK)</param>
        /// <param name="password">The password under which the new type 1 block will be encrypted</param>
        /// <param name="progress">An obect implementing the IProgress interface for monitoring the operation's progress (optional)</param>
        public async Task<SQRLIdentity> GenerateIdentityBlock1(byte[] iuk, String password, SQRLIdentity identity, IProgress<KeyValuePair<int,string>> progress=null, int encTime=5)
        {
            if (!SodiumInitialized)
                SodiumInit();

            if (!identity.HasBlock(1))
                identity.Blocks.Add(new SQRLBlock1());

            byte[] initVector = Sodium.SodiumCore.GetRandomBytes(12);
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);
            byte[] imk = CreateIMK(iuk);
            byte[] ilk = CreateILK(iuk);
            var key = await EnScryptTime(password, randomSalt, (int)Math.Pow(2, 9), encTime, progress, "Generating Block 1");

            var identityT = await Task.Run(() =>
             {
                 identity.Block1.ScryptInitVector = initVector;
                 identity.Block1.ScryptRandomSalt = randomSalt;
                 identity.Block1.IterationCount = (uint)key.Key;

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


                 byte[] encryptedData = AesGcmEncrypt(unencryptedKeys.ToArray(), plainText.ToArray(), initVector, key.Value); //Should be 80 bytes
                 identity.Block1.EncryptedIMK = encryptedData.ToList().GetRange(0, 32).ToArray();
                 identity.Block1.EncryptedILK = encryptedData.ToList().GetRange(32, 32).ToArray();
                 identity.Block1.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length - 16, 16).ToArray();
                 return identity;
             });
            return identityT;
        }

        /// <summary>
        /// Generates SQRL Identity Block 2 from the given unencrypted IUK.
        /// </summary>
        /// <param name="iuk"></param>
        /// <param name="rescueCode"></param>
        /// <param name="identity"></param>
        public async Task<SQRLIdentity> GenerateIdentityBlock2(byte[] iuk, String rescueCode, SQRLIdentity identity, IProgress<KeyValuePair<int,string>> progress = null, int encTime=5)
        {
            if (!SodiumInitialized)
                SodiumInit();

            if (!identity.HasBlock(2))
                identity.Blocks.Add(new SQRLBlock2());

            byte[] initVector = new byte[12];
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);

            var key = await EnScryptTime(rescueCode, randomSalt, (int)Math.Pow(2, 9), encTime, progress,"Generating Block 2");
            var identityT = await Task.Run(() =>
            {
                identity.Block2.RandomSalt = randomSalt;

                identity.Block2.IterationCount = (uint)key.Key;

                List<byte> plainText = new List<byte>();
                plainText.AddRange(GetBytes(identity.Block2.Length));
                plainText.AddRange(GetBytes(identity.Block2.Type));
                plainText.AddRange(identity.Block2.RandomSalt);
                plainText.Add(identity.Block2.LogNFactor);
                plainText.AddRange(GetBytes(identity.Block2.IterationCount));


                byte[] encryptedData = AesGcmEncrypt(iuk, plainText.ToArray(), initVector, key.Value); //Should be 80 bytes
                identity.Block2.EncryptedIdentityLock = encryptedData.ToList().GetRange(0, 32).ToArray(); ;
                identity.Block2.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length - 16, 16).ToArray();
                return identity;
            });
            return identityT;
        }

        /// <summary>
        /// Converts the given input data to a byte array. Supported types are:
        /// <list type="bullet">
        /// <item><description>sbyte</description></item>
        /// <item><description>UInt16</description></item>
        /// <item><description>UInt32</description></item>
        /// <item><description>String</description></item>
        /// <item><description>byte[]</description></item>
        /// </list>
        /// </summary>
        /// <param name="v">The object to turn into a byte array.</param>
        /// <returns>Returns the input converted to a byte array for supported types
        /// and <c>null</c> if the given type is not supported.</returns>
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
        /// Encrypts a given message using the AES-GCM Authenticated Encryption.
        /// </summary>
        /// <param name="message">The message to be encrypted</param>
        /// <param name="additionalData">The additional data used for authenticating the encryption</param>
        /// <param name="iv">The initialization vector for the AES-GCM encryption</param>
        /// <param name="key">The key for the AES-GCM encryption</param>
        public byte[] AesGcmEncrypt(byte[] message, byte[] additionalData, byte[] iv, byte[] key)
        {
            if (!SodiumInitialized)
                SodiumInit();

            //Had to override Sodium Core to allow more than 16 bytes of additional data
            byte[] cipherText = Sodium.SecretAeadAes.Encrypt(message, iv, key, additionalData);

            return cipherText;
        }

        /// <summary>
        /// Formats a rescue code string for displaying it to the user by
        /// adding a dash every 4th character.
        /// 
        /// <para>The resulting formatted rescue code should look something like this:</para>
        /// <c>1234-5678-9012-3456-7890-1234</c>
        /// 
        /// </summary>
        /// <param name="rescueCode">The unformatted rescue code string</param>
        public static string FormatRescueCodeForDisplay(string rescueCode)
        {
            return Regex.Replace(rescueCode, ".{4}(?!$)", "$0-");
        }

        /// <summary>
        /// Cleans the given rescue code string from any formatting by
        /// removing any dashes ("-") and spaces (" ").
        /// </summary>
        /// <param name="rescueCode">The formatted rescue code string to be cleaned</param>
        public static string CleanUpRescueCode(string rescueCode)
        {
            return rescueCode.Trim().Replace(" ", "").Replace("-", "");
        }

        /// <summary>
        /// Generates a base56-encoded "textual version" of the given identity.
        /// </summary>
        /// <param name="sqrlId">The identity to be encoded</param>
        public string GenerateTextualIdentityFromSqrlID(SQRLIdentity sqrlId)
        {
            return GenerateTextualIdentityBase56(sqrlId.Block2.ToByteArray().Concat(sqrlId.Block3.ToByteArray()).ToArray());
        }

        /// <summary>
        /// Generates a base56-encoded "textual version" of the given identity bytes.
        /// </summary>
        /// <param name="identity">The raw byte data of the identity to be encoded</param>
        public string GenerateTextualIdentityBase56(byte[] identity)
        {

            int maxLength = (int)Math.Ceiling((double)(identity.Length * 8) / (Math.Log(ENCODING_BASE) / Math.Log(2)));
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
                    bigNum = BigInteger.DivRem(bigNum, ENCODING_BASE, out BigInteger bigRemainder);
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
        /// Generates a checksum character for a given base56-encoded textual identity line.
        /// </summary>
        /// <param name="dataBytes">The bytes to create the checksum character from</param>
        public char GetBase56CheckSum(byte[] dataBytes)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] hash = Sodium.CryptoHash.Sha256(dataBytes);
            BigInteger bigI = new BigInteger(hash.Concat(new byte[] { 0 }).ToArray());
            BigInteger.DivRem(bigI, ENCODING_BASE, out BigInteger remainder);
            return BASE56_ALPHABETH[(int)remainder];
        }

        /// <summary>
        /// Formats the Textual Identity for display using a format of 
        /// 20 characters per line and space separated quads.
        /// <para>The output looks something like this:</para>
        /// <c>KjpJ ZyVK 5Ypd D6sk DCs8<br></br>
        /// vKh9 FDdP xUQD QHtZ Btua<br></br>
        /// BW3F wGdV pLxk NsLT 9jrM<br></br>
        /// WYJG cvZw q32D bdXF s5U9<br></br>
        /// 96Fi V3j8 K5U5 F3DS 42gG<br></br>
        /// cbTa w8Z</c>
        /// 
        /// </summary>
        /// <param name="textID">The unformatted textual identity</param>
        public static string FormatTextualIdentity(char[] textID)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < textID.Length; i++)
            {
                sb.Append(textID[i]);

                if (i + 1 < textID.Length)
                {
                    if ((i + 1) % 20 == 0)
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
        /// Creates a full SQRL identity from a base-56 encoded "textual version",
        /// which only contains the block type 2 (and 3 if present).
        /// </summary>
        /// <param name="identityTxt">The base-56 encoded "textual version" of the identity</param>
        /// <param name="rescueCode">The identity's rescue code</param>
        /// <param name="newPassword">The new password for encrypting the identity's block 1 keys</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional)</param>
        public async Task<SQRLIdentity> DecodeSqrlIdentityFromText(string identityTxt, string rescueCode, string newPassword, Progress<KeyValuePair<int, string>> progress = null)
        {
            byte[] id = Base56DecodeIdentity(identityTxt, false);
            SQRLIdentity identity = new SQRLIdentity();
            identity.Block2.FromByteArray(id.Take(73).ToArray());
            if (id.Length > 73)
            {
                var block3Length = BitConverter.ToUInt16(id.Skip(73).Take(2).ToArray());

                byte[] block3 = id.Skip(73).Take(block3Length).ToArray();
                identity.Block3.FromByteArray(block3);
            }

            var iukRespose = await this.DecryptBlock2(identity, rescueCode, progress);
            if(iukRespose.Item1)
                identity = await this.GenerateIdentityBlock1(iukRespose.Item2, newPassword, identity, progress);

            return identity;
        }

        /// <summary>
        /// Decodes a base-56 encoded "textual identity" into a byte array.
        /// </summary>
        /// <param name="identityStr">The base-56 encoded "textual version" of the identity</param>
        /// <param name="bypassCheck">If set to <c>true</c>, the result of the verification of the textual identity will be ignored</param>
        public byte[] Base56DecodeIdentity(string identityStr, bool bypassCheck = false)
        {
            byte[] identity = null;
            if (VerifyEncodedIdentity(identityStr) || bypassCheck)
            {
                identityStr = Regex.Replace(identityStr, @"\s+", "").Replace("\r\n", "");

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < identityStr.Length - 1; i++)
                {
                    if ((i + 1) % 20 == 0)
                        continue;
                    sb.Append(identityStr[i]);
                }
                identityStr = sb.ToString();

                int expectedNumberOfBytes = (int)(identityStr.Length * (Math.Log(ENCODING_BASE) / Math.Log(2)) / 8);
                BigInteger powVal = 0;
                BigInteger bigInt = 0;
                for (int i = 0; i < identityStr.Length; i++)
                {
                    if (powVal.IsZero)
                        powVal = 1;
                    else
                        powVal *= ENCODING_BASE;

                    int idex = Array.IndexOf(BASE56_ALPHABETH, identityStr[i]);
                    BigInteger newVal = BigInteger.Multiply(powVal, idex);
                    bigInt = BigInteger.Add(bigInt, newVal);
                }

                //List<byte> identityArray = bigInt.ToByteArray().ToList();
                identity = bigInt.ToByteArray();
                if (identity.Length > expectedNumberOfBytes)
                    identity = identity.Take(identity.Length - 1).ToArray();

                int lengthDiff = expectedNumberOfBytes - identity.Length;
                if (lengthDiff > 0)
                {
                    for (int i = 0; i < lengthDiff; i++)
                        identity = identity.Concat(new byte[] { 0 }).ToArray();
                }


            }
            return identity;
        }

        /// <summary>
        /// Verifies the validity of a base56-encodeded identity.
        /// </summary>
        /// <param name="identityStr">The base-56 encoded "textual version" of the identity which should be checked</param>
        /// <returns>Returns <c>true</c>if the verfification succeeds, and <c>false</c> otherwise.</returns>
        public bool VerifyEncodedIdentity(string identityStr)
        {
            //Remove White Space
            identityStr = Regex.Replace(identityStr, @"\s+", "").Replace("\r\n", "");

            byte lineNr = 0;
            for (int i = 0; i < identityStr.Length; i += 20)
            {
                int checkSumPosition = i + 19;
                int checkSumDataLength = 19;
                if (checkSumPosition >= identityStr.Length)
                {
                    checkSumPosition = identityStr.Length - 1;
                    checkSumDataLength = checkSumPosition - i;
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
        /// Decrypts a SQRL identity's type 1 block and provides access to the unencrypted
        /// Identity Master Key (IMK) and the Identity Lock Key (ILK).
        /// </summary>
        /// <param name="identity">The identity containing the type 1 block to be decrypted</param>
        /// <param name="password">The identity's password</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional)</param>
        /// <returns>Returns a <c>Tuple</c> containing a <c>bool</c> representing the operation's success, the decrypted IMK and the decrypted ILK</returns>
        public async Task<Tuple<bool, byte[], byte[]>> DecryptBlock1(SQRLIdentity identity, string password, IProgress<KeyValuePair<int,string>> progress = null)
        {
            byte[] key = await EnScryptCT(password, identity.Block1.ScryptRandomSalt, (int)Math.Pow(2, identity.Block1.LogNFactor), (int)identity.Block1.IterationCount, progress, "Decrypting Block 1");
            bool allgood = false;
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

            var tupl = await Task.Run(() =>
            {
                byte[] encryptedKeys = identity.Block1.EncryptedIMK.Concat(identity.Block1.EncryptedILK).Concat(identity.Block1.VerificationTag).ToArray();
                byte[] result = null;
                try
                {
                    result = Sodium.SecretAeadAes.Decrypt(encryptedKeys, identity.Block1.ScryptInitVector, key, plainText.ToArray());
                }
                catch(Exception ex)
                {
                    Console.Error.WriteLine($"Failed to Decrypt: {ex.ToString()} Call Stack: {ex.StackTrace}");
                }
                byte[] imk = null;
                byte[] ilk = null;
                if (result != null)
                {
                    imk = result.Skip(0).Take(32).ToArray();
                    ilk = result.Skip(32).Take(32).ToArray();
                    allgood = true;
                }
                else
                {
                    ilk = null;
                    imk = null;
                    allgood = false;
                }
                return new Tuple<bool,byte[],byte[]>(allgood, imk, ilk);
            });
            return tupl;
        }

        /// <summary>
        /// Decrypts SQRL identity Block 2 (requires rescue code)
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="rescueCode"></param>
        /// <param name="iuk"></param>
        /// <returns></returns>
        public async Task<Tuple<bool,byte[], string>> DecryptBlock2(SQRLIdentity identity, string rescueCode, IProgress<KeyValuePair<int, string>> progress = null)
        {
            byte[] key = await EnScryptCT(rescueCode, identity.Block2.RandomSalt, (int)Math.Pow(2, identity.Block2.LogNFactor), (int)identity.Block2.IterationCount, progress,"Decrypting Block 2");

            var tupl = await Task.Run(() =>
            {
                List<byte> plainText = new List<byte>();
                bool allGood = false;
                plainText.AddRange(GetBytes(identity.Block2.Length));
                plainText.AddRange(GetBytes(identity.Block2.Type));
                plainText.AddRange(identity.Block2.RandomSalt);
                plainText.Add(identity.Block2.LogNFactor);
                plainText.AddRange(GetBytes(identity.Block2.IterationCount));
                byte[] iuk = null;
                byte[] initVector = new byte[12];
                byte[] encryptedKeys = identity.Block2.EncryptedIdentityLock.Concat(identity.Block2.VerificationTag).ToArray();
                byte[] result = null;
                try
                {
                    result = Sodium.SecretAeadAes.Decrypt(encryptedKeys, initVector, key, plainText.ToArray());
                }
                catch(Exception x)
                {
                    Console.Error.WriteLine($"Failed to Decrypt: {x.ToString()} CallStack: {x.StackTrace}");
                }
                if (result != null)
                {
                    iuk = result.Skip(0).Take(32).ToArray();
                    allGood= true;
                }
                else
                {
                    iuk = null;
                    allGood = false;
                }
                return new Tuple<bool, byte[], string>(allGood, iuk, (!allGood?"Failed to Decrypt bad Password or Rescue Code":""));
            });
            return tupl;
        }


        public Dictionary<byte[], Tuple<byte[],Sodium.KeyPair>> CreatePriorSiteKeys(List<byte[]> oldIUKs, Uri domain, String altID)
        {
            Dictionary<byte[], Tuple<byte[],Sodium.KeyPair>> priorSiteKeys = new Dictionary<byte[], Tuple<byte[], Sodium.KeyPair>>();
            foreach(var oldIUK in oldIUKs)
            {
                priorSiteKeys.Add(oldIUK,new Tuple<byte[], Sodium.KeyPair>(CreateSiteSeed(domain,altID,CreateIMK(oldIUK)), CreateSiteKey(domain, altID, CreateIMK(oldIUK))));
            }
            return priorSiteKeys;
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
        public  Sodium.KeyPair CreateSiteKey(Uri domain, String altID, byte[] imk, bool test = false)
        {
            byte[] siteSeed = CreateSiteSeed(domain, altID, imk, test);
            Sodium.KeyPair kp = Sodium.PublicKeyAuth.GenerateKeyPair(siteSeed);

            return kp;
        }

        /// <summary>
        /// Creates and returns the so called "Indexed Secret" (INS) 
        /// for the given, server-provided "Secret Index" (SIN).
        /// 
        /// </summary>
        public byte[] CreateIndexedSecret(Uri domain, String altID, byte[] imk, byte[] secretIndex, bool test = false)
        {
            byte[] siteSeed = CreateSiteSeed(domain, altID, imk, test);
            byte[] key = EnHash(siteSeed);
            byte[] indexedSecret = Sodium.SecretKeyAuth.SignHmacSha256(secretIndex, key);

            return indexedSecret;
        }

        /// <summary>
        /// Creates and returns the so called "Indexed Secret" (INS)
        /// for the given, server-provided "Secret Index" using an existing site seed
        /// </summary>
        /// <param name="siteSeed"></param>
        /// <param name="secretIndex"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        public byte[] CreateIndexedSecret(byte[] siteSeed, byte[] secretIndex, bool test = false)
        {
            
            byte[] key = EnHash(siteSeed);
            byte[] indexedSecret = Sodium.SecretKeyAuth.SignHmacSha256(secretIndex, key);

            return indexedSecret;
        }

        private byte[] CreateSiteSeed(Uri domain, String altID, byte[] imk, bool test = false)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] domainBytes = Encoding.UTF8.GetBytes(domain.DnsSafeHost + (test ? (domain.LocalPath.Equals("/") ? "" : domain.LocalPath) : ""));

            var nvC = HttpUtility.ParseQueryString(domain.Query);
            if (nvC["x"] != null)
            {
                string extended = domain.LocalPath.Substring(0, int.Parse(nvC["x"]));
                domainBytes = domainBytes.Concat(Encoding.UTF8.GetBytes(extended)).ToArray();
            }

            if (!string.IsNullOrEmpty(altID))
            {
                domainBytes = domainBytes.Concat(new byte[] { 0 }).Concat(Encoding.UTF8.GetBytes(altID)).ToArray();
            }

            byte[] siteSeed = Sodium.SecretKeyAuth.SignHmacSha256(domainBytes, imk);

            return siteSeed;
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
        public  SQRLServerResponse GenerateIdentCommand(Uri sqrl, KeyPair siteKP, string priorServerMessaage, string[] opts, out string message, StringBuilder addClientData = null)
        {
            if (!SodiumInitialized)
                SodiumInit();

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
                if (addClientData != null)
                    client.Append(addClientData);
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");


                Dictionary<string, string> strContent = GenerateResponse( siteKP, client, priorServerMessaage);
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
        //GenerateSQRLCommand(SQRLCommands command,Uri sqrlUri, KeyPair currentSiteKeyPair, string plainTextServer, StringBuilder additionalClientData=null, SQRLOptions opts = null,KeyPair priorKey=null, byte[] ursKey=null)
        public SQRLServerResponse GenerateQueryCommand(Uri sqrl, KeyPair siteKP, SQRLOptions opts = null, string encodedServerMessage=null,int count = 0, Dictionary<byte[],Tuple<byte[],KeyPair>> priorSiteKeys=null)
        {
            
            SQRLServerResponse serverResponse = null;
            if(encodedServerMessage==null)
            {
                encodedServerMessage = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(sqrl.OriginalString), Utilities.Base64Variant.UrlSafeNoPadding);
            }
            serverResponse = GenerateSQRLCommand(SQRLCommands.query, sqrl, siteKP, encodedServerMessage, null, opts, priorSiteKeys?.First().Value.Item2);
            if(serverResponse.CommandFailed && serverResponse.TransientError && count<=3)
            {
                serverResponse = GenerateQueryCommand(serverResponse.NewNutURL, siteKP,opts, serverResponse.FullServerRequest,++count, priorSiteKeys);
            }
            else if(!serverResponse.CommandFailed && !serverResponse.PreviousIDMatch && !serverResponse.CurrentIDMatch && priorSiteKeys?.Count>1)
            {
                priorSiteKeys.Remove(priorSiteKeys.First().Key);
                serverResponse = GenerateQueryCommand(serverResponse.NewNutURL, siteKP, opts, serverResponse.FullServerRequest, ++count, priorSiteKeys);
            }
            else if(!serverResponse.CommandFailed && serverResponse.PreviousIDMatch)
            {
                serverResponse.PriorMatchedKey = priorSiteKeys.First();
            }

            return serverResponse;
        }

        /// <summary>
        /// Sends a new Identity (along with VUK/SUK) to server along with an ident command
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="encodedServerMessage"></param>
        /// <param name="ilk"></param>
        /// <param name="opts"></param>
        /// <returns></returns>
        public SQRLServerResponse GenerateNewIdentCommand(Uri sqrl, KeyPair siteKP, string encodedServerMessage, byte[] ilk, SQRLOptions opts = null, StringBuilder sin=null)
        {

            SQRLServerResponse serverResponse = null;
            var sukvuk = GetSukVuk(ilk);
            StringBuilder addClientData = new StringBuilder();
            addClientData.AppendLineWindows($"suk={Sodium.Utilities.BinaryToBase64(sukvuk.Key, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            addClientData.AppendLineWindows($"vuk={Sodium.Utilities.BinaryToBase64(sukvuk.Value, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            if (sin != null)
                addClientData.Append(sin);
            serverResponse = GenerateSQRLCommand(SQRLCommands.ident, sqrl, siteKP, encodedServerMessage, addClientData, opts, null);

            return serverResponse;
        }

        public SQRLServerResponse GenerateIdentCommandWithReplace(Uri sqrl, KeyPair siteKP, string encodedServerMessage, byte[] ilk,byte[] ursKey, KeyPair priorKey, SQRLOptions opts = null, StringBuilder sin=null)
        {

            SQRLServerResponse serverResponse = null;
            var sukvuk = GetSukVuk(ilk);
            StringBuilder addClientData = new StringBuilder();
            addClientData.AppendLineWindows($"suk={Sodium.Utilities.BinaryToBase64(sukvuk.Key, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            addClientData.AppendLineWindows($"vuk={Sodium.Utilities.BinaryToBase64(sukvuk.Value, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            if (sin != null)
                addClientData.Append(sin);
            serverResponse = GenerateSQRLCommand(SQRLCommands.ident, sqrl, siteKP, encodedServerMessage, addClientData, opts, priorKey, ursKey);

            return serverResponse;
        }

        ///
        public SQRLServerResponse GenerateEnableCommand(Uri sqrl, KeyPair siteKP, string encodedServerMessage,byte[] ursKey, StringBuilder addClientData=null, SQRLOptions opts = null)
        {

            SQRLServerResponse serverResponse = null;
            
            serverResponse = GenerateSQRLCommand(SQRLCommands.enable, sqrl, siteKP, encodedServerMessage, addClientData, opts,null, ursKey);

            return serverResponse;
        }


        /// <summary>
        /// Generates a response command
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="priorServerMessaage"></param>
        /// <param name="command"></param>
        /// <param name="opts"></param>
        /// <param name="addClientData"></param>
        /// <returns></returns>
        public  SQRLServerResponse GenerateCommand(Uri sqrl, KeyPair siteKP, string priorServerMessaage, string command, SQRLOptions opts,  StringBuilder addClientData = null, KeyPair priorSiteKP=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            SQRLServerResponse serverResponse = null;
            
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez SQRL Client");

                StringBuilder client = new StringBuilder();
                client.AppendLineWindows($"ver={CLIENT_VERSION}");
                client.AppendLineWindows($"cmd={command}");
                if (opts != null)
                    client.AppendLineWindows($"opt={opts}");
                if (addClientData != null)
                    client.Append(addClientData);
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");
                if(priorSiteKP!=null)
                    client.AppendLineWindows($"pidk={Sodium.Utilities.BinaryToBase64(priorSiteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");

                Dictionary<string, string> strContent = GenerateResponse( siteKP, client, priorServerMessaage, priorSiteKP);
                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrl.Host, sqrl.IsDefaultPort ? 443 : sqrl.Port);

            }

            return serverResponse;

        }

        /// <summary>
        /// Generates a command with a URS Signature
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="ursKey"></param>
        /// <param name="priorServerMessaage"></param>
        /// <param name="command"></param>
        /// <param name="opts"></param>
        /// <param name="addClientData"></param>
        /// <returns></returns>
        public  SQRLServerResponse GenerateCommandWithURS(Uri sqrl, KeyPair siteKP, byte[] ursKey, string priorServerMessaage, string command, SQRLOptions opts = null, StringBuilder addClientData = null, KeyPair priorMatchedKey=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            SQRLServerResponse serverResponse = null;

            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez's SQRL Client");

                StringBuilder client = new StringBuilder();
                client.AppendLineWindows($"ver={CLIENT_VERSION}");
                client.AppendLineWindows($"cmd={command}");
                if (opts != null)
                    client.AppendLineWindows($"opt={opts}");
                if (addClientData != null)
                    client.Append(addClientData);
                client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(siteKP.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");
                if(priorMatchedKey!=null)
                {
                    client.AppendLineWindows($"pidk={Sodium.Utilities.BinaryToBase64(priorMatchedKey.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");
                }

                string encodedClient = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(client.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
                string encodedServer = priorServerMessaage;

                byte[] signature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), siteKP.PrivateKey);
                string encodedSignature = Sodium.Utilities.BinaryToBase64(signature, Utilities.Base64Variant.UrlSafeNoPadding);
                byte[] ursSignature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), ursKey);
                string encodedUrsSignature = Sodium.Utilities.BinaryToBase64(ursSignature, Utilities.Base64Variant.UrlSafeNoPadding);

                

                Dictionary<string, string> strContent = new Dictionary<string, string>()
                {
                    {"client",encodedClient },
                    {"server",encodedServer },
                    {"ids",encodedSignature },
                    {"urs",encodedUrsSignature },
                };

                if(priorMatchedKey!=null)
                {
                    byte[] priorSignature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), priorMatchedKey.PrivateKey);
                    string priorEncodedSignature = Sodium.Utilities.BinaryToBase64(priorSignature, Utilities.Base64Variant.UrlSafeNoPadding);
                    strContent.Add("pids", priorEncodedSignature);
                }

                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrl.Host, sqrl.IsDefaultPort ? 443 : sqrl.Port);
                ZeroFillByteArray(ref ursKey);
            }

            return serverResponse;

        }

        /// <summary>
        /// Generates a Signature with the provided signatureID (urs,ids,pids..etc) and server , client signed with the given key
        /// </summary>
        /// <param name="signatureID"></param>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public KeyValuePair<string,string> GenerateSignature(string signatureID, string encodedServer, string client, byte[] key)
        {
            string encodedClient = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(client.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            
            byte[] signature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), key);
            string encodedSignature = Sodium.Utilities.BinaryToBase64(signature, Utilities.Base64Variant.UrlSafeNoPadding);
            return new KeyValuePair<string, string>(signatureID, encodedSignature);
        }

        /// <summary>
        /// Generates URS Signature given a URS Key Server and Client
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="ursKey"></param>
        /// <returns></returns>
        public KeyValuePair<string,string> GenerateURS(string encodedServer, string client, byte[] ursKey)
        {
            return GenerateSignature("urs", encodedServer, client, ursKey);
        }


        /// <summary>
        /// Generates PIDS Signature given a server, client and pidkKey
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="pidkKey"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GeneratePIDS(string encodedServer, string client, byte[] pidkKey)
        {
            return GenerateSignature("pids", encodedServer, client, pidkKey);
        }

        /// <summary>
        /// Generates IDS Signature given server, client and idkKey
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        /// <param name="idkKey"></param>
        /// <returns></returns>
        public KeyValuePair<string, string> GenerateIDS(string encodedServer, string client, byte[] idkKey)
        {
            return GenerateSignature("ids", encodedServer, client, idkKey);
        }


        public SQRLServerResponse GenerateSQRLCommand(SQRLCommands command,Uri sqrlUri, KeyPair currentSiteKeyPair, string encodedServer, StringBuilder additionalClientData=null, SQRLOptions opts = null,KeyPair priorKey=null, byte[] ursKey=null)
        {
            SQRLServerResponse serverResponse = null;
            
            StringBuilder client = new StringBuilder();
            client.AppendLineWindows($"ver={CLIENT_VERSION}");
            client.AppendLineWindows($"cmd={Enum.GetName(typeof(SQRLCommands),command)}");
            //If options were provided append them to the client message
            if (opts != null)
                client.AppendLineWindows($"opt={opts}");

            //If additional client data was provided append them to the client message
            if (additionalClientData != null)
                client.Append(additionalClientData);
            
            //Append Site Public Key
            client.AppendLineWindows($"idk={Sodium.Utilities.BinaryToBase64(currentSiteKeyPair.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");

            if(priorKey!=null)
            {
                client.AppendLineWindows($"pidk={Sodium.Utilities.BinaryToBase64(priorKey.PublicKey, Utilities.Base64Variant.UrlSafeNoPadding)}");
            }

            string encodedClient = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(client.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            

            KeyValuePair<string,string> ids = GenerateIDS(encodedServer, client.ToString(), currentSiteKeyPair.PrivateKey);
            Dictionary<string, string> strContent = new Dictionary<string, string>()
            {
                {"client",encodedClient },
                {"server",encodedServer },
            };
            //Add Ids
            strContent.Add(ids.Key,ids.Value);

            if(priorKey!=null)
            {
                var pids = GeneratePIDS(encodedServer, client.ToString(), priorKey.PrivateKey);
                strContent.Add(pids.Key, pids.Value);
            }

            if(ursKey!=null)
            {
                var urs = GenerateURS(encodedServer, client.ToString(), ursKey);
                strContent.Add(urs.Key, urs.Value);
            }
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", "Jose Gomez's SQRL Client");
                var content = new FormUrlEncodedContent(strContent);
                var response = wc.PostAsync($"https://{sqrlUri.Host}{(sqrlUri.IsDefaultPort ? "" : $":{sqrlUri.Port}")}{sqrlUri.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrlUri.Host, sqrlUri.IsDefaultPort ? 443 : sqrlUri.Port);
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
        private  Dictionary<string, string> GenerateResponse(KeyPair siteKP, StringBuilder client, StringBuilder server, KeyPair priorKP=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            string encodedServer = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(server.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            return GenerateResponse(siteKP, client, encodedServer, priorKP);
        }



        /// <summary>
        /// Generates a Client Message to the Server
        /// </summary>
        /// <param name="sqrl"></param>
        /// <param name="siteKP"></param>
        /// <param name="client"></param>
        /// <param name="server"></param>
        /// <returns></returns>
        private  Dictionary<string, string> GenerateResponse(KeyPair siteKP, StringBuilder client, string server, KeyPair priorKP = null)
        {
            if (!SodiumInitialized)
                SodiumInit();

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

            if(priorKP!=null)
            {
                byte[] priorSignature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), priorKP.PrivateKey);
                string priorEncodedSignature = Sodium.Utilities.BinaryToBase64(priorSignature, Utilities.Base64Variant.UrlSafeNoPadding);
                strContent.Add("pids", priorEncodedSignature);
            }
            return strContent;
        }


        public async Task<KeyValuePair<string,SQRLIdentity>> RekeyIdentity(SQRLIdentity identity, string rescueCode, string newPassword, IProgress<KeyValuePair<int,string>> progress)
        {
            SQRLIdentity newID = new SQRLIdentity();
            var oldIukData = await this.DecryptBlock2(identity, rescueCode, progress);
            string newRescueCode = CreateRescueCode();
            byte[] newIUK = CreateIUK();
            if (oldIukData.Item1)
            {
                newID= await GenerateIdentityBlock1(newIUK, newPassword, newID, progress);
                newID= await GenerateIdentityBlock2(newIUK, newRescueCode, newID, progress);
                GenerateIdentityBlock3(oldIukData.Item2, identity, newID, CreateIMK(oldIukData.Item2), CreateIMK(newIUK));
            }
            return new KeyValuePair<string, SQRLIdentity>(newRescueCode,newID);
        }

        public void GenerateIdentityBlock3(byte[] oldIuk, SQRLIdentity oldIdentity, SQRLIdentity newID, byte[] oldImk, byte[] newImk)
        {

            byte[] decryptedBlock3 = null;
            List<byte> unencryptedOldKeys = new List<byte>();
            unencryptedOldKeys.AddRange(oldIuk);
            int skip = 0;
            if (oldIdentity.HasBlock(3) && oldIdentity.Block3.EncryptedPrevIUKs.Count > 0)
            {
                
                decryptedBlock3 = DecryptBlock3(oldImk, oldIdentity, out bool allGood);
                if (allGood)
                {
                    skip = 0;
                    int ct = 0;
                    while (skip < decryptedBlock3.Length)
                    {
                        unencryptedOldKeys.AddRange(decryptedBlock3.Skip(skip).Take(32).ToArray());
                        skip += 32;
                        ;
                        if (++ct >= 3)
                            break;
                    }
                }
                else
                    throw new Exception("Failed to Decrypt Block 3, bad ILK");
            }
            List<byte> plainText = new List<byte>();
            ushort oldLength = (ushort)(oldIdentity.HasBlock(3) ? oldIdentity.Block3.Length : 54);
            ushort newLength = (ushort)(oldLength + 32 > 150 ? 150 : oldLength + 32);
            plainText.AddRange(GetBytes(newLength));
            if (!newID.HasBlock(3)) newID.Blocks.Add(new SQRLBlock3());
            newID.Block3.Length = newLength;
            plainText.AddRange(GetBytes(newID.Block3.Type));
            plainText.AddRange(GetBytes((ushort)(unencryptedOldKeys.Count / 32)));
            newID.Block3.Edition = (ushort)(unencryptedOldKeys.Count / 32);
            byte[] result = AesGcmEncrypt(unencryptedOldKeys.ToArray(), plainText.ToArray(),new byte[12], newImk);
            skip = 0;
            while (skip+16 < result.Length)
            {
                newID.Block3.EncryptedPrevIUKs.Add(result.Skip(skip).Take(32).ToArray());
                skip += 32;
            }
            newID.Block3.VerificationTag = result.Skip(result.Length - 16).Take(16).ToArray();
            
        }

        public byte[] DecryptBlock3(byte[] ikm, SQRLIdentity identity, out bool boolAllGood)
        {
            List<byte> plainText = new List<byte>();
            plainText.AddRange(GetBytes(identity.Block3.Length));
            plainText.AddRange(GetBytes(identity.Block3.Type));
            plainText.AddRange(GetBytes(identity.Block3.Edition));
            List<byte> encryptedKeys = new List<byte>();
            boolAllGood = false;
            identity.Block3.EncryptedPrevIUKs.ForEach(x => encryptedKeys.AddRange(x));
            encryptedKeys.AddRange(identity.Block3.VerificationTag);
            byte[] result = null;
            try
            {
                result = Sodium.SecretAeadAes.Decrypt(encryptedKeys.ToArray(), new byte[12], ikm, plainText.ToArray());
                boolAllGood = true;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Failed to Decrypt: {ex.ToString()} CallStack: {ex.StackTrace}");
            }
            return result;
        }


        /// <summary>
        /// Zeroes out a byte array to remove our keys from memory
        /// </summary>
        /// <param name="key"></param>
        public static void ZeroFillByteArray(ref byte[] key)
        {
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = 0;
            }
        }


    }


}

public static class UtilClass
{
    public static string CleanUpString(this string s)
    {
        return s.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\\n", "");
    }

    public static void ZeroFill(this byte[] ary)
    {
        SQRLUtilsLib.SQRL.ZeroFillByteArray(ref ary);
    }

    public static StringBuilder AppendLineWindows(this StringBuilder sb, string s)
    {
        sb.Append(s);
        sb.Append("\r\n");
        return sb;
    }
}
