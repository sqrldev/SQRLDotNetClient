using Sodium;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Serilog;

namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents the available SQRL client commands.
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
        private static Lazy<SQRL> _instance = null;
        private static bool SodiumInitialized = false;
        private static readonly char[] BASE56_ALPHABETH = { '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        private const int ENCODING_BASE = 56;
        private const int CLIENT_VERSION = 1;

        /// <summary>
        /// Represents the "User-Agent" header that will be sent by all HTTP
        /// queries performed by the library.
        /// </summary>
        public static string UserAgentHeader { get; set; } = "OSS SQRL Library for .NET Core";

        /// <summary>
        /// The server component handling the Client-Protected-Session (CPS).
        /// </summary>
        public SQRLCPSServer cps=null;

        /// <summary>
        /// Creates a new instance of the SQRL library and optionally
        /// starts the CPS server. This constructor is private, please use
        /// <c>SQRL.GetInstance()</c> to obtain a <c>SQRL</c> instance.
        /// </summary>
        /// <param name="startCPS">Set to true if the CPS server should be started, or false otherwise.</param>
        private SQRL(bool startCPS=false)
        {
            Log.Information("Creating new SQRL library instance");

            SodiumInit();

            if (startCPS)
            {
                Log.Information("Starting CPS server");
                this.cps = new SQRLCPSServer();
            }
        }

        /// <summary>
        /// Returns a singleton instance of the SQRL library. If an instance
        /// was already created before, it will be returned instead of creating
        /// another one. Only on the first call of this method, a new instance 
        /// will be created.
        /// </summary>
        /// <param name="startCPS">Set to true if the CPS server should be started, or false otherwise.</param>
        public static SQRL GetInstance(bool startCPS = false)
        {
            if (_instance == null)
            {
                _instance = new Lazy<SQRL>(() => new SQRL(startCPS));
            }

            return _instance.Value;
        }

        /// <summary>
        /// Creates and returns a random Identity Unlock Key (IUK).
        /// </summary>
        public static byte[] CreateIUK()
        {
            SodiumInit();

            Log.Information($"Creating IUK");
            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        /// <summary>
        /// Creates and returns a 24 character random "rescue code".
        /// </summary>
        public static string CreateRescueCode()
        {
            SodiumInit();

            Log.Information($"Creating rescue code");

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
        /// Creates and returns an Unlock Request Signing Key (URSK) from the given
        /// Identity Unlock Key (IUK) and Server Unlock Key (SUK).
        /// </summary>
        /// <param name="IUK">The identity's Identity Unlock Key (IUK).</param>
        /// <param name="SUK">The Server Unlock Key (SUK).</param>
        public static byte[] GetURSKey(byte[] IUK, byte[] SUK)
        {
            SodiumInit();

            Log.Information($"Creating Unlock Request Signing Key (URSK)");

            var bytesToSign = Sodium.ScalarMult.Mult(IUK, SUK);
            var ursKeyPair = Sodium.PublicKeyAuth.GenerateKeyPair(bytesToSign);

            return ursKeyPair.PrivateKey;
        }

        /// <summary>
        /// Creates and regturns an Identity Master Key (IMK), derived from the given 
        /// Identity Unlock Key (IUK).
        /// </summary>
        /// <param name="iuk">The identity's Identity Unlock Key (IUK).</param>
        public static byte[] CreateIMK(byte[] iuk)
        {
            SodiumInit();

            Log.Information($"Creating Identity Master Key (IMK)");
            return EnHash(iuk);
        }

        /// <summary>
        /// Creates and returns an Identity Lock Key (ILK), derived from the given
        /// Identity Unlock Key (IUK).
        /// </summary>
        /// <param name="iuk">The identity's Identity Unlock Key (IUK).</param>
        public static byte[] CreateILK(byte[] iuk)
        {
            SodiumInit();

            Log.Information($"Creating Identity Lock Key (ILK)");
            return Sodium.ScalarMult.Base(iuk);
        }

        /// <summary>
        /// Generates and returns a Random Lock Key (RLK).
        /// </summary>
        public static byte[] RandomLockKey()
        {
            SodiumInit();

            Log.Information($"Creating Random Lock Key (RLK)");
            return Sodium.SodiumCore.GetRandomBytes(32);
        }

        /// <summary>
        /// Creates and returns a Server Unlock Key (SUK) / Verification Unlock Key (VUK) keypair,
        /// derived from the given Identity Lock Key (ILK).
        /// </summary>
        /// <param name="ILK">The identity's Identity Lock Key (ILK).</param>
        public static SukVukResult GetSukVuk(byte[] ILK)
        {
            SodiumInit();

            Log.Information($"Creating SUK/VUK keypair");

            var RLK = RandomLockKey();
            var SUK = Sodium.ScalarMult.Base(RLK);

            var bytesToSign = Sodium.ScalarMult.Mult(RLK, ILK);
            var vukKeyPair = Sodium.PublicKeyAuth.GenerateKeyPair(bytesToSign);
            var VUK = vukKeyPair.PublicKey;

            return new SukVukResult(SUK, VUK);
        }

        /// <summary>
        /// Initializes the "Libsodium" crypto library.
        /// </summary>
        private static void SodiumInit()
        {
            if (!SodiumInitialized)
            {
                Log.Information("Initializing SodiumCore");
                SodiumCore.Init();
                SodiumInitialized = true;
            }
        }

        /// <summary>
        /// Runs the given data trough the "EnHash" algorithm and returns the result.
        /// </summary>
        /// <remarks>
        /// SHA256 is iterated 16 times with each successive output XORed to 
        /// form a 1’s complement sum to produce the final result.
        /// </remarks>
        /// <param name="data">The input data to be EnHash'ed.</param>
        public static byte[] EnHash(byte[] data)
        {
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
        /// Run the "Scrypt" memory hard key derivation function on the given <paramref name="password"/> 
        /// for a determined amount of <paramref name="secondsToRun"/>, using the given <paramref name="randomSalt"/> 
        /// and <paramref name="logNFactor"/>.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="randomSalt">Random data which is being used as salt for Scrypt.</param>
        /// <param name="logNFactor">Log N Factor for Scrypt.</param>
        /// <param name="secondsToRun">Amount of time to run Scrypt (determines iteration count).</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <param name="progressText">A string representing a text descrition for the progress indicator (optional).</param>
        public static async Task<EnScryptTimeResult> EnScryptTime(String password, byte[] randomSalt, int logNFactor, int secondsToRun, 
            IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            SodiumInit();

            Log.Information($"Running EnScryptTime for {secondsToRun} seconds");

            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            int count = 0;

            return await Task.Run(() =>
            {
                count = 0;
                while (Math.Abs((DateTime.Now - startTime).TotalSeconds) < secondsToRun)
                {
                    byte[] key = PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

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
                return new EnScryptTimeResult(count, xorKey);
            });
        }

        /// <summary>
        /// Run the "Scrypt" memory hard key derivation function for a specified 
        /// number of interations to recreate the time-generated value.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="randomSalt">Random data which is being used as salt for Scrypt.</param>
        /// <param name="logNFactor">Log N Factor for Scrypt.</param>
        /// <param name="nrOfIterations">Number of Scrypt iterations (inclusive).</param>
        public static async Task<byte[]> EnScryptCT(String password, byte[] randomSalt, int logNFactor, int nrOfIterations, 
            IProgress<KeyValuePair<int, string>> progress = null, string progressText = null)
        {
            SodiumInit();

            Log.Information($"Running EnScryptCT for {nrOfIterations} iterations");

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            
            var kvp = await Task.Run(() =>
            {
                byte[] xorKey = new byte[32];
                int count = 0;
                while (count < nrOfIterations)
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
                        int prog = (int)(((double)count / (double)nrOfIterations) * 100);
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
        /// Generates a new type 0 block for the given <paramref name="imk"/> and places 
        /// it into the given <paramref name="identity"/>. If no block of type 0 is present 
        /// in the given identity, it will be created. Otherwise, it will be overwritten.
        /// </summary>
        /// <param name="imk">The unencrypted Identity Master Key (IMK).</param>
        /// <param name="identity">The identity for which to generate the type 0 block.</param>
        /// <returns></returns>
        public static SQRLIdentity GenerateIdentityBlock0(byte[] imk, SQRLIdentity identity)
        {
            if (identity == null)
                throw new ArgumentException("A valid identity must be provided!");

            Log.Information($"Generating identity block of type 0");

            if (!identity.HasBlock(0))
                identity.Blocks.Add(new SQRLBlock0());

            var siteKeyPair = CreateSiteKey(null, "", imk);
            identity.Block0.GenesisIdentifier = siteKeyPair.PublicKey;
            identity.Block0.UniqueIdentifier = SodiumCore.GetRandomBytes(32);
                       
            return identity;
        }

        /// <summary>
        /// Generates a new type 1 block for the given identity based on the given parameters. 
        /// If no block of type 1 is present in the given identity, it will be created. Otherwise,
        /// it will be overwritten.
        /// Also, a new random initialization vector and scrypt random salt will be
        /// created and used for the generation of the type 1 block.
        /// </summary>
        /// <param name="iuk">The unencrypted Identity Unlock Key (IUK) for creating the block 1 keys (IMK/ILK).</param>
        /// <param name="password">The password under which the new type 1 block will be encrypted.</param>
        /// <param name="identity">The identity for which to generate the type 1 block.</param>
        /// <param name="progress">An obect implementing the IProgress interface for monitoring the operation's progress (optional).</param>
        /// <param name="encTime">The time in seconds to run the EnScrypt PBKDF on <paramref name="password"/>. Defaults to 5 seconds.</param>
        public static async Task<SQRLIdentity> GenerateIdentityBlock1(byte[] iuk, String password, SQRLIdentity identity, IProgress<KeyValuePair<int,string>> progress=null, int encTime=5)
        {
            byte[] imk = CreateIMK(iuk);
            byte[] ilk = CreateILK(iuk);

            return await GenerateIdentityBlock1(imk, ilk, password, identity, progress, encTime);
        }

        /// <summary>
        /// Generates a new type 1 block for the given identity based on the given parameters. 
        /// If no block of type 1 is present in the given identity, it will be created. Otherwise,
        /// it will be overwritten.
        /// Also, a new random initialization vector and scrypt random salt will be
        /// created and used for the generation of the type 1 block.
        /// </summary>
        /// <param name="imk">The unencrypted Identity Master Key (IMK).</param>
        /// <param name="ilk">The unencrypted Identity Lock Key (ILK).</param>
        /// <param name="password">The password under which the new type 1 block will be encrypted.</param>
        /// <param name="identity">The identity for which to generate the type 1 block.</param>
        /// <param name="progress">An obect implementing the IProgress interface for monitoring the operation's progress (optional).</param>
        /// <param name="encTime">The time in seconds to run the EnScrypt PBKDF on <paramref name="password"/>. Defaults to 5 seconds.</param>
        public static async Task<SQRLIdentity> GenerateIdentityBlock1(byte[] imk, byte[] ilk, string password, SQRLIdentity identity, IProgress<KeyValuePair<int, string>> progress=null, int encTime=5)
        {
            SodiumInit();

            Log.Information($"Generating identity block of type 1");

            if (!identity.HasBlock(1))
                identity.Blocks.Add(new SQRLBlock1());

            byte[] initVector = Sodium.SodiumCore.GetRandomBytes(12);
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);
            var enScryptResult = await EnScryptTime(password, randomSalt, (int)Math.Pow(2, 9), encTime, progress, "Generating Block 1");

            var identityT = await Task.Run(() =>
            {
                identity.Block1.AesGcmInitVector = initVector;
                identity.Block1.ScryptRandomSalt = randomSalt;
                identity.Block1.IterationCount = (uint)enScryptResult.IterationCount;

                List<byte> plainText = new List<byte>();
                plainText.AddRange(GetBytes(identity.Block1.Length));
                plainText.AddRange(GetBytes(identity.Block1.Type));
                plainText.AddRange(GetBytes(identity.Block1.InnerBlockLength));
                plainText.AddRange(GetBytes(identity.Block1.AesGcmInitVector));
                plainText.AddRange(GetBytes(identity.Block1.ScryptRandomSalt));
                plainText.Add(identity.Block1.LogNFactor);
                plainText.AddRange(GetBytes(identity.Block1.IterationCount));
                plainText.AddRange(GetBytes(identity.Block1.OptionFlags.FlagsValue));
                plainText.Add(identity.Block1.HintLength);
                plainText.Add(identity.Block1.PwdVerifySeconds);
                plainText.AddRange(GetBytes(identity.Block1.PwdTimeoutMins));

                IEnumerable<byte> unencryptedKeys = imk.Concat(ilk);

                byte[] encryptedData = AesGcmEncrypt(unencryptedKeys.ToArray(), plainText.ToArray(), initVector, enScryptResult.Key); //Should be 80 bytes
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
        /// <param name="iuk">The identity's Identity Unlock Key (IUK).</param>
        /// <param name="rescueCode">The identity's secret "rescue code".</param>
        /// <param name="identity">The <c>SQRLIdentity</c> for which to create the new type 2 block.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional)</param>
        /// <param name="encTime">The time in seconds to run the memory hard key derivation function (optional, defaults to 5 seconds).</param>
        public static async Task<SQRLIdentity> GenerateIdentityBlock2(byte[] iuk, String rescueCode, SQRLIdentity identity, IProgress<KeyValuePair<int,string>> progress = null, int encTime=5)
        {
            SodiumInit();

            if (!identity.HasBlock(2))
                identity.Blocks.Add(new SQRLBlock2());

            Log.Information($"Generating identity block of type 2");

            byte[] initVector = new byte[12];
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);

            var enScryptResult = await EnScryptTime(rescueCode, randomSalt, (int)Math.Pow(2, 9), encTime, progress,"Generating Block 2");
            var identityT = await Task.Run(() =>
            {
                identity.Block2.RandomSalt = randomSalt;
                identity.Block2.IterationCount = (uint)enScryptResult.IterationCount;

                List<byte> plainText = new List<byte>();
                plainText.AddRange(GetBytes(identity.Block2.Length));
                plainText.AddRange(GetBytes(identity.Block2.Type));
                plainText.AddRange(identity.Block2.RandomSalt);
                plainText.Add(identity.Block2.LogNFactor);
                plainText.AddRange(GetBytes(identity.Block2.IterationCount));

                byte[] encryptedData = AesGcmEncrypt(iuk, plainText.ToArray(), initVector, enScryptResult.Key); //Should be 80 bytes
                identity.Block2.EncryptedIUK = encryptedData.ToList().GetRange(0, 32).ToArray(); ;
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
        private static IEnumerable<byte> GetBytes(object v)
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
        /// Encrypts a given message using the AES-GCM Authenticated Encryption and returns the result.
        /// </summary>
        /// <param name="message">The message to be encrypted.</param>
        /// <param name="additionalData">The additional data used for authenticating the encryption.</param>
        /// <param name="iv">The initialization vector for the AES-GCM encryption.</param>
        /// <param name="key">The key for the AES-GCM encryption.</param>
        public static byte[] AesGcmEncrypt(byte[] message, byte[] additionalData, byte[] iv, byte[] key)
        {
            SodiumInit();

            Log.Information($"Running AesGcmEncrypt");

            //Had to override Sodium Core to allow more than 16 bytes of additional data
            byte[] cipherText = Sodium.SecretAeadAes.Encrypt(message, iv, key, additionalData);

            return cipherText;
        }

        /// <summary>
        /// Formats a rescue code string for displaying it to the user by
        /// adding a dash every 4th character and returns the result.
        /// 
        /// <para>The resulting formatted rescue code should look something like this:</para>
        /// <c>1234-5678-9012-3456-7890-1234</c>
        /// 
        /// </summary>
        /// <param name="rescueCode">The unformatted rescue code string.</param>
        public static string FormatRescueCodeForDisplay(string rescueCode)
        {
            Log.Information($"Formatting rescue code for display");
            return Regex.Replace(rescueCode, ".{4}(?!$)", "$0-");
        }

        /// <summary>
        /// Cleans the given rescue code string from any formatting by
        /// removing any dashes ("-") and spaces (" ") and returns the result.
        /// </summary>
        /// <param name="rescueCode">The formatted rescue code string to be cleaned.</param>
        public static string CleanUpRescueCode(string rescueCode)
        {
            Log.Information($"Cleaning up rescue code");
            if (!string.IsNullOrEmpty(rescueCode)) return rescueCode.Trim().Replace(" ", "").Replace("-", "");
            else return string.Empty;
        }

        /// <summary>
        /// Generates and returns a base56-encoded "textual version" of the given identity.
        /// </summary>
        /// <param name="sqrlId">The identity to be encoded.</param>
        public static string GenerateTextualIdentityFromSqrlID(SQRLIdentity sqrlId)
        {
            return GenerateTextualIdentityBase56(sqrlId.Block2.ToByteArray().Concat(sqrlId.Block3.ToByteArray()).ToArray());
        }

        /// <summary>
        /// Generates and returns a base56-encoded "textual version" of the given identity bytes.
        /// </summary>
        /// <param name="identity">The raw byte data of the identity to be encoded.</param>
        public static string GenerateTextualIdentityBase56(byte[] identity)
        {
            Log.Information($"Generating textual identity");

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
        /// Generates and returns a checksum character for a given base56-encoded textual identity line.
        /// </summary>
        /// <param name="dataBytes">The bytes to create the checksum character from.</param>
        public static char GetBase56CheckSum(byte[] dataBytes)
        {
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
        /// <param name="textID">The unformatted textual identity.</param>
        public static string FormatTextualIdentity(char[] textID)
        {
            Log.Information($"Formatting textual identity");

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
        /// <param name="identityTxt">The base-56 encoded "textual version" of the identity.</param>
        /// <param name="rescueCode">The identity's rescue code.</param>
        /// <param name="newPassword">The new password for encrypting the identity's block 1 keys.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        public static async Task<SQRLIdentity> DecodeSqrlIdentityFromText(string identityTxt, string rescueCode, string newPassword, Progress<KeyValuePair<int, string>> progress = null)
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

            var result = await DecryptBlock2(identity, rescueCode, progress);
            if (result.DecryptionSucceeded)
                identity = await GenerateIdentityBlock1(result.Iuk, newPassword, identity, progress);

            return identity;
        }

        /// <summary>
        /// Decodes a base-56 encoded "textual identity" into a byte array.
        /// </summary>
        /// <param name="identityStr">The base-56 encoded "textual version" of the identity.</param>
        /// <param name="bypassCheck">If set to <c>true</c>, the result of the verification of the textual identity will be ignored.</param>
        public static byte[] Base56DecodeIdentity(string identityStr, bool bypassCheck = false)
        {
            Log.Information($"Base56-decoding textual identity");

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
        /// <param name="identityStr">The base-56 encoded "textual version" of the identity which should be checked.</param>
        /// <returns>Returns <c>true</c>if the verfification succeeds, and <c>false</c> otherwise.</returns>
        public static bool VerifyEncodedIdentity(string identityStr)
        {
            Log.Information($"Verifying textual identity");

            //Remove whitespace
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
                {
                    Log.Warning($"Textual identity verification failed");
                    return false;
                }

                lineNr++;
            }

            Log.Information($"Textual identity verification succeeded");
            return true;
        }

        /// <summary>
        /// Decrypts a SQRL identity's type 1 block and provides access to the unencrypted
        /// Identity Master Key (IMK) and the Identity Lock Key (ILK).
        /// </summary>
        /// <param name="identity">The identity containing the type 1 block to be decrypted.</param>
        /// <param name="password">The identity's password.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <returns>Returns an object containing the operation's success status, the decrypted IMK and the decrypted ILK.</returns>
        public static async Task<DecryptBlock1Result> DecryptBlock1(SQRLIdentity identity, string password, IProgress<KeyValuePair<int,string>> progress = null)
        {
            Log.Information($"Decryptin identity block 1");

            byte[] key = await EnScryptCT(password, identity.Block1.ScryptRandomSalt, (int)Math.Pow(2, identity.Block1.LogNFactor), (int)identity.Block1.IterationCount, progress, "Decrypting Block 1");
            bool allgood = false;
            List<byte> plainText = new List<byte>();

            plainText.AddRange(GetBytes(identity.Block1.Length));
            plainText.AddRange(GetBytes(identity.Block1.Type));
            plainText.AddRange(GetBytes(identity.Block1.InnerBlockLength));
            plainText.AddRange(GetBytes(identity.Block1.AesGcmInitVector));
            plainText.AddRange(GetBytes(identity.Block1.ScryptRandomSalt));
            plainText.Add(identity.Block1.LogNFactor);
            plainText.AddRange(GetBytes(identity.Block1.IterationCount));
            plainText.AddRange(GetBytes(identity.Block1.OptionFlags.FlagsValue));
            plainText.Add(identity.Block1.HintLength);
            plainText.Add(identity.Block1.PwdVerifySeconds);
            plainText.AddRange(GetBytes(identity.Block1.PwdTimeoutMins));

            return await Task.Run(() =>
            {
                byte[] encryptedKeys = identity.Block1.EncryptedIMK
                    .Concat(identity.Block1.EncryptedILK)
                    .Concat(identity.Block1.VerificationTag).ToArray();

                byte[] result = null;
                try
                {
                    result = Sodium.SecretAeadAes.Decrypt(encryptedKeys, identity.Block1.AesGcmInitVector, key, plainText.ToArray());
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

                if (!allgood) Log.Warning($"Decryption of identity block 1 failed!");

                return new DecryptBlock1Result(allgood, imk, ilk);
            });
        }

        /// <summary>
        /// Decrypts a SQRL identity's type 2 block and provides access to the unencrypted
        /// Identity Unlock Key (IUK).
        /// </summary>
        /// <param name="identity">The identity containing the type 2 block to be decrypted.</param>
        /// <param name="rescueCode">The identity's secret rescue code.</param>
        /// <param name="progress">An object implementing the IProgress interface for tracking the operation's progress (optional).</param>
        /// <returns>Returns an object representing the operation's success, the decrypted IUK and an optional error message.</returns>
        public static async Task<DecryptBlock2Result> DecryptBlock2(SQRLIdentity identity, string rescueCode, IProgress<KeyValuePair<int, string>> progress = null)
        {
            Log.Information($"Decrypting identity block 2");

            byte[] key = await EnScryptCT(rescueCode, identity.Block2.RandomSalt, (int)Math.Pow(2, identity.Block2.LogNFactor), 
                (int)identity.Block2.IterationCount, progress,"Decrypting Block 2");

            return await Task.Run(() =>
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
                byte[] encryptedKeys = identity.Block2.EncryptedIUK.Concat(identity.Block2.VerificationTag).ToArray();
                byte[] result = null;
                try
                {
                    result = Sodium.SecretAeadAes.Decrypt(encryptedKeys, initVector, key, plainText.ToArray());
                }
                catch(Exception x)
                {
                    Console.Error.WriteLine($"Failed to decrypt: {x.ToString()} CallStack: {x.StackTrace}");
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

                if (!allGood) Log.Warning($"Decryption of identity block 2 failed!");

                return new DecryptBlock2Result(allGood, iuk, (!allGood ? 
                    "Decryption failed. Bad password or rescue code" : ""));
            });
        }

        /// <summary>
        /// Creates a site-specific ECDH public-private key pair for each of the provided previous Identity Unlock Keys (IUKs).
        /// </summary>
        /// <param name="oldIUKs">The list of previous Identity Unlock Keys (IUKs).</param>
        /// <param name="domain">The domain for which to generate the key pairs.</param>
        /// <param name="altID">The "Alternate Id" that should be used for the keypair generation.</param>
        /// <returns>Returns a <c>Dictionary</c> where the key represents the PIUK and the value contains an object
        /// encapsulating the site seed as well as the actual ECDH public-private key pair for that particular PIUK.</returns>
        public static Dictionary<byte[], PriorSiteKeysResult> CreatePriorSiteKeys(List<byte[]> oldIUKs, Uri domain, String altID)
        {
            Dictionary<byte[], PriorSiteKeysResult> priorSiteKeys = new Dictionary<byte[], PriorSiteKeysResult>();
            foreach(var oldIUK in oldIUKs)
            {
                PriorSiteKeysResult result = new PriorSiteKeysResult(
                    CreateSiteSeed(domain, altID, CreateIMK(oldIUK)),
                    CreateSiteKey(domain, altID, CreateIMK(oldIUK)));

                priorSiteKeys.Add(oldIUK, result);
            }

            return priorSiteKeys;
        }

        /// <summary>
        /// Creates a site-specific ECDH public-private key pair for the given domain and altID (if available).
        /// </summary>
        /// <param name="domain">The domain for which to generate the key pair.
        /// Can be null tp produce a key pair for an empty domain.</param>
        /// <param name="altID">The "Alternate Id" that should be used for the keypair generation.</param>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="test">This is specifically for the vector tests since they don't use the x param (should be fixed).</param>
        public static Sodium.KeyPair CreateSiteKey(Uri domain, String altID, byte[] imk, bool test = false)
        {
            byte[] siteSeed = CreateSiteSeed(domain, altID, imk, test);
            Sodium.KeyPair kp = Sodium.PublicKeyAuth.GenerateKeyPair(siteSeed);

            return kp;
        }

        /// <summary>
        /// Creates and returns the so called "Indexed Secret" (INS) 
        /// for the given, server-provided, "Secret Index" (SIN).
        /// </summary>
        /// <param name="domain">The domain for which to generate the Indexed Secret.</param>
        /// <param name="altID">The "Alternate Id" that should be used.</param>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="secretIndex">The server-provided Secret Index (SIN).</param>
        /// <param name="test">This is specifically for the vector tests since they don't use the x param (should be fixed).</param>
        public static byte[] CreateIndexedSecret(Uri domain, String altID, byte[] imk, byte[] secretIndex, bool test = false)
        {
            byte[] siteSeed = CreateSiteSeed(domain, altID, imk, test);
            byte[] key = EnHash(siteSeed);
            byte[] indexedSecret = Sodium.SecretKeyAuth.SignHmacSha256(secretIndex, key);

            return indexedSecret;
        }

        /// <summary>
        /// Creates and returns the so called "Indexed Secret" (INS)
        /// for the given, server-provided, "Secret Index" (SIN) using an existing site seed.
        /// </summary>
        /// <param name="siteSeed">The precomputed site seed.</param>
        /// <param name="secretIndex">The server-provided Secret Index (SIN).</param>
        /// <param name="test">This is specifically for the vector tests since they don't use the x param (should be fixed).</param>
        public static byte[] CreateIndexedSecret(byte[] siteSeed, byte[] secretIndex, bool test = false)
        {
            byte[] key = EnHash(siteSeed);
            byte[] indexedSecret = Sodium.SecretKeyAuth.SignHmacSha256(secretIndex, key);

            return indexedSecret;
        }

        /// <summary>
        /// Creates a cryptographic seed from the given domain and altID (if available), which is
        /// used for driving seeded cryptograhic functions such as <c>crypto_sign_seed_keypair()</c>.
        /// In SQRL, this seed is used for creating site-specific key pairs as well as for creating
        /// the so called "Indexed Secret" (INS) from a server-provided "Secret Index" (SIN).
        /// </summary>
        /// <param name="domain">The domain for which to generate the Indexed Secret.
        /// Can be null tp produce the seed for an empty domain.</param>
        /// <param name="altID">The "Alternate Id" that should be used.</param>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="test">This is specifically for the vector tests since they don't use the x param (should be fixed).</param>
        private static byte[] CreateSiteSeed(Uri domain, String altID, byte[] imk, bool test = false)
        {
            byte[] domainBytes = { };

            if (!SodiumInitialized)
                SodiumInit();
            
            if (domain != null)
            {
                domainBytes = Encoding.UTF8.GetBytes(domain.DnsSafeHost + (test ? (domain.LocalPath.Equals("/") ? "" : domain.LocalPath) : ""));

                var nvC = HttpUtility.ParseQueryString(domain.Query);
                if (nvC["x"] != null)
                {
                    string extended = domain.LocalPath.Substring(0, int.Parse(nvC["x"]));
                    domainBytes = domainBytes.Concat(Encoding.UTF8.GetBytes(extended)).ToArray();
                }
            }

            if (!string.IsNullOrEmpty(altID))
            {
                domainBytes = domainBytes.Concat(new byte[] { 0 }).Concat(Encoding.UTF8.GetBytes(altID)).ToArray();
            }

            byte[] siteSeed = Sodium.SecretKeyAuth.SignHmacSha256(domainBytes, imk);

            return siteSeed;
        }

        /// <summary>
        /// Generates an <c>ident</c> request and posts it to the server.
        /// </summary>
        /// <param name="sqrl">The server URI</param>
        /// <param name="siteKP">The precomputed site-specific ECDH key pair.</param>
        /// <param name="priorServerMessaage">The base64_url-encoded prior server message.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message (SUK, CPS etc).</param>
        /// <param name="message"></param>
        /// <param name="addClientData">Optional additional data to be appended to the client message (e.g. VUK / SUK etc.).</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object, representing the server's response details.</returns>
        public static SQRLServerResponse GenerateIdentCommand(Uri sqrl, KeyPair siteKP, string priorServerMessaage, string[] opts, out string message, StringBuilder addClientData = null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            SQRLServerResponse serverResponse = null;
            message = "";
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);
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
        /// Generates a <c>query</c> command, posts it to the server and returns the server's response.
        /// If a transient error occurs, the query is repeated up to 3 times (see count parameter).
        /// </summary>
        /// <param name="sqrl">The SQRL server URI</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="encodedServerMessage">Optional base64_url-encoded contents for the server parameter.</param>
        /// <param name="count">The number of times this function was called successively.</param>
        /// <param name="priorSiteKeys">An optional list of Previous Identity Key (PIDK) key pairs to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateQueryCommand(Uri sqrl, KeyPair siteKP, SQRLOptions opts = null, string encodedServerMessage=null, int count = 0, Dictionary<byte[], PriorSiteKeysResult> priorSiteKeys=null)
        {
            SQRLServerResponse serverResponse = null;
            if(encodedServerMessage==null)
            {
                encodedServerMessage = Sodium.Utilities.BinaryToBase64(
                    Encoding.UTF8.GetBytes(sqrl.OriginalString), 
                    Utilities.Base64Variant.UrlSafeNoPadding);
            }
            serverResponse = GenerateSQRLCommand(SQRLCommands.query, sqrl, siteKP, 
                encodedServerMessage, null, opts, priorSiteKeys?.First().Value.KeyPair);
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
        /// Sends a new identity (along with VUK/SUK) to server along with an ident command
        /// and returns the server's response.
        /// </summary>
        /// <param name="sqrl">The SQRL server URI.</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="encodedServerMessage">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="ilk">The Identity Lock Key (ILK).</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="sin">Optional base64_url-encoded Secret Index (SIN) parameter to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateNewIdentCommand(Uri sqrl, KeyPair siteKP, string encodedServerMessage, byte[] ilk, SQRLOptions opts = null, StringBuilder sin=null)
        {
            var sukvuk = GetSukVuk(ilk);
            StringBuilder addClientData = new StringBuilder();
            addClientData.AppendLineWindows($"suk={Sodium.Utilities.BinaryToBase64(sukvuk.Suk, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            addClientData.AppendLineWindows($"vuk={Sodium.Utilities.BinaryToBase64(sukvuk.Vuk, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            if (sin != null)
                addClientData.Append(sin);

            return GenerateSQRLCommand(SQRLCommands.ident, sqrl, siteKP, encodedServerMessage, addClientData, opts, null);
        }

        /// <summary>
        /// Generates an ident command and sends new SUK and VUK but signs it with the old URS, effectively replacing the old identity with a new rekeyed one.
        /// </summary>
        /// <param name="sqrl">The SQRL server URI.</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="encodedServerMessage">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="ilk">The Identity Lock Key (ILK).</param>
        /// <param name="ursKey">The Unlock Request Signing Key (URSK).</param>
        /// <param name="priorKey">The Previous Identity Key (PIDK) key pair to be appended to the client message.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="sin">Optional base64_url-encoded Secret Index (SIN) parameter to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateIdentCommandWithReplace(Uri sqrl, KeyPair siteKP, string encodedServerMessage, byte[] ilk, byte[] ursKey, KeyPair priorKey, SQRLOptions opts = null, StringBuilder sin=null)
        {
            var sukvuk = GetSukVuk(ilk);
            StringBuilder addClientData = new StringBuilder();
            addClientData.AppendLineWindows($"suk={Sodium.Utilities.BinaryToBase64(sukvuk.Suk, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            addClientData.AppendLineWindows($"vuk={Sodium.Utilities.BinaryToBase64(sukvuk.Vuk, Sodium.Utilities.Base64Variant.UrlSafeNoPadding)}");
            if (sin != null)
                addClientData.Append(sin);

            return GenerateSQRLCommand(SQRLCommands.ident, sqrl, siteKP, encodedServerMessage, addClientData, opts, priorKey, ursKey);
        }

        /// <summary>
        /// Generates an <c>enable</c> client command, posts it to the server and returns the server's response.
        /// </summary>
        /// <param name="sqrl">The SQRL server URI.</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="encodedServerMessage">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="ursKey">The Unlock Request Signing Key (URSK).</param>
        /// <param name="addClientData">Optional additional data to be appended to the client message.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateEnableCommand(Uri sqrl, KeyPair siteKP, string encodedServerMessage, byte[] ursKey, StringBuilder addClientData=null, SQRLOptions opts = null)
        {
            return GenerateSQRLCommand(SQRLCommands.enable, sqrl, siteKP, encodedServerMessage, addClientData, opts,null, ursKey);
        }

        /// <summary>
        /// Generates a client command, posts it to the server and returns the server's response.
        /// </summary>
        /// <param name="sqrl">The SQRL server URI.</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="priorServerMessage">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="command">The client command to be sent.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="addClientData">Optional additional data to be appended to the client message.</param>
        /// <param name="priorSiteKP">Optional Previous Identity Key (PIDK) key pair to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateCommand(Uri sqrl, KeyPair siteKP, string priorServerMessage, string command, SQRLOptions opts, StringBuilder addClientData = null, KeyPair priorSiteKP=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            SQRLServerResponse serverResponse = null;
            
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);

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

                Dictionary<string, string> strContent = GenerateResponse( siteKP, client, priorServerMessage, priorSiteKP);
                var content = new FormUrlEncodedContent(strContent);

                var response = wc.PostAsync($"https://{sqrl.Host}{(sqrl.IsDefaultPort ? "" : $":{sqrl.Port}")}{sqrl.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrl.Host, sqrl.IsDefaultPort ? 443 : sqrl.Port);
            }

            return serverResponse;
        }

        /// <summary>
        /// Generates a client command with an Unlock Request Signature (URS), 
        /// posts it to the server and returns the server's response.
        /// </summary>
        /// <param name="sqrl">The SQRL server URI.</param>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="ursKey">The Unlock Request Signing Key (URSK).</param>
        /// <param name="priorServerMessage">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="command">The client command to be sent.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="addClientData">Optional additional data to be appended to the client message.</param>
        /// <param name="priorMatchedKey">Optional Previous Identity Key (PIDK) key pair to be appended to the client message.</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateCommandWithURS(Uri sqrl, KeyPair siteKP, byte[] ursKey, string priorServerMessage, string command, SQRLOptions opts = null, StringBuilder addClientData = null, KeyPair priorMatchedKey=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            SQRLServerResponse serverResponse = null;

            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);

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
                string encodedServer = priorServerMessage;

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
        /// Generates a signature with the provided <paramref name="signatureID"/> (urs, ids, pids... etc.)
        /// for the given <paramref name="encodedServer"/> and <paramref name="client"/> parameter values, 
        /// signed by the given <paramref name="key"/>.
        /// </summary>
        /// <param name="signatureID">The type of the signature to be created (urs, ids, pids... etc.).</param>
        /// <param name="encodedServer">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="client">The unencoded contents for the client parameter.</param>
        /// <param name="key">The private key for signing the message.</param>
        /// <returns>Returns a <c>SignatureResult</c> object containing the chosen signature id (urs, ids, pids... etc.)
        /// and the base64_url-encoded signature.</returns>
        public static SignatureResult GenerateSignature(string signatureID, string encodedServer, string client, byte[] key)
        {
            string encodedClient = Sodium.Utilities.BinaryToBase64(
                Encoding.UTF8.GetBytes(client.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            
            byte[] signature = Sodium.PublicKeyAuth.SignDetached(Encoding.UTF8.GetBytes(encodedClient + encodedServer), key);
            string encodedSignature = Sodium.Utilities.BinaryToBase64(signature, Utilities.Base64Variant.UrlSafeNoPadding);

            return new SignatureResult(signatureID, encodedSignature);
        }

        /// <summary>
        /// Generates an Unlock Request Signature (URS) for the given a <paramref name="encodedServer"/> 
        /// and <paramref name="client"/> parameter values, signed by <paramref name="ursKey"/>.
        /// </summary>
        /// <param name="encodedServer">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="client">The unencoded contents for the client parameter.</param>
        /// <param name="ursKey">The Unlock Request Signing Key (URSK).</param>
        /// <returns>Returns a <c>SignatureResult</c> object containing the signature id ("urs") 
        /// and the base64_url-encoded signature.</returns>
        public static SignatureResult GenerateURS(string encodedServer, string client, byte[] ursKey)
        {
            return GenerateSignature("urs", encodedServer, client, ursKey);
        }

        /// <summary>
        /// Generates a Previous Identity Signature (PIDS) for the given a <c>server</c> and <c>client</c>
        /// parameter values, signed by <c>pidkKey</c>.
        /// </summary>
        /// <param name="encodedServer">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="client">The unencoded contents for the client parameter.</param>
        /// <param name="pidkKey">The Previous Identity Key (PIDK).</param>
        /// <returns>Returns a <c>SignatureResult</c> object, containing the signature id ("pids")
        /// and the base64_url-encoded signature.</returns>
        public static SignatureResult GeneratePIDS(string encodedServer, string client, byte[] pidkKey)
        {
            return GenerateSignature("pids", encodedServer, client, pidkKey);
        }

        /// <summary>
        /// Generates an Identity Signature (IDS) for the given a <c>server</c> and <c>client</c>
        /// parameter values, signed by <c>idkKey</c>.
        /// </summary>
        /// <param name="encodedServer">The base64_url-encoded contents for the server parameter.</param>
        /// <param name="client">The unencoded contents for the client parameter.</param>
        /// <param name="idkKey">The site-specific private Identity Key (IDK).</param>
        /// <returns>Returns a <c>SignatureResult</c> object, containing the signature id ("ids")
        /// and the base64_url-encoded signature.</returns>
        public static SignatureResult GenerateIDS(string encodedServer, string client, byte[] idkKey)
        {
            return GenerateSignature("ids", encodedServer, client, idkKey);
        }

        /// <summary>
        /// Creates a SQRL command, posts it to the server and receives the server's response.
        /// </summary>
        /// <param name="command">The command to send to the server.</param>
        /// <param name="sqrlUri">The SQRL server URI.</param>
        /// <param name="currentSiteKeyPair">The site-specific ECDH public-private key pair.</param>
        /// <param name="encodedServer">The base64_url-encoded content of the server parameter.</param>
        /// <param name="additionalClientData">Optional additional data to be appended to the client message.</param>
        /// <param name="opts">Optional SQRL options to be appended to the client message.</param>
        /// <param name="priorKey">The optional prior site-specific ECDH public-private key pair.</param>
        /// <param name="ursKey">The optional Unlock Request Signing Key(URSK).</param>
        /// <returns>Returns a <c>SQRLServerResponse</c> object representing the server's response details.</returns>
        public static SQRLServerResponse GenerateSQRLCommand(SQRLCommands command,Uri sqrlUri, KeyPair currentSiteKeyPair, string encodedServer, StringBuilder additionalClientData=null, SQRLOptions opts = null,KeyPair priorKey=null, byte[] ursKey=null)
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

            var ids = GenerateIDS(encodedServer, client.ToString(), currentSiteKeyPair.PrivateKey);
            Dictionary<string, string> strContent = new Dictionary<string, string>()
            {
                {"client",encodedClient },
                {"server",encodedServer },
            };
            //Add Ids
            strContent.Add(ids.SignatureType, ids.Signature);

            if(priorKey!=null)
            {
                var pids = GeneratePIDS(encodedServer, client.ToString(), priorKey.PrivateKey);
                strContent.Add(pids.SignatureType, pids.Signature);
            }

            if(ursKey!=null)
            {
                var urs = GenerateURS(encodedServer, client.ToString(), ursKey);
                strContent.Add(urs.SignatureType, urs.Signature);
            }
            using (HttpClient wc = new HttpClient())
            {
                wc.DefaultRequestHeaders.Add("User-Agent", UserAgentHeader);
                var content = new FormUrlEncodedContent(strContent);
                var response = wc.PostAsync($"https://{sqrlUri.Host}{(sqrlUri.IsDefaultPort ? "" : $":{sqrlUri.Port}")}{sqrlUri.PathAndQuery}", content).Result;
                var result = response.Content.ReadAsStringAsync().Result;
                serverResponse = new SQRLServerResponse(result, sqrlUri.Host, sqrlUri.IsDefaultPort ? 443 : sqrlUri.Port);
            }

            return serverResponse;
        }

        /// <summary>
        /// Generates a dictionary of base64_url-encoded URL parameters for creating a client response.
        /// </summary>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="client">The contents of the client parameter.</param>
        /// <param name="server">The contents of the server parameter.</param>
        /// <param name="priorKP">The prior site-specific ECDH public-private key pair.</param>
        private static Dictionary<string, string> GenerateResponse(KeyPair siteKP, StringBuilder client, StringBuilder server, KeyPair priorKP=null)
        {
            if (!SodiumInitialized)
                SodiumInit();

            string encodedServer = Sodium.Utilities.BinaryToBase64(Encoding.UTF8.GetBytes(server.ToString()), Utilities.Base64Variant.UrlSafeNoPadding);
            return GenerateResponse(siteKP, client, encodedServer, priorKP);
        }

        /// <summary>
        /// Generates a dictionary of base64_url-encoded URL parameters for creating a client response.
        /// </summary>
        /// <param name="siteKP">The site-specific ECDH public-private key pair.</param>
        /// <param name="client">The contents of the client parameter.</param>
        /// <param name="server">The contents of the server parameter.</param>
        /// <param name="priorKP">The prior site-specific ECDH public-private key pair.</param>
        private static Dictionary<string, string> GenerateResponse(KeyPair siteKP, StringBuilder client, string server, KeyPair priorKP = null)
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

        /// <summary>
        /// Rekeys an identity using the provided <c>rescueCode</c> and <c>newPassword</c>.
        /// </summary>
        /// <param name="identity">The identity to get rekeyed.</param>
        /// <param name="rescueCode">The rescue code for the given identity.</param>
        /// <param name="newPassword">The new master password for the rekeyed identity.</param>
        /// <param name="progressBlock1">An objects implementing the IProgress interface for tracking the operation's progress for block 1 (optional).</param>
        /// <param name="progressBlock2">An objects implementing the IProgress interface for tracking the operation's progress for block 2 (optional).</param>
        /// <returns>Returns a <c>RekeyIdentityResult</c> object containingthe newly generated rescue code and the rekeyed <c>SQRLIdentity</c>.</returns>
        public static async Task<RekeyIdentityResult> RekeyIdentity(SQRLIdentity identity, string rescueCode, string newPassword, Progress<KeyValuePair<int,string>> progressBlock1=null, Progress<KeyValuePair<int, string>> progressBlock2=null)
        {
            SQRLIdentity newID = null;
            var oldIukData = await SQRL.DecryptBlock2(identity, rescueCode, progressBlock1);
            string newRescueCode = CreateRescueCode();
            byte[] newIUK = CreateIUK();
            
            if (oldIukData.DecryptionSucceeded)
            {
                newID = new SQRLIdentity();
                newID = GenerateIdentityBlock0(CreateIMK(newIUK), newID);
                newID.IdentityName = identity.IdentityName + " (rekeyed)";
                var block1Task = GenerateIdentityBlock1(newIUK, newPassword, newID, progressBlock1);
                var block2Task =  GenerateIdentityBlock2(newIUK, newRescueCode, newID, progressBlock2);
                await Task.WhenAll(block1Task, block2Task);
                GenerateIdentityBlock3(oldIukData.Iuk, identity, newID, CreateIMK(oldIukData.Iuk), CreateIMK(newIUK));
            }
            return new RekeyIdentityResult(newRescueCode, newID, oldIukData.DecryptionSucceeded);
        }

        /// <summary>
        /// Generates or overwrites the type 3 block within <c>newId</c> using the given <c>oldIuk</c>
        /// as well as all previous IUKs from the type 3 block of <c>oldIdentity</c> if available.
        /// </summary>
        /// <param name="oldIuk">The old Identity Unlock Key (IUK) to add to thenew  type 3 block.</param>
        /// <param name="oldIdentity">The old identity, whose block 3 which will be be added in with the new identity's block 3.</param>
        /// <param name="newID">The identity which will hold the newly created block 3.</param>
        /// <param name="oldImk">The Identiy Master Key (IMK) of the of the <c>oldIdentity</c>.</param>
        /// <param name="newImk">The new Identity Master Key (IMK) under which the newly created block 3 will be encrypted.</param>
        public static void GenerateIdentityBlock3(byte[] oldIuk, SQRLIdentity oldIdentity, SQRLIdentity newID, byte[] oldImk, byte[] newImk)
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

        /// <summary>
        /// Decrypts an identity's type 3 block using the provided Identity Master Key (IMK).
        /// </summary>
        /// <param name="imk">The identity's unencrypted Identity Master Key (IMK).</param>
        /// <param name="identity">The identity containing the type 3 block to be decrypted.</param>
        /// <param name="allGood">Indicates whether the operation complented successfully.</param>
        /// <returns>Returns a contiguous block of all encrypted bytes from the identity's type 3 block, 
        /// which includes all the PIUKs as well as the authentication tag.</returns>
        public static byte[] DecryptBlock3(byte[] imk, SQRLIdentity identity, out bool allGood)
        {
            List<byte> plainText = new List<byte>();
            plainText.AddRange(GetBytes(identity.Block3.Length));
            plainText.AddRange(GetBytes(identity.Block3.Type));
            plainText.AddRange(GetBytes(identity.Block3.Edition));
            List<byte> encryptedKeys = new List<byte>();
            allGood = false;
            identity.Block3.EncryptedPrevIUKs.ForEach(x => encryptedKeys.AddRange(x));
            encryptedKeys.AddRange(identity.Block3.VerificationTag);
            byte[] result = null;
            try
            {
                result = Sodium.SecretAeadAes.Decrypt(encryptedKeys.ToArray(), new byte[12], imk, plainText.ToArray());
                allGood = true;
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Failed to Decrypt: {ex.ToString()} CallStack: {ex.StackTrace}");
            }
            return result;
        }

        /// <summary>
        /// Zeroes out a byte array to remove our keys from memory.
        /// </summary>
        /// <param name="key">The byte array to be zeroed out.</param>
        public static void ZeroFillByteArray(ref byte[] key)
        {
            for (int i = 0; i < key.Length; i++)
            {
                key[i] = 0;
            }
        }
    }

    /// <summary>
    /// Represents the results of decrypting a SQRL type 1 block.
    /// </summary>
    public class DecryptBlock1Result
    {
        /// <summary>
        /// Indicates whether the decryption succeeded or not.
        /// </summary>
        public bool DecryptionSucceeded = false;

        /// <summary>
        /// If the decryption operation succeeded, this will hold the
        /// unencrypted Identity Master Key (IMK).
        /// </summary>
        public byte[] Imk;

        /// <summary>
        /// If the decryption operation succeeded, this will hold the
        /// unencrypted Identity Lock Key (ILK).
        /// </summary>
        public byte[] Ilk;

        public DecryptBlock1Result(bool operationSucceeded, byte[] imk, byte[] ilk)
        {
            this.DecryptionSucceeded = operationSucceeded;
            this.Imk = imk;
            this.Ilk = ilk;
        }
    }

    /// <summary>
    /// Represents the results of decrypting a SQRL type 2 block.
    /// </summary>
    public class DecryptBlock2Result
    {
        /// <summary>
        /// Indicates whether the decryption succeeded or not.
        /// </summary>
        public bool DecryptionSucceeded = false;

        /// <summary>
        /// If the decryption operation succeeded, this will hold the
        /// unencrypted Identity Unlock Key (IUK).
        /// </summary>
        public byte[] Iuk;

        /// <summary>
        /// If the decryption operation failed, this will hold an
        /// error message explaining the reason for the error.
        /// </summary>
        public string ErrorMessage;

        public DecryptBlock2Result(bool operationSucceeded, byte[] iuk, string errorMessage)
        {
            this.DecryptionSucceeded = operationSucceeded;
            this.Iuk = iuk;
            this.ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Represents the results of creating a prior site key pair.
    /// </summary>
    public class PriorSiteKeysResult
    {
        /// <summary>
        /// A binary value created using the site's domain/URL which 
        /// is used as a seed for a number of cryptographic functions.
        /// </summary>
        public byte[] SiteSeed;

        /// <summary>
        /// The ECDH public-private key pair for a particular site/PIUK.
        /// </summary>
        public Sodium.KeyPair KeyPair;

        public PriorSiteKeysResult(byte[] siteSeed, Sodium.KeyPair keyPair)
        {
            this.SiteSeed = siteSeed;
            this.KeyPair = keyPair;
        }
    }

    /// <summary>
    /// Represents the results of rekeying an identity.
    /// </summary>
    public class RekeyIdentityResult
    {
        /// <summary>
        /// The rescue code for the new, rekeyed identity.
        /// </summary>
        public string NewRescueCode;

        /// <summary>
        /// The new, rekeyed identity.
        /// </summary>
        public SQRLIdentity RekeyedIdentity;

        public bool Success { get; set; } = false;

        public RekeyIdentityResult(string newRescueCode, SQRLIdentity rekeyedIdentity, bool Success)
        {
            this.NewRescueCode = newRescueCode;
            this.RekeyedIdentity= rekeyedIdentity;
            this.Success = Success;
        }
    }

    /// <summary>
    /// Represents the results of creting a Server Unlock Key (SUK) / 
    /// Verification Unlock Key (VUK) key pair.
    /// </summary>
    public class SukVukResult
    {
        /// <summary>
        /// The generated Server Unlock Key (SUK).
        /// </summary>
        public byte[] Suk;

        /// <summary>
        /// The generted Verification Unlock Key (VUK).
        /// </summary>
        public byte[] Vuk;

        public SukVukResult(byte[] suk, byte[] vuk)
        {
            this.Suk = suk;
            this.Vuk = vuk;
        }
    }

    /// <summary>
    /// Represents the results of creting a SQRL protocol signature.
    /// </summary>
    public class SignatureResult
    {
        /// <summary>
        /// The type of the signature to be created (urs, ids, pids... etc.).
        /// </summary>
        public string SignatureType;

        /// <summary>
        /// The Base64URL-encoded signature.
        /// </summary>
        public string Signature;

        public SignatureResult(string signatureType, string signature)
        {
            this.SignatureType = signatureType;
            this.Signature = signature;
        }
    }

    /// <summary>
    /// Represents the results of a "timed" EnScrypt PBKDF operation.
    /// </summary>
    public class EnScryptTimeResult
    {
        /// <summary>
        /// The scrypt iteration count that was determined by running the 
        /// scrypt function for the specified amount of time.
        /// </summary>
        public int IterationCount;

        /// <summary>
        /// The key that was derived by running the input trough the EnScrypt PBKDF.
        /// </summary>
        public byte[] Key;

        public EnScryptTimeResult(int iterationCount, byte[] key)
        {
            this.IterationCount = iterationCount;
            this.Key = key;
        }
    }
}

/// <summary>
/// This class provides extension methods for commonly used utility functions.
/// </summary>
public static class UtilClass
{
    /// <summary>
    /// Clears whitespace such as spaces, tabs, newlines and the literal "\n" from the string.
    /// </summary>
    public static string CleanUpString(this string s)
    {
        return s.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\\n", "");
    }

    /// <summary>
    /// Fills the byte array with Zeroes, overwriting the current contents.
    /// </summary>
    public static void ZeroFill(this byte[] ary)
    {
        SQRLUtilsLib.SQRL.ZeroFillByteArray(ref ary);
    }

    /// <summary>
    /// Appends the given string s, followed by a windows-style line break ("CR LF").
    /// </summary>
    /// <param name="s">The string to be appended.</param>
    public static StringBuilder AppendLineWindows(this StringBuilder sb, string s)
    {
        sb.Append(s);
        sb.Append("\r\n");
        return sb;
    }

    /// <summary>
    /// Perform a deep copy of the object.
    /// </summary>
    /// <typeparam name="T">The type of object being copied.</typeparam>
    /// <param name="source">The object instance to copy.</param>
    /// <returns>The copied object.</returns>
    public static T Clone<T>(this T source)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", nameof(source));
        }

        // Don't serialize a null object, simply return the default for that object
        if (Object.ReferenceEquals(source, null))
        {
            return default(T);
        }

        IFormatter formatter = new BinaryFormatter();
        Stream stream = new MemoryStream();
        using (stream)
        {
            formatter.Serialize(stream, source);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }

    /// <summary>
    /// Converts a byte array to its hexadecimal string representation.
    /// </summary>
    public static string ToHex(this byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }
}