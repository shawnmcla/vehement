using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vehement.Common
{
    public struct ProgramHeader
    {
        static byte[] MAGIC = new byte[] { 0xBE, 0xEF, 0xCA, 0xFE };
        public const int VERSION = 1;
        public const int DEFAULT_HEADER_SIZE = 4 + 2 + 1 + 4 + 2;

        public ushort Version { get; set; } = VERSION;
        public byte Endianness { get; set; } = 0; // Little
        public int Options { get; set; } = 0;
        public ushort StaticSegmentSize { get; set; } = 0;
        public int SizeBytes { get; set; } = 0;

        public byte[] ToBytes()
        {
            List<byte> bytes = new();
            bytes.AddRange(MAGIC);
            bytes.Add((byte)(Version & 0xFF));
            bytes.Add((byte)(Version >> 8));
            bytes.Add(Endianness);

            bytes.Add((byte)((Options >> 8 * 0) & 0xFF));
            bytes.Add((byte)((Options >> 8 * 1) & 0xFF));
            bytes.Add((byte)((Options >> 8 * 2) & 0xFF));
            bytes.Add((byte)((Options >> 8 * 3) & 0xFF));

            bytes.Add((byte)(StaticSegmentSize & 0xFF));
            bytes.Add((byte)(StaticSegmentSize >> 8));

            return bytes.ToArray();
        }

        public ProgramHeader() { }

        public static ProgramHeader Default()
        {
            return new ProgramHeader
            {
                Version = VERSION,
                Endianness = 0,
                Options = 0,
                StaticSegmentSize = 0
            };
        }

        public static ProgramHeader? FromBytes(IList<byte> bytes)
        {
            if (!(bytes[0] == MAGIC[0] && bytes[1] == MAGIC[1] && bytes[2] == MAGIC[2] && bytes[3] == MAGIC[3]))
                return null;

            ushort version = (ushort)(bytes[4] | (bytes[5] << 8));
            byte endianness = bytes[6];
            int options = bytes[7] | (bytes[8] << 8) | (bytes[9] << 16) | (bytes[10] << 24);
            ushort staticSegmentSize = (ushort)(bytes[11] | (bytes[12] << 8));

            ProgramHeader h = new()
            {
                Version = version,
                Endianness = endianness,
                Options = options,
                StaticSegmentSize = staticSegmentSize,
                SizeBytes = 13
            };

            return h;
        }
    }
}
