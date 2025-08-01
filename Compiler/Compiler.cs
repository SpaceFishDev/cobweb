namespace Cobweb{
    public class Compiler
    {
        public string CompiledSource = "";
        public List<Instruction> Instructions;
        public List<Function> IlFunctions;
        public List<(string, int)> ProgramStrings;
        public List<(double, int)> ProgramDoubles;
        public string CurrentFunction;
        List<ArgumentDefinition> ArgumentDefinitions;
        public string DataSection = "bits 64\nsection .data\n";
        public string TextSection = "section .text\nglobal main\nextern malloc\nextern realloc\n";
        public Compiler(List<Instruction> instructions, List<Function> functions)
        {
            Instructions = instructions;
            IlFunctions = functions;
            ArgumentDefinitions = new();
            ProgramStrings = new();
            ProgramDoubles = new();
            CurrentFunction = "";
            GetProgramDoubles();
            GetProgramStrings();
            BuildTables();
            GetArguments();
            while (Pos < Instructions.Count)
            {
                TextSection += $"; {Instructions[Pos].ToString()}\n";
                TextSection += Compile(Instructions[Pos]);
                ++Pos;
            }
            CompiledSource = DataSection + "\n" + TextSection;
        }
        public void GetArguments()
        {
            string func = "";
            ArgumentDefinition curr = new();
            foreach (Instruction ins in Instructions)
            {
                if (ins.Type == InstructionType.LABEL)
                {
                    foreach (Function f in IlFunctions)
                    {
                        if (f.Name == ins.Arguments[0].Value)
                        {
                            if (curr.functionName != "")
                            {
                                ArgumentDefinitions.Add(curr);
                            }
                            func = f.Name;
                            curr = new();
                            curr.Arguments = new();
                            curr.functionName = func;
                            break;
                        }
                    }
                }
                if (ins.Type == InstructionType.ARG_DECL)
                {
                    curr.Arguments.Add((ins.Arguments[1].Value, ins.Arguments[0].Value));
                }
            }
        }
        int Pos = 0;
        string[] elf64Args = { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };
        string[] elf64DoubleArgs = { "xmm0","xmm1","xmm2","xmm3","xmm4","xmm5","xmm6","xmm7"}; 
        public string CompilePush(Instruction current)
        {
            switch (current.Arguments[0].Type)
            {
                case ArgType.NUMBER:
                    {
                        return $"movsd xmm8, qword [double_{Pos}]\nsub rsp, 8\nmovsd [rsp], xmm8\n";
                    }
                case ArgType.STRING:
                    {
                        return $"mov r12, qword string_{Pos}\npush qword r12\n";
                    }
                case ArgType.VARIABLE:
                    {
                        ArgumentDefinition def = new();
                        foreach (var argdef in ArgumentDefinitions)
                        {
                            if (argdef.functionName == CurrentFunction)
                            {
                                def = argdef;
                            } 
                        }
                        int regidx = 0;
                        int doubleidx = 0;
                        (string name, string type) Var = ("","");
                        foreach (var v in def.Arguments)
                        {
                            if (v.name == current.Arguments[0].Value)
                            {
                                Var = v;
                                break;
                            }
                            if (v.type == "List")
                            {
                                // lists have 2 elements which are args, ptr and size
                                regidx += 2;
                            }
                            else if (v.type == "Str")
                            {
                                regidx++;
                            }
                            else if (v.type == "Number") 
                            {
                                doubleidx++;
                            }
                        }
                        switch (Var.type)
                        {
                            case "Str":
                                {
                                    string result = $"mov rax, {elf64Args[regidx]}\nsub rsp, 8\nmov [rsp], rax\n";
                                    return result;
                                }
                            case "List":
                                {
                                    return $"mov rax, {elf64Args[regidx]}\nsub rsp, 8\nmov [rsp], rax\nmov rax, {elf64Args[regidx+1]}\nsub rsp, 8\nmov [rsp], rax\n";
                                } 
                            case "Number":
                                {
                                    return $"movsd xmm8, {elf64DoubleArgs[doubleidx]}\nsub rsp, 8\nmovsd [rsp], xmm8\n";
                                }
                        }
                    }
                    break;
            }
            return "";
        }
        public string CompileLabel(Instruction current)
        {
            bool IsFunc = false;
            foreach (var f in IlFunctions)
            {
                if (f.Name == current.Arguments[0].Value)
                {
                    IsFunc = true;
                }
            }
            if (IsFunc)
            {
                CurrentFunction = current.Arguments[0].Value;
                if (CurrentFunction == "main")
                {
                    return $"main:\n";
                }
                return $"{current.Arguments[0].Value}:\npush rbp\nmov rbp, rsp\n";
            }
            return $"{current.Arguments[0].Value}:\n";
        }
        public string CompileFunctionCall(Instruction current)
        {
            ArgumentDefinition func = new();
            foreach (var f in ArgumentDefinitions)
            {
                if (f.functionName == current.Arguments[0].Value)
                {
                    func = f;
                }
            }
            int argRNum = 0;
            int argDNum = 0;
            foreach (var v in func.Arguments)
            {
                if (v.type == "Number")
                {
                    argDNum++;
                }
                if (v.type == "Str")
                {
                    argRNum++;
                }
                if (v.type == "List")
                {
                    argRNum += 2;
                }
            }
            string res = "";
            for (int i = 0; i < argRNum; ++i)
            {
                res += $"push qword {elf64Args[i]}\n";
            }
            for (int i = 0; i < argDNum; ++i)
            {
                res += $"movsd xmm8, {elf64DoubleArgs[i]}\nsub rsp, 8\nmovsd [rsp], xmm8\n";
            }
            int stackOff = argRNum * 8 + argDNum * 8;
            for (int i = 0; i < argRNum; ++i)
            {
                res += $"mov {elf64Args[i]}, qword [rsp+{stackOff + (i*8)}]\n";
            }
            for (int i = 0; i < argDNum; ++i)
            {
                res += $"movsd {elf64DoubleArgs[i]}, qword [rsp+{stackOff + (i*8)}]\n";
            }
            res += $"call {current.Arguments[0].Value}\n";
            res += "movsd xmm9, xmm0\n";
            for (int i = argDNum; i > 0; --i)
            {
                res += $"movsd {elf64DoubleArgs[i-1]}, qword [rsp]\nadd rsp, 8\n";
            }
            for (int i = argRNum; i > 0; --i)
            {
                res += $"mov {elf64Args[i]}, qword [rsp]\nadd rsp, 8\n";
            }
            res += $"add rsp, {argRNum*8+argDNum*8}\n";
            Function function = new();
            foreach (var f in IlFunctions)
            {
                if (f.Name == func.functionName)
                {
                    function = f;
                }
            }
            if (function.Type == VariableType.Number)
            {
                res += "sub rsp, 8\nmovsd [rsp], xmm9\n";
            }
            else if (function.Type == VariableType.Str)
            {
                res += "sub rsp, 8\nmov [rsp], rax\n";
            }
            else if (function.Type == VariableType.List)
            {
                res += "sub rsp, 8\nmov [rsp], rax\nsub rsp, 8\nmov [rsp], rbx\n";
            }
            
            return res;
        }
        public string Compile(Instruction current)
        {
            switch (current.Type)
            {
                case InstructionType.PUSH:
                    {
                        return CompilePush(current);
                    }
                case InstructionType.LABEL:
                    {
                        return CompileLabel(current);
                    }
                case InstructionType.ADD:
                    {
                        return $"movsd xmm8, qword [rsp]\nadd rsp, 8\nmovsd xmm9, qword [rsp]\nadd rsp, 8\naddsd xmm9, xmm8\nsub rsp, 8\nmovsd [rsp], xmm9\n";
                    }
                case InstructionType.SUB:
                    {
                        return $"movsd xmm8, qword [rsp]\nadd rsp, 8\nmovsd xmm9, qword [rsp]\nadd rsp, 8\nsubsd xmm9, xmm8\nsub rsp, 8\nmovsd [rsp], xmm9\n";
                    }
                case InstructionType.DIV:
                    {
                        return $"movsd xmm8, qword [rsp]\nadd rsp, 8\nmovsd xmm9, qword [rsp]\nadd rsp, 8\ndivsd xmm9, xmm8\nsub rsp, 8\nmovsd [rsp], xmm9\n";
                    }
                case InstructionType.MUL:
                    {
                        return $"movsd xmm8, qword [rsp]\nadd rsp, 8\nmovsd xmm9, qword [rsp]\nadd rsp, 8\nmulsd xmm9, xmm8\nsub rsp, 8\nmovsd [rsp], xmm9\n";
                    }
                case InstructionType.CALL:
                    {
                        return CompileFunctionCall(current);
                    }
                case InstructionType.CONDITIONAL_JUMP:
                    {
                        Instruction prev = Instructions[Pos - 1];
                        string res = "";
                        switch (current.Arguments[1].Value)
                        {
                            case "Number":
                                {
                                    res += "movsd xmm8, qword [rsp]\nadd rsp, 8\nmovsd xmm9, qword [rsp]\nadd rsp, 8\ncomisd xmm9, xmm8\n";            
                                }
                                break;
                            case "Str":
                                {
                                    res += "pop qword rbx\npop qword rax\ncmp rbx, rax\n";
                                }
                                break;
                        }
                        switch (prev.Type)
                        {
                            case InstructionType.CMP_EQ:
                                {
                                    res += $"je {current.Arguments[0].Value}\n";
                                }
                                break;
                            case InstructionType.CMP_LESS:
                                {
                                    res += $"jb {current.Arguments[0].Value}\n";
                                }
                                break;
                            case InstructionType.CMP_MORE:
                                {
                                    res += $"ja {current.Arguments[0].Value}\n";
                                }
                                break;

                        }
                        return res;
                    }
                case InstructionType.JMP:
                    {
                        return $"jmp {current.Arguments[0].Value}\n";
                    } 
                case InstructionType.RETURN:
                    {
                        string res = "";
                        Function f = new();
                        foreach (var func in IlFunctions)
                        {
                            if (func.Name == CurrentFunction)
                            {
                                f = func; 
                            }
                        }
                        if (f.Type == VariableType.Number)
                        {
                            res += "movsd xmm0, qword [rsp]\nadd rsp, 8\n";
                        }
                        else if (f.Type == VariableType.Str)
                        {
                            res += "mov rax, qword [rsp]\nadd rsp, 8\n";
                        }
                        else if (f.Type == VariableType.List)
                        {
                            res += "mov rbx, [rsp]\nadd rsp, 8\nmov rax, [rsp]\nadd rsp,8\n";
                        }
                        if (f.Name == "main")
                        {
                            if (f.Type == VariableType.Number)
                            {
                                res += "cvttsd2si rax, xmm0\n"; 
                            }
                            res += "mov rdi, rax\nmov rax, 60\nsyscall\n";
                            return res;
                        }
                        res += $"mov rsp, rbp\npop rbp\nret\n";
                        return res;
                    }
                    
            }
            return "";
        }
        public void BuildTables()
        {
            foreach ((string str, int pos) str in ProgramStrings)
            {
                DataSection += $"string_{str.pos}: db \"{str.str}\",0\n";
            }
            foreach((double d, int pos) dbl in ProgramDoubles){
                if(dbl.d % 1 == 0){
                    DataSection += $"double_{dbl.pos}: dq {dbl.d}.0\n";
                }else{
                    DataSection += $"double_{dbl.pos}: dq {dbl.d}.0\n";
                }
            }
        }
        public void GetProgramStrings()
        {
            int pos = 0;
            while (pos < Instructions.Count)
            {
                if (Instructions[pos].Type == InstructionType.PUSH)
                {
                    if (Instructions[pos].Arguments[0].Type == ArgType.STRING)
                    {
                        ProgramStrings.Add((Instructions[pos].Arguments[0].Value.Replace("\"", ""), pos));
                    }
                }
                ++pos;
            }
        }
        public void GetProgramDoubles()
        {
            int pos = 0;
            while (pos < Instructions.Count)
            {
                if (Instructions[pos].Type == InstructionType.PUSH)
                {
                    if (Instructions[pos].Arguments[0].Type == ArgType.NUMBER)
                    {
                        ProgramDoubles.Add((double.Parse(Instructions[pos].Arguments[0].Value),pos));
                    }
                }
                ++pos;
            }
        }

    }
}