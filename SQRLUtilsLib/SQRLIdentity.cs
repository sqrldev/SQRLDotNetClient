using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SQRLUtilsLib
{
    /// <summary>
    /// Represents a SQRL identity stored in SQRL's "S4" storage format.
    /// </summary>
    /// <remarks>
    /// More information about SQRL's binary "S4" storage format can be found 
    /// at https://www.grc.com/sqrl/SQRL_Cryptography.pdf starting on page 20.
    /// </remarks>
    public class SQRLIdentity
    {
        /// <summary>
        /// The name of the identity. Defaults to an empty string.
        /// </summary>
        public string IdentityName { get; set; }

        /// <summary>
        /// Creates a new <c>SQRLIdentity</c> object and optionally gives
        /// it a <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the identity (optional).</param>
        public SQRLIdentity(string name="") 
        {
            Blocks = new List<ISQRLBlock>();
            this.IdentityName = name;
        }

        /// <summary>
        /// The plaintext sqrl file header specified in the S4 storage format.
        /// </summary>
        public const String SQRLHEADER = "sqrldata";
        
        /// <summary>
        /// A list of all blocks stored within the identity.
        /// </summary>
        public List<ISQRLBlock> Blocks { get; set; }

        /// <summary>
        /// The identity's type 1 block (User access password authenticated & encrypted data).
        /// </summary>
        public SQRLBlock1 Block1 { get { return (SQRLBlock1)GetBlock(1); } }

        /// <summary>
        /// The identity's type 2 block (Rescue code encrypted data).
        /// </summary>
        public SQRLBlock2 Block2 { get { return (SQRLBlock2)GetBlock(2); } }

        /// <summary>
        /// The identity's type 3 block (Encrypted previous identity unlock keys). 
        /// Since SQRL identities only carry a type 3 block if they were rekeyed at 
        /// least once, this may be <c>null</c>!
        /// </summary>
        public SQRLBlock3 Block3 { get { return (SQRLBlock3)GetBlock(3); } }

        /// <summary>
        /// Checks if a block of the given <paramref name="blockType"/> exists within the identity.
        /// Returns <c>true</c> if it does, or <c>false</c> otherwise.
        /// </summary>
        /// <param name="blockType">The type of the block to be checked.</param>
        public bool HasBlock(ushort blockType)
        {
            return (GetBlock(blockType) != null);
        }

        /// <summary>
        /// Returns the block with the given <paramref name="blockType"/> if it 
        /// exists within the identity, or <c>null</c> otherwise.
        /// </summary>
        /// <param name="blockType">The type of the block to be fetched.</param>
        public ISQRLBlock GetBlock(ushort blockType)
        {
            foreach (var block in Blocks) 
                if (block.Type == blockType) return block;
            return null;
        }

        /// <summary>
        /// Returns the raw byte representation of the entire identity.
        /// </summary>
        public byte[] ToByteArray()
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(SQRLHEADER);
            foreach(var block in Blocks) data = data.Concat(block.ToByteArray()).ToArray();
            return data;
        }

        /// <summary>
        /// Writes the raw byte representation of the current identity 
        /// to a file with the given <paramref name="fileName"/>. If the
        /// target file aleady exists, it is overwritten!
        /// </summary>
        /// <param name="fileName">The full file path of the identity file to be created.</param>
        public void WriteToFile(string fileName)
        {
            File.WriteAllBytes(fileName, this.ToByteArray());
        }

        /// <summary>
        /// Creates a SQRLIdentity object from the specified <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The full file path of the identity file to be parsed.</param>
        /// <returns>The imported identity object, or <c>null</c> if the file does not exist.</returns>
        public static SQRLIdentity FromFile(string file)
        {
            if (!File.Exists(file)) return null;
            
            byte[] fileBytes = File.ReadAllBytes(file);
            return SQRLIdentity.FromByteArray(fileBytes);
        }

        /// <summary>
        /// Parses a SQRL identity from the given <paramref name="identityData"/> byte array.
        /// </summary>
        /// <param name="identityData">The raw byte representation of a SQRL identity.</param>
        /// <param name="noHeader">Indicates whether the plaintext sqrl header is missing and checking for it should be skipped or not.</param>
        public static SQRLIdentity FromByteArray(byte[] identityData, bool noHeader=false)
        {
            SQRLIdentity id = new SQRLIdentity();
            bool isBase64 = false;
            int skip = SQRLHEADER.Length;
            // Check header
            if (!noHeader)
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

    /// <summary>
    /// Defines the miminumum requirements for a SQRL identity block 
    /// adhering to the specification of the S4 identity storage format.
    /// </summary>
    public interface ISQRLBlock
    {
        /// <summary>
        /// The whole block's length in bytes when stored in the S4 format.
        /// </summary>
        ushort Length { get; }

        /// <summary>
        /// The block type as specified in the S4 identity storage format.
        /// </summary>
        ushort Type { get; }

        /// <summary>
        /// Generates an identity block object by parsing the raw <paramref name="blockData"/> byte array.
        /// </summary>
        void FromByteArray(byte[] blockData);

        /// <summary>
        /// Converts the identity block object into its raw byte representation.
        /// </summary>
        byte[] ToByteArray();
    }

    /// <summary>
    /// Represents an "unknown" SQRL identity block type.
    /// </summary>
    public class SQRLBlock : ISQRLBlock
    {
        public ushort Length { get; set; }
        public ushort Type { get; set; }

        /// <summary>
        /// The raw data bytes following the block's length and type.
        /// </summary>
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

    /// <summary>
    /// Represents a type 1 identity block (User access password authenticated & encrypted data) 
    /// as specified in the SQRL's S4 identity storage format.
    /// </summary>
    /// <remarks>
    /// Check https://www.grc.com/sqrl/SQRL_Cryptography.pdf on page 21 for further
    /// information.
    /// </remarks>
    public class SQRLBlock1 : ISQRLBlock
    {
        public ushort Length { get; } = 125;
        public ushort Type { get; } = 1;

        /// <summary>
        ///  Length in bytes of the unencrypted (plaintext) but authenticated data for this
        ///  block. This is the length of the so-called “additional authenticated data” (aad) 
        ///  of the block's AES-GCM cipher. The length specification spans from the first byte 
        ///  of the block (the first byte of the whole block's length specification) up to, 
        ///  but not including, the beginning of the block's encrypted data.
        /// </summary>
        public ushort InnerBlockLength { get; } = 45;

        /// <summary>
        /// The AES-GCM initialization vector / nonce.
        /// </summary>
        public byte[] AesGcmInitVector { get; set; }

        /// <summary>
        /// The random salt for the scrypt memory-hard PBKDF.
        /// This must never be reused with the same key!
        /// </summary>
        public byte[] ScryptRandomSalt { get; set; }

        /// <summary>
        /// The memory consumption factor for the scrypt memory-hard PBKDF.
        /// Defaults to 9.
        /// </summary>
        public byte LogNFactor { get; set; } = 9;

        /// <summary>
        /// The time consumption factor for the scrypt memory-hard PBKDF.
        /// </summary>
        public uint IterationCount { get; set; }

        /// <summary>
        /// A set of bitflags representing several user-configurable client options.
        /// </summary>
        /// <remarks>
        /// Check https://www.grc.com/sqrl/SQRL_Cryptography.pdf on page 22 for further
        /// information.
        /// </remarks>
        public ushort OptionFlags { get; set; } = 499;

        /// <summary>
        /// The length of the so called "QuickPass", which consists of the first
        /// x characters from the identity's full master password.
        /// </summary>
        public byte HintLength { get; set; } = 4;

        /// <summary>
        /// Specifies the length of time in seconds SQRL's EnScrypt function will run 
        /// in order to deeply hash the user's password to generate the Identity Master 
        /// Key's (IMK) symmetric key.
        /// </summary>
        public byte PwdVerifySeconds { get; set; } = 5;

        /// <summary>
        /// Specifies the length of time in seconds that the "QuickPass" is allowed to 
        /// remain active before it is erased and the full password must be re-entered.
        /// </summary>
        public ushort PwdTimeoutMins { get; set; } = 15;

        /// <summary>
        /// The encrypted Identity Master Key (IMK).
        /// </summary>
        public byte[] EncryptedIMK { get; set; }

        /// <summary>
        /// The encrypted Identity Lock Key (ILK).
        /// </summary>
        public byte[] EncryptedILK { get; set; }

        /// <summary>
        /// The verification tag created by the AES-GCM authenticated encryption.
        /// </summary>
        public byte[] VerificationTag { get; set; }

        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length != 125)
                throw new Exception("Invalid Block 1, incorrect number of bytes");
            this.AesGcmInitVector = blockData.Skip(6).Take(12).ToArray();
            this.ScryptRandomSalt = blockData.Skip(18).Take(16).ToArray();
            this.LogNFactor = blockData.Skip(34).Take(1).First();
            this.IterationCount = BitConverter.ToUInt32(blockData.Skip(35).Take(4).ToArray());
            this.OptionFlags = BitConverter.ToUInt16(blockData.Skip(39).Take(2).ToArray());
            this.HintLength = blockData.Skip(41).Take(1).First();
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
            byteAry.AddRange(AesGcmInitVector);
            byteAry.AddRange(ScryptRandomSalt);
            byteAry.Add(LogNFactor);
            byteAry.AddRange(BitConverter.GetBytes(IterationCount));
            byteAry.AddRange(BitConverter.GetBytes(OptionFlags));
            byteAry.Add(HintLength);
            byteAry.Add(PwdVerifySeconds);
            byteAry.AddRange(BitConverter.GetBytes(PwdTimeoutMins));
            byteAry.AddRange(EncryptedIMK);
            byteAry.AddRange(EncryptedILK);
            byteAry.AddRange(VerificationTag);

            return byteAry.ToArray();
        }
    }

    /// <summary>
    /// Represents a type 2 identity block (Rescue code encrypted data) 
    /// as specified in the SQRL's S4 identity storage format.
    /// </summary>
    /// <remarks>
    /// Check https://www.grc.com/sqrl/SQRL_Cryptography.pdf on page 23 for further
    /// information.
    /// </remarks>
    public class SQRLBlock2 : ISQRLBlock
    {
        public ushort Length { get; set; } = 73;
        public ushort Type { get; set; } = 2;

        /// <summary>
        /// The random salt for the scrypt memory-hard PBKDF.
        /// This must never be reused with the same key!
        /// </summary>
        public byte[] RandomSalt { get; set; }

        /// <summary>
        /// The memory consumption factor for the scrypt memory-hard PBKDF.
        /// Defaults to 9.
        /// </summary>
        public byte LogNFactor { get; set; } = 9;

        /// <summary>
        /// The time consumption factor for the scrypt memory-hard PBKDF.
        /// </summary>
        public uint IterationCount { get; set; }

        /// <summary>
        /// The encrypted Identity Unlock Key (IUK).
        /// </summary>
        public byte[] EncryptedIUK { get; set; }

        /// <summary>
        /// The verification tag created by the AES-GCM authenticated encryption.
        /// </summary>
        public byte[] VerificationTag { get; set; }

        public void FromByteArray(byte[] blockData)
        {
            if (blockData.Length != 73)
                throw new Exception("Invalid Block 2, incorrect number of bytes");
            this.RandomSalt = blockData.Skip(4).Take(16).ToArray();
            this.LogNFactor = blockData.Skip(20).Take(1).First();
            this.IterationCount = BitConverter.ToUInt32(blockData.Skip(21).Take(4).ToArray());
            this.EncryptedIUK = blockData.Skip(25).Take(32).ToArray();
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
            byteAry.AddRange(EncryptedIUK);
            byteAry.AddRange(VerificationTag);

            return byteAry.ToArray();
        }
    }

    /// <summary>
    /// Represents a type 3 identity block (Encrypted previous identity unlock keys) 
    /// as specified in the SQRL's S4 identity storage format.
    /// </summary>
    /// <remarks>
    /// Check https://www.grc.com/sqrl/SQRL_Cryptography.pdf on page 23 for further
    /// information.
    /// </remarks>
    public class SQRLBlock3 : ISQRLBlock
    {
        public ushort Length { get; set; } = 54;
        public ushort Type { get; } = 3;

        /// <summary>
        /// The count of all Previous Identity Unlock Keys (PIUKs).
        /// </summary>
        public ushort Edition { get; set; } = 0;

        /// <summary>
        /// A list of all encrypted Previous Identity Unlock Keys (PIUKs).
        /// </summary>
        public List<byte[]> EncryptedPrevIUKs { get; set; }

        /// <summary>
        /// The verification tag created by the AES-GCM authenticated encryption.
        /// </summary>
        public byte[] VerificationTag { get; set; }

        /// <summary>
        /// Creates a new <c>SQRLBlock3</c> object.
        /// </summary>
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
