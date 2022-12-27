using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Vehement.Common
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Word
    {
        public enum MathType
        {
            U16,
            I16,
            F16,
        }

        [FieldOffset(0)]
        public ushort u16;
        [FieldOffset(0)]
        public Half f16;
        [FieldOffset(0)]
        public short i16;
        
        [FieldOffset(4)]
        private MathType mathType = MathType.U16;
        
        public byte Low => (byte)(u16 & 0xFF);
        public byte High => (byte)(u16 >> 8);

        public static implicit operator int(Word word)
        {
            return word.u16;
        }

        public static Word operator +(Word left, Word right)
        {
            switch(left.mathType)
            {
                case MathType.F16:
                    return new Word((Half)(left.f16 + right.f16), MathType.F16);
                case MathType.I16:
                    return new Word((short)(left.i16+ right.i16), MathType.I16);
                case MathType.U16:
                default:
                    return new Word((ushort)(left.u16 + right.u16));
            }
        }

        public static Word operator -(Word left, Word right)
        {
            switch (left.mathType)
            {
                case MathType.F16:
                    return new Word((Half)(left.f16 - right.f16), MathType.F16);
                case MathType.I16:
                    return new Word((short)(left.i16 - right.i16), MathType.I16);
                case MathType.U16:
                default:
                    return new Word((ushort)(left.u16 - right.u16));
            }
        }


        public static Word operator *(Word left, Word right)
        {
            switch (left.mathType)
            {
                case MathType.F16:
                    return new Word((Half)(left.f16 * right.f16), MathType.F16);
                case MathType.I16:
                    return new Word((short)(left.i16 * right.i16), MathType.I16);
                case MathType.U16:
                default:
                    return new Word((ushort)(left.u16 * right.u16));
            }
        }

        public static Word operator /(Word left, Word right)
        {
            switch (left.mathType)
            {
                case MathType.F16:
                    return new Word((Half)(left.f16 / right.f16), MathType.F16);
                case MathType.I16:
                    return new Word((short)(left.i16 / right.i16), MathType.I16);
                case MathType.U16:
                default:
                    return new Word((ushort)(left.u16 / right.u16));
            }
        }


        private Word(MathType mathType)
        {
            this.mathType = mathType;
        }

        public Word(Half value, MathType mathType = MathType.F16) : this(mathType)
        {
            f16 = value;
        }

        public Word(short value, MathType mathType = MathType.I16) : this(mathType)
        {
            i16 = value;
        }

        public Word(ushort value, MathType mathType = MathType.U16) : this(mathType)
        {
            u16 = value;
        }

        public Word(byte low, byte high, MathType mathType = MathType.U16) : this(mathType)
        {
            u16 = (ushort)(low | (ushort)(high << 8));
        }

        public override string ToString()
        {
            return $"[ WORD: Raw 0x{u16:X4} | u16 {u16} | i16 {i16} | f16 {f16} ]";
        }
    }
}
