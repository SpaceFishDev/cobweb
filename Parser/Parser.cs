namespace Cobweb{
    public class Parser
    {
        public Node Tree;
        public int Position;
        public List<Token> Tokens;
        public Token Current
        {
            get
            {
                if (Position >= Tokens.Count)
                {
                    return new();
                }
                return Tokens[Position];
            }
        }
        public void Next()
        {
            ++Position;
        }
        public bool Expect(TokenType type)
        {
            if (Position + 1 > Tokens.Count)
            {
                return false;
            }
            if (Tokens[Position + 1].Type != type)
            {
                return false;
            }
            return true;
        }
        public Parser()
        {
            Tokens = new();
        }
        public Parser(string src)
        {
            Lexer lexer = new(src);
            lexer.LexAll();
            Tokens = lexer.Tokens;
            Tree = new(NodeType.program, new());
            Parse();
        }
        public bool IsSymbol(TokenType type)
        {
            switch (type)
            {
                case TokenType.MINUS:
                case TokenType.MULTIPLY:
                case TokenType.DIVIDE:
                case TokenType.PLUS:
                case TokenType.BOOLEQ:
                case TokenType.BOOLLESS:
                case TokenType.BOOLLESSEQ:
                case TokenType.BOOLMORE:
                case TokenType.BOOLMOREEQ:
                case TokenType.BOOLNOTEQ:
                    return true;
            }
            return false;
        }
        public bool ExpectValue(string val)
        {
            if (Position + 1 > Tokens.Count)
            {
                return false;
            }
            if (Tokens[Position + 1].Data != val)
            {
                return false;
            }
            return true;
        }
        public Node ParseFunctionCall()
        {
            Node FunctionNode = new(NodeType.function_call, Current);
            Next();
            Next();

            while (Current.Type != TokenType.BRACE_CLOSE)
            {
                Node expr = ParseExpr();
                FunctionNode.AddChild(expr);
            }
            Next();
            return FunctionNode;
        }
        public Node ParseListInit()
        {
            Node listNode = new(NodeType.list_initializer, Current);
            Next();
            while (Current.Type != TokenType.SQUARE_CLOSE)
            {
                Node n = ParseExpr();
                listNode.AddChild(n);
            }
            Next();
            return listNode;
        }
        public Node ParseLiteral()
        {
            
            switch (Current.Type)
            {
                case TokenType.ID:
                    {
                        Next();
                        if (Current.Type == TokenType.SQUARE_OPEN)
                        {
                            --Position;
                            Node literal = new(NodeType.literal, Current);
                            Next();
                            Node idx = ParseIndex();
                            Next();
                            literal.AddChild(idx);
                            return literal;
                        }
                        return new(NodeType.literal, Tokens[Position - 1]);
                    }
                case TokenType.NUMBER:
                case TokenType.STRING:
                    {
                        Next();
                        return new(NodeType.literal, Tokens[Position - 1]);
                    }
            }
            return new();
        }
        public Node ParseBasic()
        {
            if (Current.Type == TokenType.SQUARE_OPEN)
            {
                return ParseListInit();    
            }
            if (Current.Type == TokenType.ID)
            {
                if (Expect(TokenType.BRACE_OPEN))
                {
                    return ParseFunctionCall();
                }
            }
            return ParseLiteral();
        }
        public Node ParseFactor()
        {
            Next();
            return ParseExpr();
        }
        public Node ParsePrimary()
        {
            int Start = Position;
            Node op = new();
            int scope = 0;
            while (!(IsSymbol(Current.Type) && scope == 0))
            {
                if (Current.Type == TokenType.BRACE_OPEN)
                {
                    ++scope;
                }
                if (Current.Type == TokenType.BRACE_CLOSE)
                {
                    --scope;
                }
                Next();
            }
            Token curr = Current;
            Position = Start;
            Node left = ParseBasic();
            if (IsSymbol(Current.Type))
            {
                Node right = ParseFactor();
                op.AddChild(left);
                op.AddChild(right);
                op.Type = NodeType.binexpr;
                op.NodeToken = curr; 
            }
            if (op.Children.Count > 0)
            {
                return op;
            }
            return left;
        }
        public Node ParseElse()
        {
            if (Current.Data != "else")
            {
                Next();
                return new();
            }
            Node elseNode = new(NodeType._else, Current);
            Next();
            Node expr = ParseExpr();
            elseNode.AddChild(expr);
            return elseNode;
        }
        public Node ParseIf()
        {
            Node ifNode = new(NodeType._if, Current);
            Next();
            Node expr = ParseExpr();
            if (expr.Children[0].Type == NodeType.function_call)
            {
                Next();    
            }
            if (Current.Data != "then")
            {
                return new();
            }
            Next();
            Node expr1 = ParseExpr();
            ifNode.AddChild(expr);
            ifNode.AddChild(expr1);
            if (Current.Data == "else")
            {
                Node elseNode = ParseElse();
                ifNode.AddChild(elseNode);
            }
            return ifNode;
        }
        public Node ParseBinExpr()
        {
            return ParsePrimary();
        }
        public Node ParseIndex()
        {
            if (Current.Type != TokenType.SQUARE_OPEN)
            {
                return new();
            }
            if (!Expect(TokenType.NUMBER) && !Expect(TokenType.ID))
            {
                return new();
            }
            Next();
            Node idx = new(NodeType.index, Current);
            Next();
            return idx;
        }
        public Node ParseExpr()
        {

            Node expr = new(NodeType.expr, new());
            if (Current.Type == TokenType.SQUARE_OPEN)
            {
                expr.AddChild(ParseListInit());
                return expr;    
            }
            if (Current.Data == "if")
            {
                Node ifNode = ParseIf();
                expr.AddChild(ifNode);
                return expr;
            }
            if (Expect(TokenType.BRACE_OPEN))
            {
                int Start = Position;
                Node f = ParseFunctionCall();
                if (IsSymbol(Current.Type))
                {
                    Position = Start;
                    Node binExpr = ParsePrimary();
                    expr.AddChild(binExpr);
                    if (binExpr.Children[0].Type != NodeType.function_call)
                    {
                        Next();
                    }
                    return expr;
                }
                --Position;
                expr.AddChild(f);
                return expr;
            }
            Next();
            if (IsSymbol(Current.Type))
            {
                --Position;
                Node bin = ParseBinExpr();
                expr.AddChild(bin);
                return expr;
            }
            --Position;
            Node n = ParseLiteral();
            expr.AddChild(n);
            return expr;
        }
        public Node ParseFunction()
        {
            if (Current.Type != TokenType.ID)
            {
                return new();
            }
            if (Current.Data != "f")
            {
                return new();
            }
            Node func = new();
            func.Type = NodeType.function;
            Next();
            func.NodeToken.Data += Current.Data;
            Node arguments = new(NodeType.func_def__args, new());
            Next();
            while (Current.Type != TokenType.ARROW)
            {
                if (Current.Type == TokenType.ID)
                {
                    arguments.AddChild(new(NodeType.literal, Current));
                }
                Next();
            }
            Next();
            Node expr = ParseExpr();
            func.AddChild(expr);
            if(Current.Type != TokenType.ID)
            {
                Next();    
            }
            func.AddChild(arguments);
            return func;
        }
        public Node Parse()
        {
            while (Current.Type != TokenType.END_OF_FILE)
            {
                if (Current.Data == "f")
                {
                    Tree.AddChild(ParseFunction());
                }
                else
                {
                    return Tree;
                }
            }
            return Tree;
        }

    }
}