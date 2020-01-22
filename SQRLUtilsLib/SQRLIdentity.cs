using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SQRLUtilsLib
{
    public class SQRLIdentity
    {
        public string IdentityName { get; set; }

        public SQRLIdentity(string name="") 
        {
            Blocks = new List<ISQRLBlock>();
            this.IdentityName = name;
        }

        public const String SQRLHEADER = "sqrldata";
        public List<ISQRLBlock> Blocks { get; set; }
        public SQRLBlock1 Block1 { get { return (SQRLBlock1)GetBlock(1); } }
        public SQRLBlock2 Block2 { get { return (SQRLBlock2)GetBlock(2); } }
        public SQRLBlock3 Block3 { get { return (SQRLBlock3)GetBlock(3); } }

        /// <summary>
        /// Returns true if a block of the given type exists within the identity,
        /// or false otherwise
        /// </summary>
        public bool HasBlock(ushort blockType)
        {
            return (GetBlock(blockType) != null);
        }

        /// <summary>
        /// Returns the block with the given block type if it exists within the
        /// identity, or null otherwise
        /// </summary>
        public ISQRLBlock GetBlock(ushort blockType)
        {
            foreach (var block in Blocks) 
                if (block.Type == blockType) return block;
            return null;
        }

        /// <summary>
        /// Returns the raw byte representation of the current identity
        /// </summary>
        public byte[] ToByteArray()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(SQRLHEADER);
            foreach(var block in Blocks) data = data.Concat(block.ToByteArray()).ToArray();
            return data;
        }

        /// <summary>
        /// Writes the raw byte representation of the current identity 
        /// to a file with the given file name.
        /// </summary>
        public void WriteToFile(string fileName)
        {
            File.WriteAllBytes(fileName, this.ToByteArray());
        }

        /// <summary>
        /// Creates a SQRLIdentity object from the specified file 
        /// </summary>
        /// <returns>The imported identity object, or null if the file does not exist.</returns>
        public static SQRLIdentity FromFile(string file)
        {
            if (!File.Exists(file)) return null;
            
            byte[] fileBytes = File.ReadAllBytes(file);
            return SQRLIdentity.FromByteArray(fileBytes);
        }

        /// <summary>
        /// Parses a SQRL identity from the given raw byte array 
        /// </summary>
        public static SQRLIdentity FromByteArray(byte[] identityData, bool texttual =false)
        {
            SQRLIdentity id = new SQRLIdentity();
            bool isBase64 = false;
            int skip = SQRLHEADER.Length;
            // Check header
            if (!texttual)
            {
                string sqrlHeader = System.Text.Encoding.UTF8.GetString(identityData.Take(8).ToArray());
                if (!sqrlHeader.Equals(SQRLHEADER, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Invalid File Exception, not a valid SQRL Identity File");
                if (sqrlHeader.Equals(SQRLHEADER.ToUpper())) isBase64 = true;
            }
            else
                skip = 0;

            // Remove header
            identityData = identityData.Skip(skip).ToArray();

            // If we're dealing with a base64url-encoded identity, 
            // decode it to binary first
            if (isBase64) identityData = Sodium.Utilities.Base64ToBinary(
                System.Text.Encoding.UTF8.GetString(identityData), string.Empty, Sodium.Utilities.Base64Variant.UrlSafeNoPadding);

            int i = 0;
            while (i < identityData.Length)
            {
                // we need to be able to read type and length
                if (i + 4 > identityData.Length) break;

                ushort blockLength = BitConverter.ToUInt16(identityData.Skip(i).Take(2).ToArray());
                ushort blockType = BitConverter.ToUInt16(identityData.Skip(i + 2).Take(2).ToArray());

                // check if specified length exceeds real length
                if (i + blockLength > identityData.Length) break;

                switch (blockType)
                {
                    case 1:
                        SQRLBlock1 block1 = new SQRLBlock1();
                        block1.FromByteArray(identityData.Skip(i).Take(blockLength).ToArray());
                        id.Blocks.Add(block1);
                        break;

                    case 2:
                        SQRLBlock2 block2= new SQRLBlock2();
                        block2.FromByteArray(identityData.Skip(i).Take(blockLength).ToArray());
                        id.Blocks.Add(block2);
                        break;

                    case 3:
                        SQRLBlock3 block3 = new SQRLBlock3();
                        block3.FromByteArray(identityData.Skip(i).Take(blockLength).ToArray());
                        id.Blocks.Add(block3);
                        break;

                    default:
                        SQRLBlock block = new SQRLBlock();
                        block.FromByteArray(identityData.Skip(i).Take(blockLength).ToArray());
                        id.Blocks.Add(block);
                        break;
                }

                i += blockLength;
            }

            return id;
        }
    }


    public interface ISQRLBlock
    {
        byte[] ToByteArray();
        ushort Length { get; }
        ushort Type { get; }
        void FromByteArray(byte[] blockData);

    }

    /// <summary>
    /// Represents an "unknown" SQRL block type
    /// </summary>
    public class SQRLBlock : ISQRLBlock
    {
        public ushort Length { get; set; }
        public ushort Type { get; set; }
        public byte[] BlockData { get; set; }

        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length < 4)
                throw new Exception("Invalid Block, incorrect number of bytes");
            this.Length = BitConverter.ToUInt16(blockData.Take(2).ToArray());
            this.Type = BitConverter.ToUInt16(blockData.Skip(2).Take(2).ToArray());
            
            if (blockData.Length < this.Length)
                throw new Exception("Invalid Block, incorrect number of bytes");
            this.BlockData = blockData.Skip(4).Take(this.Length-4).ToArray();
        }

        public byte[] ToByteArray()
        {
            List<byte> byteAry = new List<byte>();
            byteAry.AddRange(BitConverter.GetBytes(Length));
            byteAry.AddRange(BitConverter.GetBytes(Type));
            byteAry.AddRange(BlockData);
            return byteAry.ToArray();
        }

    }
    public class SQRLBlock1 : ISQRLBlock
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
            byteAry.Add(LogNFactor);
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

    public class SQRLBlock2 : ISQRLBlock
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

    public class SQRLBlock3 : ISQRLBlock
    {
        public ushort Length { get; set; } = 54;
        public ushort Type { get; } = 3;

        public ushort Edition { get; set; } = 0;

        public List<byte[]> EncryptedPrevIUKs { get; set; }
        
        public byte[] VerificationTag { get; set; }
        
        public SQRLBlock3()
        {
            EncryptedPrevIUKs = new List<byte[]>();
        }
        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length != 54 && blockData.Length != 86 && blockData.Length != 118 && blockData.Length != 150)
                throw new Exception("Invalid Block 3, incorrect number of bytes");
            this.Length = BitConverter.ToUInt16(blockData.Skip(0).Take(2).ToArray());
            this.Edition = BitConverter.ToUInt16(blockData.Skip(4).Take(2).ToArray());
            int skip = 6;
            for(int i =0; i < this.Edition; i++)
            {
                if (this.EncryptedPrevIUKs == null)
                    this.EncryptedPrevIUKs = new List<byte[]>();

                this.EncryptedPrevIUKs.Add(blockData.Skip(skip).Take(32).ToArray());
                skip += 32;
            }
            this.VerificationTag = blockData.Skip(skip).Take(16).ToArray();
            
        }

        public byte[] ToByteArray()
        {
            List<byte> byteAry = new List<byte>();
            if (this.EncryptedPrevIUKs.Count > 0)
            {
                byteAry.AddRange(BitConverter.GetBytes(Length));
                byteAry.AddRange(BitConverter.GetBytes(Type));
                byteAry.AddRange(BitConverter.GetBytes(Edition));
                this.EncryptedPrevIUKs.ForEach(x => byteAry.AddRange(x));
                byteAry.AddRange(VerificationTag);
            }

            return byteAry.ToArray();
        }
    }
}
