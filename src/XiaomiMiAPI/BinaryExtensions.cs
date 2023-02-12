using System;
using System.IO;

namespace XiaomiMiAPI
{
    internal static class BinaryExtensions
    {
        /// <summary>
        /// Reverse all bytes in the array.
        /// Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static void WriteBE(this BinaryWriter binWrtr, UInt16 value)
        {
            binWrtr.Write((byte)((UInt16)value >> 8));
            binWrtr.Write((byte)((UInt16)value));
        }

        public static void WriteBE(this BinaryWriter binWrtr, UInt32 value)
        {
            binWrtr.Write((byte)((UInt32)value >> 24));
            binWrtr.Write((byte)((UInt32)value >> 16));
            binWrtr.Write((byte)((UInt32)value >> 8));
            binWrtr.Write((byte)((UInt32)value));
        }

        public static UInt16 ReadUInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(UInt16)).Reverse(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(Int16)).Reverse(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(UInt32)).Reverse(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(Int32)).Reverse(), 0);
        }

        public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }
    }
}
