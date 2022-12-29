#define VERBOSE
namespace Vehement.Common
{
    public enum Reg : byte
    {
        Reg1, // Also function return value
        Reg2,
        Reg3,
        Reg4,
        Reg5,
        Reg6,
        Reg7,
        Reg8,
        Pc,
        Sp,
        Sb,
        Flags
    }

    [Flags]
    public enum FlagsValue : byte
    {
        CmpEqual = 0b_0001,
        CmpLessThan = 0b_0010,
        CmpGreaterThan = 0b_0100,
        Interrupt = 0b_0000_1000,
        Reserved2 = 0b_0001_0000,
        Reserved3 = 0b_0010_0000,
        Reserved4 = 0b_0100_0000,
        Reserved5 = 0b_1000_0000,
    }
}
