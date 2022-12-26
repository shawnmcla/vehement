namespace Vehement.Common
{
    public static class OpsMapper {
        public static Dictionary<Op, Mnemonic> keyValuePairs = new Dictionary<Op, Mnemonic>()
        {
            { Op.NOOP, Mnemonic.NOOP },

            { Op.MOV_MEM_REG, Mnemonic.MOV },
            { Op.MOV_REG_MEM, Mnemonic.MOV },
            { Op.MOV_REG_IMM, Mnemonic.MOV },
            { Op.MOV_REG_REG, Mnemonic.MOV },

            { Op.ADD, Mnemonic.ADD },
            { Op.SUB, Mnemonic.SUB },
            { Op.MUL, Mnemonic.MUL },
            { Op.DIV, Mnemonic.DIV },

            { Op.JUMP, Mnemonic.JUMP },
            { Op.JUMP_Z, Mnemonic.JUMP_Z },
            { Op.JUMP_NZ, Mnemonic.JUMP_NZ },
            
            { Op.PUSH, Mnemonic.PUSH },
            { Op.POP, Mnemonic.POP },

            { Op.CALL, Mnemonic.CALL },
            { Op.RET, Mnemonic.RET },

            { Op.PRINT, Mnemonic.PRINT },
            { Op.HALT, Mnemonic.HALT },
        };

        public static Mnemonic GetMnemonic(Op op)
        {
            return keyValuePairs[op];
        }
}

public enum Mnemonic : byte
{
    NOOP,
    MOV,
    ADD,
    SUB,
    MUL,
    DIV,
    JUMP,
    JUMP_Z,
    JUMP_NZ,
    PUSH,
    POP,
    CALL,
    RET,
    PRINT,
    HALT
}

public enum Op : byte
{
    NOOP = 0,

    MOV_REG_IMM, // [reg8], [imm16] (Mov imm into register)
    MOV_REG_REG, // [reg8], [reg8]  (Mov value of second reg to first)
    MOV_REG_MEM, // [reg8], [adr16] (Mov value of memory address to reg)
    MOV_MEM_REG, // [adr16], [reg8] (Mov value of register into mem at address)

    ADD,        // [reg8], [reg8], [reg8] (Add regs 2 and 3, store in reg 1)
    SUB,        // [reg8], [reg8], [reg8] (Sub ..
    MUL,        // [reg8], [reg8], [reg8] (Mul ..
    DIV,        // [reg8], [reg8], [reg8] (Div ..

    ADDF,       // [reg8], [reg8], [reg8]
    SUBF,       // [reg8], [reg8], [reg8]
    MULF,       // [reg8], [reg8], [reg8]
    DIVF,       // [reg8], [reg8], [reg8]

    JUMP,       // [off16] (Unconditional)
    JUMP_Z,     // [off16] (Jump to program+off16 IF top of stack is 0)
    JUMP_NZ,    // [off16] ( ... not zero)

    PUSH,       // [reg8] Push value of reg to top of stack
    POP,        // [reg8] Pop value off top of stack, move into reg
    CALL,       // [off16] Like a jump, but also sets up some registers:
    RET,        // Restore SP and SB from registers 6 and 7.
                // Jump back to address stored in reg8
    PRINT,      // [adr16] Prints a VString to the Console pointed to by adr16.
    HALT,       // Terminate
}
}