namespace Cobweb{
    public struct Instruction
    {
        public InstructionType Type;
        public List<InstructionArgument> Arguments;
        public Instruction()
        {
            Arguments = new();
        }
        public override string ToString()
        {
            string r = "";
            foreach (var arg in Arguments)
            {
                r += $"[{arg.Type}: {arg.Value}],";    
            }
            return $"{Type}: {r}";
        }
    }
}