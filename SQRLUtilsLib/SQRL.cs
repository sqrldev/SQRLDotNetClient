using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace SQRLUtilsLib
{
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
        public string CreateNewRescueCode()
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
        }

        /// <summary>
        ///  EnHash Algorithm
        ///  SHA256 is iterated 16 times with each
        ///  successive output XORed to form a 1’s complement sum to produce the final result
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] enHash(byte[] data)
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
            count = 0;
            while(Math.Abs((DateTime.Now-startTime).TotalSeconds)<secondsToRun)
            {
                passwordBytes = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

                if (count == 0)
                {
                    passwordBytes.CopyTo(xorKey, 0);
                }
                else
                {
                    BitArray og = new BitArray(xorKey);
                    BitArray newG = new BitArray(passwordBytes);
                    BitArray newXor = og.Xor(newG);
                    newXor.CopyTo(xorKey, 0);
                }

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

            byte[] passwordBytes = Encoding.ASCII.GetBytes(password);
            DateTime startTime = DateTime.Now;
            byte[] xorKey = new byte[32];
            int count = 0;
            while (count<intCount)
            {
                passwordBytes = Sodium.PasswordHash.ScryptHashLowLevel(passwordBytes, randomSalt, logNFactor, 256, 1, (uint)32);

                if (count == 0)
                {
                    passwordBytes.CopyTo(xorKey, 0);
                }
                else
                {
                    BitArray og = new BitArray(xorKey);
                    BitArray newG = new BitArray(passwordBytes);
                    BitArray newXor = og.Xor(newG);
                    newXor.CopyTo(xorKey, 0);
                }

                count++;
            }

            return xorKey;
        }


        public object[] GenerateIdentityBlock1(byte[] iuk, String password)
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
            key = enScriptTime(password, randomSalt, 512, 5, out iterationCount);

            object[] block1 = new object[14];
            block1[0] = (UInt16)125; //Length
            block1[1] = (UInt16)1; //Type
            block1[2] = (UInt16)45;
            block1[3] = Sodium.Utilities.BinaryToHex(initVector);
            block1[4] = Sodium.Utilities.BinaryToHex(randomSalt);
            block1[5] = sbyte.Parse("9");
            block1[6] = (UInt32)iterationCount;
            block1[7] = (UInt16)499;
            block1[8] = sbyte.Parse("4");
            block1[9] = sbyte.Parse("5");
            block1[10] = (UInt16)15;

            IEnumerable<byte> unencryptedKeys = imk.Concat(ilk);
            for(int i=0; i< 11;i++)
            {
                additionalData.AddRange(GetBytes(block1[i]));
            }

            byte[] encryptedData = aesGcmEncrypt(unencryptedKeys.ToArray(), additionalData.ToArray(), initVector, key);
            block1[11] = Sodium.Utilities.BinaryToHex(encryptedData.ToList().GetRange(0, 32).ToArray());
            block1[12] = Sodium.Utilities.BinaryToHex(encryptedData.ToList().GetRange(32, 32).ToArray());
            block1[13] = Sodium.Utilities.BinaryToHex(encryptedData.ToList().GetRange(encryptedData.Length-16, 16).ToArray());

            return block1;
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
           else return null;
        }

        public byte[] aesGcmEncrypt(byte[] message, byte[] additionalData, byte[] iv, byte[] key )
        {
            long length = message.Length + 16;
            byte[] cipherText = new byte[length];

            if (!SodiumInitialized)
                SodiumInit();

            cipherText = Sodium.SecretAeadAes.Encrypt(message, iv, key, additionalData);

            return cipherText;
        }

    }
}
