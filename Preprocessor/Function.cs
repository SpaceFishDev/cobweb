namespace Cobweb{
    public struct Function
    {
        public string Name;
        public VariableType Type;
        public Node expression;
        public List<Variable> Args;
        public override string ToString()
        {
            string ls = "";
            foreach (var arg in Args)
            {
                ls += arg.ToString() + "\n";    
            }
            return $"Function {Name} [{Type}]:\nArgs:\n{ls}\nExpr:\n{expression}";
        }
        public bool Used;
    }
}