using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace SQRLUtilsLib
{
    public class SQRLIdentity
    {
        public SQRLIdentity()
        {
            this.Block1 = new SQRLBlock1();
            this.Block2 = new SQRLBlock2();
        }

        public const String SQRLHEADER = "sqrldata";
        public SQRLBlock1 Block1 { get; set; }
        public SQRLBlock2 Block2 { get; set; }

       public byte[] ToByteArray()
        {
            return System.Text.Encoding.UTF8.GetBytes(SQRLHEADER).Concat(this.Block1.ToByteArray().Concat(Block2.ToByteArray()).ToArray()).ToArray();
        }

        public void WriteToFile(string fileName)
        {
            File.WriteAllBytes(fileName, this.ToByteArray());
        }

     
    }


    public interface SQRLBlock
    {
        byte[] ToByteArray();
        ushort Length { get; }
        ushort Type { get; }
        void FromByteArray(byte[] blockData);

    }
    public class SQRLBlock1: SQRLBlock
    {
        public ushort Length { get; } = 125;
        public ushort Type { get; } = 1;
        public ushort InnerBlockLength { get; } = 45;

        public byte[] ScryptInitVector { get; set; }

        public byte[] ScryptRandomSalt { get; set; }

        public byte LogNFactor { get; set; } = 9;

        public uint IterationCount { get; set; }

        public ushort OptionFlags { get; set; } = 499;

        public byte HintLenght { get; set; } = 4;

        public byte PwdVerifySeconds { get; set; } = 5;

        public ushort PwdTimeoutMins { get; set; } = 15;

        public byte[] EncryptedIMK { get; set; }

        public byte[] EncryptedILK { get; set; }

        public byte[] VerificationTag { get; set; }

        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length != 125)
                throw new Exception("Invalid Block 1, incorrect number of bytes");
            this.ScryptInitVector = blockData.Skip(6).Take(12).ToArray();
            this.ScryptRandomSalt = blockData.Skip(18).Take(16).ToArray();
            this.LogNFactor = blockData.Skip(34).Take(1).First();
            this.IterationCount = BitConverter.ToUInt32(blockData.Skip(35).Take(4).ToArray());
            this.OptionFlags = BitConverter.ToUInt16(blockData.Skip(39).Take(2).ToArray());
            this.HintLenght = blockData.Skip(41).Take(1).First();
            this.PwdVerifySeconds = blockData.Skip(42).Take(1).First();
            this.PwdTimeoutMins = BitConverter.ToUInt16(blockData.Skip(43).Take(2).ToArray());
            this.EncryptedIMK = blockData.Skip(45).Take(32).ToArray();
            this.EncryptedILK = blockData.Skip(77).Take(32).ToArray();
            this.VerificationTag = blockData.Skip(109).Take(16).ToArray();

        }

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
        public ushort Length { get; set; } = 73;
        public ushort Type { get; set; } = 2;

        public byte[] RandomSalt { get; set; }

        public byte LogNFactor { get; set; } = 9;

        public uint IterationCount { get; set; }

        public byte[] EncryptedIdentityLock { get; set; }

        public byte[] VerificationTag { get; set; }

        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length != 73)
                throw new Exception("Invalid Block 2, incorrect number of bytes");
            this.RandomSalt = blockData.Skip(4).Take(16).ToArray();
            this.LogNFactor = blockData.Skip(20).Take(1).First();
            this.IterationCount = BitConverter.ToUInt32(blockData.Skip(21).Take(4).ToArray());
            this.EncryptedIdentityLock = blockData.Skip(25).Take(32).ToArray();
            this.VerificationTag = blockData.Skip(57).Take(16).ToArray();
        }

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
