namespace Cobweb{
    public struct Variable
    {
        public string Name;
        public VariableType Type;
        public Variable(string name, VariableType type)
        {
            Name = name;
            Type = type;
        }
        public override string ToString()
        {
            return $"Variable {Name} [{Type}]";
        }
    }
}