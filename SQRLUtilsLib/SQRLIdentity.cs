using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace SQRLUtilsLib
{
    public class SQRLIdentity
    {
        public SQRLIdentity()
        {
            this.Block1 = new SQRLBlock1();
            this.Block2 = new SQRLBlock2();
        }
        public SQRLBlock1 Block1 { get; set; }
        public SQRLBlock2 Block2 { get; set; }

       public byte[] ToByteArray()
        {
            return this.Block1.ToByteArray().Concat(Block2.ToByteArray()).ToArray();
        }
    }


    public interface SQRLBlock
    {
        byte[] ToByteArray();
    }
    public class SQRLBlock1: SQRLBlock
    {
        public ushort Length { get; } = 125;
        public ushort Type { get; } = 1;
        public ushort InnerBlockLength { get; } = 45;

        public byte[] ScryptInitVector { get; set; }

        public byte[] ScryptRandomSalt { get; set; }

        public byte LogNFactor { get; } = 9;

        public uint IterationCount { get; set; }

        public ushort OptionFlags { get; } = 499;

        public byte HintLenght { get; } = 4;

        public byte PwdVerifySeconds { get; } = 5;

        public ushort PwdTimeoutMins { get; } = 15;

        public byte[] EncryptedIMK { get; set; }

        public byte[] EncryptedILK { get; set; }

        public byte[] VerificationTag { get; set; }

        public byte[] ToByteArray()
        {
            List<byte> byteAry = new List<byte>();
            byteAry.AddRange(BitConverter.GetBytes(Length));
            byteAry.AddRange(BitConverter.GetBytes(Type));
            byteAry.AddRange(BitConverter.GetBytes(InnerBlockLength));
            byteAry.AddRange(ScryptInitVector);
            byteAry.AddRange(ScryptRandomSalt);
            byteAry.Add( LogNFactor );
            byteAry.AddRange(BitConverter.GetBytes(IterationCount));
            byteAry.AddRange(BitConverter.GetBytes(OptionFlags));
            byteAry.Add(HintLenght);
            byteAry.Add(PwdVerifySeconds);
            byteAry.AddRange(BitConverter.GetBytes(PwdTimeoutMins));
            byteAry.AddRange(EncryptedIMK);
            byteAry.AddRange(EncryptedILK);
            byteAry.AddRange(VerificationTag);

            return byteAry.ToArray();
        }
    }

    public class SQRLBlock2 : SQRLBlock
    {
        public ushort Length { get; } = 73;
        public ushort Type { get; } = 2;

        public byte[] RandomSalt { get; set; }

        public byte LogNFactor { get; } = 9;

        public uint IterationCount { get; set; }

        public byte[] EncryptedIdentityLock { get; set; }

        public byte[] VerificationTag { get; set; }

        public byte[] ToByteArray()
        {
            List<byte> byteAry = new List<byte>();
            byteAry.AddRange(BitConverter.GetBytes(Length));
            byteAry.AddRange(BitConverter.GetBytes(Type));
            byteAry.AddRange(RandomSalt);
            byteAry.Add(LogNFactor);
            byteAry.AddRange(BitConverter.GetBytes(IterationCount));
            byteAry.AddRange(EncryptedIdentityLock);
            byteAry.AddRange(VerificationTag);

            return byteAry.ToArray();
        }
       
    }
}
