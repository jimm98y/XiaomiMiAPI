using System;
using System.IO;

namespace XiaomiMiAPI.API
{
    /// <summary>
    /// Xiaomi Mi Message.
    /// </summary>
    internal struct MiMessage
    {
        public const UInt16 MAGIC = 0x2131;

        /// <summary>
        /// Magic number is always 0x2131.
        /// </summary>
        public UInt16 MagicNumber;

        /// <summary>
        /// Length of the data including the header.
        /// </summary>
        public UInt16 PacketLength;

        /// <summary>
        /// Always 0 except for the "hello" packet when it's 0xFFFFFFFF.
        /// </summary>
        public UInt32 Unknown1;

        /// <summary>
        /// Unique number identifying the device. In "hello" packet it's 0xFFFFFFFF.
        /// </summary>
        public UInt32 DeviceId;

        /// <summary>
        /// Looks the same in all requests/reponses.
        /// </summary>
        public UInt32 Stamp;

        /// <summary>
        /// MD5 checksum or device token in the response to the "hello" packet.
        /// </summary>
        public byte[] Checksum;

        /// <summary>
        /// Payload in AES-128 encrypted format with PKCS7 padding (CBC).
        /// </summary>
        public byte[] Data;

        public static MiMessage FromBytes(byte[] bytes)
        {
            var ret = new MiMessage();
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryReader br = new BinaryReader(ms);
                ret.MagicNumber = br.ReadUInt16BE();
                ret.PacketLength = br.ReadUInt16BE();
                ret.Unknown1 = br.ReadUInt32BE();
                ret.DeviceId = br.ReadUInt32BE();
                ret.Stamp = br.ReadUInt32BE();
                ret.Checksum = br.ReadBytes(16);
                ret.Data = br.ReadBytes(ret.PacketLength - 32);
                return ret;
            }
        }

        public static int ToInt32BigEndian(byte[] buf, int i)
        {
            return (buf[i] << 24) | (buf[i + 1] << 16) | (buf[i + 2] << 8) | buf[i + 3];
        }

        public byte[] ToBytes()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter br = new BinaryWriter(ms);
                br.WriteBE(this.MagicNumber);
                br.WriteBE(this.PacketLength);
                br.WriteBE(this.Unknown1);
                br.WriteBE(this.DeviceId);
                br.WriteBE(this.Stamp);
                br.Write(this.Checksum);
                br.Write(this.Data);
                return ms.ToArray();
            }
        }
    }
}
