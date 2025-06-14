namespace Cobweb{
    public class IlGenerator
    {
        public string OutputSrc = "";
        public Node Tree;
        public List<Function> Functions = new();
        public IlGenerator(Node tree, List<Function> functions)
        {
            Tree = tree;
            Functions = functions;
        }
        private Arithmetic ProcessCurrent(Node n)
        {
            switch (n.Type)
            {
                case NodeType.expr:
                    {
                        if (n.Children.Count > 0)
                        {
                            return ProcessCurrent(n.Children[0]);
                        }
                    }
                    break;
                case NodeType.binexpr:
                    {
                        switch (n.NodeToken.Type)
                        {
                           case TokenType.PLUS:
                                {
                                    return new(ArithmeticType.ADD, n);
                                } 
                           case TokenType.MINUS:
                                {
                                    return new(ArithmeticType.SUB, n);
                                } 
                           case TokenType.MULTIPLY:
                                {
                                    return new(ArithmeticType.MUL, n);
                                } 
                           case TokenType.DIVIDE:
                                {
                                    return new(ArithmeticType.DIV, n);
                                } 
                            case TokenType.BOOLLESS:
                                {
                                    return new(ArithmeticType.LESS, n);
                                }
                            case TokenType.BOOLMORE:
                                {
                                    return new(ArithmeticType.MORE, n);
                                }
                            case TokenType.BOOLLESSEQ:
                                {
                                    return new(ArithmeticType.LESSEQ, n);
                                }
                            case TokenType.BOOLMOREEQ:
                                {
                                    return new(ArithmeticType.MOREEQ, n);
                                }
                            case TokenType.BOOLEQ:
                                {
                                    return new(ArithmeticType.EQ, n);
                                }
                            case TokenType.BOOLNOTEQ:
                                {
                                    return new(ArithmeticType.NOTEQ, n);
                                }
                        }
                    }
                    break; 
            }
            return new(ArithmeticType.LIT, n);
        }
        private List<Arithmetic> ProcessRecursive(Node n)
        {
            if (n.Type == NodeType.expr)
            {
                if (n.Children.Count > 0)
                {
                    n = n.Children[0];    
                }    
            }
            Arithmetic curr = ProcessCurrent(n);
            if (curr.Type == ArithmeticType.LIT)
            {
                List<Arithmetic> ls = new();
                ls.Add(curr);
                return ls;
            }

            List<Arithmetic> left = ProcessRecursive(n.Children[0]);
            List<Arithmetic> right = ProcessRecursive(n.Children[1]);
            List<Arithmetic> currentList = [.. left, curr, .. right];
            return currentList;
        }
        private int CountType(Node current, NodeType type)
        {
            int i = 0;
            if (current.Type == type)
            {
                ++i;
            }
            foreach (var c in current.Children)
            {
                int x = CountType(c, type);
                i += x;    
            }
            return i;
        }
        private List<(Arithmetic Left, Arithmetic Op, Arithmetic Right)> ProcessArithmetic(Node n)
        {
            var a = ProcessRecursive(n);
            List<(Arithmetic, Arithmetic, Arithmetic)> groups = new();
            for (int i = 0; i < a.Count; ++i)
            {
                if (a[i].Type != ArithmeticType.LIT)
                {
                    var group = (a[i - 1], a[i], a[i + 1]);
                    groups.Add(group);
                }
            }
            return groups;
        }
        private bool FindNodeRecursive(Node n, Node Current)
        {
            bool eq = Current.NodeToken.Column == n.NodeToken.Column && Current.NodeToken.Row == n.NodeToken.Row;
            if (!eq)
            {
                foreach (var c in Current.Children)
                {
                    if (FindNodeRecursive(n, c))
                    {
                        eq = true;
                        break;
                    }    
                }
            }
            return eq;
        }
        private Function findParentFunction(Node n)
        {
            Node current = new();
            bool found = false;
            Function ret = new();
            foreach (Function f in Functions)
            {
                current = f.expression;
                bool eq = FindNodeRecursive(n, current);
                if (eq)
                {
                    ret = f;
                    break;
                }
            }
            return ret;
        }
        int IfIdx = 0;
        private string GenIl(Node t)
        {
            switch (t.Type)
            {
                case NodeType._else:
                    {
                        string res = "";
                        Function f = findParentFunction(t);
                        res += GenIl(t.Children[0]);
                        res += $"{f.Name}_end_{IfIdx}:\n";
                        ++IfIdx;
                        return res;
                    }
                case NodeType._if:
                    {
                        string res = "";
                        Function f = findParentFunction(t);
                        res += $"{f.Name}_if_{IfIdx}:\n";
                        res += GenIl(t.Children[0]);
                        res += $"cjmp ";
                        res += $"{f.Name}_end_{IfIdx}\n";
                        res += $"{f.Name}_then_{IfIdx}:\n";
                        res += GenIl(t.Children[1]);
                        if (t.Children.Count > 2)
                        {
                            int offset = CountType(t.Children[2], NodeType._else);
                            res += $"jmp {f.Name}_end_{IfIdx + offset }\n";
                        }
                        res += $"{f.Name}_end_{IfIdx}:\n";
                        ++IfIdx;
                        if (t.Children.Count > 2)
                        {
                            res += GenIl(t.Children[2]);
                        }
                        return res;
                    }
                case NodeType.function_call:
                    {
                        string res = "";
                        foreach (var c in t.Children)
                        {
                            res += GenIl(c);
                        }
                        res += $"call {t.NodeToken.Data}\n";
                        return res;
                    }
                case NodeType.literal:
                    {
                        if (t.Children.Count > 0)
                        {
                            if (t.Children[0].Type == NodeType.index)
                            {
                                return $"idx {t.Children[0].NodeToken.Data}\n";
                            }
                        }
                        if (t.NodeToken.Type == TokenType.STRING)
                        {
                            return $"push \"{t.NodeToken.Data}\"\n";
                        }
                        return $"push {t.NodeToken.Data}\n";
                    }
                case NodeType.list_initializer:
                    {
                        string res = "lsi\n";
                        foreach (var c in t.Children)
                        {
                            res += GenIl(c);
                            res += "expand\n";
                            res += "append\n";
                        }
                        return res;
                    }
                case NodeType.binexpr:
                    {
                        var ar = ProcessArithmetic(t);
                        string res = "";
                        int idx = 0;
                        foreach (var a in ar)
                        {
                            if (idx == 0)
                            {
                                res += GenIl(a.Left.AssociatedNode);
                                res += GenIl(a.Right.AssociatedNode);
                            }
                            else
                            {
                                res += GenIl(a.Right.AssociatedNode);
                            }
                            if (((int)a.Op.Type) < 5)
                            {
                                string[] mappings = { "add", "sub", "mul", "div" };
                                res += mappings[((int)(a.Op.Type)) - 1];
                                res += "\n";
                            }
                            if (((int)a.Op.Type) >= 5)
                            {
                                string[] booleanmappings = { "cmp_more", "cmp_less", "cmp_less_eq", "cmp_more_eq", "cmp_not_eq", "cmp_eq" };
                                res += booleanmappings[((int)a.Op.Type) - 5];
                                res += "\n";
                            }
                            ++idx;
                        }
                        return res;
                    }
                case NodeType.expr:
                    {
                        string result = "";
                        foreach (var child in t.Children)
                        {
                            result += GenIl(child);
                        }
                        return result;
                    }
                case NodeType.program:
                    {
                        string result = "";
                        foreach (var child in t.Children)
                        {
                            result += GenIl(child);
                        }
                        return result;
                    }
                case NodeType.function:
                    {
                        string res = "";
                        Function f = new();
                        foreach (Function func in Functions)
                        {
                            if (func.Name == t.NodeToken.Data)
                            {
                                f = func;
                            }
                        }
                        res += "\n" + f.Name + ":\n";
                        string args = "";
                        foreach (var arg in f.Args)
                        {
                            args += $"arg {arg.Type} {arg.Name}\n";
                        }
                        List<string> splitArgs = args.Split("\n").ToList();
                        splitArgs.Reverse();
                        foreach (var argument in splitArgs)
                        {
                            res += argument + "\n";
                        }
                        res += GenIl(f.expression);
                        res += "ret\n";
                        return res;
                    }
            }
            return "";
        }
        public void GenerateIl()
        {
            OutputSrc = GenIl(Tree);
        }
    }
}