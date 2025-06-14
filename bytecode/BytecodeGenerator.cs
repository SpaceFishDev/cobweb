namespace Cobweb{
    public class BytecodeGenerator
    {
        public int Position;
        public string Source;

        public char Current
        {
            get
            {
                if (Position < Source.Length)
                {
                    return Source[Position];
                }
                return '\0';
            }
        }
        public List<Instruction> Instructions;
        private List<string> Labels;
        public BytecodeGenerator(string src)
        {
            Source = src;
            Position = 0;
            Instructions = new();
            Labels = new();
            foreach (var s in PublicParams.PublicFunctions)
            {
                Labels.Add(s);    
            }
        }
        private bool DoesLabelExist(string Label)
        {
            foreach(var l in Labels)
            {
                if(l == Label)
                {
                    return true;  
                } 
            }   
            return false;
        }
        public void FindAllLabels()
        {
            string curr = "";
            for (int i = 0; i < Source.Length; ++i)
            {
                if (char.IsWhiteSpace(Source[i]))
                {
                    curr = "";
                }
                else if (Source[i] == ':')
                {
                    Labels.Add(curr);
                    curr = "";
                }
                else
                {
                    curr += Source[i];
                }
            }
        }
        public Instruction ParseInstruction()
        {
            if (char.IsWhiteSpace(Current))
            {
                ++Position;
                return ParseInstruction();
            }

            if (char.IsLetter(Current))
            {
                string ins = "";
                while (!char.IsWhiteSpace(Current))
                {
                    ins += Current;
                    ++Position;
                }
                switch (ins)
                {
                    case "ret":
                        {
                            Instruction inst = new();
                            inst.Type = InstructionType.RETURN;
                            return inst;
                        }
                    case "jmp":
                        {
                            Instruction inst = new();
                            while(char.IsWhiteSpace(Current))
                            {
                                ++Position;
                            }
                            
                            string arg = "";
                            while(!char.IsWhiteSpace(Current))
                            {
                                arg += Current;    
                                ++Position;
                            }
                            if(!DoesLabelExist(arg))
                            {
                                return new();   
                            }
                            InstructionArgument argument = new();
                            argument.Type = ArgType.LABEL;
                            argument.Value = arg;
                            inst.Arguments.Add(argument);
                            inst.Type = InstructionType.JMP;
                            return inst;
                        }
                    case "cjmp":
                        {
                            Instruction inst = new();
                            while(char.IsWhiteSpace(Current))
                            {
                                ++Position;
                            }
                            
                            string arg = "";
                            while(!char.IsWhiteSpace(Current))
                            {
                                arg += Current;    
                                ++Position;
                            }
                            if(!DoesLabelExist(arg))
                            {
                                return new();   
                            }
                            InstructionArgument argument = new();
                            argument.Type = ArgType.LABEL;
                            argument.Value = arg;
                            inst.Arguments.Add(argument);
                            inst.Type = InstructionType.CONDITIONAL_JUMP;
                            return inst;
                        }
                    case "arg":
                        {
                            Instruction inst = new();
                            while (char.IsWhiteSpace(Current))
                            {
                                ++Position;    
                            }
                            string arg0 = "";
                            while (!char.IsWhiteSpace(Current))
                            {
                                arg0 += Current;    
                                ++Position;
                            }
                            string arg1 = "";
                            while (char.IsWhiteSpace(Current))
                            {
                                ++Position;    
                            }
                            while (!char.IsWhiteSpace(Current))
                            {
                                arg1 += Current;
                                ++Position;
                            }
                            InstructionArgument argument0 = new();
                            InstructionArgument argument1 = new();
                            argument0.Type = ArgType.VAR_TYPE;
                            argument1.Type = ArgType.VARIABLE;
                            argument0.Value = arg0;
                            argument1.Value = arg1;
                            inst.Arguments.Add(argument0); 
                            inst.Arguments.Add(argument1);
                            inst.Type = InstructionType.ARG_DECL;
                            return inst;
                        }
                    case "cmp_more":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "cmp_less":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "cmp_less_eq":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "cmp_more_eq":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "cmp_eq":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "cmp_not_eq":
                        {

                            Instruction inst = new();
                            inst.Type = InstructionType.CMP_MORE;
                            return inst;
                        }
                    case "add":
                        {
                            Instruction inst = new();
                            inst.Type = InstructionType.ADD;
                            return inst;
                        }
                    case "sub":
                        {
                            Instruction inst = new();
                            inst.Type = InstructionType.SUB;
                            return inst;
                        }
                    case "mul":
                        {
                            Instruction inst = new();
                            inst.Type = InstructionType.MUL;
                            return inst;
                        }
                    case "div":
                        {
                            Instruction inst = new();
                            inst.Type = InstructionType.DIV;
                            return inst;
                        }
                    case "call":
                        {
                            string arg = "";
                            while (char.IsWhiteSpace(Current))
                            {
                                ++Position;    
                            }
                            while (!char.IsWhiteSpace(Current))
                            {
                                arg += Current;
                                ++Position;    
                            }
                            foreach (var label in Labels)
                            {
                                if (label == arg)
                                {
                                    Instruction inst = new();
                                    InstructionArgument argument = new();
                                    argument.Type = ArgType.LABEL;
                                    argument.Value = arg;
                                    inst.Arguments.Add(argument);
                                    inst.Type = InstructionType.CALL;
                                    return inst;
                                }    
                            }
                            return new();
                        }
                    case "push":
                        {
                            Instruction instruction = new();
                            while (char.IsWhiteSpace(Current))
                            {
                                ++Position;
                            }
                            string arg = "";
                            if (Current == '"')
                            {
                                arg += Current;
                                ++Position;
                                while (Current != '"')
                                {
                                    arg += Current;
                                    ++Position;
                                }
                            }
                            else
                            {
                                while (!char.IsWhiteSpace(Current))
                                {
                                    arg += Current;
                                    ++Position;
                                }
                            }
                            InstructionArgument argument = new();
                            if (char.IsNumber(arg[0]))
                            {
                                argument.Type = ArgType.NUMBER;
                            }
                            if (char.IsLetter(arg[0]))
                            {
                                foreach (var label in Labels)
                                {
                                    if (label == arg)
                                    {
                                        argument.Type = ArgType.LABEL;    
                                    }    
                                }
                                if (argument.Type != ArgType.LABEL)
                                {
                                    argument.Type = ArgType.VARIABLE;
                                }
                            }
                            if (arg[0] == '"')
                            {
                                ++Position;
                                argument.Type = ArgType.STRING;  
                            }
                            argument.Value = arg;
                            instruction.Arguments.Add(argument);
                            instruction.Type = InstructionType.PUSH;
                            return instruction;
                        } 
                        case "append":
                        {
                            ++Position;
                            Instruction inst = new();
                            inst.Arguments = new();
                            inst.Type = InstructionType.LIST_APPEND;
                            return inst;
                        }
                        case "expand":
                        {
                            ++Position;
                            Instruction inst = new();
                            inst.Arguments = new();
                            inst.Type = InstructionType.LIST_EXPAND;
                            return inst;
                        }
                        case "lsi":
                        {
                            ++Position;
                            Instruction inst = new();
                            inst.Arguments = new();
                            inst.Type = InstructionType.LIST_INIT;
                            return inst;
                        }
                        case "idx":
                        {
                            ++Position;
                            Instruction inst = new();
                            string idx = "";
                            while (char.IsWhiteSpace(Current))
                            {
                                ++Position;
                            }
                            if (char.IsNumber(Current))
                            {
                                while (char.IsNumber(Current))
                                {
                                    idx += Current;
                                    ++Position;
                                }
                                InstructionArgument argument = new();
                                argument.Type = ArgType.NUMBER;
                                argument.Value = idx;
                                inst.Arguments.Add(argument);
                                inst.Type = InstructionType.INDEX;
                                ++Position;
                                return inst;
                            }
                            while (!char.IsWhiteSpace(Current))
                            {
                                idx += Current;
                                ++Position;
                            }
                            InstructionArgument arg = new();
                            arg.Type = ArgType.VARIABLE;
                            inst.Arguments.Add(arg);
                            inst.Type = InstructionType.INDEX;
                            ++Position;
                            return inst;
                        }
                }
                if (ins[ins.Length - 1] == ':')
                {
                    Instruction inst = new();
                    inst.Type = InstructionType.LABEL;
                    InstructionArgument arg = new();
                    arg.Type = ArgType.LABEL;
                    arg.Value = ins.Replace(":", "");
                    inst.Arguments.Add(arg);
                    Labels.Add(arg.Value);
                    return inst;
                }
            }
            return new();
        }
        public void ParseAllInstructions()
        {
            FindAllLabels();
            while (Current != '\0')
            {
                Instructions.Add(ParseInstruction());    
            }
        }
    }
}