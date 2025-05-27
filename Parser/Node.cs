namespace Cobweb{
    public struct Node
    {
        public NodeType Type;
        public Token NodeToken;
        public List<Node> Children;
        public Node()
        {
            Children = new();
        }
        public Node(NodeType type, Token nodeToken)
        {
            Type = type;
            NodeToken = nodeToken;
            Children = new();
        }
        public void AddChild(Node n)
        {
            Children.Add(n);
        }
        public string GetStr()
        {
            return $"NODE:({Type}, {NodeToken.ToString()})";   
        }
        private string indent(string str, int idt)
        {
            string res = "";
            for (int i = 0; i < idt; ++i)
            {
                res += " --> ";
            }
            res += str;
            return res;
        }
        public string strRecursive(int idt)
        {
            string result = "";
            string dat = indent(GetStr(), idt) + "\n";
            string indented_open = indent("{", idt);
            for (int i = 0; i < Children.Count; ++i)
            {
                result += Children[i].strRecursive(idt + 1);
            }
            string indented_close = indent("}", idt);
            result = "\n" + dat + indented_open +"\n" + result + "\n" + indented_close ;
            return result;
            
        }
        public override string ToString()
        {
            return strRecursive(0);
        }
    }
}