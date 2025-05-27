using System;

namespace Cobweb{

    public class Interpreter
    {
        public char[] Stack;
        public Instruction[] Program;
        public int StackPointer = 0;
        public int ProgramCounter = 0;
        public int MemPos = 0;
        public char[] Memory;
        public Dictionary<string, (List<int>, int)> VariablePositions = new();
        public List<(string, int)> Labels = new();
        private string CurrentLabel = "";
        public Interpreter(List<Instruction> ins)
        {
            Program = ins.ToArray();
            Stack = new char[1024 * 8];
            Memory = new char[1024 * 8];
        }
        public void DumpStack()
        {
            Console.WriteLine("Stack Dump:");
            int idx = 0;
            while(idx < StackPointer)
            {
                char b = Stack[idx];
                Console.Write(idx);
                Console.WriteLine($": {(int)b}");
                ++idx;
            }
        }
        public void DumpMem()
        {
            Console.WriteLine("Memory Dump:");
            int idx = 0;
            while (idx < MemPos)
            {
                char b = Memory[idx];
                Console.Write(idx);
                Console.WriteLine($": {(int)b}");
                ++idx;
            }
        }
        public void Pop(int sz)
        {
            StackPointer -= sz;
        }
        public void SolveLabels()
        {
            int ip = 0;
            foreach (var ins in Program)
            {
                if (ins.Type == InstructionType.LABEL)
                {
                    Labels.Add((ins.Arguments[0].Value,ip));
                }
                ++ip;

            }
        }
        public string GetStr()
        {
            string str = "";
            StackPointer -= sizeof(double);
            byte[] ptr = new byte[sizeof(double)];
            for (int i = 0; i < sizeof(double); ++i)
            {
                ptr[i] = (byte)Stack[StackPointer + i];     
            }
            int pointer = BitConverter.ToInt32(ptr, 0);
            while (true)
            {
                if (Memory[pointer] == 0)
                {
                    break;
                }
                str += Memory[pointer];
                pointer++;
            }
            return str;   
        }
        public double GetDouble()
        {
            StackPointer -= sizeof(double);
            byte[] data = new byte[sizeof(double)];
            for (int i = 0; i < sizeof(double); ++i)
            {
                data[i] = (byte)Stack[StackPointer + i];
            }
            return BitConverter.ToDouble(data,0);
        }
        public void PrintNum()
        {
            double n = GetDouble();
            Console.WriteLine(n);
            StackPointer += sizeof(double);
        }
        public void CallBuiltin(string name)
        {
            switch (name)
            {
                case "print_num":
                    {
                        PrintNum();
                        return;
                    }
            }
        }
        public void Push(InstructionArgument arg)
        {
            switch (arg.Type)
            {
                case ArgType.VARIABLE:
                    {
                        foreach (var variable_name in VariablePositions.Keys)
                        {
                            string split = variable_name.Split(':')[0];
                            if (split == arg.Value)
                            {
                                (List<int> Pos, int Size) value = VariablePositions[variable_name];
                                char[] data = new char[value.Size];
                                for (int i = 0; i < value.Size; ++i)
                                {
                                    data[i] = Stack[value.Pos[value.Pos.Count - 1] + i]; 
                                }
                                foreach (var c in data)
                                {
                                    Stack[StackPointer] = c;
                                    ++StackPointer;
                                }
                            }
                        }
                        return;
                    } 
                case ArgType.NUMBER:
                    {
                        double val = double.Parse(arg.Value);
                        byte[] bytes = BitConverter.GetBytes(val);
                        foreach (var b in bytes)
                        {
                            Stack[StackPointer] = (char)b;
                            StackPointer++;
                        }
                        return;
                    }    
                case ArgType.STRING:
                    {
                        string val = arg.Value.Replace("\"", "");
                        int ptr = MemPos;
                        foreach (var c in val)
                        {
                            Memory[MemPos] = c;
                            ++MemPos;
                        }
                        Memory[MemPos] = (char)0;
                        ++MemPos;
                        byte[] bytes = BitConverter.GetBytes((double)ptr);
                        foreach (var b in bytes)
                        {
                            Stack[StackPointer] = (char)b;
                            StackPointer++;   
                        }
                        return;
                    }
            }
        }
        public Instruction Current
        {
            get
            {
                if (ProgramCounter < Program.Length)
                {
                    return Program[ProgramCounter];    
                }
                return new();
            }
        }
        private (double A, double B) getAB()
        {
            
            int posA = StackPointer - sizeof(double) * 2;
            int posB = StackPointer - sizeof(double);
            byte[] bytesA = new byte[sizeof(double)];
            byte[] bytesB = new byte[sizeof(double)];
            StackPointer -= sizeof(double) * 2;
            int i = 0;
            for (; i < sizeof(double); ++i)
            {
                bytesA[i] = (byte)Stack[StackPointer + i];
            }
            for (; i < sizeof(double) * 2; ++i)
            {
                bytesB[i - sizeof(double)] = (byte)Stack[StackPointer + i];    
            }

            double a = BitConverter.ToDouble(bytesA,0);
            double b = BitConverter.ToDouble(bytesB,0);
            return (a, b);
        }
        int argSize = 0;
        public void Run()
        {
            SolveLabels();
            foreach (var label in Labels)
            {
                if (label.Item1 == "main")
                {
                    ProgramCounter = label.Item2;
                }
            }
            while (ProgramCounter < Program.Length)
            {
                switch (Current.Type)
                {
                    case InstructionType.CALL:
                        {
                            bool builtinFound = false;
                            foreach (string builtin in PublicParams.PublicFunctions)
                            {
                                if (builtin == Current.Arguments[0].Value)
                                {
                                    CallBuiltin(builtin);
                                    ++ProgramCounter;
                                    builtinFound = true;
                                    break;
                                }
                            }
                            if (builtinFound)
                            {
                                continue;
                            } 
                            foreach ((string n, int i) label in Labels)
                            {
                                if (label.n == Current.Arguments[0].Value)
                                {
                                    int curr = ProgramCounter + 1;
                                    byte[] bytes = BitConverter.GetBytes(curr);
                                    foreach (byte b in bytes)
                                    {
                                        Stack[StackPointer] = (char)b;
                                        ++StackPointer;
                                    }
                                    ProgramCounter = label.i;
                                    break;

                                }
                            }
                        }
                        break;
                    case InstructionType.JMP:
                        {
                            foreach (var label in Labels)
                            {
                                if (label.Item1 == Current.Arguments[0].Value)
                                {
                                    ProgramCounter = label.Item2;    
                                }    
                            }
                        }break;
                    case InstructionType.CONDITIONAL_JUMP:
                        {
                            foreach (var label in Labels)
                            {
                                if (label.Item1 == Current.Arguments[0].Value)
                                {
                                    byte[] bytes = new byte[sizeof(double)];
                                    StackPointer -= sizeof(double);
                                    for (int i = 0; i < sizeof(double); ++i)
                                    {
                                        bytes[i] = (byte)Stack[i + StackPointer];
                                    }
                                    double res = BitConverter.ToDouble(bytes, 0);
                                    if (res == 1)
                                    {
                                        ProgramCounter = label.Item2;
                                    }
                                    else
                                    {
                                        ++ProgramCounter;
                                    }
                                    break;
                                }
                            }
                        
                        } break;
                    case InstructionType.RETURN:
                        {
                            double returnValue = GetDouble();
                            int pos = 0;
                            if (StackPointer == 0)
                            {
                                ++ProgramCounter;
                                goto end;
                            }
                            StackPointer -= sizeof(int);
                            byte[] bytes = new byte[sizeof(int)];
                            for (int i = 0; i < sizeof(int); ++i)
                            {
                                bytes[i] = (byte)Stack[StackPointer + i];
                            }
                            pos = BitConverter.ToInt32(bytes, 0);
                            ProgramCounter = pos;
                            StackPointer -= argSize;
                            argSize = 0;
                            end:
                            byte[] returnVal = new byte[sizeof(double)];
                            returnVal = BitConverter.GetBytes(returnValue);
                            foreach (var b in returnVal)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;
                            }
                        } break;
                    case InstructionType.CMP_NOTEQ:
                        {
                            var ab = getAB();
                            double result = (ab.A != ab.B) ? 1 : 0;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.CMP_EQ:
                        {
                            var ab = getAB();
                            double result = (ab.A == ab.B) ? 1 : 0;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.CMP_MORE:
                        {
                            var ab = getAB();
                            double result = (ab.A > ab.B) ? 1 : 0;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.CMP_LESS:
                        {
                            var ab = getAB();
                            double result = (ab.A < ab.B) ? 1 : 0;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.DIV:
                        {
                            var ab = getAB();
                            double result = ab.A / ab.B;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.MUL:
                        {
                            var ab = getAB();
                            double result = ab.A * ab.B;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.ADD:
                        {
                            var ab = getAB();
                            double result = ab.A + ab.B;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.SUB:
                        {
                            var ab = getAB();
                            double result = ab.A - ab.B;
                            byte[] bytes = BitConverter.GetBytes(result);
                            foreach (byte b in bytes)
                            {
                                Stack[StackPointer] = (char)b;
                                ++StackPointer;    
                            }
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.LABEL:
                        {
                            CurrentLabel = Current.Arguments[0].Value;
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.ARG_DECL:
                        {
                            var t = Current.Arguments[0];
                            var n = Current.Arguments[1];
                            string Name = n.Value + ":" + CurrentLabel;
                            int size = 0;
                            int position = StackPointer;
                            if (t.Value == "Number")
                            {
                                size = sizeof(double);
                            }
                            else if (t.Value == "String")
                            {
                                size = sizeof(double);
                                // Strings are pointers to the actual string, their size is sizeof(int)
                            }
                            position -= size + argSize + sizeof(int);
                            if (!VariablePositions.ContainsKey(Name))
                            {
                                VariablePositions.Add(Name, (position, size));
                            }
                            else
                            {
                                VariablePositions[Name] = (position, size);
                            }
                            argSize += size;
                            ++ProgramCounter;
                        }
                        break;
                    case InstructionType.PUSH:
                        {
                            Push(Current.Arguments[0]);
                            ++ProgramCounter;
                        }
                        break;
                    default:
                        {
                            ++ProgramCounter;
                        }
                        break;
                }

            }
        }
        
    }
    public class Program
    {
        public static string InputFile = "main.cbw";
        public static string OutputFile = "a.o";

        public static void Main(string[] Args)
        {
            int i = 0;
            foreach (var arg in Args)
            {
                if (arg == "-o")
                {
                    if (Args.Length < i + 1)
                    {
                        Console.WriteLine("After '-o' an output file is required.");
                        return;
                    }
                    OutputFile = Args[i + 1];
                }
                else
                {
                    if (arg != OutputFile)
                    {
                        InputFile = arg;
                    }
                }
                ++i;
            }
            if (!File.Exists(InputFile))
            {
                Console.WriteLine($"File {InputFile} Doesnt exist.");
                return;
            }
            string src = File.ReadAllText(InputFile);
            Parser parser = new(src);
            Preprocesser preprocesser = new(parser.Tree);
            preprocesser.FindVariables();
            preprocesser.FindTypes();
            preprocesser.FindFunctionTypes();
            IlGenerator generator = new(preprocesser.Tree, preprocesser.Functions);
            generator.GenerateIl();
            Console.WriteLine(parser.Tree);
            foreach (var F in preprocesser.Functions)
            {
                Console.WriteLine(F);
            }
            Console.WriteLine("IL:");
            Console.WriteLine(generator.OutputSrc);
            BytecodeGenerator bytecodeGenerator = new(generator.OutputSrc);
            bytecodeGenerator.ParseAllInstructions();
            Interpreter interpreter = new(bytecodeGenerator.Instructions);
            foreach (var ins in bytecodeGenerator.Instructions)
            {
                Console.WriteLine(ins);
            }
            interpreter.Run();
            interpreter.DumpStack();
            interpreter.DumpMem();
        }
    }
}