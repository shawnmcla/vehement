namespace Vehement.Assembler
{
    public class CompilationResult
    {
        public byte[] Bytecode { get; set; }
        public (string label, int offset)? LabelToResolve { get; set; } = null;

        public CompilationResult(byte[] bytecode)
        {
            Bytecode = bytecode;
        }

        public CompilationResult(byte[] bytecode, string labelToResolve, int offset)
        {
            Bytecode = bytecode;
            LabelToResolve = (labelToResolve, offset);
        }
    }
}