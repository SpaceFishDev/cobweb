namespace Cobweb
{
    public class Preprocesser
    {
        public List<Function> Functions;
        public Node Tree;
        public Preprocesser(Node tree)
        {
            Tree = tree;
            Functions = new();
        }
        public void FindVariables()
        {
            foreach (var child in Tree.Children)
            {
                if (child.Type == NodeType.function)
                {
                    List<Variable> funcVars = new();
                    Node args = child.Children[1];
                    foreach (var arg in args.Children)
                    {
                        funcVars.Add(new(arg.NodeToken.Data, VariableType.NoType));
                    }
                    Function f = new();
                    f.Name = child.NodeToken.Data;
                    f.Type = VariableType.NoType;
                    f.expression = child.Children[0];
                    f.Args = funcVars;
                    Functions.Add(f);

                }
            }
        }
        private VariableType GetExprType(Node n)
        {
            foreach (var child in n.Children)
            {
                if (child.Type == NodeType.function_call)
                {
                    foreach (Function f in Functions)
                    {
                        if (f.Name == child.NodeToken.Data)
                        {
                            if (f.Type != VariableType.NoType)
                            {
                                return f.Type;
                            }
                        }
                    }
                }
                if (child.Type == NodeType._if)
                {
                    return GetExprType(child.Children[1]);
                }
                if (child.Type == NodeType.list_initializer)
                {
                    return VariableType.List;
                }
                if (child.Type == NodeType.literal)
                {
                    if (child.Children.Count > 0)
                    {
                        if (child.Children[0].Type == NodeType.index)
                        {
                            return VariableType.Number;
                        }
                    }
                    switch (child.NodeToken.Type)
                    {
                        case TokenType.STRING:
                            {
                                return VariableType.Str;
                            }
                        case TokenType.ID:
                            {
                                return VariableType.NoType;
                            }
                        case TokenType.NUMBER:
                            {
                                return VariableType.Number;
                            }
                    }
                }
                if (child.Type == NodeType.binexpr)
                {
                    VariableType t = GetExprType(child.Children[0]);
                    if (t == VariableType.NoType)
                    {
                        return GetExprType(child.Children[1]);
                    }
                    return t;
                }
            }
            return VariableType.NoType;
        }
        private void WalkTreeForTypes(Function f, Node Current)
        {
            if (Current.Type == NodeType.function_call)
            {
                Function function_ = new();
                int idx = 0;
                foreach (Function func in Functions)
                {
                    if (func.Name == Current.NodeToken.Data)
                    {
                        function_ = func;
                        break;
                    }
                    ++idx;
                }
                List<Variable> vars = new();
                for (int i = 0; i < Current.Children.Count; ++i)
                {

                    VariableType type = GetExprType(Current.Children[i]);
                    if (function_.Args != null)
                    {
                        Variable v = new(function_.Args[i].Name, function_.Args[i].Type);
                        v.Type = type;
                        vars.Add(v);
                    }
                }
                function_.Used = true;
                function_.Args = vars;
                if (idx < Functions.Count)
                {
                    Functions[idx] = function_;
                }
            }
            foreach (var c in Current.Children)
            {
                WalkTreeForTypes(f, c);
            }
        }
        private void SecondWalk(Function f, Node n)
        {
            if (n.Type == NodeType.literal && n.Children.Count == 0)
            {
                var type = VariableType.NoType;
                foreach (var v in f.Args)
                {
                    if (n.NodeToken.Data == v.Name)
                    {
                        type = v.Type;
                    }
                }
                int idx = 0;
                foreach (var func in Functions)
                {
                    if (func.Name == f.Name)
                    {
                        break;
                    }
                    ++idx;
                }
                var function = Functions[idx];
                if (type != VariableType.NoType)
                {
                    function.Type = type;
                }
                // if (function.Type == VariableType.NoType)
                // {
                    // function.Type = VariableType.Number; // Gonna assume number probably dumb but who cares.
                // }
                Functions[idx] = function;
            }
            else
            {
                foreach (var c in n.Children)
                {
                    SecondWalk(f, c);
                }
            }
        }
        private void SortOutTypes(Function f)
        {
            WalkTreeForTypes(f, f.expression);
        }
        public void FindTypes()
        {
            for (int i = 0; i < Functions.Count; ++i)
            {
                var func = Functions[i];
                SortOutTypes(func);
            }
        }
        public void FindFunctionTypes()
        {
            int idx = 0;
            for (; idx < Functions.Count; ++idx)
            {
                Function f = Functions[idx];
                f.Type = GetExprType(f.expression);
                Functions[idx] = f;
                SecondWalk(f, f.expression);
            }
        }
    }
}