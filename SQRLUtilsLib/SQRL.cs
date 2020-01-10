using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace SQRLUtilsLib
{
    /// <summary>
    /// This library performs a lot of the crypto needed for a SQRL Client.
    /// A lot of the code here was adapted from @AlexHouser's IdTool at https://github.com/sqrldev/IdTool
    /// </summary>
    public class SQRL
    {
        private static bool SodiumInitialized = false;
        
        
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
                    temp =Sodium.SodiumCore.GetRandomBytes(1)[0];
                }

                int n = temp % 100;
                tempBytes[i] = (char)('0' + (n / 10));
                tempBytes[i + 1] = (char)('0' + (n % 10));
            }

            return  new String(tempBytes);
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
                if(i==0)
                {
                    data.CopyTo(xor,0);
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
        public byte[] enScriptTime(String password, byte[] randomSalt, int logNFactor, int secondsToRun, out int count)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            byte[] key = new byte[32];
            count = 0;
            while(Math.Abs((DateTime.Now-startTime).TotalSeconds)<secondsToRun)
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
        public byte[] enScriptCT(String password, byte[] randomSalt, int logNFactor, int intCount)
        {
            if (!SodiumInitialized)
                SodiumInit();

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            byte[] key = new byte[32];
            int count = 0;
            while (count<intCount)
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
            List<byte> additionalData= new List<byte>();
            int iterationCount = 0;
            byte[] imk = CreateIMK(iuk);
            byte[] ilk = CreateILK(iuk);
            key = enScriptTime(password, randomSalt, (int)Math.Pow(2,9), 5, out iterationCount);
            
            object[] block1 = new object[14];
            block1[0] = (UInt16)125; //Length
            block1[1] = (UInt16)1; //Type
            block1[2] = (UInt16)45; //inner block length
            block1[3] = initVector; //Init Vector
            block1[4] = randomSalt; //Random Salt
            block1[5] = sbyte.Parse("9"); //N Log
            block1[6] = (UInt32)iterationCount; //Iteration Count
            block1[7] = (UInt16)499; //Flags
            block1[8] = sbyte.Parse("4"); //Hint Length
            block1[9] = sbyte.Parse("5"); //PW Verify Sec
            block1[10] = (UInt16)15; //Time out in Minutes
            identity.Block1.ScryptInitVector = initVector;
            identity.Block1.ScryptRandomSalt = randomSalt;
            identity.Block1.IterationCount = (uint)iterationCount;

            IEnumerable<byte> unencryptedKeys = imk.Concat(ilk);
            for(int i=0; i< 11;i++)
            {
                additionalData.AddRange(GetBytes(block1[i]));
            }
            
            byte[] encryptedData = aesGcmEncrypt(unencryptedKeys.ToArray(), additionalData.ToArray(), initVector, key); //Should be 80 bytes
            identity.Block1.EncryptedIMK = encryptedData.ToList().GetRange(0, 32).ToArray();
            identity.Block1.EncryptedILK = encryptedData.ToList().GetRange(32, 32).ToArray();
            identity.Block1.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length-16, 16).ToArray();
        }


        public void GenerateIdentityBlock2(byte[] iuk, String rescueCode, SQRLIdentity identity)
        {
            if (!SodiumInitialized)
                SodiumInit();
            byte[] initVector = Sodium.SodiumCore.GetRandomBytes(12);
            byte[] randomSalt = Sodium.SodiumCore.GetRandomBytes(16);
            byte[] key = new byte[32];
            List<byte> additionalData = new List<byte>();
            int iterationCount = 0;
            object[] block2 = new object[6];
            block2[0] = (UInt16)73;
            block2[1] = (UInt16)2;
            block2[2] = randomSalt;
            block2[3] = sbyte.Parse("9");
            key = enScriptTime(rescueCode, randomSalt, (int)Math.Pow(2, 9), 5, out iterationCount);
            block2[4] = (UInt32)iterationCount;
            for (int i = 0; i < 5; i++)
            {
                additionalData.AddRange(GetBytes(block2[i]));
            }

            identity.Block2.RandomSalt = randomSalt;
            identity.Block2.IterationCount = (uint)iterationCount;
            byte[] encryptedData = aesGcmEncrypt(iuk, additionalData.ToArray(), initVector, key); //Should be 80 bytes
            identity.Block2.EncryptedIdentityLock = encryptedData.ToList().GetRange(0, 32).ToArray(); ;
            identity.Block2.VerificationTag = encryptedData.ToList().GetRange(encryptedData.Length - 16, 16).ToArray(); ;

        }

        private IEnumerable<byte> GetBytes(object v)
        {
           if(v.GetType()==typeof(UInt16))
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
           else if(v.GetType() == typeof(UInt32))
            {
                return BitConverter.GetBytes((UInt32)v);
            }
           else if(v.GetType()==typeof(byte[]))
            {
                return (byte[])v;
            }
           else return null;
        }

        public byte[] aesGcmEncrypt(byte[] message, byte[] additionalData, byte[] iv, byte[] key )
        {
            long length = message.Length + 16;
            byte[] cipherText = new byte[length];

            if (!SodiumInitialized)
                SodiumInit();

            //Had to override Sodium Core to allow more than 16 bytes of additional data
            cipherText = Sodium.SecretAeadAes.Encrypt(message, iv, key, additionalData);

            return cipherText;
        }

    }
}
