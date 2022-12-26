using Vehement.Common;
using Vehement;

namespace VMTest
{
    [TestClass]
    public class VMTest
    {
        [TestMethod]
        public void TestMovInstructions()
        {
            {
                // Test MOV_REG_IMM (register number) (16 bit value)
                VM vm = new(new byte[] {
                (byte)Op.MOV_REG_IMM, 0x00, 0xFF, 0x00,
                (byte)Op.MOV_REG_IMM, 0x01, 0xFF, 0xAB,
                (byte)Op.HALT });
                vm.RunUntilHalt();
                Assert.AreEqual(0xFF, vm.Registers[0]);
                Assert.AreEqual(0xABFF, vm.Registers[1]);
            }

            {
                // Test MOV_MEM_REG (16 bit heap address offset) (register number)
                //  and MOV_REG_MEM (register number) (16 bit heap address offset)
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xEF, 0xBE, // MOV 0xBEEF to $reg0
                    (byte)Op.MOV_MEM_REG, 0xAB, 0x00, 0x00, // MOV value of $reg0 to $0x00AB
                    (byte)Op.MOV_REG_MEM, 0x01, 0xAB, 0x00, // MOV value of $0x00AB to $reg1
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();

                Assert.AreEqual(0xBEEF, vm.Registers[0]);
                Assert.AreEqual(0xBEEF, vm.Registers[1]);
                Assert.AreEqual(0xBEEF, vm.Heap[0xAb + 1] << 8 | vm.Heap[0xAB]);
            }

            {
                // Test MOV_REG_REG (register number) (register number)
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xEF, 0xBE, // MOV 0xBEEF to $reg0
                    (byte)Op.MOV_REG_REG, 0x01, 0x00,       // MOV value of $reg0 into $reg1
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();

                Assert.AreEqual(0xBEEF, vm.Registers[0]);
                Assert.AreEqual(0xBEEF, vm.Registers[1]);
            }
        }

        [TestMethod]
        public void TestIntegerArithmetic()
        {
            {
                // Test ADD
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xEF, 0x00, // Set $reg0 to 0x00EF
                    (byte)Op.MOV_REG_IMM, 0x01, 0x00, 0xBE, // Set $reg1 to 0xBE00
                    (byte)Op.ADD,         0x02, 0x00, 0x01, // Add $reg0 and $reg1, store in $reg2
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();
                Assert.AreEqual(0xBEEF, vm.Registers[2]);
            }

            {
                // Test SUB
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xFF, 0xCF, // Set $reg0 to 0x00EF
                    (byte)Op.MOV_REG_IMM, 0x01, 0x10, 0x11, // Set $reg1 to 0xBE00
                    (byte)Op.SUB,         0x02, 0x00, 0x01, // Subtract $reg1 from $reg0, store in $reg2
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();
                Assert.AreEqual(0xBEEF, vm.Registers[2]);
            }

            {
                // Test MUL
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xA5, 0x3F, // Set $reg0 to 0x3FA5
                    (byte)Op.MOV_REG_IMM, 0x01, 0x03, 0x00, // Set $reg1 to 0x0003
                    (byte)Op.MUL,         0x02, 0x00, 0x01, // Multiply $reg0 and $reg1, store in $reg2
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();
                Assert.AreEqual(0xBEEF, vm.Registers[2]);
            }

            {
                // Test DIV
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xD0, 0x48, // Set $reg0 to 0x48D0
                    (byte)Op.MOV_REG_IMM, 0x01, 0x04, 0x00, // Set $reg1 to 0x0004
                    (byte)Op.DIV,         0x02, 0x00, 0x01, // Divide $reg0 by $reg1, store in $reg2
                    (byte)Op.HALT
                });
                vm.RunUntilHalt();
                Assert.AreEqual(0x1234, vm.Registers[2]);
            }
        }

        [TestMethod]
        public void TestStack()
        {
            {
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xEF, 0xBE, // MOV 0xBEEF into $reg0
                    (byte)Op.PUSH, 0x00,                    // PUSH value of $reg0 onto stack
                    (byte)Op.POP,  0x01,                    // POP value at top of stack into $reg1
                    (byte)Op.HALT
                });

                Assert.AreEqual(0, vm.Sp);
                vm.Step(); // mov
                vm.Step(); // PUSH
                Assert.AreEqual(2, vm.Sp);
                vm.Step(); // POP
                Assert.AreEqual(0, vm.Sp);

                Assert.AreEqual(0xBEEF, vm.Registers[0]);
                Assert.AreEqual(0xBEEF, vm.Registers[1]);
            }
        }

        [TestMethod]
        public void TestJump()

        {
            {
                VM vm = new(new byte[]
                {
                    (byte)Op.JUMP, 0xEF, 0xBE
                });

                vm.Step();
                Assert.AreEqual(0xBEEF, vm.Pc);
            }

            {
                VM vm = new(new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0xEF, 0xBE, // 0..  3
                    (byte)Op.JUMP,        0x17, 0x00,       // 4..  6
                    (byte)Op.MOV_REG_IMM, 0x00, 0x00, 0x00, // 7..  10
                    (byte)Op.MOV_REG_IMM, 0x01, 0xEF, 0xBE, // 11.. 14
                    (byte)Op.MOV_REG_IMM, 0x02, 0xEF, 0xBE, // 15.. 18
                    (byte)Op.MOV_REG_IMM, 0x03, 0xEF, 0xBE, // 19.. 22
                    (byte)Op.MOV_REG_REG, 0x01, 0x03,       // 23.. 25
                    (byte)Op.HALT
                });

                vm.RunUntilHalt();
                Assert.AreEqual(0xBEEF, vm.Registers[0]);
                Assert.AreEqual(0, vm.Registers[1]);
                Assert.AreEqual(0, vm.Registers[2]);
                Assert.AreEqual(0, vm.Registers[3]);
            }
        }

        [TestMethod]
        public void TestCall()
        {
            {
                byte[] program = new byte[]
                {
                    // 0x00
                    (byte)Op.MOV_REG_IMM, 0x00, 0x05, 0x00, // $reg0 = 5
                    (byte)Op.MOV_REG_IMM, 0x01, 0x07, 0x00, // $reg1 = 7
                    (byte)Op.PUSH, 0x00, // Push arguments onto stack..
                    (byte)Op.PUSH, 0x01,
                    (byte)Op.CALL, 0x11, 0x00,
                    (byte)Op.HALT,
                    // 0x0D
                    // Function: adds the two parameters and doubles the result
                    (byte)Op.POP, 0x00, // Pop second parameter into $reg0
                    (byte)Op.POP, 0x01, // Pop first parameter into $reg1
                    (byte)Op.MOV_REG_IMM, 0x02, 0x02, 0x00, // Set $reg2 to `2`
                    (byte)Op.ADD, 0x00, 0x00, 0x01,         // Set $reg0 to $reg0 + $reg1
                    (byte)Op.MUL, 0x00, 0x00, 0x02,         // Set $reg0 to $reg0 * $reg2 (2) 
                    (byte)Op.RET                            // Return (return value is conventionally in reg0)
                };

                VM vm = new(program);
                vm.RunUntilHalt();

                Assert.AreEqual(24, vm.Registers[0]);
            }

            {
                byte[] program = new byte[]
                {
                    (byte)Op.MOV_REG_IMM, 0x00, 0x01, 0x00, // 3
                    (byte)Op.MOV_REG_IMM, 0x01, 0x02, 0x00, // 7
                    (byte)Op.MOV_REG_IMM, 0x02, 0x03, 0x00, // 11
                    (byte)Op.MOV_REG_IMM, 0x03, 0x04, 0x00, // 15
                    (byte)Op.MOV_REG_IMM, 0x04, 0x05, 0x00, // 19
                    (byte)Op.PUSH, 0x00,                    // 21
                    (byte)Op.PUSH, 0x01,                    // 23
                    (byte)Op.PUSH, 0x02,                    // 25
                    (byte)Op.PUSH, 0x03,                    // 27
                    (byte)Op.PUSH, 0x04,                    // 29
                    (byte)Op.CALL, 0x22, 0x00,              // 32
                    (byte)Op.HALT,                          // 33

                    (byte)Op.NOOP,                          // 34
                    (byte)Op.RET                            // 35
                };

                VM vm = new(program);
                for (int i = 0; i < 10; i++)
                    vm.Step();

                // Check before CALL..
                Assert.AreEqual(1, vm.Registers[0]);
                Assert.AreEqual(2, vm.Registers[1]);
                Assert.AreEqual(3, vm.Registers[2]);
                Assert.AreEqual(4, vm.Registers[3]);
                Assert.AreEqual(5, vm.Registers[4]);
                Assert.AreEqual(0, vm.Sb);
                Assert.AreEqual(10, vm.Sp);
                int pcReturnAddress = vm.Pc + 3;
                // Check after call before RET
                vm.Step(); // CALL
                Assert.AreEqual(34, vm.Pc); // Check that we jumped to correct address
                Assert.AreEqual(10, vm.Sb); // Check that stack base is now end of stack
                Assert.AreEqual(0, vm.Registers[5]); // Check that SB was backed up
                Assert.AreEqual(10, vm.Registers[6]); // Check that SP was backed up
                Assert.AreEqual(pcReturnAddress, vm.Registers[7]); // Check that return address is correct
                // Check after RET
                vm.Step(); // Noop
                vm.Step(); // RET
                Assert.AreEqual(pcReturnAddress, vm.Pc); // Check that we are back where we should be
                Assert.AreEqual(0, vm.Sb); // Check that SB was restored
                Assert.AreEqual(10, vm.Sp); // Check that SP was restored
            }
        }
    }
}