using System;
using System.Reflection.Emit;
using System.Diagnostics;
namespace Cobweb{
    public class Interpreter
    {
        public List<Instruction> Instructions;
        public List<Function> Functions;
        public Interpreter(List<Instruction> ins, List<Function> funs)
        {
            Instructions = ins;
            Functions = funs;
            Memory = new byte[1024 * 1024];
            Stack = new byte[1024 * 1024];
            ArgsD = new();
            ArgsI = new();
            ProgramStrings = new();
            CallStack = new();
            int mempos = 0;
            foreach (var i in ins)
            {
                if (i.Type == InstructionType.PUSH && i.Arguments[0].Type == ArgType.STRING)
                {
                    int pos = mempos;
                    foreach (char c in i.Arguments[0].Value)
                    {
                        byte b = (byte)c;
                        Memory[mempos] = b;
                        ++mempos;
                    }
                    mempos++;
                    ProgramStrings.Add((i.Arguments[0].Value, pos));
                } 
            }
            MemPos = mempos;
            CurrentFunction = "";
        }
        public string CurrentFunction;
        public List<double> ArgsD;
        public List<int> ArgsI; // for pointers 
        public List<(List<int> ArgsI, List<double> ArgsD, int Pos, string Name)> CallStack;
        public byte[] Memory;
        public byte[] Stack;
        public int Pos;
        public double Dreturn;
        public int Ireturn;
        public List<(string, int)> ProgramStrings;
        int MemPos = 0;
        public Instruction Current
        {
            get
            {
                return Instructions[Pos];
            }
        }
        public double StackTop
        {
            get
            {
                byte[] bytes = new byte[sizeof(double)];
                int n = 0;
                int top = stackPointer + sizeof(double); 
                for (int i = stackPointer; i < top; ++i)
                {
                    bytes[n] = Stack[i];
                    ++n;
                }
                return BitConverter.ToDouble(bytes);
            }
        }
        int stackPointer = 1024 * 1024;
        public void PushBytes(byte[] bytes)
        {
            stackPointer -= bytes.Length;
            int i = 0;
            foreach (byte b in bytes)
            {
                Stack[stackPointer + i] = b;
                ++i;
            }   
        }
        public void Push()
        {
            switch (Current.Arguments[0].Type)
            {
                case ArgType.NUMBER:
                {
                        double val = double.Parse(Current.Arguments[0].Value);
                        var bytes = BitConverter.GetBytes(val);
                        PushBytes(bytes);
                } break;
                case ArgType.STRING:
                    {
                        stackPointer -= sizeof(int);
                        int val = 0;
                        foreach (var s in ProgramStrings)
                        {
                            if (s.Item1 == Current.Arguments[0].Value)
                            {
                                val = s.Item2;
                                break;
                            }
                        }
                        var bytes = BitConverter.GetBytes(val);
                        PushBytes(bytes);
                    } break;
                case ArgType.VARIABLE:
                    {
                        int Didx = 0;
                        int Iidx = 0;
                        Function func = new();
                        foreach (var f in Functions)
                        {
                            if (f.Name == CurrentFunction)
                            {
                                func = f;
                                break;
                            }
                        }
                        Variable variable = new();
                        if (func.Args == null)
                        {
                            func.Args = new();
                        }
                        foreach (var v in func.Args)
                        {
                            if (v.Name == Current.Arguments[0].Value)
                            {
                                variable = v;
                                break;
                            }
                            if (v.Type == VariableType.Number)
                            {
                                ++Didx;
                            }
                            if (v.Type == VariableType.Str)
                            {
                                ++Iidx;   
                            }
                            if (v.Type == VariableType.List)
                            {
                                Iidx += 2;
                            }
                        }
                        if (variable.Type == VariableType.Number)
                        {
                            double value = ArgsD[Didx];
                            var bytes = BitConverter.GetBytes(value);
                            PushBytes(bytes);
                        }
                        else if (variable.Type == VariableType.Str)
                        {
                            int value = ArgsI[Iidx];
                            var bytes = BitConverter.GetBytes(value);
                            PushBytes(bytes);
                        }
                        else if (variable.Type == VariableType.List)
                        {
                            int length = ArgsI[Iidx+1];
                            int pos = ArgsI[Iidx];
                            PushBytes(BitConverter.GetBytes(pos));
                            PushBytes(BitConverter.GetBytes(length)); 
                        }
                    } break;
            } 
        }
        public double PopNum()
        {
            double value = 0;
            byte[] bytes = new byte[sizeof(double)];
            stackPointer += sizeof(double);
            if (stackPointer > Stack.Count())
            {
                return 0;
            }
            int i = stackPointer - sizeof(double);
            int n = 0;
            while (i < stackPointer)
            {
                bytes[n] = Stack[i];
                ++n;
                ++i;
            }
            value = BitConverter.ToDouble(bytes);
            return value;   
        }
        public int PopI()
        {
            int value = 0;
            stackPointer += sizeof(int);
            int i = stackPointer - sizeof(int);
            int n = 0;
            byte[] bytes = new byte[sizeof(int)];
            for (; i < stackPointer; ++i)
            {
                bytes[n] = Stack[i];
                ++n;
            }
            value = BitConverter.ToInt32(bytes);
            return value;
        }
        public void CallFunction()
        {
            int pos = Pos;
            double[] arrD = new double[ArgsD.Count];
            int[] arrI = new int[ArgsI.Count];
            ArgsD.CopyTo(arrD);
            ArgsI.CopyTo(arrI);
            CallStack.Add((arrI.ToList(), arrD.ToList(), pos, CurrentFunction));
            Function func = new();
            foreach (Function f in Functions)
            {
                if (f.Name == Current.Arguments[0].Value)
                {
                    func = f;
                }
            }
            if (func.Name == null)
            {
                func.Name = "";
            }
            CurrentFunction = func.Name;
            pos = 0;
            foreach (var ins in Instructions)
            {
                if (ins.Type == InstructionType.LABEL)
                {
                    if (ins.Arguments[0].Value == func.Name)
                    {
                        break;
                    }
                }
                ++pos;
            }
            Pos = pos;
            int idxD = 0;
            int idxI = 0;
            if (func.Args == null)
            {
                func.Args = new();
            }
            foreach (var v in func.Args)
            {
                if (v.Type == VariableType.Number)
                {
                    double d = PopNum();
                    ArgsD.Add(d);
                    ++idxD;
                }
                if (v.Type == VariableType.Str)
                {
                    int i = PopI();
                    ArgsI.Add(i);
                    ++idxI;
                }
                if (v.Type == VariableType.List)
                {
                    int i = PopI();
                    ArgsI.Add(i);
                    ++idxI;
                    i = PopI();
                    ArgsI.Add(i);
                    ++idxI;
                }
            }
            ArgsD.Reverse();
            ArgsI.Reverse();
        }
        public void ReturnFromFunction()
        {
            Function f = new();
            foreach (var func in Functions)
            {
                if (func.Name == CurrentFunction)
                {
                    f = func;
                }
            }
            if (CallStack.Count == 1)
            {
                Pos = Instructions.Count + 1;
                return;
            }
            var call = CallStack[CallStack.Count - 1];
            CallStack.RemoveAt(CallStack.Count - 1);
            CurrentFunction = call.Name;
            ArgsD = call.ArgsD;
            ArgsI = call.ArgsI;
            Pos = call.Pos;
            if (f.Type == VariableType.Number)
            {
                double d = PopNum();
                Dreturn = d;
            }
            if (f.Type == VariableType.Str)
            {
                int i = PopI();
                Ireturn = i;
            }
            if (f.Type == VariableType.List)
            {
                int length = PopI();
                int pointer = PopI();
                Ireturn = pointer;
                Dreturn = (double)length;
            }
        }
        public void Run()
        {
            for (int i = 0; i < Instructions.Count; ++i)
            {
                if (Current.Type == InstructionType.LABEL)
                {
                    if (Current.Arguments[0].Value == "main")
                    {
                        break;
                    }
                }
                ++Pos;
            }
            CurrentFunction = "main";
            CallStack.Add((new(), new(), 1, "main"));
            while (Pos < Instructions.Count)
            {
                if (Pos > 0)
                {
                    if (Instructions[Pos - 1].Type == InstructionType.CALL)
                    {
                        Function func = new();
                        foreach (var f in Functions)
                        {
                            if (f.Name == Instructions[Pos - 1].Arguments[0].Value)
                            {
                                func = f;
                            }
                        }
                        if (func.Type == VariableType.Number)
                        {
                            PushBytes(BitConverter.GetBytes(Dreturn));
                        }
                        else if (func.Type == VariableType.Str)
                        {
                            PushBytes(BitConverter.GetBytes(Ireturn));
                        }
                        else if (func.Type == VariableType.List)
                        {
                            int pos = Ireturn;
                            int length = (int)Dreturn;
                            PushBytes(BitConverter.GetBytes(pos));
                            PushBytes(BitConverter.GetBytes(length));
                        }
                    }
                }
                switch (Current.Type)
                {
                    case InstructionType.INDEX:
                        {
                            if (Current.Arguments[0].Type == ArgType.NUMBER)
                            {
                                int length = PopI();
                                int pos = PopI();
                                int idx = int.Parse(Current.Arguments[0].Value);
                                int n = 0;
                                byte[] bytes = new byte[sizeof(double)];
                                for (int i = pos + (idx*sizeof(double)); n < sizeof(double); ++i)
                                {
                                    bytes[n] = Memory[i];
                                    ++n;
                                }
                                PushBytes(bytes);
                            }
                            if (Current.Arguments[0].Type == ArgType.VARIABLE)
                            {
                                int length = PopI();
                                int pos = PopI();
                                string vIdx = Current.Arguments[0].Value;
                                Function f = new();
                                foreach (var func in Functions)
                                {
                                    if (func.Name == CurrentFunction)
                                    {
                                        f = func;
                                        break;
                                    }
                                }
                                Variable v = new();
                                int dIdx = 0;
                                int iIdx = 0;
                                foreach (var Var in f.Args)
                                {
                                    if (Var.Name == vIdx)
                                    {
                                        v = Var;
                                        break;
                                    }
                                    if (Var.Type == VariableType.Number)
                                    {
                                        ++dIdx;
                                    }
                                }
                                double d = ArgsD[dIdx];
                                int dI = (int)d;
                                int pos_real = pos + (dI * sizeof(double));
                                byte[] bytes = new byte[sizeof(double)];
                                int n = 0;
                                for (int i = pos_real; n < sizeof(double); ++i)
                                {
                                    bytes[n] = Memory[i];
                                    ++n;
                                }
                                PushBytes(bytes);
                            }
                        } break;
                    case InstructionType.LIST_INIT:
                        {
                            int pos = MemPos;
                            int len = 0;
                            PushBytes(BitConverter.GetBytes(pos));
                            PushBytes(BitConverter.GetBytes(len));
                        } break;
                    case InstructionType.LIST_EXPAND:
                        {
                            double d = PopNum();
                            int length = PopI();
                            int pos = PopI();
                            MemPos += sizeof(double);
                            length += sizeof(double);
                            PushBytes(BitConverter.GetBytes(d));
                            PushBytes(BitConverter.GetBytes(pos));
                            PushBytes(BitConverter.GetBytes(length));
                        } break;
                    case InstructionType.LIST_APPEND:
                        {
                            int length = PopI();
                            int pos = PopI();
                            double v = PopNum();
                            byte[] bytes = BitConverter.GetBytes(v);
                            int i = 0;
                            int pos_new = pos + (length*sizeof(double));
                            pos_new -= sizeof(double);
                            foreach (byte b in bytes)
                            {
                                Memory[pos_new + i] = bytes[i];
                                ++i;
                            }
                            PushBytes(BitConverter.GetBytes(pos));
                            PushBytes(BitConverter.GetBytes(length));
                        } break;
                    case InstructionType.CMP_EQ:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = (double)Convert.ToInt32(a == b);
                            PushBytes(BitConverter.GetBytes(res));
                        } break;
                    case InstructionType.CMP_MORE:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = (double)Convert.ToInt32(a > b);
                            PushBytes(BitConverter.GetBytes(res));
                        } break;
                    case InstructionType.CMP_LESS:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = (double)Convert.ToInt32(a < b);
                            PushBytes(BitConverter.GetBytes(res));
                        } break;
                    case InstructionType.CONDITIONAL_JUMP:
                        {
                            double d = PopNum();
                            if (d > 0)
                            {
                                int pos = 0;
                                foreach (Instruction ins in Instructions)
                                {
                                    if (ins.Type == InstructionType.LABEL && ins.Arguments[0].Value == Current.Arguments[0].Value)
                                    {
                                        break;
                                    }
                                    ++pos;
                                }
                                Pos = pos;
                            }
                        } break;
                    case InstructionType.JMP:
                        {
                            int pos = 0;
                            foreach (Instruction ins in Instructions)
                            {
                                if (ins.Type == InstructionType.LABEL && ins.Arguments[0].Value == Current.Arguments[0].Value)
                                {
                                    break;
                                }
                                ++pos;
                            }
                            Pos = pos;
                        } break;
                    case InstructionType.PUSH:
                    {
                            Push();
                    } break;
                    case InstructionType.ADD:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = a + b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.SUB:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = a - b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.MUL:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = a * b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.DIV:
                        {
                            double b = PopNum();
                            double a = PopNum();
                            double res = a / b;
                            byte[] bytes = BitConverter.GetBytes(res);
                            PushBytes(bytes);
                        }   break;
                    case InstructionType.CALL:
                        {
                            CallFunction();
                        } break;
                    case InstructionType.RETURN:
                        {
                            ReturnFromFunction();
                        } break;
                }
                ++Pos;
            }
        }
        public void StackDump()
        {
            Console.WriteLine("Stack:");
            for (int i = stackPointer; i < 1024 * 1024; ++i)
            {
                Console.WriteLine($"{(1024*1024)-i}: {Stack[i]}");
            }
        }
    }
    public class Program
    {
        public static string InputFile = "main.cbw";
        public static string OutputFile = "a.o";

        public static void Main(string[] Args)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            bool interpret = true;
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
                    interpret = false;
                }
                else if (arg == "-i")
                {
                    interpret = true;
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
            // foreach (var F in preprocesser.Functions)
            // {
            // Console.WriteLine(F);
            // }
            Console.WriteLine("IL:");
            Console.WriteLine(generator.OutputSrc);
            BytecodeGenerator bytecodeGenerator = new(generator.OutputSrc);
            bytecodeGenerator.ParseAllInstructions();
            foreach (Instruction ins in bytecodeGenerator.Instructions)
            {
                Console.WriteLine(ins);
            }
            if (!interpret)
            {
                Compiler compiler = new(bytecodeGenerator.Instructions, generator.Functions);
                File.WriteAllText(OutputFile + ".asm", compiler.CompiledSource);
                // Console.WriteLine(compiler.CompiledSource);
            }
            if (interpret)
            {
                Interpreter interpreter = new(bytecodeGenerator.Instructions, generator.Functions);
                interpreter.Run();
                interpreter.StackDump();
                double d = interpreter.PopNum();
                Console.WriteLine($"Return Value: {d}");
            }
        }
    }
}